using Game.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class DestroyEventSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() }
            });
        }

        protected override void OnUpdate()
        {
            ForEach((Entity entity) =>
            {
                PostUpdateCommands.DestroyEntity(entity);
            }, m_Group);
        }
    }

    [UpdateInGroup(typeof(EntityLifecycleGroup))]
    public class DestroyEntitySystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>() }
            });
        }

        protected override void OnUpdate()
        {
            var groupLength = m_Group.CalculateLength();
            var totalCount = 1024;
            var maxDestroyCount = math.select(totalCount, groupLength, groupLength > totalCount);

            ForEach((Entity entity) =>
            {
                if (maxDestroyCount <= 0)
                {
                    return;
                }

                PostUpdateCommands.DestroyEntity(entity);
                maxDestroyCount--;
            }, m_Group);
        }
    }
}