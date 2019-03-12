using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class PlayChargingAnimationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = Entities.WithAll<ViewReference, Target, Destination>().WithNone<Attacking, TargetInRange, FacingTarget, Dead>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            Entities.With(m_Group).ForEach((ref ViewReference viewReference, ref AttackSpeed attackSpeed) =>
            {
                var animator = EntityManager.GetComponentObject<Animator>(viewReference.Value);
                animator.speed = 1;
                animator.Play("Charging");
            });
        }
    }
}