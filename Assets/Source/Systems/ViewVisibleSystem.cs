using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class ViewVisibleSystem : ComponentSystem
    {
        private ComponentGroup m_VisibleGroup;
        private ComponentGroup m_InvisbleGroup;

        protected override void OnUpdate()
        {
            m_VisibleGroup = Entities.WithAll<MaxSqrViewDistanceFromCamera, ViewVisible>().ToComponentGroup();
            m_InvisbleGroup = Entities.WithAll<MaxSqrViewDistanceFromCamera>().WithNone<ViewVisible>().ToComponentGroup();
        }
    }
}