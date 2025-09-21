using System;
using UnityEngine;
using System.Collections;
using System.Collections.Concurrent;
using KokoroSharp;
using KokoroSharp.Core;

namespace KokoroSharpUnity
{
    public class KokoroUnity : MonoBehaviour
    {
        public AudioSource UnityAudioSource;
        private static ConcurrentQueue<System.Action> mainThreadActions = new ConcurrentQueue<System.Action>();

        /*Example Usage

        private KokoroTTS kokoroTTS;
        private void Awake()
        {
            kokoroTTS = KokoroTTS.LoadModel();
        }
        public void GenerateSpeech(string dialogue, string voiceName)
        {
            KokoroVoice voice = KokoroVoiceManager.GetVoice(voiceName);
            kokoroTTS.SpeakFast(dialogue, voice);
        }
        */
        public static void EnqueueAction(Action action)
        {
            mainThreadActions.Enqueue(action);
        }
        public void PlayOneShotWithCallback(AudioSource source, AudioClip clip, Action action)
        {
            source.PlayOneShot(clip);
            StartCoroutine(InvokeAfterDelay(action, clip.length));
        }
        private IEnumerator InvokeAfterDelay(Action action, float time)
        {
            yield return new WaitForSeconds(time);
            action?.Invoke();
            yield break;
        }
        private void Update()
        {
            while (mainThreadActions.TryDequeue(out System.Action action))
            {
                action?.Invoke();
            }
        }
    }
}
