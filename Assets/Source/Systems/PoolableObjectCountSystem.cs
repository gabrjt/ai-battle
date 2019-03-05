using Game.Components;
using TMPro;
using Unity.Entities;

namespace Game.Systems
{
    [DisableAutoCreation]
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
            //var destroySystem = World.GetExistingManager<DestroyGameObjectSystem>();
            //var count = destroyGameObjectSystem.m_HealthBarPool.Count;

            ForEach((TextMeshProUGUI poolableObjectCountText, ref PoolableObjectCount poolableObjectCount) =>
            {
                //poolableObjectCount.Value = count;
                //poolableObjectCountText.text = $"{count:#0} Poolable Objects";
            });
        }
    }
}