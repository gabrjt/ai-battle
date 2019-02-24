using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    public class RemoveDestinationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref DestinationReached destinationReached) =>
            {
                PostUpdateCommands.RemoveComponent<Destination>(destinationReached.This);
            });
        }
    }
}