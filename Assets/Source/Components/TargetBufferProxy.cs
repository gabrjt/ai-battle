using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    [InternalBufferCapacity(3)]
    public struct TargetBufferElement : IBufferElementData
    {
        // These implicit conversions are optional, but can help reduce typing.
        public static implicit operator Entity(TargetBufferElement e) { return e.Value; }

        public static implicit operator TargetBufferElement(Entity e) { return new TargetBufferElement { Value = e }; }

        public Entity Value;
    }

    public class TargetBufferProxy : DynamicBufferProxy<TargetBufferElement>
    {
        public const int InternalBufferCapacity = 3;
    }
}