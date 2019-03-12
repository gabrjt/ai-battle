using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class PlayIdleAnimationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<ViewReference>(), ComponentType.ReadOnly<Idle>() },
                None = new[] { ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<ViewReference>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<TargetInRange>() },
                None = new[] { ComponentType.ReadOnly<FacingTarget>() }
            });
        }

        protected override void OnUpdate()
        {
            Entities.With(m_Group).ForEach((ref ViewReference viewReference) =>
            {
                var animator = EntityManager.GetComponentObject<Animator>(viewReference.Value);
                animator.speed = 1;
                animator.Play("Idle");
            });
        }
    }
}