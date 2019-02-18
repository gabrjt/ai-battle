using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    public class UpdateTargetDestinationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((Entity entity, ref Target target, ref Destination destination, ref Position position, ref AttackDistance attackDistance) =>
            {
                var targetDestination = EntityManager.GetComponentData<Position>(target.Value).Value;
                var direction = math.normalizesafe(targetDestination - position.Value);
                destination.Value = targetDestination - direction * attackDistance.Value;
            });
        }
    }
}