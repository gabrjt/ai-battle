using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    public class SetProjectileDestroySystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref Collided collided) =>
            {
                var entity = collided.This;
                if (!EntityManager.HasComponent<Destroy>(entity))
                {
                    PostUpdateCommands.AddComponent(entity, new Destroy());
                }
            });
        }
    }
}