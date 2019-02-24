using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class PlayIdleAnimationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((Animator animator, ref View view) =>
            {
                var owner = view.Owner;
                if (EntityManager.HasComponent<Idle>(owner) && !EntityManager.HasComponent<Destination>(owner) && !EntityManager.HasComponent<Target>(owner))
                {
                    animator.Play("Idle");
                }
            });
        }
    }
}