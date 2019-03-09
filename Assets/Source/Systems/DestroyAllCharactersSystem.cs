using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class DestroyAllCharactersSystem : ComponentSystem
    {
        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            RequireSingletonForUpdate<DestroyAllCharacters>();
        }

        protected override void OnUpdate()
        {
            EntityManager.AddComponent(Entities.WithAll<Character>().WithNone<Destroy>().ToComponentGroup(), ComponentType.ReadWrite<Destroy>());
            EntityManager.AddComponent(Entities.WithAll<Character>().WithNone<Disabled>().ToComponentGroup(), ComponentType.ReadWrite<Disabled>());
        }
    }
}