using Game.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Systems
{
    [UpdateAfter(typeof(SetCameraSingletonSystem))]
    public class SetViewVisibleSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct Job : IJobProcessComponentData<View, OwnerPosition>
        {
            public float3 CameraPosition;

            public void Execute(ref View view, ref OwnerPosition ownerPosition)
            {
                view.IsVisible = math.distancesq(CameraPosition, ownerPosition.Value) < math.lengthsq(view.MaxSqrDistanceFromCamera);
            }
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            RequireSingletonForUpdate<Camera>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (!HasSingleton<CameraSingleton>()) return inputDeps; // TODO: use RequireSingletonForUpdate.

            return new Job
            {
                CameraPosition = EntityManager.GetComponentObject<Transform>(GetSingleton<CameraSingleton>().Owner).parent.position
            }.Schedule(this, inputDeps);
        }
    }
}