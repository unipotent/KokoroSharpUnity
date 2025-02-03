namespace KokoroSharp;

using KokoroSharp.Core;

using System;

/// <summary> Contains audio samples and callbacks for playback management. </summary>
/// <remarks> Used by <see cref="KokoroPlayback"/> to queue and manage audio samples with completion/cancellation tracking. </remarks>
public class PlaybackHandle {
    public float[] Samples;
    public Action OnStarted;
    public Action OnSpoken;
    public Action<(float time, float percentage)> OnCanceled;

    public KokoroPlayback Owner { get; init; }
    public bool Aborted { get; private set; }
    public void Abort(bool raiseCancelCallback = true) {
        Aborted = true;
        if (raiseCancelCallback) { OnCanceled?.Invoke((0f, 0f)); }
    }

    public PlaybackHandle(float[] samples, Action OnStarted, Action OnSpoken, Action<(float time, float percentage)> OnCanceled) => (Samples, this.OnStarted, this.OnSpoken, this.OnCanceled) = (samples, OnStarted, OnSpoken, OnCanceled);
}

/// <summary> A handle containing callbacks covering the full lifetime of a 'Speak' request. </summary>
/// <remarks> The included delegates can be subscribed to for fine updates over the specific synthesis's lifetime. </remarks>
public class SynthesisHandle {
    /// <summary> Callback raised when playback for given speech request just started. </summary>
    /// <remarks> Can be used to retrieve info about the original task, including spoken text, and phonemes. </remarks>
    public Action<SpeechStartPacket> OnSpeechStarted;

    /// <summary> Callback raised when a given text segment was spoken successfully. This includes the last segment. </summary>
    /// <remarks> Note that some contents of this packet are GUESSED, which means they might not be accurate. </remarks>
    public Action<SpeechProgressPacket> OnSpeechProgressed;

    /// <summary> Callback raised when the whole given text was spoken successfully. </summary>
    /// <remarks> Can be used to retrieve info about the original task, including spoken text, and phonemes. </remarks>
    public Action<SpeechCompletionPacket> OnSpeechCompleted;

    /// <summary> Callback raised when a segment was aborted, during speech, or before it even started. Can retrieve which parts were spoken, in part or in full. </summary>
    /// <remarks> Note that "Cancel" will STILL be raised with (0f,0%) for packets that were canceled before being played. </remarks>
    public Action<SpeechCancelationPacket> OnSpeechCanceled;

    /// <summary> The inference job this handle is connected to. </summary>
    public KokoroJob Job { get; init; }

    /// <summary> Contains the handles of the audio playback instances that are ready to be played. </summary>
    public List<PlaybackHandle> ReadyPlaybackHandles { get; } = [];



    //public Action OnSynthesisStarted;
    //public Action OnSynthesisProgressed;
    //public Action OnSynthesisCompleted;

    //public int CurrentStep { get; set; }
    //public PlaybackHandle CurrentPlaybackHandle => ReadyPlaybackHandles[CurrentStep];
}
