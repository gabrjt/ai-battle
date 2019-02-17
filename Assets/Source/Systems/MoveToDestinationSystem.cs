using Game.Components;
using Unity.Entities;
using UnityEngine.AI;

namespace Game.Systems
{
    public class MoveToDestinationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((NavMeshAgent navMeshAgent, ref Destination destination) =>
            {
                navMeshAgent.SetDestination(destination.Value);
            });
        }
    }
}