using Game.Components;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using MRandom = Unity.Mathematics.Random;

namespace Game.Systems
{
    public class SearchForTargetSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private EntityArchetype m_Archetype;

        private LayerMask m_LayerMask;

        private MRandom m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.Create<SearchingForTarget>() },
                Any = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Destination>() },
                None = new[] { ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dead>() }
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<Components.Event>(), ComponentType.ReadOnly<TargetFound>());

            m_LayerMask = LayerMask.NameToLayer("Entity");

            m_Random = new MRandom((uint)System.Environment.TickCount);
        }

        protected override void OnUpdate()
        {
            ForEach((Entity entity, ref SearchingForTarget searchForTarget, ref Position position) =>
            {
                if (searchForTarget.StartTime + searchForTarget.Interval <= Time.time)
                {
                    var positionValue = position.Value;

                    var targetArray = Physics.OverlapSphere(position.Value, searchForTarget.Radius, 1 << m_LayerMask)
                        .Where(collider => collider.GetComponent<GameObjectEntity>().Entity != entity)
                        .OrderBy(collider => math.distance(collider.transform.position, positionValue))
                        .ToArray();

                    if (targetArray.Length == 0)
                    {
                        searchForTarget.StartTime = Time.time;
                    }
                    else
                    {
                        foreach (var target in targetArray)
                        {
                            var targetEntity = target.GetComponent<GameObjectEntity>().Entity;

                            if (EntityManager.HasComponent<Dead>(targetEntity)) continue;

                            var targetFound = PostUpdateCommands.CreateEntity(m_Archetype);
                            PostUpdateCommands.SetComponent(targetFound, new TargetFound
                            {
                                This = entity,
                                Other = targetEntity
                            });

                            break;
                        }
                    }
                }
            }, m_Group);
        }
    }
}