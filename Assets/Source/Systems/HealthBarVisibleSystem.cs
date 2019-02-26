﻿using Game.Components;
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

            ForEach((Entity entity, ref HealthBar healthBar, ref Owner owner, ref OwnerPosition ownerPosition) =>
            {
                var gameObject = EntityManager.GetComponentObject<RectTransform>(entity).parent.gameObject;
                var isVisible = healthBar.IsVisible;

                var images = gameObject.GetComponentsInChildren<Image>();
                foreach (var image in images)
                {
                    image.enabled = isVisible;
                }
            });
        }
    }
}