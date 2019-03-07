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
            if (!HasSingleton<PoolableObjectCount>() || !EntityManager.Exists(GetSingleton<PoolableObjectCount>().Owner)) return; // TODO: remove this when RequireSingletonForUpdate is working.

            var poolableObjectCount = GetSingleton<PoolableObjectCount>();

            if (!EntityManager.HasComponent<TextMeshProUGUI>(poolableObjectCount.Owner)) return;

            var destroySystem = World.GetExistingManager<DestroySystem>();

            var count = destroySystem.m_CharacterPool.Count +
                        destroySystem.m_KnightPool.Count +
                        destroySystem.m_OrcWolfRiderPool.Count +
                        destroySystem.m_SkeletonPool.Count +
                        destroySystem.m_HealthBarPool.Count;

            poolableObjectCount.Value = count;
            SetSingleton(poolableObjectCount);

            var characterCountText = EntityManager.GetComponentObject<TextMeshProUGUI>(poolableObjectCount.Owner);
            characterCountText.text = $"{count:#0} Poolable Objects";
        }
    }
}