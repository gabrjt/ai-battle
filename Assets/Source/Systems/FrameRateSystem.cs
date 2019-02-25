using Game.Components;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class FrameRateSystem : ComponentSystem
    {
        private float m_FPS;

        protected override void OnUpdate()
        {
            ForEach((TextMeshProUGUI frameRateText, ref FrameRate frameRate) =>
            {
                var fps = 1 / Time.deltaTime;
                m_FPS -= (m_FPS - fps) * Time.deltaTime;
                frameRateText.text = $"{m_FPS:#0} FPS";
            });
        }
    }
}