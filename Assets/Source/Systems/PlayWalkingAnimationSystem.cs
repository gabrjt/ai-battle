﻿using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class PlayWalkingAnimationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destination>() },
                None = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Target>() }
            });
        }

        protected override void OnUpdate()
        {
            ForEach((ref ViewReference viewReference) =>
            {
                EntityManager.GetComponentObject<Animator>(viewReference.Value).Play("Walking");
            }, m_Group);
        }
    }
}