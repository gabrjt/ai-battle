using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class ViewVisibleSystem : ComponentSystem
    {
        private ComponentGroup m_VisibleGroup;
        private ComponentGroup m_InvisbleGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_VisibleGroup = Entities.WithAll<Translation, ViewInfo, ViewVisible>().ToComponentGroup();
            m_InvisbleGroup = Entities.WithAll<Translation, ViewInfo>().WithNone<ViewVisible>().ToComponentGroup();

            var maxSqrViewDistanceFromCameraEntity = EntityManager.CreateEntity(ComponentType.ReadWrite<MaxSqrViewDistanceFromCamera>());
            EntityManager.SetComponentData(maxSqrViewDistanceFromCameraEntity, new MaxSqrViewDistanceFromCamera { Value = 10000 });

            RequireSingletonForUpdate<CameraArm>();
            RequireSingletonForUpdate<MaxSqrViewDistanceFromCamera>();
        }

        protected override void OnUpdate()
        {
            var cameraTranslation = float3.zero;
            var maxSqrViewDistanceFromCamera = GetSingleton<MaxSqrViewDistanceFromCamera>().Value;

            ForEach((ref CameraArm cameraArm, ref Translation translation) =>
            {
                cameraTranslation = translation.Value;
            });

            Entities.With(m_VisibleGroup).ForEach((Entity entity, ref Translation translation) =>
            {
                var sqrDistance = math.distancesq(translation.Value, cameraTranslation);

                if (sqrDistance <= maxSqrViewDistanceFromCamera) return;

                PostUpdateCommands.RemoveComponent<ViewVisible>(entity);
            });

            Entities.With(m_InvisbleGroup).ForEach((Entity entity, ref Translation translation) =>
            {
                var sqrDistance = math.distancesq(translation.Value, cameraTranslation);

                if (sqrDistance > maxSqrViewDistanceFromCamera) return;

                PostUpdateCommands.AddComponent(entity, new ViewVisible());
            });
        }
    }
}