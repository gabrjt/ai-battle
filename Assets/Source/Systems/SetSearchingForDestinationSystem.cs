using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(SetBarrier))]
    public class SetSearchingForDestinationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref IdleTimeExpired idleTimeExpired) =>
            {
                if (EntityManager.HasComponent<SearchingForDestination>(idleTimeExpired.This)) return;

                PostUpdateCommands.AddComponent(idleTimeExpired.This, new SearchingForDestination());
            });
        }
    }
}