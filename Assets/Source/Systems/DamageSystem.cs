using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateBefore(typeof(ClampHealthSystem))]
    [UpdateAfter(typeof(EndFrameBarrier))]
    public class DamageSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private EntityArchetype m_Archetype;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Damaged>() },
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.Create<Event>(), ComponentType.Create<Killed>());
        }

        protected override void OnUpdate()
        {
            var entityCommandBuffer = World.GetExistingManager<EndFrameBarrier>().CreateCommandBuffer();

            ForEach((ref Damaged damaged) =>
            {
                if (EntityManager.Exists(damaged.Other))
                {
                    var targetHealth = EntityManager.GetComponentData<Health>(damaged.Other);
                    targetHealth.Value -= damaged.Value;

                    if (targetHealth.Value <= 0)
                    {
                        if (EntityManager.Exists(damaged.This))
                        {
                            var killed = entityCommandBuffer.CreateEntity(m_Archetype);
                            entityCommandBuffer.SetComponent(killed, new Killed
                            {
                                This = damaged.This,
                                Other = damaged.Other
                            });
                        }
                    }

                    PostUpdateCommands.SetComponent(damaged.Other, targetHealth);
                }
            });
        }
    }
}