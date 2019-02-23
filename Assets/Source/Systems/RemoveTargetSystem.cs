using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    public class RemoveTargetSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref Killed killed) =>
            {
                PostUpdateCommands.RemoveComponent<Target>(killed.This);
            });
        }
    }
}