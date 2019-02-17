using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    public class SetTargetSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((Entity entity, ref TargetFound targetFound) =>
            {
                PostUpdateCommands.AddComponent(entity, new Target { Value = targetFound.Value });

                if (EntityManager.HasComponent<Idle>(entity))
                {
                    PostUpdateCommands.RemoveComponent<Idle>(entity);
                }
            });
        }
    }
}