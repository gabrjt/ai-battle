using Game.Components;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Systems
{
    [DisableAutoCreation]
    public class HealthBarVisibleSystem : ComponentSystem
    {
        private struct SpawnData
        {
            public Entity Owner;

            public float3 OwnerPosition;

            public GameObject GameObject;

            public bool IsVisible;
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            RequireSingletonForUpdate<CameraSingleton>();
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<CameraSingleton>()) return; // TODO: use RequireSingletonForUpdate.

            var spawnDataHashSet = new HashSet<SpawnData>();

            ForEach((Entity entity, ref HealthBar healthBar, ref HealthBarOwnerPosition healthBarOwnerPosition) =>
            {
                spawnDataHashSet.Add(new SpawnData
                {
                    Owner = healthBar.Owner,
                    GameObject = EntityManager.GetComponentObject<RectTransform>(entity).parent.gameObject,
                    IsVisible = healthBar.IsVisible,
                    OwnerPosition = healthBarOwnerPosition.Value
                });
            });

            foreach (var spawnData in spawnDataHashSet)
            {
                var owner = spawnData.Owner;
                var ownerPosition = spawnData.OwnerPosition;
                var gameObject = spawnData.GameObject;
                var isVisible = spawnData.IsVisible;

                var wasVisible = gameObject.activeInHierarchy;

                gameObject.SetActive(isVisible);

                if (isVisible && !wasVisible)
                {
                    var entity = gameObject.GetComponentInChildren<GameObjectEntity>().Entity;

                    gameObject.name = $"Health Bar {entity.Index}";

                    EntityManager.SetComponentData(entity, new HealthBar
                    {
                        Owner = owner,
                        IsVisible = isVisible,
                        MaxSqrDistanceFromCamera = 50 * 50
                    });

                    EntityManager.SetComponentData(entity, new HealthBarOwnerPosition
                    {
                        Value = ownerPosition
                    });

                    var transform = gameObject.GetComponent<RectTransform>();
                    transform.position = EntityManager.GetComponentObject<Camera>(GetSingleton<CameraSingleton>().Owner).WorldToScreenPoint(spawnData.OwnerPosition + math.up());
                }
            }
        }
    }
}