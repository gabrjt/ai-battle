using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateBefore(typeof(OwnerRotationSystem))]
    public class RotateTowardsTargetSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref Position position, ref Rotation rotation, ref Target target) =>
            {
                if (EntityManager.HasComponent<Position>(target.Value))
                {
                    var targetPosition = EntityManager.GetComponentData<Position>(target.Value).Value;
                    var direction = math.normalizesafe(targetPosition - position.Value);

                    rotation.Value = quaternion.LookRotation(direction, math.up());
                }
            });
        }
    }
}