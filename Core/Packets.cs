namespace KokoroSharp;

using KokoroSharp.Core;


/// <summary> Callback packet that gets sent when part of the speech playback was completed. </summary>
/// <remarks> Contains info about the part that was spoken since the previous packet was sent. </remarks>
public struct SpeechStartPacket {
    /// <summary> The full list of phonemes that started being spoken. </summary>
    public char[] PhonemesToSpeak;

    /// <summary> The full text that started being spoken. </summary>
    public string TextToSpeak;

    /// <summary> The Kokoro Job this speech packet is connected to. </summary>
    public KokoroJob RelatedJob;
}

/// <summary> Callback packet that gets sent when part of the speech playback was completed. </summary>
/// <remarks> Contains info about the part that was spoken since the previous packet was sent. </remarks>
public struct SpeechProgressPacket {
    /// <summary> The phonemes that were spoken since the previous "SpeechProgress" packet was sent. </summary>
    /// <remarks> Note that unlike <b>SpokenText_BestGuess</b>, these will be 100% accurate. </remarks>
    public char[] PhonemesSpoken;

    /// <summary> The text that was spoken since the previous "SpeechProgress" packet was sent... probably </summary>
    /// <remarks> <b>NOTE:</b> It might not be accurate because Kokoro doesn't provide per-spoken-phoneme info to ONNX, so we can only infer segments. </remarks>
    public string SpokenText_BestGuess;

    /// <summary> The Kokoro Job this speech packet is connected to. </summary>
    public KokoroJob RelatedJob;

    /// <summary> The Kokoro Job Step this speech packet is connected to. </summary>
    public KokoroJob.KokoroJobStep RelatedStep;
}

/// <summary> Callback packet that gets sent when the speech playback was interrupted. </summary>
/// <remarks> Note that "Cancel" will STILL be raised with (0f,0%) for packets that were canceled before being played. </remarks>
public struct SpeechCancelationPacket {

    /// <summary> The phonemes that were spoken since the beginning of this speech/KokoroJob. </summary>
    /// <remarks> Note that these have <b>INDEED</b> been spoken but they do NOT include the last segment's phonemes. </remarks>
    public char[] PhonemesSpoken_PrevSegments_Certain;

    /// <summary> The phonemes that were spoken on the last segment before cancelation... probably. </summary>
    /// <remarks> Note that these ONLY include the last segment's <b>best guess</b> of phonemes, based on the percentage spoken. </remarks>
    public char[] PhonemesSpoken_LastSegment_BestGuess;

    /// <summary> The phonemes that were spoken since the beginning of this speech/KokoroJob.... probably. </summary>
    /// <remarks> Note that ones on the last segment will likely NOT be accurate, as they're based on the percentage spoken. </remarks>
    public char[] PhonemesSpoken_BestGuess;

    /// <summary> The text that was spoken since the beginning of this speech/KokoroJob... probably </summary>
    /// <remarks> <b>NOTE:</b> It might not be accurate because Kokoro doesn't provide per-spoken-phoneme info to ONNX, so we can only infer segments. </remarks>
    public string SpokenText_BestGuess;

    /// <summary> The Kokoro Job this speech packet is connected to. </summary>
    public KokoroJob RelatedJob;

    /// <summary> The Kokoro Job Step this speech packet is connected to. </summary>
    public KokoroJob.KokoroJobStep RelatedStep;
}

/// <summary> Callback packet that gets sent when the speech playback completes successfully. </summary>
public struct SpeechCompletionPacket {
    /// <summary> The phonemes that were spoken during this speech/KokoroJob. </summary>
    public char[] PhonemesSpoken;

    /// <summary> The text that was spoken during this speech/KokoroJob. </summary>
    public string SpokenText;

    /// <summary> The Kokoro Job this speech packet is connected to. </summary>
    public KokoroJob RelatedJob;

    /// <summary> The Kokoro Job Step this speech packet is connected to. </summary>
    public KokoroJob.KokoroJobStep RelatedStep;
}
