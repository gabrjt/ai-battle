using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    
    public class PlayAttackingAnimationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref ViewReference viewReference, ref Target target, ref Attacking attack, ref AttackSpeed attackSpeed) =>
            {
                var animator = EntityManager.GetComponentObject<Animator>(viewReference.Value);
                animator.speed = attackSpeed.Value;
                animator.Play("Attacking");
            });
        }
    }
}