using Game.Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    public class SearchForTargetSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private LayerMask m_LayerMask;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.Create<SearchForTarget>() },
                Any = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Destination>() }
            });

            m_LayerMask = LayerMask.NameToLayer("Entity");
        }

        protected override void OnUpdate()
        {
            ForEach((ref SearchForTarget searchForTarget, ref Position position) =>
            {
                if (searchForTarget.StartTime + searchForTarget.SearchForTargetTime <= Time.time)
                {
                    var colliderArray = Physics.OverlapSphere(position.Value, searchForTarget.Radius,1 << m_LayerMask);

                    foreach (var collider in colliderArray)
                    {
                        Debug.Log($"Found target {collider.name}");
                    }
                    searchForTarget.StartTime = Time.time;
                }
            }, m_Group);
        }
    }
}