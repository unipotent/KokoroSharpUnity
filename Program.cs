using System.Diagnostics;
namespace KokoroSharp;
using NAudio.Wave;

internal class Program {
    static void Main(string[] args) {
        var mPath = @"C:\Users\lyrco\Downloads\kokoro-v0_19.onnx";
        var vPath = @"C:\Users\lyrco\Downloads\espeak-ng\voiceStyle.npy";
        var kokoro = new KokoroMinimal(mPath, vPath);
        while (true) {
            var x = Console.ReadLine();
            var s = Tokenizer.Tokenize(x);
            Debug.WriteLine(string.Join(',', s));

            var f = kokoro.TokensToWav_SingleBatch(s, 1);
            Debug.WriteLine(f.Length);
            PlayAudio(f);
        }
    }


    static void PlayAudio(float[] audioData, int sampleRate = 24000) {
        var waveFormat = new WaveFormat(24000, 16, 1);
        using var waveOut = new WaveOutEvent();
        waveOut.Init(new RawSourceWaveStream(audioData.Select(f => (short) (f * short.MaxValue)).SelectMany(BitConverter.GetBytes).ToArray(), 0, audioData.Length * 2, waveFormat));
        waveOut.Play();
        while (waveOut.PlaybackState == PlaybackState.Playing) { Thread.Sleep(100); }
    }
}
