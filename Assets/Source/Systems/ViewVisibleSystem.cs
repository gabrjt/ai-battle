using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class ViewVisibleSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<View>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<OwnerPosition>(), ComponentType.ReadOnly<Visible>() }
            });

            RequireSingletonForUpdate<CameraSingleton>();
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<CameraSingleton>()) return; // TODO: use RequireSingletonForUpdate.

            ForEach((Entity entity, ref Visible visible, ref Owner owner, ref OwnerPosition ownerPosition) =>
            {
                var gameObject = EntityManager.GetComponentObject<Transform>(entity).gameObject;

                var meshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var meshRenderer in meshRenderers)
                {
                    meshRenderer.enabled = visible.Value;
                }
            }, m_Group);
        }
    }
}