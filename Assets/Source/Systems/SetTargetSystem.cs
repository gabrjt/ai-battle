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
                var entity = targetFound.This;
                PostUpdateCommands.AddComponent(entity, new Target { Value = targetFound.Value });
            });
        }
    }
}