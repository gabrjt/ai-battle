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
                PostUpdateCommands.AddComponent(collided.This, new Destroy());
            });
        }
    }
}