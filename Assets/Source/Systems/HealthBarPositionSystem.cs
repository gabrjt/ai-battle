using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace Game.Systems
{
    [UpdateAfter(typeof(PostLateUpdate))]
    public class HealthBarPositionSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private Camera m_Camera;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<HealthBar>(), ComponentType.ReadOnly<RectTransform>(), ComponentType.ReadOnly<OwnerPosition>() }
            });

            RequireSingletonForUpdate<CameraSingleton>();
            RequireForUpdate(m_Group);
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<CameraSingleton>()) return; // TODO: remove this when RequireSingletonForUpdate is working.

            m_Camera = EntityManager.GetComponentObject<Camera>(GetSingleton<CameraSingleton>().Owner);

            ForEach((RectTransform rectTransform, ref OwnerPosition ownerPosition) =>
            {
                var transform = rectTransform.parent;
                transform.position = m_Camera.WorldToScreenPoint(ownerPosition.Value + math.up());
            }, m_Group);
        }
    }
}