using Game.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems
{
    public class HealthBarVisibleSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<HealthBar>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<OwnerPosition>(), ComponentType.ReadOnly<Visible>() }
            });

            RequireSingletonForUpdate<CameraSingleton>();
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<CameraSingleton>()) return; // TODO: use RequireSingletonForUpdate.

            ForEach((Entity entity, ref Visible visible, ref Owner owner, ref OwnerPosition ownerPosition) =>
            {
                var gameObject = EntityManager.GetComponentObject<RectTransform>(entity).parent.gameObject;

                var images = gameObject.GetComponentsInChildren<Image>();
                foreach (var image in images)
                {
                    image.enabled = visible.Value;
                }
            }, m_Group);
        }
    }
}