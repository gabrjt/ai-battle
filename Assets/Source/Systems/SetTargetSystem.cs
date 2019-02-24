using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    public class SetTargetSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref TargetFound targetFound) =>
            {
                PostUpdateCommands.AddComponent(targetFound.This, new Target { Value = targetFound.Value });
            });
        }
    }
}