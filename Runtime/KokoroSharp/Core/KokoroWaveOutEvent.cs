
using NAudio.Wave;
using System;
using UnityEngine;

namespace KokoroSharp.Core
{
    /// <summary> Base class for cross platform audio playback, with API mostly compatible with NAudio's <see cref="WaveOutEvent"/> API. </summary>
    /// <remarks> Each platform (Windows/Linux/MacOS) derives from this to expose a nice interface back to KokoroSharp. </remarks>
    public abstract class KokoroWaveOutEvent
    {
        public RawSourceWaveStream stream { get; private set; }

        /// <summary> The state of the playback (Playing/Stopped). </summary>
        public abstract PlaybackState PlaybackState { get; }

        /// <summary> Initializes the audio buffer with an audio stream. </summary>
        public void Init(RawSourceWaveStream stream) => this.stream = stream;

        /// <summary> Begins playing back the audio stream this instance was initialized with. </summary>
        public abstract void Play();

        /// <summary> Immediately stops the playback. Does not touch the 'stream' though. </summary>
        public abstract void Stop();

        /// <summary> Adjust the volume of the playback. [0.0, to 1.0] </summary>
        public abstract void SetVolume(float volume);

        /// <summary> Disposes the instance, freeing up any memory or threads it uses. </summary>
        public abstract void Dispose();

        /// <summary> Gets the percentage of how much audio has already been played back. </summary>
        /// <remarks> NOTE that for non-windows platforms, this is an approximate. </remarks>
        public virtual float CurrentPercentage => stream.Position / (float)stream.Length;

        /// <summary> Pause not supported for simplicity. </summary>
        public void Pause() => throw new NotImplementedException("We're not gonna support this.");
    }

}
