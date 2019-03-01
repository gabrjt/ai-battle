using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    
    public class PlayDyingAnimationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref ViewReference viewReference, ref Dead dead) =>
            {
                var animator = EntityManager.GetComponentObject<Animator>(viewReference.Value);
                animator.speed = 1;
                animator.Play("Dying");
            });
        }
    }
}