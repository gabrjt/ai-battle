using Game.Components;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class FrameRateSystem : ComponentSystem
    {
        private float m_FPS;

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<FrameRate>(), ComponentType.Create<TextMeshProUGUI>() }
            });
        }

        protected override void OnUpdate()
        {
            ForEach((TextMeshProUGUI frameRateText) =>
            {
                var fps = 1 / Time.deltaTime;
                m_FPS -= (m_FPS - fps) * Time.deltaTime;
                frameRateText.text = $"{m_FPS:#0} FPS";
            }, m_Group);
        }
    }
}