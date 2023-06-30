
using System;
using Data;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Systems
{


[BurstCompile]
public partial struct SpawnerSystem : ISystem
{

    public int SpawnCount;
    public Random Rand;
    
    public float3 CurrentPosition;
    public void OnCreate( ref SystemState state )
    {
        Rand = new Unity.Mathematics.Random( ( uint )System.Guid.NewGuid().GetHashCode() );
        SpawnCount = 0;
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Queries for all Spawner components. Uses RefRW because this system wants
        // to read from and write to the component. If the system only needed read-only
        // access, it would use RefRO instead.
        EntityCommandBuffer ecb = new EntityCommandBuffer( Allocator.Temp );
        foreach (RefRW<Spawner> spawner in SystemAPI.Query<RefRW<Spawner>>())
        {
            ProcessSpawner(ref state, spawner, ref ecb);
        }

        ecb.Playback( state.EntityManager );
    }

    private void ProcessSpawner(ref SystemState state, RefRW<Spawner> spawner, ref EntityCommandBuffer ecb)
    {
        // If the next spawn time has passed.
        if (spawner.ValueRO.NextSpawnTime < SystemAPI.Time.ElapsedTime)
        {
            float3 currentPosition = spawner.ValueRW.SpawnPosition;
            currentPosition.x += spawner.ValueRW.Spacing * SpawnCount;
            float3 startPosition = currentPosition;
            for ( int i = 0; i < spawner.ValueRW.SpawnWidth; i++ )
            {
                for ( int j = 0; j < spawner.ValueRW.SpawnHeight; j++ )
                {
                    // Spawns a new entity and positions it at the spawner.
                    Entity newEntity = ecb.Instantiate(spawner.ValueRO.Prefab);
                    ecb.RemoveComponent<LinkedEntityGroup>( newEntity );
                    float3 spawnPosition = currentPosition;

                 
                    
                    spawnPosition.y = 0.0f;
                    ecb.SetComponent(newEntity, LocalTransform.FromPosition( spawnPosition ));
                    
                    UnitComponentData unitComponentData = new UnitComponentData
                    {
                        Speed = 1.4f,
                        CurrentBufferIndex = 0,
                        MinDistance = 1.5f,
                        Rand = new Random( Rand.NextUInt() )
                    };
                    ecb.AddComponent(newEntity, unitComponentData);
                    
                    float3 targetPos = Rand.NextFloat3(new float3( -100.0f,0.0f,-100.0f ), new float3( 100.0f,0.0f,100.0f ));
                    targetPos.y = 0.0f;
                    
                    NavAgent_Component navAgentComponent = new NavAgent_Component
                    {
                        fromLocation = spawnPosition,
                        toLocation = targetPos,
                        routed = false
                    };
                    
                    ecb.AddComponent<NavAgent_Component>( newEntity, navAgentComponent );
                    ecb.AddBuffer<NavAgent_Buffer>(newEntity);
                    
                 
                    
                    ecb.AddComponent<NavAgent_Owner>( newEntity, new NavAgent_Owner
                    {
                        Owner = newEntity
                    } );
                    
                   
                    currentPosition = new float3( currentPosition.x + spawner.ValueRW.Spacing, currentPosition.y, currentPosition.z );
                }
                currentPosition = new Vector3( startPosition.x , currentPosition.y, currentPosition.z + spawner.ValueRW.Spacing );
            }

            //float3 targetPos = spawnPosition +
            //                   spawner.ValueRW.Rand.NextFloat3(
            //                       new float3( spawner.ValueRO.MaxDistanceFromSpawnPosition ) );
            //targetPos.y = 0.0f;
            /*NavAgent_Component navAgentComponent = new NavAgent_Component
            {
                fromLocation = spawnPosition,
                toLocation = targetPos,
                routed = false
            };

            ecb.AddComponent<NavAgent_Component>( newEntity, navAgentComponent );
            ecb.AddBuffer<NavAgent_Buffer>(newEntity);
            
            ecb.AddComponent(newEntity, new UnitComponentData
            {
                speed =  spawner.ValueRW.Rand.NextInt(1, 5),
                currentBufferIndex = 0,
                minDistance = 1.5f,
                offset = float3.zero,
                Rand = spawner.ValueRW.Rand
            });
            
            ecb.AddComponent<NavAgent_Owner>( newEntity, new NavAgent_Owner
            {
                Owner = newEntity
            } );*/
            
            // Resets the next spawn time.
            spawner.ValueRW.NextSpawnTime = (float)SystemAPI.Time.ElapsedTime + spawner.ValueRO.SpawnRate;
        }
    }
}


}
