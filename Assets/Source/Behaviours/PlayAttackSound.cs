using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Behaviours
{
    [RequireComponent(typeof(AudioSource))]
    public class PlayAttackSound : MonoBehaviour
    {
        private AudioSource[] m_AudioSourceArray;

        private Random m_Random;

        private void Awake()
        {
            m_AudioSourceArray = GetComponents<AudioSource>();

            m_Random = new Random((uint)System.Environment.TickCount);
        }

        private AudioSource GetRandomAudio()
        {
            return m_AudioSourceArray[m_Random.NextInt(m_AudioSourceArray.Length)];
        }

        public void PlayAtPoint(float3 point)
        {
            var isPlaying = true;
            AudioSource audioSource = null;

            while (isPlaying)
            {
                audioSource = GetRandomAudio();
                isPlaying = audioSource.isPlaying;
            }

            AudioSource.PlayClipAtPoint(audioSource.clip, point);
        }
    }
}