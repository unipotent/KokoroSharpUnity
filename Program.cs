using System.Diagnostics;
namespace KokoroSharp;
using NAudio.Wave;

internal class Program {
    static void Main(string[] args) {
        var kokoro = new KokoroModel(@"C:\Users\lyrco\Downloads\kokoro-v0_19.onnx");
        
        var vPath = @"C:\Users\lyrco\Downloads\espeak-ng\voiceStyle.npy";
        while (true) {
            var x = Console.ReadLine();
            var s = Tokenizer.Tokenize(x);
            Debug.WriteLine(string.Join(',', s));

            var wave = kokoro.TokensToWav_SingleBatch(s, vPath, 1);
            PlayAudio(wave);
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
