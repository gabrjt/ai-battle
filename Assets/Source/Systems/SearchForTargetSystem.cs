using Game.Components;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Systems
{
    public class SearchForTargetSystem : ComponentSystem
    {
        private class Comparer : IComparer<Collider>
        {
            public float3 Position;

            public int Compare(Collider lhs, Collider rhs)
            {
                var lhsSqrDistance = math.distancesq(lhs.transform.position, Position);
                var rhsSqrDistance = math.distancesq(rhs.transform.position, Position);

                return lhsSqrDistance.CompareTo(rhsSqrDistance);
            }
        }

        private ComponentGroup m_Group;

        private EntityArchetype m_Archetype;

        private LayerMask m_LayerMask;

        private int m_Layer;

        private Random m_Random;

        private readonly Comparer m_Comparer = new Comparer();

        private Collider[] m_CachedColliderArray = new Collider[10];

        private EntityCommandBuffer m_EntityCommandBuffer;

        private F_EDD<SearchingForTarget, Position> m_OnUpdate;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.Create<SearchingForTarget>() },
                Any = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Destination>() },
                None = new[] { ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dead>() }
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.Create<Components.Event>(), ComponentType.Create<TargetFound>());

            m_LayerMask = LayerMask.NameToLayer("Entity");
            m_Layer = 1 << m_LayerMask;

            m_Random = new Random((uint)System.Environment.TickCount);
            m_OnUpdate = OnUpdate;
        }

        protected override void OnUpdate()
        {
            m_EntityCommandBuffer = World.GetExistingManager<EndFrameBarrier>().CreateCommandBuffer();

            ForEach(m_OnUpdate, m_Group);
        }

        private void OnUpdate(Entity entity, ref SearchingForTarget searchForTarget, ref Position position)
        {
            if (searchForTarget.StartTime + searchForTarget.Interval <= Time.time)
            {
                var positionValue = position.Value;
                var count = Physics.OverlapSphereNonAlloc(position.Value, searchForTarget.Radius, m_CachedColliderArray, m_Layer);

                if (count > 0)
                {
                    m_Comparer.Position = positionValue;

                    Array.Sort(m_CachedColliderArray, 0, count, m_Comparer);

                    var colliderIndex = 0;

                    do
                    {
                        var target = m_CachedColliderArray[colliderIndex];
                        var targetEntity = target.GetComponent<GameObjectEntity>().Entity;

                        if (entity == targetEntity || EntityManager.HasComponent<Dead>(targetEntity) || EntityManager.HasComponent<Destroy>(targetEntity)) continue;

                        var targetFound = m_EntityCommandBuffer.CreateEntity(m_Archetype);

                        m_EntityCommandBuffer.SetComponent(targetFound, new TargetFound
                        {
                            This = entity,
                            Other = targetEntity
                        });

                        break;
                    }
                    while (++colliderIndex < count);

                    Array.Clear(m_CachedColliderArray, 0, count);
                }
                else
                {
                    searchForTarget.StartTime = Time.time;
                }
            }
        }
    }
}