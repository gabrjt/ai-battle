using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    //[UpdateAfter(typeof(ProjectileCollisionSystem))]
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

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Killed>());
        }

        protected override void OnUpdate()
        {
            ForEach((ref Damaged damaged) =>
            {
                if (EntityManager.Exists(damaged.Target))
                {
                    var targetHealth = EntityManager.GetComponentData<Health>(damaged.Target);
                    targetHealth.Value -= damaged.Value;

                    if (targetHealth.Value <= 0)
                    {
                        if (EntityManager.Exists(damaged.Source))
                        {
                            var killed = PostUpdateCommands.CreateEntity(m_Archetype);
                            PostUpdateCommands.SetComponent(killed, new Killed
                            {
                                This = damaged.Source,
                                Target = damaged.Target
                            });
                        }
                    }

                    PostUpdateCommands.SetComponent(damaged.Target, targetHealth);
                }
            });
        }
    }
}