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
                if (EntityManager.Exists(target.Value) && EntityManager.HasComponent<Position>(target.Value))
                {
                    var targetDestination = EntityManager.GetComponentData<Position>(target.Value).Value;
                    var distance = math.distance(position.Value, targetDestination);

                    if (distance < attackDistance.Minimum || distance > attackDistance.Maximum)
                    {
                        var direction = math.normalizesafe(targetDestination - position.Value);
                        destination.LastValue = destination.Value;
                        destination.Value = targetDestination - direction * attackDistance.Minimum;
                    }
                }
            });
        }
    }
}