using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    public class RemoveSearchingForTargetSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref TargetFound targetFound) =>
            {
                PostUpdateCommands.RemoveComponent<SearchingForTarget>(targetFound.This);
            });
        }
    }
}