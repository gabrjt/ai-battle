using Game.Components;
using TMPro;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class PoolableObjectCountSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<PoolableObjectCount>(), ComponentType.ReadWrite<TextMeshProUGUI>() }
            });

            RequireSingletonForUpdate<PoolableObjectCount>();
        }

        protected override void OnUpdate()
        {
            var viewPoolSystem = World.GetExistingManager<ViewPoolSystem>();
            var count = viewPoolSystem.m_KnightPool.Count + viewPoolSystem.m_OrcWolfRiderPool.Count + viewPoolSystem.m_SkeletonPool.Count;

            ForEach((TextMeshProUGUI poolableObjectCountText, ref PoolableObjectCount poolableObjectCount) =>
            {
                poolableObjectCount.Value = count;
                poolableObjectCountText.text = $"{count:#0} Poolable Objects";
            }, m_Group);
        }
    }
}