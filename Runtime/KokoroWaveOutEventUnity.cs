using KokoroSharp.Core;
using NAudio.Wave;
using System;
using UnityEngine;
using KokoroSharp.Utilities;
using UnityEngine.Audio; 

namespace KokoroSharpUnity
{
    public class KokoroWaveOutEventUnity : KokoroWaveOutEvent
    {
        KokoroUnity kokoroUnity;
        public AudioSource audioSource;
        private AudioClip audioClip;
        private PlaybackState playbackState = PlaybackState.Stopped;
        private float volume = 1.0f;
        private bool disposed = false;

        // Audio data processing
        private float[] audioData;
        private int sampleRate;
        private int channels;
        private volatile bool isPlaying = false;

        public KokoroWaveOutEventUnity()
        {
            ExecuteOnMainThread(()=>
            {
                kokoroUnity = UnityEngine.Object.FindFirstObjectByType<KokoroUnity>();
                if (kokoroUnity == null)
                {
                    GameObject obj = new GameObject("KokoroUnity");
                    kokoroUnity = obj.AddComponent<KokoroUnity>();
                    kokoroUnity.audioSource = new GameObject("AudioSource", typeof(AudioSource)).GetComponent<AudioSource>();
                }
            }
        }
        public override PlaybackState PlaybackState
        {
            get
            {
                return playbackState;
            }
        }

        public override void Play()
        {
            if (disposed || stream == null)
                return;

            ExecuteOnMainThread(() =>
            {
                if (audioSource == null)
                    InitializeAudioSource();

                ConvertStreamToAudioClip();

                if (audioClip != null)
                {
                    playbackState = PlaybackState.Playing;
                    isPlaying = true;
                    kokoroUnity.PlayOneShotWithCallback(audioSource, audioClip, delegate { stream.Position = stream.Length; playbackState = PlaybackState.Stopped; });
                }
            });
        }

        public override void Stop()
        {
            if (disposed)
                return;

            ExecuteOnMainThread(() =>
            {
                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }

                playbackState = PlaybackState.Stopped;
                isPlaying = false;

            });
        }

        public override void SetVolume(float volume)
        {
            this.volume = Mathf.Clamp01(volume);

            ExecuteOnMainThread(() =>
            {
                if (audioSource != null)
                {
                    audioSource.volume = this.volume;
                }
            });
        }

        public override void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            ExecuteOnMainThread(() =>
            {

                if (audioSource != null)
                {
                    if (audioSource.isPlaying)
                        audioSource.Stop();
                }

                if (audioClip != null)
                {
                    audioSource.clip = null;
                    UnityEngine.Object.Destroy(audioClip);
                    audioClip = null;
                }

                audioData = null;
                playbackState = PlaybackState.Stopped;


            });
        }

        private void InitializeAudioSource()
        {
            if (stream == null || disposed)
                return;
            audioSource = kokoroUnity.UnityAudioSource;
        }

        private void ConvertStreamToAudioClip()
        {
            sampleRate = stream.WaveFormat.SampleRate;
            channels = stream.WaveFormat.Channels;
            int bitsPerSample = stream.WaveFormat.BitsPerSample;

            // Read all data from stream
            stream.Position = 0;
            byte[] rawData = new byte[stream.Length];
            stream.Read(rawData, 0, (int)stream.Length);

            if (bitsPerSample == 16)
            {
                audioData = new float[rawData.Length / 2];
                for (int i = 0; i < audioData.Length; i++)
                {
                    short sample = BitConverter.ToInt16(rawData, i * 2);
                    audioData[i] = Mathf.Clamp(sample / 32768.0f, -1.0f, 1.0f);
                }
            }
            else if (bitsPerSample == 32)
            {
                if (stream.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    audioData = new float[rawData.Length / 4];
                    for (int i = 0; i < audioData.Length; i++)
                    {
                        float sample = BitConverter.ToSingle(rawData, i * 4);
                        audioData[i] = Mathf.Clamp(sample, -1.0f, 1.0f); // Clamp to prevent clipping
                    }
                }
                else
                {
                    // 32-bit int
                    audioData = new float[rawData.Length / 4];
                    for (int i = 0; i < audioData.Length; i++)
                    {
                        int sample = BitConverter.ToInt32(rawData, i * 4);
                        audioData[i] = Mathf.Clamp(sample / 2147483648.0f, -1.0f, 1.0f);
                    }
                }
            }
            else if (bitsPerSample == 8)
            {
                // 8-bit PCM (unsigned)
                audioData = new float[rawData.Length];
                for (int i = 0; i < audioData.Length; i++)
                {
                    audioData[i] = Mathf.Clamp((rawData[i] - 128) / 128.0f, -1.0f, 1.0f); // Clamp and convert to [-1, 1] range
                }
            }
            else
            {
                Debug.LogError($"Unsupported bit depth {bitsPerSample}");
                return;
            }
            audioClip = AudioClip.Create(
                "KokoroAudio",
                audioData.Length / channels,
                channels,
                sampleRate,
                false
            );

            audioClip.SetData(audioData, 0);

        }

        private static void ExecuteOnMainThread(System.Action action)
        {
            if (action == null)
                return;

            KokoroUnity.EnqueueAction(action);
        }
    }
}