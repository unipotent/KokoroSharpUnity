namespace KokoroSharp;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using System.Collections.Generic;

public class KokoroModel {
    readonly InferenceSession _session;

    static float[,,] voiceStyle;

    public KokoroModel(string modelPath, string voicePath = null) {
        var options = new SessionOptions() { EnableMemoryPattern = true, InterOpNumThreads = 8, IntraOpNumThreads = 8 };
        //options.AppendExecutionProvider_CUDA(0);
        _session = new InferenceSession(modelPath, options);
    }

    public float[] TokensToWav_SingleBatch(int[] tokens, string voicePath, float speed = 1) {
        var voiceStyle = NumSharp.np.Load<float[,,]>(voicePath); // 510, 1, 256
        int maxPhonemeLength = voiceStyle.GetLength(0);
        var voiceDims = voiceStyle.GetLength(2);

        if (tokens.Length > maxPhonemeLength) { Array.Resize(ref tokens, maxPhonemeLength); }

        // Form Kokoro's input (<start>{text}<end>)
        var inputTokens = new int[tokens.Length + 2];
        inputTokens[0] = 0; // <start>
        Array.Copy(tokens, 0, inputTokens, 1, tokens.Length);
        inputTokens[^1] = 0; // <end>

        var tokenTensor = new DenseTensor<long>([1, inputTokens.Length]); // Batch size = 1
        for (int i = 0; i < inputTokens.Length; i++) { tokenTensor[0, i] = inputTokens[i]; }
        var styleTensor = new DenseTensor<float>([1, voiceDims]);
        for (int j = 0; j < voiceDims; j++) { styleTensor[0, j] = voiceStyle[tokens.Length, 0, j]; }
        var speedTensor = new DenseTensor<float>(new[] { speed }, [1]);
        
        // Create input data map
        var inputs = new List<NamedOnnxValue> {
            NamedOnnxValue.CreateFromTensor("tokens", tokenTensor),
            NamedOnnxValue.CreateFromTensor("style", styleTensor),
            NamedOnnxValue.CreateFromTensor("speed", speedTensor)
        };
        
        using var results = _session.Run(inputs);
        var x = results[0].AsTensor<float>();
        return [.. x];
    }
}