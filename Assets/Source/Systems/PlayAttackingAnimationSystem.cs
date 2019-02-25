using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class PlayAttackingAnimationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref ViewReference viewReference, ref Attack attack) =>
            {
                EntityManager.GetComponentObject<Animator>(viewReference.Value).Play("Attacking");
            });
        }
    }
}