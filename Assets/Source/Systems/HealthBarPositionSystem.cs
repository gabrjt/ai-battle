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
        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            RequireSingletonForUpdate<CameraSingleton>();
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<CameraSingleton>()) return; // TODO: remove this when RequireSingletonForUpdate is working.

            var camera = EntityManager.GetComponentObject<Camera>(GetSingleton<CameraSingleton>().Owner);

            ForEach((RectTransform rectTransform, ref HealthBar healthBar, ref OwnerPosition ownerPosition) =>
            {
                var transform = rectTransform.parent;
                transform.position = camera.WorldToScreenPoint(ownerPosition.Value + math.up());
            });
        }
    }
}