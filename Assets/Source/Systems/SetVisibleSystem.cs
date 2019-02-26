using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Systems
{
    [UpdateAfter(typeof(SetCameraSingletonSystem))]
    public class SetVisibleSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct Job : IJobProcessComponentData<Visible, OwnerPosition, SqrMaxDistanceFromCamera>
        {
            public float3 CameraPosition;

            public void Execute(ref Visible visible, [ReadOnly] ref OwnerPosition ownerPosition, [ReadOnly] ref SqrMaxDistanceFromCamera sqrMaxDistanceFromCamera)
            {
                visible.Value = math.distancesq(CameraPosition, ownerPosition.Value) < math.lengthsq(sqrMaxDistanceFromCamera.Value);
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