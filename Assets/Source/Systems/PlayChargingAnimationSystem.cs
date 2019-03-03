using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

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
                All = new[] { ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<ViewReference>() },
                None = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Attacking>() }
            });
        }

        protected override void OnUpdate()
        {
            ForEach((NavMeshAgent navMeshAgent, ref ViewReference viewReference, ref Position position, ref Target target, ref AttackDistance attackDistance) =>
            {
                var animator = EntityManager.GetComponentObject<Animator>(viewReference.Value);
                animator.speed = 1;

                if (navMeshAgent.pathPending || math.distance(position.Value, EntityManager.GetComponentData<Position>(target.Value).Value) <= attackDistance.Max)
                {
                    animator.Play("Idle");
                }
                else
                {
                    animator.Play("Charging");
                }
            }, m_Group);
        }
    }
}