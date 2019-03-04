﻿using Unity.Mathematics;
using UnityEngine;

namespace Game.Behaviours
{
    [RequireComponent(typeof(AudioSource))]
    public class PlaySpawnedSoundBehaviour : MonoBehaviour
    {
        private AudioSource m_AudioSource;

        private void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();
        }

        public void PlayAtPoint(float3 point)
        {
            if (m_AudioSource.isPlaying) return;

            AudioSource.PlayClipAtPoint(m_AudioSource.clip, point);
        }
    }
}