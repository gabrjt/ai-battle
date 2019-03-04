using Game.MonoBehaviours;
using Game.Components;
using Game.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

public class PlaySpawnedSoundSystem : JobComponentSystem
{
    private struct Initialized : ISystemStateComponentData { }

    [BurstCompile]
    private struct ConsolidateJob : IJobChunk
    {
        public NativeHashMap<Entity, Initialized>.Concurrent AddInitializedEntityMap;

        public NativeQueue<Entity>.Concurrent RemoveInitializedEntityQueue;

        [ReadOnly]
        public ArchetypeChunkEntityType EntityType;

        [ReadOnly]
        public ArchetypeChunkComponentType<Initialized> InitializedType;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var entityArray = chunk.GetNativeArray(EntityType);

            if (!chunk.Has(InitializedType))
            {
                for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    AddInitializedEntityMap.TryAdd(entityArray[entityIndex], new Initialized());
                }
            }
            else
            {
                for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    RemoveInitializedEntityQueue.Enqueue(entityArray[entityIndex]);
                }
            }
        }
    }

    private struct AddInitializedJob : IJob
    {
        public NativeHashMap<Entity, Initialized> AddInitializedEntityMap;

        public EntityCommandBuffer CommandBuffer;

        public void Execute()
        {
            var entityArray = AddInitializedEntityMap.GetKeyArray(Allocator.Temp);

            for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
            {
                var entity = entityArray[entityIndex];
                CommandBuffer.AddComponent(entity, AddInitializedEntityMap[entity]);
            }

            entityArray.Dispose();
        }
    }

    private struct RemoveInitializedJob : IJob
    {
        public NativeQueue<Entity> RemoveInitializedEntityQueue;

        public EntityCommandBuffer CommandBuffer;

        public void Execute()
        {
            while (RemoveInitializedEntityQueue.TryDequeue(out var entity))
            {
                CommandBuffer.RemoveComponent<Initialized>(entity);
            }
        }
    }

    private ComponentGroup m_Group;

    private NativeHashMap<Entity, Initialized> m_AddInitializedEntityMap;

    private NativeQueue<Entity> m_RemoveInitializedEntityQueue;

    protected override void OnCreateManager()
    {
        base.OnCreateManager();

        m_Group = GetComponentGroup(new EntityArchetypeQuery
        {
            All = new[] { ComponentType.ReadOnly<View>(), ComponentType.ReadOnly<Visible>() },
            None = new[] { ComponentType.Create<Initialized>() }
        }, new EntityArchetypeQuery
        {
            All = new[] { ComponentType.Create<Initialized>() },
            None = new[] { ComponentType.ReadOnly<Visible>() },
        });

        m_RemoveInitializedEntityQueue = new NativeQueue<Entity>(Allocator.Persistent);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        DisposeMap();

        m_AddInitializedEntityMap = new NativeHashMap<Entity, Initialized>(m_Group.CalculateLength(), Allocator.TempJob);

        var setBarrier = World.GetExistingManager<SetBarrier>();
        var removeBarrier = World.GetExistingManager<RemoveBarrier>();

        inputDeps = new ConsolidateJob
        {
            AddInitializedEntityMap = m_AddInitializedEntityMap.ToConcurrent(),
            RemoveInitializedEntityQueue = m_RemoveInitializedEntityQueue.ToConcurrent(),
            EntityType = GetArchetypeChunkEntityType(),
            InitializedType = GetArchetypeChunkComponentType<Initialized>(true)
        }.Schedule(m_Group, inputDeps);

        var addInitializedDeps = new AddInitializedJob
        {
            AddInitializedEntityMap = m_AddInitializedEntityMap,
            CommandBuffer = setBarrier.CreateCommandBuffer()
        }.Schedule(inputDeps);

        var removeInitializedDeps = new RemoveInitializedJob
        {
            RemoveInitializedEntityQueue = m_RemoveInitializedEntityQueue,
            CommandBuffer = removeBarrier.CreateCommandBuffer()
        }.Schedule(inputDeps);

        inputDeps = JobHandle.CombineDependencies(addInitializedDeps, removeInitializedDeps);

        inputDeps.Complete();

        var entityArray = m_AddInitializedEntityMap.GetKeyArray(Allocator.Temp);

        for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
        {
            var entity = entityArray[entityIndex];
            var transform = EntityManager.GetComponentObject<Transform>(entity);

            transform.GetComponentInChildren<PlaySpawnedSound>().PlayAtPoint(transform.position);
        }

        entityArray.Dispose();

        setBarrier.AddJobHandleForProducer(inputDeps);
        removeBarrier.AddJobHandleForProducer(inputDeps);

        return inputDeps;
    }

    protected override void OnStopRunning()
    {
        base.OnStopRunning();

        DisposeMap();
    }

    protected override void OnDestroyManager()
    {
        base.OnDestroyManager();

        Dispose();
    }

    private void DisposeMap()
    {
        if (m_AddInitializedEntityMap.IsCreated)
        {
            m_AddInitializedEntityMap.Dispose();
        }
    }

    public void Dispose()
    {
        DisposeMap();

        if (m_RemoveInitializedEntityQueue.IsCreated)
        {
            m_RemoveInitializedEntityQueue.Dispose();
        }
    }
}