using Game.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems
{
    public class ViewVisibleSystem : ComponentSystem
    {
        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            RequireSingletonForUpdate<CameraSingleton>();
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<CameraSingleton>()) return; // TODO: use RequireSingletonForUpdate.

            ForEach((Entity entity, ref View view, ref Owner owner, ref OwnerPosition ownerPosition) =>
            {
                var gameObject = EntityManager.GetComponentObject<Transform>(entity).gameObject;
                var isVisible = view.IsVisible;

                var meshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var meshRenderer in meshRenderers)
                {
                    meshRenderer.enabled = isVisible;
                }
            });
        }
    }
}