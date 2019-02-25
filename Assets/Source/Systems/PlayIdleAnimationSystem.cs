﻿using Game.Components;
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
                All = new[] { ComponentType.ReadOnly<Idle>() },
                None = new[] { ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Attack>() }
            });
        }

        protected override void OnUpdate()
        {
            ForEach((ref ViewReference viewReference) =>
            {
                EntityManager.GetComponentObject<Animator>(viewReference.Value).Play("Idle");
            }, m_Group);
        }
    }
}