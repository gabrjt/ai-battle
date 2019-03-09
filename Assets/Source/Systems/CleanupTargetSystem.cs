using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(CleanupGroup))]
    public class CleanupTargetSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((Entity entity, ref Target target) =>
            {
                if (EntityManager.Exists(target.Value)) return;

                PostUpdateCommands.RemoveComponent<Target>(entity);
            });
        }
    }
}