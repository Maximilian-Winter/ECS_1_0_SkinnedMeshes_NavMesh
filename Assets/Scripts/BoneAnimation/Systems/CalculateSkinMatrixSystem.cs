using Unity.Burst;
using Unity.Collections;
using Unity.Deformations;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Jobs;

[RequireMatchingQueriesForUpdate]
[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateBefore(typeof(DeformationsInPresentation))]
[BurstCompile]
internal partial struct CalculateSkinMatrixSystemBase : ISystem
{
    EntityQuery m_BoneEntityQuery;
    EntityQuery m_RootEntityQuery;

    public void OnCreate( ref SystemState state )
    {
        m_BoneEntityQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<LocalToWorld>(),
            ComponentType.ReadOnly<BoneTag>()
        );

        m_RootEntityQuery =  state.GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<RootTag>()
        );
    }
    [BurstCompile]
    public void OnDestroy( ref SystemState state )
    {
    }
    [BurstCompile]
    public void OnUpdate( ref SystemState state )
    {
          var boneCount = m_BoneEntityQuery.CalculateEntityCount();
          var rootCount = m_RootEntityQuery.CalculateEntityCount();
          
        var bonesLocalToWorld = new NativeParallelHashMap<Entity, float4x4>(boneCount, Allocator.TempJob);
        var rootWorldToLocal = new NativeParallelHashMap<Entity, float4x4>(rootCount, Allocator.TempJob);
        var bonesLocalToWorldParallel = bonesLocalToWorld.AsParallelWriter();
        var rootWorldToLocalParallel = rootWorldToLocal.AsParallelWriter();

        var dependency = state.Dependency;

        var bones = new GetBonesLocalToWorldParallel { BonesLocalToWorld = bonesLocalToWorldParallel }.ScheduleParallel(dependency);

        var root =
            new GetRootWorldToLocalParallel { RootWorldToLocal = rootWorldToLocalParallel }.ScheduleParallel(
                bones );
        

        dependency = JobHandle.CombineDependencies(dependency, root);

        dependency =
            new CalculateSkinMatrices { BonesLocalToWorld = bonesLocalToWorld, RootWorldToLocal = rootWorldToLocal }.ScheduleParallel( dependency );

        state.Dependency = JobHandle.CombineDependencies(bonesLocalToWorld.Dispose(dependency), rootWorldToLocal.Dispose(dependency));
    }
    [BurstCompile]
    public partial struct GetBonesLocalToWorldParallel : IJobEntity
    {
        public NativeParallelHashMap < Entity, float4x4 >.ParallelWriter BonesLocalToWorld;
        [BurstCompile]
        public void Execute( Entity entity, LocalToWorld  localToWorld, BoneTag boneTag )
        {
            BonesLocalToWorld.TryAdd( entity, localToWorld.Value );
        }
    }
    
    [BurstCompile]
    public partial struct GetRootWorldToLocalParallel : IJobEntity
    {
        public NativeParallelHashMap<Entity, float4x4>.ParallelWriter RootWorldToLocal;
        [BurstCompile]
        public void Execute( Entity entity, LocalToWorld  localToWorld, RootTag rootTag )
        {
            RootWorldToLocal.TryAdd( entity, math.inverse(localToWorld.Value) );
        }
    }
    [BurstCompile]
    public partial struct CalculateSkinMatrices : IJobEntity
    {
        [ReadOnly]
        public NativeParallelHashMap < Entity, float4x4 > BonesLocalToWorld;
        [ReadOnly]
        public NativeParallelHashMap < Entity, float4x4 > RootWorldToLocal;
        [BurstCompile]
        public void Execute(
            ref DynamicBuffer < SkinMatrix > skinMatrices,
            in DynamicBuffer < BindPose > bindPoses,
            in DynamicBuffer < BoneEntity > bones,
            in RootEntity root )
        {
            for (int i = 0; i < skinMatrices.Length; ++i)
            {
                // Grab localToWorld matrix of bone
                var boneEntity = bones[i].Value;
                var rootEntity = root.Value;
     
                // #TODO: this is necessary for LiveLink?
                if (!BonesLocalToWorld.ContainsKey(boneEntity) || !RootWorldToLocal.ContainsKey(rootEntity))
                    return;
     
                var matrix = BonesLocalToWorld[boneEntity];
     
                // Convert matrix relative to root
                var rootMatrixInv = RootWorldToLocal[rootEntity];
                matrix = math.mul(rootMatrixInv, matrix);
     
                // Compute to skin matrix
                var bindPose = bindPoses[i].Value;
                matrix = math.mul(matrix, bindPose);
     
                // Assign SkinMatrix
                skinMatrices[i] = new SkinMatrix
                {
                    Value = new float3x4(matrix.c0.xyz, matrix.c1.xyz, matrix.c2.xyz, matrix.c3.xyz)
                };
            }
        }
    }
}

