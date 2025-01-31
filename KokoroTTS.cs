namespace KokoroSharp;

using Microsoft.ML.OnnxRuntime;

using System.Collections.Concurrent;

/// <summary> Highest level module that allows easy inference with the model. </summary>
/// <remarks> Contains a background worker thread that dispatches queued jobs/actions linearly. </remarks>
public class KokoroTTS : IDisposable {
    readonly KokoroModel model;
    readonly ConcurrentQueue<KokoroJob> queuedJobs = [];

    KokoroPlayback activePlayback;
    volatile bool hasExited;

    /// <summary> Creates a new Kokoro TTS Engine instance, loading the model into memory and initializing a background worker thread to continuously scan for newly queued jobs, dispatching them in order, when it's free. </summary>
    /// <remarks> If 'options' is specified, the model will be loaded with them. This is particularly useful when needing to run on non-CPU backends, as the default backend otherwise is the CPU with 8 threads. </remarks>
    public KokoroTTS(string modelPath, SessionOptions options = null) {
        model = new(modelPath, options);

        new Thread(async () => {
            while (!hasExited) {
                await Task.Delay(10);
                while (!hasExited && queuedJobs.TryDequeue(out var job)) {
                    while (!hasExited && !job.isDone) {
                        job.Progress(model);
                        await Task.Delay(1);
                    }
                }
            }
        }).Start();
    }

    /// <summary> Enqueues a job for the Kokoro TTS engine, scheduling it to be processed when the engine is free. </summary>
    /// <remarks> The job will be automatically dispatched when all prior jobs have been completed or canceled. Canceled jobs resolve and get skipped when their order arrives. </remarks>
    public KokoroJob EnqueueJob(KokoroJob job) {
        queuedJobs.Enqueue(job);
        return job;
    }

    /// <summary> Speaks the text with the specified voice, without segmenting it, resulting in a slower, yet potentially higher quality response. </summary>
    /// <remarks> This is the simplest, highest-level version of the interface. For more fine-grained controls, see <see cref="EnqueueJob(KokoroJob)"/>.</remarks>
    /// <param name="text"> The text to speak. </param>
    /// <param name="voice"> The voice that will speak it. Can also be a <see cref="KokoroVoice"/>. </param>
    public void Speak(string text, float[,,] voice) {
        StopPlayback();
        var tokens = Tokenizer.Tokenize(text);
        var job = EnqueueJob(KokoroJob.Create(tokens, voice, 1, null));
        activePlayback = new KokoroPlayback(job);
        foreach (var step in job.Steps) { step.OnStepComplete = activePlayback.Enqueue; }
    }

    /// <summary> Segments the text before speaking it with the specified voice, resulting in an almost immediate response for the first chunk, with a potential hit in quality. </summary>
    /// <remarks> This is the simplest, highest-level version of the interface. For more fine-grained controls, see <see cref="EnqueueJob(KokoroJob)"/>.</remarks>
    /// <param name="text"> The text to speak. </param>
    /// <param name="voice"> The voice that will speak it. Can also be a <see cref="KokoroVoice"/>. </param>
    public void SpeakFast(string text, float[,,] voice) {
        StopPlayback();
        var tokens = Segmentation.SplitToSegments(Tokenizer.Tokenize(text));
        var job = EnqueueJob(KokoroJob.Create(tokens, voice, 1, null));
        activePlayback = new KokoroPlayback(job);
        foreach (var step in job.Steps) { step.OnStepComplete = activePlayback.Enqueue; }
    }

    /// <summary> Immediately cancels any ongoing playbacks and requests triggered by any of the "Speak" methods. </summary>
    public void StopPlayback() {
        activePlayback?.job?.Cancel();
        activePlayback?.Dispose();
        activePlayback = null;
    }

    /// <summary> Immediately cancels any ongoing registered jobs and playbacks, frees memory taken by the model, and notifies the background worker thread to exit. </summary>
    /// <remarks> Note that this will not free up memory that the voices take (~25MB if ALL languages are loaded). </remarks>
    public void Dispose() {
        hasExited = true;
        StopPlayback();
        queuedJobs.Clear();
        model.Dispose();
    }
}
