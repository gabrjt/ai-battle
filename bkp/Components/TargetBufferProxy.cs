using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    [InternalBufferCapacity(TargetBufferProxy.InternalBufferCapacity)]
    public struct TargetBuffer : IBufferElementData
    {
        // These implicit conversions are optional, but can help reduce typing.
        public static implicit operator Entity(TargetBuffer e) { return e.Value; }

        public static implicit operator TargetBuffer(Entity e) { return new TargetBuffer { Value = e }; }

        public Entity Value;
    }

    public class TargetBufferProxy : DynamicBufferProxy<TargetBuffer>
    {
        public const int InternalBufferCapacity = 5;
    }
}