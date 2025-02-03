namespace KokoroSharp.Tokenization;

public static class SpeechGuesser {
    static char[] punctuation = [.. Tokenizer.punctuation];

    /// <summary> Tries to make a guess on what the spoken text is, based on the phonemes that were spoken. </summary>
    /// <returns> The best-guess of the part that was spoken. </returns>
    public static string GuessSpeech_LowEffort(GuessPacket guessPacketInfo) {
        //var originalText = guessPacketInfo.OriginalText;
        //var preprocessedText = Tokenizer.PreprocessText(originalText);
        //var segmentedText = originalText.Split(punctuation);


        return "Not implemented yet";
    }
}

public struct GuessPacket {
    public string OriginalText;
    public char[] PreSpokenPhonemes;
    public char[] SegmentPhonemes;
    public char[] AllPhonemes;
    public float SegmentT;
}
