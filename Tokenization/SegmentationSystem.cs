namespace KokoroSharp.Tokenization;

using static Tokenizer;

/// <summary>
/// <para> Allows defining various rules helpful for customizing the segmentation pipeline. Segmentation allows *chunking* the text so the first parts of it will be processed quicker. </para>
/// <para> This is crucial to allow seamless audio playback, because follow-up chunks can be processed in the background while the audio output from the previous chunks is playing. </para>
/// <para> <b>The general segmentation rules apply as follows:</b> </para>
/// <para> - First, we prefer [(replace, splitting between 3 paras) segmenting by <see cref="PunctuationTokens"/>, or spaces, in this order, but if shit, words may be cut. Careful note]</para>
/// <para> - First, the algorithm tries segmenting on exact <see cref="PunctuationTokens"/>, within the allowed limits. </para>
/// <para> - .. if there were no punctuation tokens available there, we try to segment on a <see cref="SegmentationSystem.spaceToken"/> that appears within the segment's range. </para>
/// <para> - .. and if no space tokens were found either, we cut at the maximum allowed length. This may cut words in the middle, so plan accordingly. </para>
/// <para> <i>Note: Do not use too small or too long numbers for these sequences. Usually the defaults will work for every machine with a small impact on initial response quality.</i> </para>
/// <para> <i>It is recommended to provide your users with an option on whether they want SUPER FAST, or SUPER GOOD response, and provide different strategies. </i> </para>
/// </summary>
public class SegmentationStrategy {
    /// <summary> The minimum allowed length of the first segment. Ensures the first segment includes AT LEAST this many tokens. </summary>
    /// <remarks> Recommended to keep this small, to allow instant responses. </remarks>
    public int MinFirstSegmentLength = 1;

    /// <summary> The maximum allowed length of the first segment. *NOTE: Having this too small might cut words in the middle* </summary>
    /// <remarks> Recommended to keep this small, but not too small, to allow instant responses. </remarks>
    public int MaxFirstSegmentLength = 40;

    /// <summary> The maximum allowed length of the second segment. *NOTE: Having this too small might cut words in the middle* </summary>
    /// <remarks> Recommended to be a reasonable size based on the first segment's expected length, for seamless audio playback. </remarks>
    public int MaxSecondSegmentLength = 100;

    /// <summary> The minimum allowed length of follow-up segments. Any 100% valid punctuation found after THIS many tokens will mark a new segment. </summary>
    /// <remarks> These can be long since they'll be processed in the background while the audio is playing. </remarks>
    public int MinFollowupSegmentsLength = 200;
}

/// <summary> Helper class that allows turning text tokens into segments, allowing us to get the first response of the model quicker. </summary>
/// <remarks> This allows us to begin playing back the audio of the first sentence, while the model processes the rest of the sequence on the background. </remarks>
public static class SegmentationSystem {
    static int spaceToken = Vocab[' '];
    static SegmentationStrategy defaultSegmentationStrategy = new();

    /// <summary> Turns the input tokens into multiple segments, aggressively optimized for streaming. Then returns the segments in a list. </summary>
    /// <remarks> This is just so the audio can be played back with the first part, while the model is still processing the rest of the sequence. </remarks>
    /// <param name="segmentationStrategy">The segmentation strategy that will be used to chunk the tokens into segments.</param>
    public static List<int[]> SplitToSegments(int[] tokens, SegmentationStrategy segmentationStrategy = null) {
        segmentationStrategy ??= defaultSegmentationStrategy;
        var (minFirstSegmentLength, maxFirstSegmentLength, maxSecondSegmentLength, minFollowupSegmentsLength) = (segmentationStrategy.MinFirstSegmentLength, segmentationStrategy.MaxFirstSegmentLength, segmentationStrategy.MaxSecondSegmentLength, segmentationStrategy.MinFollowupSegmentsLength);

        if (tokens.Length <= maxFirstSegmentLength) { return [tokens]; }

        List<(int start, int end)> potentialSegments = [];


        // First segment
        for (int i = minFirstSegmentLength; i < maxFirstSegmentLength; i++) {
            if (PunctuationTokens.Contains(tokens[i])) { potentialSegments.Add((0, i + 1)); break; }
        }
        if (potentialSegments.Count == 0) {
            for (int i = minFirstSegmentLength + (maxFirstSegmentLength - minFirstSegmentLength) / 2; i < tokens.Length; i++) {
                if (tokens[i] == spaceToken) { potentialSegments.Add((0, i)); break; }
            }
            if (potentialSegments.Count == 0) { potentialSegments.Add((0, maxFirstSegmentLength)); }
        }

        // Second segment
        if (tokens.Length < maxSecondSegmentLength + potentialSegments[0].end + 1) { potentialSegments.Add((potentialSegments[0].end + 1, tokens.Length)); }
        else {
            for (int i = potentialSegments[0].end + 1; i < potentialSegments[0].end + 1 + maxSecondSegmentLength; i++) {
                if (PunctuationTokens.Contains(tokens[i])) { potentialSegments.Add((potentialSegments[^1].end + 1, i + 1)); break; }
            }
            if (potentialSegments.Count == 1) {
                for (int i = potentialSegments[0].end + 1 + maxSecondSegmentLength; i <= potentialSegments[0].end + 1; i--) {
                    if (tokens[i] == spaceToken) { potentialSegments.Add((potentialSegments[0].end + 1, i)); break; }
                }
                if (potentialSegments.Count == 1) { potentialSegments.Add((potentialSegments[0].end + 1, Math.Min(tokens.Length, potentialSegments[0].end + maxSecondSegmentLength))); }
            }
        }

        // Follow-up segments
        for (int i = potentialSegments[^1].end + 1; i < tokens.Length; i++) {
            var start = potentialSegments[^1].end + 1;
            if (i == tokens.Length - 1) { potentialSegments.Add((start, i + 1)); }
            else if (PunctuationTokens.Contains(tokens[i])) {
                while (i < tokens.Length - 1 && PunctuationTokens.Contains(tokens[i + 1])) { ++i; }
                potentialSegments.Add((start, ++i));
                while (i < tokens.Length - 1 && tokens[i + 1] == spaceToken) { ++i; }
            }
        }

        // Go through the potential segments and finalize them.
        List<int[]> segmentsList = [];
        var currentSegment = new List<int>();
        for (int i = 0; i < potentialSegments.Count; i++) {
            currentSegment.AddRange(GetSegmentTokens(i));
            if (i >= potentialSegments.Count - 1) { continue; }
            else if (segmentsList.Count <= 1) { Flush(); }
            else if (currentSegment.Count + GetSegmentCount(i + 1) > minFollowupSegmentsLength) { Flush(); }
            else { currentSegment.Add(Vocab[' ']); }
        }
        if (currentSegment.Count != 0) { Flush(); }

        return segmentsList;


        // Helper methods
        int[] GetSegmentTokens(int i) => tokens[potentialSegments[i].start..potentialSegments[i].end];
        int GetSegmentCount(int i) => potentialSegments[i].end - potentialSegments[i].start;
        void Flush() { segmentsList.Add([.. currentSegment]); currentSegment.Clear(); }
    }
}
