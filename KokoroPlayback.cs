namespace KokoroSharp;

using NAudio.Wave;
using System.Collections.Concurrent;

/// <summary> Helper class that can simplify audio playback from Kokoro Inference Jobs. Can be either reused or live with a specific KokoroJob instance. </summary>
/// <remarks> Internally hosts a background worker thread that keeps checking for any queued samples, and plays them back if there's nothing else playing, in the same order they were queued. </remarks>
public class KokoroPlayback : IDisposable {
    readonly WaveFormat waveFormat = new(24000, 16, 1);
    readonly WaveOutEvent waveOut = new();
    readonly ConcurrentQueue<float[]> queuedSamples = [];

    volatile bool hasExited;

    public KokoroJob job { get; }

    /// <summary> If true, the output audio of the model will be nicified before being played back. </summary>
    /// <remarks> Nicification includes trimming silent start and finish, and attempting to reduce noise. </remarks>
    public static bool NicifySamples { get; set; }

    /// <summary> Creates an audio playback instance, and causes it to automatically play back all samples added via <see cref="Enqueue(float[])"/>. </summary>
    /// <remarks> If 'job' is specified, the instance will automatically cease when the job is completed or canceled. </remarks>
    public KokoroPlayback(KokoroJob job = null) {
        this.job = job;

        new Thread(async () => {
            while (!hasExited) {
                await Task.Delay(100);
                while (!hasExited && queuedSamples.TryDequeue(out var f)) {
                    if (NicifySamples) { f = PostProcessSamples(f); }
                    waveOut.Init(new RawSourceWaveStream(GetBytes(f), 0, f.Length * 2, waveFormat));
                    waveOut.Play();
                    while (!hasExited && waveOut.PlaybackState == PlaybackState.Playing) { await Task.Delay(10); }
                }
                if (queuedSamples.IsEmpty && job?.isDone == true) { Dispose(); }
            }
        }).Start();
    }

    /// <summary> Enqueues specified audio samples for playback. They will be played once all previously queued samples have been played. </summary>
    public void Enqueue(float[] samples) => queuedSamples.Enqueue(samples);

    /// <summary> Immediately stops the playback and notifies the background worker thread to exit. </summary>
    /// <remarks> Note that this DOES NOT terminate any <see cref="KokoroJob"/>s related to this instance. </remarks>
    public void Dispose() {
        hasExited = true;
        waveOut.Stop();
        waveOut.Dispose();
    }

    /// <summary> Performs some pre-processing on target samples, like trimming silence, and discarding potential noise. </summary>
    /// <remarks> Returns a new array with the processed audio samples. Note that the returned array will likely be smaller in size. </remarks>
    public static float[] PostProcessSamples(float[] samples) {
        var (start, end) = (0, samples.Length - 1);
        while (start < samples.Length && Math.Abs(samples[start]) <= 0.01f) { start++; }
        while (end > start && Math.Abs(samples[end]) <= 0.005f) { end--; }
        for (int i = 0; i < samples.Length; i++) { if (Math.Abs(samples[i]) < 0.001f) { samples[i] = 0; } }

        float[] trimmedSamples = new float[end - start + 1];
        Array.Copy(samples, start, trimmedSamples, 0, trimmedSamples.Length);
        return trimmedSamples;
    }

    /// <summary> Converts given 16bit audio sample array to bytes. </summary>
    public static byte[] GetBytes(float[] samples) => samples.Select(f => (short) (f * short.MaxValue)).SelectMany(BitConverter.GetBytes).ToArray();
}
