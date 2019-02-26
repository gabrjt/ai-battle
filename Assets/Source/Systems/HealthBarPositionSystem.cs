using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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

            ForEach((RectTransform rectTransform, ref HealthBar healthBar) =>
            {
                var transform = rectTransform.parent;
                if (EntityManager.HasComponent<Position>(healthBar.Owner))
                {
                    transform.position = camera.WorldToScreenPoint(EntityManager.GetComponentData<Position>(healthBar.Owner).Value + math.up());
                }
            });
        }
    }
}