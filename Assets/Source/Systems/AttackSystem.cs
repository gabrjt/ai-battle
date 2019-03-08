using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateAfter(typeof(DisengageSystem))]
    public class AttackSystem : ComponentSystem
    {
        private ComponentGroup m_Group;
        private EntityArchetype m_Archetype;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<TargetInRange>(), ComponentType.ReadOnly<AttackDamage>(), ComponentType.ReadOnly<AttackDuration>(), ComponentType.ReadOnly<AttackSpeed>() },
                None = new[] { ComponentType.ReadWrite<Attacking>(), ComponentType.ReadOnly<Cooldown>(), ComponentType.ReadOnly<Dying>() }
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Damaged>());
        }

        protected override void OnUpdate()
        {
        }
    }
}