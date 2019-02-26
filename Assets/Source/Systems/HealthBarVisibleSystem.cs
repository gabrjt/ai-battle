using Game.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems
{
    public class HealthBarVisibleSystem : ComponentSystem
    {
        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            RequireSingletonForUpdate<CameraSingleton>();
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<CameraSingleton>()) return; // TODO: use RequireSingletonForUpdate.

            ForEach((Entity entity, ref HealthBar healthBar, ref HealthBarOwnerPosition healthBarOwnerPosition) =>
            {
                var owner = healthBar.Owner;
                var gameObject = EntityManager.GetComponentObject<RectTransform>(entity).parent.gameObject;
                var isVisible = healthBar.IsVisible;
                var ownerPosition = healthBarOwnerPosition.Value;

                gameObject.GetComponent<Image>().enabled = isVisible;
                var images = gameObject.GetComponentsInChildren<Image>();
                foreach (var image in images)
                {
                    image.enabled = isVisible;
                }
            });
        }
    }
}