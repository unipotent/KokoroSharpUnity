namespace KokoroSharp;

using KokoroSharp.Core;
using KokoroSharp.Tokenization;

using Microsoft.ML.OnnxRuntime;

using System.Diagnostics;

/// <summary> Highest level module that allows easy inference with the model. </summary>
/// <remarks> Contains a background worker thread that dispatches queued jobs/actions linearly. </remarks>
public sealed class KokoroTTS : KokoroEngine {
    /// <summary> Callback raised when playback for given speech request just started. </summary>
    /// <remarks> Can be used to retrieve info about the original task, including spoken text, and phonemes. </remarks>
    public event Action<SpeechStartPacket> OnSpeechStarted;

    /// <summary> Callback raised when a given text segment was spoken successfully. This includes the last segment. </summary>
    /// <remarks> Note that some contents of this packet are GUESSED, which means they might not be accurate. </remarks>
    public event Action<SpeechProgressPacket> OnSpeechProgressed;

    /// <summary> Callback raised when the whole given text was spoken successfully. </summary>
    /// <remarks> Can be used to retrieve info about the original task, including spoken text, and phonemes. </remarks>
    public event Action<SpeechCompletionPacket> OnSpeechCompleted;

    /// <summary> Callback raised when a segment was aborted, during speech, or before it even started. Can retrieve which parts were spoken, in part or in full. </summary>
    /// <remarks> Note that "Cancel" will STILL be raised with (0f,0%) for packets that were canceled before being played. </remarks>
    public event Action<SpeechCancelationPacket> OnSpeechCanceled;

    /// <summary> If true, the output audio of the model will be *nicified* before being played back. </summary>
    /// <remarks> Nicification includes trimming silent start and finish, and attempting to reduce noise. </remarks>
    public bool NicifyAudio { get; set; } = true;

    KokoroPlayback activePlayback;

    /// <summary> Creates a new Kokoro TTS Engine instance, loading the model into memory and initializing a background worker thread to continuously scan for newly queued jobs, dispatching them in order, when it's free. </summary>
    /// <remarks> If 'options' is specified, the model will be loaded with them. This is particularly useful when needing to run on non-CPU backends, as the default backend otherwise is the CPU with 8 threads. </remarks>
    public KokoroTTS(string modelPath, SessionOptions options = null) : base(modelPath, options) { }

    /// <summary> Speaks the text with the specified voice, without segmenting it, resulting in a slower, yet potentially higher quality response. </summary>
    /// <remarks> This is the simplest, highest-level interface of the library. For more fine-grained controls, see <see cref="KokoroEngine"/>.</remarks>
    /// <param name="text"> The text to speak. </param>
    /// <param name="voice"> The voice that will speak it. Can also be a <see cref="KokoroVoice"/>. </param>
    /// <returns> A handle with delegates regarding speech progress. Those can be subscribed to for updates regarding the lifetime of the synthesis. </returns>
    public SynthesisHandle Speak(string text, KokoroVoice voice) {
        StopPlayback();
        var tokens = Tokenizer.Tokenize(text, voice.GetLangCode());
        if (tokens.Length > KokoroModel.maxTokens) {
            Debug.WriteLine($"Max token count the model supports is {KokoroModel.maxTokens}, but got {tokens.Length}. Defaulting to automatic segmentation.");
            return SpeakFast(text, voice);
        }

        var job = EnqueueJob(KokoroJob.Create(tokens, voice, 1, null));
        activePlayback = new KokoroPlayback() { AssignedJob = job, NicifySamples = NicifyAudio };

        var handle = new SynthesisHandle() { Job = job };
        job.Steps[0].OnStepComplete = (samples) => EnqueueWithCallbacks(samples, text, job.Steps[0], job, handle);
        return handle;
    }

    /// <summary> Segments the text before speaking it with the specified voice, resulting in an almost immediate response for the first chunk, with a potential hit in quality. </summary>
    /// <remarks> This is the simplest, highest-level interface of the library. For more fine-grained controls, see <see cref="KokoroEngine"/>.</remarks>
    /// <param name="text"> The text to speak. </param>
    /// <param name="voice"> The voice that will speak it. Can also be a <see cref="KokoroVoice"/>. </param>
    /// <returns> A handle with delegates regarding speech progress. Those can be subscribed to for updates regarding the lifetime of the synthesis. </returns>
    public SynthesisHandle SpeakFast(string text, KokoroVoice voice) {
        StopPlayback();
        var tokens = Segmentation.SplitToSegments(Tokenizer.Tokenize(text, voice.GetLangCode()));
        var job = EnqueueJob(KokoroJob.Create(tokens, voice, 1, null));
        activePlayback = new KokoroPlayback() { AssignedJob = job, NicifySamples = NicifyAudio };

        var phonemesCache = new List<char>();
        var handle = new SynthesisHandle() { Job = job };
        foreach (var step in job.Steps) { step.OnStepComplete = (samples) => EnqueueWithCallbacks(samples, text, step, job, handle, phonemesCache); }
        return handle;
    }

    /// <summary> Immediately cancels any ongoing playbacks and requests triggered by any of the "Speak" methods. </summary>
    public void StopPlayback() {
        activePlayback?.AssignedJob?.Cancel();
        activePlayback?.Dispose();
        activePlayback = null;
    }

    /// <inheritdoc/>
    public override void Dispose() {
        StopPlayback();
        base.Dispose();
    }

    /// <summary> This is a callback that gets invoked with the model's outputs (/audio samples) as parameters, once an inference job is complete. </summary>
    /// <remarks> It in turn relays those samples to the <see cref="KokoroPlayback"/> instance, and sets up follow-up callbacks regarding playback progress. </remarks>
    void EnqueueWithCallbacks(float[] samples, string text, KokoroJob.KokoroJobStep step, KokoroJob job, SynthesisHandle handle, List<char> phonemesCache = null) {
        phonemesCache ??= [];
        var phonemesToSpeak = job.Steps.SelectMany(x => x.Tokens ?? []).Select(x => Tokenizer.TokenToChar[x]).ToArray();
        var playbackHandle = activePlayback.Enqueue(samples, OnStartedCallback, OnCompleteCallback, OnCanceledCallback);
        handle.ReadyPlaybackHandles.Add(playbackHandle); // Marks the inference as "completed" and registers the playback handle as "ready".


        // Callbacks
        void OnStartedCallback() { // We need to add the SpeechStarted callback, but only to the very first segment.
            if ((OnSpeechStarted == null && handle.OnSpeechStarted == null) || step != job.Steps[0]) { return; }
            var startPacket = new SpeechStartPacket() {
                RelatedJob = job,
                TextToSpeak = text,
                PhonemesToSpeak = phonemesToSpeak,
            };
            OnSpeechStarted?.Invoke(startPacket);
            handle.OnSpeechStarted?.Invoke(startPacket);
        }
        void OnCompleteCallback() {
            if (OnSpeechProgressed == null && handle.OnSpeechProgressed == null && OnSpeechCompleted == null && handle.OnSpeechCompleted == null) { return; }

            var phonemes = step.Tokens.Select(x => Tokenizer.TokenToChar[x]).ToArray();
            phonemesCache.AddRange(phonemes);

            // After each segment is complete, invoke the SpeechProgressed callback.
            if (OnSpeechProgressed != null || handle.OnSpeechProgressed != null) {
                var progressPacket = new SpeechProgressPacket() {
                    RelatedJob = job,
                    RelatedStep = step,
                    SpokenText_BestGuess = step == job.Steps[^1] ? text : MakeBestGuess(1, phonemes),
                    PhonemesSpoken = phonemes,
                };
                OnSpeechProgressed?.Invoke(progressPacket);
                handle.OnSpeechProgressed?.Invoke(progressPacket);
            }

            // We also need to add the SpeechCompletion callback, but only to the very last segment.
            if ((OnSpeechCompleted != null || handle.OnSpeechCompleted != null) && step == job.Steps[^1]) {
                var completionPacket = new SpeechCompletionPacket() {
                    RelatedJob = job,
                    RelatedStep = step,
                    PhonemesSpoken = [.. phonemesCache],
                    SpokenText = text,
                };
                OnSpeechCompleted?.Invoke(completionPacket);
                handle.OnSpeechCompleted?.Invoke(completionPacket);
            }
        }
        void OnCanceledCallback((float time, float percentage) t) {
            if (OnSpeechCanceled == null && handle.OnSpeechCanceled == null) { return; }

            // Let's assume the amount of spoken phonemes linearly matches the percentage.
            var T = (int) Math.Round(step.Tokens.Length * t.percentage); // L * t
            var phonemesSpokenGuess = step.Tokens.Take(T).Select(x => Tokenizer.TokenToChar[x]);
            var cancelationPacket = new SpeechCancelationPacket() {
                RelatedJob = job,
                RelatedStep = step,
                SpokenText_BestGuess = MakeBestGuess(t.percentage, step.Tokens.Select(x => Tokenizer.TokenToChar[x]).ToArray()),
                PhonemesSpoken_BestGuess = [.. phonemesCache, .. phonemesSpokenGuess],
                PhonemesSpoken_PrevSegments_Certain = [.. phonemesCache],
                PhonemesSpoken_LastSegment_BestGuess = [.. phonemesSpokenGuess]
            };
            OnSpeechCanceled?.Invoke(cancelationPacket);
            handle.OnSpeechCanceled?.Invoke(cancelationPacket);
            phonemesCache.AddRange(phonemesSpokenGuess);
        }

        string MakeBestGuess(float percentage, char[] segmentPhonemes) {
            var packet = new GuessPacket() {
                OriginalText = text,
                AllPhonemes = phonemesToSpeak,
                PreSpokenPhonemes = [.. phonemesCache],
                SegmentPhonemes = segmentPhonemes,
                SegmentT = percentage
            };
            return SpeechGuesser.GuessSpeech_LowEffort(packet);
        }
    }
}
