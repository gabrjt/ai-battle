using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    public class PlayChargingAnimationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>() },
                None = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Attack>() }
            });
        }

        protected override void OnUpdate()
        {
            ForEach((ref ViewReference viewReference, ref Position position, ref Target target, ref AttackDistance attackDistance) =>
            {
                var animator = EntityManager.GetComponentObject<Animator>(viewReference.Value);
                if (EntityManager.HasComponent<Position>(target.Value))
                {
                    if (math.distance(position.Value, EntityManager.GetComponentData<Position>(target.Value).Value) <= attackDistance.Maximum)
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
            }, m_Group);
        }
    }
}