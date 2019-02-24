using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class PlayWalkingAnimationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((Animator animator, ref View view) =>
            {
                var owner = view.Owner;
                if (EntityManager.HasComponent<Destination>(owner) && !EntityManager.HasComponent<Idle>(owner) && !EntityManager.HasComponent<Target>(owner))
                {
                    animator.Play("Walking");
                }
            });
        }
    }
}