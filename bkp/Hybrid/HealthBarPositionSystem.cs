using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace Game.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class HealthBarPositionSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private Camera m_Camera;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<HealthBar>(), ComponentType.ReadWrite<RectTransform>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<Visible>() }
            });

            RequireSingletonForUpdate<CameraSingleton>();
            RequireForUpdate(m_Group);
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<CameraSingleton>() || !EntityManager.Exists(GetSingleton<CameraSingleton>().Owner)) return; // TODO: remove this when RequireSingletonForUpdate is working.

            m_Camera = EntityManager.GetComponentObject<Camera>(GetSingleton<CameraSingleton>().Owner);

            var positionFromEntity = GetComponentDataFromEntity<Translation>(true);

            ForEach((RectTransform rectTransform, ref Owner owner) =>
            {
                var transform = rectTransform.parent;
                transform.position = m_Camera.WorldToScreenPoint(positionFromEntity[owner.Value].Value + math.up()); // TODO: consolidate...
            }, m_Group);
        }
    }
}