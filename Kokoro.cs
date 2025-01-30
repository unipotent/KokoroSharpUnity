//namespace KokoroSharp;


//using Microsoft.ML.OnnxRuntime;
//using Microsoft.ML.OnnxRuntime.Tensors;

//using NumSharp;
//using NumSharp.Generic;

//using System.Diagnostics;

//public class EspeakConfig {

//}

//public class KokoroConfig {
//    public string model_path;
//    public string voices_path;
//    public EspeakConfig espeak_config;

//    public KokoroConfig(string modelPath, string voicesPath, EspeakConfig espeakConfig) {

//    }
//}

//public class Kokoro : IDisposable {
//    private const int MAX_PHONEME_LENGTH = 510;
//    private const int SAMPLE_RATE = 24000;

//    private InferenceSession _session;
//    private Dictionary<string, NDArray<float>> _voices;
//    private KokoroConfig _config;

//    public Kokoro(string modelPath, string voicesPath, EspeakConfig? espeakConfig = null) {
//        _config = new KokoroConfig(modelPath, voicesPath, espeakConfig);
//        KokoroLib.ValidateConfig(_config);

//        var options = new SessionOptions();
//        options.AppendExecutionProvider_DML(0);
//        _session = new InferenceSession(modelPath, options);

//        _voices = KokoroLib.LoadVoices(voicesPath); // Implement via NumSharp
//        _tokenizer = new Tokenizer(espeakConfig);
//    }

//    public static Kokoro FromSession(InferenceSession session, string voicesPath, EspeakConfig? espeakConfig = null) {
//        var instance = new Kokoro(session.ModelPath, voicesPath, espeakConfig);
//        instance._session = session;
//        return instance;
//    }

//    private (NDArray<float>, int) CreateAudio(string phonemes, NDArray<float> voice, float speed) {
//        Debug.WriteLine($"Phonemes: {phonemes}");
//        if (phonemes.Length > MAX_PHONEME_LENGTH) { Debug.WriteLine($"Truncating to {MAX_PHONEME_LENGTH} phonemes"); }

//        phonemes = phonemes.Substring(0, MAX_PHONEME_LENGTH);
//        var tokens = _tokenizer.Tokenize(phonemes);

//        // Tensor preparation
//        var inputTokens = new DenseTensor<int>(new[] { 1, tokens.Length + 2 });
//        inputTokens[0, 0] = 0;
//        for (int i = 0; i < tokens.Length; i++) { inputTokens[0, i + 1] = tokens[i]; }
//        inputTokens[0, (int) inputTokens.Length - 1] = 0;

//        var inputs = new List<NamedOnnxValue> {
//            NamedOnnxValue.CreateFromTensor("tokens", inputTokens),
//            NamedOnnxValue.CreateFromTensor("style", voice.ToTensor()),
//            NamedOnnxValue.CreateFromTensor("speed", new DenseTensor<float>(new[] { speed }, [1]))
//        };

//        using var results = _session.Run(inputs);
//        var audio = results.First().AsTensor<float>().ToArray();
//        NDArray<float>.FromMultiDimArray(audio)

//        return (audio, SAMPLE_RATE);
//    }

//    public async IAsyncEnumerable<(float[] Audio, int SampleRate)> CreateStreamAsync(
//        string text,
//        object voice,
//        float speed = 1.0f,
//        string lang = "en-us",
//        string? phonemes = null,
//        bool trim = true) {
//        //var processingTask = Task.Run(() => KokoroLib.ProcessBatchesAsync(/* ... */));
//        //await foreach (var chunk in KokoroLib.GetAudioStream(processingTask)) { yield return chunk; }
//    }

//    public void Dispose() => _session?.Dispose();
//}

//// Dummy library stubs
//public static class KokoroLib {
//    public static void ValidateConfig(KokoroConfig config) => throw new NotImplementedException();
//    public static Dictionary<string, NDArray<float>> LoadVoices(string path) => throw new NotImplementedException();
//    public static IAsyncEnumerable<(float[], int)> GetAudioStream(Task processingTask) => throw new NotImplementedException();
//}
