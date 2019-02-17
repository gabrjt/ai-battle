using Game.Components;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Systems
{
    public class UpdateTargetDestinationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((Entity entity, ref Target target, ref Destination destination) =>
            {
                var targetDestination = EntityManager.GetComponentData<Position>(target.Value).Value;
                destination.Value = targetDestination;
            });
        }
    }
}