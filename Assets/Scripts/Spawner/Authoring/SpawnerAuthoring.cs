using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Data
{

class SpawnerAuthoring : MonoBehaviour
{
    public int SpawnWidth;
    public int SpawnHeight;
    public float Spacing;
    public GameObject Prefab;
    public float SpawnRate;
    public float CohesionBias;
    public float SeparationBias;
    public float AlignmentBias; 
    public float PerceptionRadius;
    public float TargetBias;
    public float Speed;
    public int CellSize;
    //public float MaxDistanceFromSpawnPosition;
    //public uint Seed;
}

class SpawnerBaker : Baker<SpawnerAuthoring>
{
    public override void Bake(SpawnerAuthoring authoring)
    {
        AddComponent(new Spawner
        {
            Prefab = GetEntity(authoring.Prefab),
            SpawnPosition = authoring.transform.position,
            NextSpawnTime = 0.0f,
            SpawnRate = authoring.SpawnRate,
            // Rand = new  Unity.Mathematics.Random( authoring.Seed ),
            // MaxDistanceFromSpawnPosition = authoring.MaxDistanceFromSpawnPosition,
            SpawnWidth = authoring.SpawnWidth,
            SpawnHeight =  authoring.SpawnHeight,
            Spacing = authoring.Spacing,
            CohesionBias = authoring.CohesionBias,
            AlignmentBias = authoring.AlignmentBias,
            CellSize = authoring.CellSize,
            SeparationBias = authoring.SeparationBias,
            PerceptionRadius = authoring.PerceptionRadius,
            TargetBias = authoring.TargetBias,
            Speed = authoring.Speed
        });
    }
}


}
