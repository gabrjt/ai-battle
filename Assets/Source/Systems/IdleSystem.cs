using Game.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

public class IdleSystem : ComponentSystem
{
    private struct AddIdleJob : IJobChunk
    {
        [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
        [ReadOnly] public ArchetypeChunkEntityType EntityType;
        [ReadOnly] public ArchetypeChunkComponentType<Character> CharacterType;
        [NativeSetThreadIndex] private readonly int m_ThreadIndex;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var entityArray = chunk.GetNativeArray(EntityType);
            if (chunk.Has(CharacterType))
            {
                for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    CommandBuffer.AddComponent(m_ThreadIndex, entityArray[entityIndex], new Idle());
                }
            }
        }
    }

    private struct RemoveIdleJob : IJobChunk
    {
        [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
        [ReadOnly] public ArchetypeChunkEntityType EntityType;
        [ReadOnly] public ArchetypeChunkComponentType<Destination> DestinationType;
        [ReadOnly] public ArchetypeChunkComponentType<Target> TargetType;
        [NativeSetThreadIndex] private readonly int m_ThreadIndex;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var entityArray = chunk.GetNativeArray(EntityType);
            if (chunk.Has(DestinationType) || chunk.Has(TargetType))
            {
                for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    CommandBuffer.RemoveComponent<Idle>(m_ThreadIndex, entityArray[entityIndex]);
                }
            }
        }
    }

    private ComponentGroup m_ProcessGroup;
    private ComponentGroup m_AddGroup;
    private ComponentGroup m_RemoveGroup;

    protected override void OnCreateManager()
    {
        base.OnCreateManager();

        m_ProcessGroup = GetComponentGroup(new EntityArchetypeQuery
        {
            // Processar Idle
            All = new[] { ComponentType.ReadWrite<Idle>() }
        });

        m_AddGroup = GetComponentGroup(new EntityArchetypeQuery
        {
            // Adicionar Idle
            All = new[] { ComponentType.ReadOnly<Character>() },
            None = new[] { ComponentType.ReadWrite<Idle>(), ComponentType.ReadOnly<Dead>() }
        });

        m_RemoveGroup = GetComponentGroup(new EntityArchetypeQuery
        {
            // Remover Idle
            All = new[] { ComponentType.ReadWrite<Idle>() },
            Any = new[] { ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>() }
        });
    }

    protected override void OnUpdate()
    {
        EntityManager.AddComponent(m_AddGroup, typeof(Idle));
        EntityManager.RemoveComponent(m_RemoveGroup, ComponentType.ReadWrite<Idle>());
    }
}