using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    public class SetSearchingForDestinationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref IdleTimeExpired idleTimeExpired) =>
            {
                PostUpdateCommands.AddComponent(idleTimeExpired.This, new SearchingForDestination());
            });
        }
    }
}