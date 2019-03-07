using Game.Components;
using Unity.Entities;
using UnityEngine.AI;

namespace Game.Systems
{
    public class MoveToDestinationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<NavMeshAgent>(), ComponentType.ReadOnly<Destination>() },
                None = new[] { ComponentType.ReadOnly<Dead>() }
            });
        }

        protected override void OnUpdate()
        {
            ForEach((NavMeshAgent navMeshAgent, ref Destination destination) =>
            {
                if ((navMeshAgent.pathPending ||
                    navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid ||
                    navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial) && !destination.IsDirty) return;

                navMeshAgent.SetDestination(destination.Value);
            }, m_Group);
        }
    }
}