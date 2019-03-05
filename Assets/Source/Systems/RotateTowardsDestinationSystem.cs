using Game.Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [DisableAutoCreation]
    public class RotateTowardsDestinationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Transform>(),
                    ComponentType.ReadOnly<Translation>(),
                    ComponentType.ReadWrite<Rotation>(),
                    ComponentType.ReadOnly<RotationSpeed>(),
                    ComponentType.ReadOnly<Destination>()},
                None = new[] { ComponentType.ReadOnly<Target>() }
            });
        }

        protected override void OnUpdate()
        {
            ForEach((ref Translation translation, ref Rotation rotation, ref RotationSpeed rotationSpeed, ref Destination destination) =>
            {
            }, m_Group);
        }
    }
}