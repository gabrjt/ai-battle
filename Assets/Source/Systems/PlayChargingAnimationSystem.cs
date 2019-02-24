using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    public class PlayChargingAnimationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((Animator animator, ref View view) =>
            {
                var owner = view.Owner;
                if (EntityManager.HasComponent<Destination>(owner) && !EntityManager.HasComponent<Idle>(owner) && EntityManager.HasComponent<Target>(owner))
                {
                    var target = EntityManager.GetComponentData<Target>(owner).Value;

                    if (EntityManager.HasComponent<Position>(target))
                    {
                        var position = EntityManager.GetComponentData<Position>(owner).Value;
                        var targetPosition = EntityManager.GetComponentData<Position>(target).Value;

                        if (math.distance(position, targetPosition) <= EntityManager.GetComponentData<AttackDistance>(owner).Value + 1)
                        {
                            animator.Play("Idle");
                        }
                        else
                        {
                            animator.Play("Charging");
                        }
                    }
                    else
                    {
                        animator.Play("Idle");
                    }
                }
            });
        }
    }
}