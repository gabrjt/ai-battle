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

            m_Group = Entities.WithAll<PoolableObjectCount, TextMeshProUGUI>().ToComponentGroup();

            RequireSingletonForUpdate<PoolableObjectCount>();
        }

        protected override void OnUpdate()
        {
            var poolableObjectCount = GetSingleton<PoolableObjectCount>();
            var viewPoolSystem = World.GetExistingManager<ViewPoolSystem>();
            var count = viewPoolSystem.m_KnightPool.Count + viewPoolSystem.m_OrcWolfRiderPool.Count + viewPoolSystem.m_SkeletonPool.Count;

            poolableObjectCount.Value = count;
            SetSingleton(poolableObjectCount);

            Entities.With(m_Group).ForEach((TextMeshProUGUI poolableObjectCountText) =>
            {
                poolableObjectCount.Value = count;
                poolableObjectCountText.text = $"{count:#0} Poolable Objects";
            });
        }
    }
}