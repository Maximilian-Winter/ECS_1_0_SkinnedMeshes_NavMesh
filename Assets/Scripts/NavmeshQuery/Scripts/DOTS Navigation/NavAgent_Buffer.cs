using Unity.Mathematics;
using Unity.Entities;

public struct NavAgent_Buffer : IBufferElementData
{
    public float3 wayPoints;
}


public struct NavAgent_Owner : IComponentData
{
    public Entity Owner;
}