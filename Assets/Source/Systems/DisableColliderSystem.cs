using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(DeadBarrier))]
    public class DisableColliderSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((CapsuleCollider capsuleCollider, ref Dead dead) =>
            {
                capsuleCollider.enabled = false;
            });
        }
    }
}