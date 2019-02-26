using Game.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class SetHealthBarVisibleSystem : JobComponentSystem
{
    [BurstCompile]
    private struct Job : IJobProcessComponentData<HealthBar, HealthBarOwnerPosition>
    {
        public float3 CameraPosition;

        public void Execute(ref HealthBar healthBar, ref HealthBarOwnerPosition healthBarOwnerPosition)
        {
            healthBar.IsVisible = math.distancesq(CameraPosition, healthBarOwnerPosition.Value) < math.lengthsq(healthBar.MaxSqrDistanceFromCamera);
            //healthBar.IsVisible = math.distance(CameraPosition, healthBarOwnerPosition.Value) < healthBar.MaxSqrDistanceFromCamera;
        }
    }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();

        RequireSingletonForUpdate<Camera>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (!HasSingleton<CameraSingleton>() || !EntityManager.Exists(GetSingleton<CameraSingleton>().Owner))  return inputDeps; // TODO: use RequireSingletonForUpdate.

        return new Job
        {
            CameraPosition = EntityManager.GetComponentObject<Transform>(GetSingleton<CameraSingleton>().Owner).parent.position
        }.Schedule(this, inputDeps);
    }
}