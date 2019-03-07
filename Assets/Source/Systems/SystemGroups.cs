using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class EntityLifecycleGroup : ComponentSystemGroup { }
}

