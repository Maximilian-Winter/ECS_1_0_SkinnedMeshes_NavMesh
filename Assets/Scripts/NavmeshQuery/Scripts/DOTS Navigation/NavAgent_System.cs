using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.Experimental.AI;

[BurstCompile]
public partial struct NavAgent_System : ISystem
{
    private NavMeshWorld navMeshWorld;
    private bool HasAssignedQuery;
    private NavMeshQuery currentQuery;

    public void OnCreate( ref SystemState state )
    {
        navMeshWorld = NavMeshWorld.GetDefaultWorld();
        HasAssignedQuery = false;
    }

    public void OnDestroy( ref SystemState state )
    {
    }
    
    public void OnUpdate( ref SystemState state )
    {
        if ( !HasAssignedQuery )
        {
            currentQuery = new NavMeshQuery(
                navMeshWorld,
                Allocator.Persistent,
                NavAgent_GlobalSettings.instance.maxPathNodePoolSize );

            HasAssignedQuery = true;
        }

        float3 extents = NavAgent_GlobalSettings.instance.extents;
        int maxIterations = NavAgent_GlobalSettings.instance.maxIterations;
        int maxPathSize = NavAgent_GlobalSettings.instance.maxPathSize;

        var updateNavAgents = new UpdateNavAgents
        {
            CurrentQuery = currentQuery, Extents = extents, MaxIterations = maxIterations, MaxPathSize = maxPathSize,
        }.Schedule( state.Dependency );

        state.Dependency = JobHandle.CombineDependencies( updateNavAgents, state.Dependency );
        navMeshWorld.AddDependency( state.Dependency );
    }

    [BurstCompile]
    public partial struct UpdateNavAgents : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public NavMeshQuery CurrentQuery;

        public int MaxIterations;
        public int MaxPathSize;
        public float3 Extents;

        [BurstCompile]
        public void Execute(
            DynamicBuffer < NavAgent_Buffer > agentBuffer,
            RefRW < NavAgent_Component > agentComponent,
            RefRO < NavAgent_Owner > navMeshOwner )
        {
            if ( !agentComponent.ValueRO.routed )
            {
                PathQueryStatus status = PathQueryStatus.Failure;

                agentComponent.ValueRW.nml_FromLocation = CurrentQuery.MapLocation(
                    agentComponent.ValueRW.fromLocation,
                    Extents,
                    0 );

                agentComponent.ValueRW.nml_ToLocation = CurrentQuery.MapLocation(
                    agentComponent.ValueRW.toLocation,
                    Extents,
                    0 );

                if ( CurrentQuery.IsValid( agentComponent.ValueRW.nml_FromLocation ) &&
                     CurrentQuery.IsValid( agentComponent.ValueRW.nml_ToLocation ) )
                {
                    status = CurrentQuery.BeginFindPath(
                        agentComponent.ValueRW.nml_FromLocation,
                        agentComponent.ValueRW.nml_ToLocation,
                        -1 );
                }

                if ( status == PathQueryStatus.InProgress )
                {
                    status = CurrentQuery.UpdateFindPath( MaxIterations, out int iterationPerformed );
                }

                if ( status == PathQueryStatus.Success )
                {
                    status = CurrentQuery.EndFindPath( out int polygonSize );

                    NativeArray < NavMeshLocation > res = new NativeArray < NavMeshLocation >(
                        polygonSize,
                        Allocator.Temp );

                    NativeArray < StraightPathFlags > straightPathFlag =
                        new NativeArray < StraightPathFlags >( MaxPathSize, Allocator.Temp );

                    NativeArray < float > vertexSide = new NativeArray < float >( MaxPathSize, Allocator.Temp );
                    NativeArray < PolygonId > polys = new NativeArray < PolygonId >( polygonSize, Allocator.Temp );
                    int straightPathCount = 0;
                    CurrentQuery.GetPathResult( polys );

                    status = PathUtils.FindStraightPath(
                        CurrentQuery,
                        agentComponent.ValueRW.fromLocation,
                        agentComponent.ValueRW.toLocation,
                        polys,
                        polygonSize,
                        ref res,
                        ref straightPathFlag,
                        ref vertexSide,
                        ref straightPathCount,
                        MaxPathSize
                    );

                    if ( status == PathQueryStatus.Success )
                    {
                        agentBuffer.ResizeUninitialized( straightPathCount );

                        for ( int i = 0; i < straightPathCount; i++ )
                        {
                            agentBuffer[i] = new NavAgent_Buffer { wayPoints = res[i].position };
                        }

                        agentComponent.ValueRW.routed = true;
                    }

                    res.Dispose();
                    straightPathFlag.Dispose();
                    polys.Dispose();
                    vertexSide.Dispose();
                }
            }
        }
    }
}
