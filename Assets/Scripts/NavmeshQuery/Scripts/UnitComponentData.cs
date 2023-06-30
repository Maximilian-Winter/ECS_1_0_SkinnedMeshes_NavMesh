using Unity.Entities;
using Unity.Mathematics;

public struct UnitComponentData : IComponentData
{
    public float Speed;
    public int CurrentBufferIndex;
    public float MinDistance;
    public bool Reached;
    public float3 CurrentWaypoint;
    public Random Rand;

}
