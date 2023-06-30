using Unity.Entities;
using Unity.Mathematics;

namespace Data
{

public struct Spawner: IComponentData
{
    public Entity Prefab;
    public float3 SpawnPosition;
    public float NextSpawnTime;
    public float SpawnRate;
    public int SpawnWidth;
    public int SpawnHeight;
    public float Spacing;
    public float CohesionBias;
    public float SeparationBias;
    public float AlignmentBias; 
    public float PerceptionRadius;
    public float TargetBias;
    public float Speed;
    public int CellSize;
}

}
