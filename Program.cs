namespace KokoroSharp;

internal class Program {
    static KokoroTTS tts = new(@"C:\Users\lyrco\Downloads\kokoro-v0_19.onnx");
    static KokoroPlayback playback = new KokoroPlayback();

    static void Main(string[] args) {
        var nicole = new KokoroVoice() { Features = NumSharp.np.Load<float[,,]>(@"C:\Users\lyrco\Downloads\espeak-ng\nicole.npy") };
        var sarah = new KokoroVoice() { Features = NumSharp.np.Load<float[,,]>(@"C:\Users\lyrco\Downloads\espeak-ng\sarah.npy") };
        while (true) {
            var txt = Console.ReadLine();

            tts.SpeakFast(txt, KokoroVoiceManager.Mix([(sarah, 5), (nicole, 5)]));
            continue;
            var tokens = Tokenizer.Tokenize(txt);
            var ttokens = Segmentation.SplitToSegments(tokens);
            tts.EnqueueJob(KokoroJob.Create(ttokens, sarah, 1, playback.Enqueue));
            tts.EnqueueJob(KokoroJob.Create(ttokens, NumSharp.np.Load<float[,,]>(@"C:\Users\lyrco\Downloads\espeak-ng\sarah.npy"), 1, playback.Enqueue));
        }
    }
}
