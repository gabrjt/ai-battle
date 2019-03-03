using Game.Components;
using TMPro;
using Unity.Entities;

namespace Game.Systems
{
    public class PoolableObjectCountSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            RequireSingletonForUpdate<PoolableObjectCount>();
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<PoolableObjectCount>()) return; // TODO: remove this when RequireSingletonForUpdate is working.

            var poolableObjectCount = GetSingleton<PoolableObjectCount>();

            if (!EntityManager.HasComponent<TextMeshProUGUI>(poolableObjectCount.Owner)) return;

            var destroyBarrier = World.GetExistingManager<DestroyBarrier>();

            var count = destroyBarrier.m_CharacterPool.Count +
                        destroyBarrier.m_KnightPool.Count +
                        destroyBarrier.m_OrcWolfRiderPool.Count +
                        destroyBarrier.m_SkeletonPool.Count +
                        destroyBarrier.m_HealthBarPool.Count;

            poolableObjectCount.Value = count;
            SetSingleton(poolableObjectCount);

            var characterCountText = EntityManager.GetComponentObject<TextMeshProUGUI>(poolableObjectCount.Owner);
            characterCountText.text = $"{count:#0} Poolable Objects";
        }
    }
}