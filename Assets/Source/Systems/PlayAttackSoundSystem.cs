using Game.Behaviours;
using Game.Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    public class PlayAttackSoundSystem : ComponentSystem
    {
        /*
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            throw new System.NotImplementedException();
        }
        */

        protected override void OnUpdate()
        {
            var viewReferenceFromEntity = GetComponentDataFromEntity<ViewReference>(true);
            var visibleFromEntity = GetComponentDataFromEntity<Visible>(true);
            var positionFromEntity = GetComponentDataFromEntity<Position>(true);

            ForEach((ref Damaged damaged) =>
            {
                var view = viewReferenceFromEntity[damaged.This].Value;

                if (!visibleFromEntity.Exists(view)) return;

                EntityManager.GetComponentObject<Transform>(view).GetComponentInChildren<PlayAttackSoundBehaviour>().PlayAtPoint(positionFromEntity[damaged.This].Value);
            });
        }
    }
}