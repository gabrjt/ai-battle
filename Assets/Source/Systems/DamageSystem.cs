using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateBefore(typeof(ClampHealthSystem))]
    public class DamageSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref Damaged damaged) =>
            {
                var damagedEntity = damaged.Other;

                if (!EntityManager.Exists(damagedEntity) ||
                    EntityManager.HasComponent<Destroy>(damagedEntity) ||
                    !EntityManager.HasComponent<Health>(damagedEntity)) return;

                var health = EntityManager.GetComponentData<Health>(damagedEntity);
                health.Value -= damaged.Value;
                PostUpdateCommands.SetComponent(damagedEntity, health);
            });
        }
    }
}