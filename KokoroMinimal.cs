namespace KokoroSharp;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class KokoroMinimal {
    readonly InferenceSession _session;

    static float[,,] voiceStyle;

    public KokoroMinimal(string modelPath, string voicePath = null) {
        var options = new SessionOptions();
        //options.AppendExecutionProvider_DML(); // Use DirectML for GPU acceleration
        _session = new InferenceSession(modelPath, options);
        voiceStyle = NumSharp.np.Load<float[,,]>(voicePath); // 510, 1, 256
    }

    public float[] RunModel(int[] tokens, float speed) {
        // Ensure tokens are padded and truncated to match the sequence length
        const int maxPhonemeLength = 510; // Matches `MAX_PHONEME_LENGTH`
        if (tokens.Length > maxPhonemeLength) {
            Array.Resize(ref tokens, maxPhonemeLength); // Truncate if too long
        }
        var paddedTokens = new int[tokens.Length + 2]; // Add padding (start/end tokens)
        paddedTokens[0] = 0; // Start token
        Array.Copy(tokens, 0, paddedTokens, 1, tokens.Length);
        paddedTokens[paddedTokens.Length - 1] = 0; // End token

        // Prepare inputs
        var tokenTensor = new DenseTensor<long>(new[] { 1, paddedTokens.Length }); // Batch size = 1
        for (int i = 0; i < paddedTokens.Length; i++) {
            tokenTensor[0, i] = paddedTokens[i];
        }

        var styleTensor = new DenseTensor<float>(new[] { 1, 256 });
        for (int i = 0; i < 1; i++) {
            for (int j = 0; j < 256; j++) {
                styleTensor[i, j] = voiceStyle[i, 0, j]; // Match Python's `voice = voice[len(tokens)]`
            }
        }

        var speedTensor = new DenseTensor<float>(new[] { 1 });
        speedTensor[0] = speed;

        // Create input data map
        var inputs = new List<NamedOnnxValue> {
            NamedOnnxValue.CreateFromTensor("tokens", tokenTensor),
            NamedOnnxValue.CreateFromTensor("style", styleTensor),
            NamedOnnxValue.CreateFromTensor("speed", speedTensor)
        };

        using var results = _session.Run(inputs);
        var audioTensor = results.First().AsTensor<float>();
        return audioTensor.ToArray();
    }
}