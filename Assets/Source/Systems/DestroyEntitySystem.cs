using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Systems
{
    [UpdateInGroup(typeof(EntityLifecycleGroup))]
    public class DestroyEntitySystem : ComponentSystem
    {
        private struct Job : IJob
        {
            [ReadOnly] public EntityCommandBuffer CommandBuffer;
            [DeallocateOnJobCompletion] public NativeArray<Entity> EntityArray;

            public void Execute()
            {
            }
        }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>() }
            },
             new EntityArchetypeQuery
             {
                 All = new[] { ComponentType.ReadOnly<Event>() }
             });
        }

        protected override void OnUpdate()
        {
            var groupLength = m_Group.CalculateLength();
            var totalCount = (int)(groupLength * 0.25f);
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