using Game.Components;
using System.Linq;
using Unity.Entities;
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
                None = new[] { ComponentType.ReadOnly<Target>() }
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<Components.Event>(), ComponentType.ReadOnly<TargetFound>());

            m_LayerMask = LayerMask.NameToLayer("Entity");

            m_Random = new MRandom((uint)System.Environment.TickCount);
        }

        protected override void OnUpdate()
        {
            ForEach((Entity entity, ref SearchingForTarget searchForTarget, ref Position position) =>
            {
                if (searchForTarget.StartTime + searchForTarget.SearchForTargetTime <= Time.time)
                {
                    var targetArray = Physics.OverlapSphere(position.Value, searchForTarget.Radius, 1 << m_LayerMask).Where(t => t.GetComponent<GameObjectEntity>().Entity != entity).ToArray();

                    if (targetArray.Length == 0)
                    {
                        searchForTarget.StartTime = Time.time;
                    }
                    else
                    {
                        var targetFound = PostUpdateCommands.CreateEntity(m_Archetype);
                        PostUpdateCommands.SetComponent(targetFound, new TargetFound
                        {
                            This = entity,
                            Value = targetArray[m_Random.NextInt(0, targetArray.Length)].GetComponent<GameObjectEntity>().Entity
                        });
                    }
                }
            }, m_Group);
        }
    }
}