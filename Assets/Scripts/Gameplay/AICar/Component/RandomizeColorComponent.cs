using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
struct RandomizeColorData : IComponentData {}

[UnityEngine.DisallowMultipleComponent]
class RandomizeColorComponent : ComponentDataWrapper<RandomizeColorData> { }