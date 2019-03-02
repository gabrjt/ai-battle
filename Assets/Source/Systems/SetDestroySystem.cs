using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(SetBarrier))]
    public class SetDestroySystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private EntityArchetype m_Archetype;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Died>() }
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.Create<Event>(), ComponentType.Create<Destroyed>());
        }

        protected override void OnUpdate()
        {
            var entityCommandBuffer = World.GetExistingManager<EventBarrier>().CreateCommandBuffer();

            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var diedType = GetArchetypeChunkComponentType<Died>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                var diedArray = chunk.GetNativeArray(diedType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = diedArray[entityIndex].This;

                    PostUpdateCommands.AddComponent(entity, new Destroy());
                    PostUpdateCommands.AddComponent(entity, new Disabled());

                    var destroyed = entityCommandBuffer.CreateEntity(m_Archetype);
                    entityCommandBuffer.SetComponent(destroyed, new Destroyed { This = entity });
                }
            }

            chunkArray.Dispose();
        }
    }
}