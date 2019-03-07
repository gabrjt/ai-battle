using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct OrcWolfRider : IComponentData { }

    public class OrcWolfRiderProxy : ComponentDataProxy<OrcWolfRider> { }
}