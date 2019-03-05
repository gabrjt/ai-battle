using Game.Components;
using Game.Systems;
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
        }

        protected override void OnUpdate()
        {
            var destroySystem = World.GetExistingManager<DestroySystem>();
            var count = destroySystem.m_HealthBarPool.Count;

            ForEach((TextMeshProUGUI poolableObjectCountText, ref PoolableObjectCount poolableObjectCount) =>
            {
                poolableObjectCount.Value = count;
                poolableObjectCountText.text = $"{count:#0} Poolable Objects";
            });
        }
    }
}