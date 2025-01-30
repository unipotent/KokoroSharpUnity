namespace KokoroSharp;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using System.Collections.Generic;
using System.Linq;

public class KokoroMinimal {
    readonly InferenceSession _session;

    static float[,,] voiceStyle;

    public KokoroMinimal(string modelPath, string voicePath = null) {
        var options = new SessionOptions();
        //options.AppendExecutionProvider_CUDA(); // Use DirectML for GPU acceleration
        _session = new InferenceSession(modelPath, options);
        voiceStyle = NumSharp.np.Load<float[,,]>(voicePath); // 510, 1, 256
    }

    public float[] TokensToWav_SingleBatch(int[] tokens, float speed = 1) {
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

    public List<float[]> TokensToWav_MultiBatch(List<int[]> tokenBatches, float speed = 1) {
        int maxPhonemeLength = voiceStyle.GetLength(0);
        var voiceDims = voiceStyle.GetLength(2);

        // Prepare input tensors
        var batchSize = tokenBatches.Count;
        var maxTokenLength = tokenBatches.Max(tokens => Math.Min(tokens.Length, maxPhonemeLength)) + 2; // Add <start> and <end>

        var tokenTensor = new DenseTensor<long>([batchSize, maxTokenLength]);
        var styleTensor = new DenseTensor<float>([batchSize, 256]);
        var speedTensor = new DenseTensor<float>([batchSize]);

        for (int batchIdx = 0; batchIdx < batchSize; batchIdx++) {
            var tokens = tokenBatches[batchIdx];
            if (tokens.Length > maxPhonemeLength) { Array.Resize(ref tokens, maxPhonemeLength); }

            // Form input tokens for the batch (<start>{text}<end>)
            var inputTokens = new int[tokens.Length + 2];
            inputTokens[0] = 0; // <start>
            Array.Copy(tokens, 0, inputTokens, 1, tokens.Length);
            inputTokens[^1] = 0; // <end>

            for (int i = 0; i < inputTokens.Length; i++) { tokenTensor[batchIdx, i] = inputTokens[i]; }
            for (int j = 0; j < voiceDims; j++) { styleTensor[batchIdx, j] = voiceStyle[tokens.Length, 0, j]; }

            speedTensor[batchIdx] = speed;
        }

        // Create input data map
        var inputs = new List<NamedOnnxValue> {
        NamedOnnxValue.CreateFromTensor("tokens", tokenTensor),
        NamedOnnxValue.CreateFromTensor("style", styleTensor),
        NamedOnnxValue.CreateFromTensor("speed", speedTensor)
    };

        // Run inference
        var resultWavList = new List<float[]>();
        using var results = _session.Run(inputs);
        var x = results[0].AsTensor<float>();

        // Extract output for each batch
        for (int batchIdx = 0; batchIdx < batchSize; batchIdx++) {
            var wavData = x.Skip(batchIdx * x.Dimensions[1]).Take(x.Dimensions[1]).ToArray();
            resultWavList.Add(wavData);
        }

        return resultWavList;
    }
}