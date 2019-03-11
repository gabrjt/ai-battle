using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    [DisableAutoCreation]
    public class LogFrameRateSystem : ComponentSystem
    {
        private float m_FPS;

        protected override void OnUpdate()
        {
            var fps = 1 / Time.deltaTime;
            m_FPS -= (m_FPS - fps) * Time.deltaTime;
            Debug.Log($"{m_FPS:#0} FPS");
        }
    }
}