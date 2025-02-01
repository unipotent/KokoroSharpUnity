namespace KokoroSharp;

/// <summary> Sample test program that reads the console line, then plays it back with the voice. </summary>
/// <remarks> Intended to act as an introduction to the lower-level parts for users interested in advanced tasks. </remarks>
internal class Program {

    // Mixing voice A with B in this example, but you can mix numerous voices together.
    // .. keeping this outside the 'Main' method for hot-reload support.
    static (int a, int b) Mix => (2, 10);

    static void Main(string[] _) {
        // You'll need to download the model first. You can find it in https://github.com/taylorchu/kokoro-onnx/releases/tag/v0.2.0.
        KokoroTTS tts = new(@"kokoro.onnx"); // The high level inference engine provided by KokoroSharp. We instantiate once, cache it, and reuse it.
        KokoroVoiceManager.LoadVoicesFromPath("voices"); // The models are pre-bundled with the package, but they still need to be loaded manually.
        KokoroVoice kore = KokoroVoiceManager.GetVoice("af_kore"); // Once the voices are loaded, they can be retrieved instantly from memory.
        KokoroVoice nicole = KokoroVoiceManager.GetVoice("af_nicole"); // Kokoro always needs a voice for inference.

        while (true) {
            string txt = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(txt)) { continue; }

            // The easiest way to perform inference with Kokoro model.
            tts.SpeakFast(txt, KokoroVoiceManager.Mix([(kore, Mix.a), (nicole, Mix.b)]));   // Segmented with various rules (see `Segmentation.cs`). Getting an ~instant response, with a potential quality hit.
            //tts.Speak(txt, KokoroVoiceManager.Mix([(kore, mix.a), (nicole, mix.b)]));     // Without segmentations; increasing the playback response time, but may offer increased quality.

            // Although, what's MORE SUITABLE for more advanced tasks, is the `tts.EnqueueJob` method,
            // .. because it allows queueing up multiple *inference jobs* to the engine asynchronously,
            // .. and when, in order, one gets completed, the audio is also being played back in order.
            continue;

            // From here on, these will enqueue to the same `playback` instance, ensuring audio will not overlap.
            int[] tokens = Tokenizer.Tokenize(txt); // (1D)
            List<int[]> ttokens = Segmentation.SplitToSegments(tokens, minFollowupSegmentsLength: 300); // (2D)

            // Note that the `KokoroTTS` instance hosts its own instance of `KokoroPlayback`, for convenience,
            // .. but for anything more advanced than `SpeakFast`, you'll need to provide your own, or an alternative.
            KokoroPlayback playback = new KokoroPlayback();
            
            // Can inference with a 1D token array, waiting until the full inference completes before hearing back (up to 510 tokens).
            tts.EnqueueJob(KokoroJob.Create(tokens, KokoroVoiceManager.Mix([(kore, Mix.a), (nicole, Mix.b)]), speed:0.8f, playback.Enqueue));

            // Or with 2D token array, processing them segment-by-segment, hearing back as quickly as possible (same with `tts.SpeakFast()`).
            // .. 2D arrays are not restricted by the 510 token limit, because none of the segments will surpass that.
            tts.EnqueueJob(KokoroJob.Create(ttokens, kore, speed:1, playback.Enqueue));

            // And can also manually load the voice from the path you want, as a float...
            float[,,] michaelNPY = NumSharp.np.Load<float[,,]>(@"voices\am_michael.npy");
            tts.EnqueueJob(KokoroJob.Create(ttokens, michaelNPY, speed:0.8f, playback.Enqueue));

            // ...or as a KokoroVoice. Those types are fully interchangeable with each other.
            KokoroVoice onyxVoice = KokoroVoice.FromPath(@"voices\am_onyx.npy");
            tts.EnqueueJob(KokoroJob.Create(ttokens, onyxVoice, speed:1.2f, playback.Enqueue));
        }
    }
}
