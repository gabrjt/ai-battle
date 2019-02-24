using Game.Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace Game.Systems
{
    [UpdateAfter(typeof(PostLateUpdate))]
    public class HealthBarPositionSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            var camera = EntityManager.GetComponentObject<Camera>(GetSingleton<CameraSingleton>().Owner);
            ForEach((RectTransform rectTransform, ref HealthBar healthBar) =>
            {
                var transform = rectTransform.parent;
                transform.position = camera.WorldToScreenPoint(EntityManager.GetComponentData<Position>(healthBar.Owner).Value);
            });
        }
    }
}