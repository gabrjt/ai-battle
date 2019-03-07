using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(EntityLifecycleGroup))]
    [UpdateAfter(typeof(DestroyEntitySystem))]
    public class InstantiateAICharacterSystem : ComponentSystem
    {
        private ComponentGroup m_Group;
        private EntityArchetype m_Archetype;
        private EntityArchetype m_DestroyAllCharactersArchetype;
        private const int m_MaxDestroyCount = 1024;
        internal int m_TotalCount = 0xFFF;
        internal int m_LastTotalCount;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadOnly<Destroy>() }
            });

            m_Archetype = EntityManager.CreateArchetype(
                ComponentType.ReadWrite<Character>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>()
            );

            m_DestroyAllCharactersArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Components.Event>(), ComponentType.ReadWrite<DestroyAllCharacters>());

            m_LastTotalCount = m_TotalCount;
        }

        protected override void OnUpdate()
        {
            var count = m_Group.CalculateLength();
            var entityCount = m_TotalCount - count;

            DestroyExceedingCharacters(count, entityCount);
            InstantiateCharacters(entityCount);
        }

        private void DestroyExceedingCharacters(int count, int entityCount)
        {
            if (entityCount > 0) return;

            if (m_TotalCount == 0 && count > 0)
            {
                World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer().CreateEntity(m_DestroyAllCharactersArchetype);
            }
            else if (entityCount < 0)
            {
                entityCount = math.abs(entityCount);
                var entityArray = m_Group.ToEntityArray(Allocator.TempJob);
                var maxDestroyCount = math.select(entityCount, m_MaxDestroyCount, entityCount > m_MaxDestroyCount);

                for (var entityIndex = 0; entityIndex < maxDestroyCount; entityIndex++)
                {
                    PostUpdateCommands.AddComponent(entityArray[entityIndex], new Destroy());
                }

                entityArray.Dispose();
            }
        }

        private void InstantiateCharacters(int entityCount)
        {
            if (entityCount <= 0) return;

            var entityArray = new NativeArray<Entity>(entityCount, Allocator.TempJob);

            EntityManager.CreateEntity(m_Archetype, entityArray);

            entityArray.Dispose();
        }
    }
}