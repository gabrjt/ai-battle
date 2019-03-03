using Game.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Systems
{
    // TODO: reactive system
    public class DisableAICharacterSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((CapsuleCollider capsuleCollider, ref Dead dead) =>
            {
                capsuleCollider.enabled = false;
            });

            ForEach((NavMeshAgent navMeshAgent, ref Dead dead) =>
            {
                navMeshAgent.enabled = false;
            });
        }
    }
}