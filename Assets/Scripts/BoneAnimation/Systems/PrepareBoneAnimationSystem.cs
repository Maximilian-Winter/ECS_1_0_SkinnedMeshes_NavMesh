using BoneAnimation;
using BoneAnimation.DataComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Animations.Systems
{

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PrepareBoneAnimationSystem : ISystem
{
    private EntityQuery m_BoneEntityQuery;

    public void OnCreate( ref SystemState state )
    {
        m_BoneEntityQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<LocalToWorld>(),
            ComponentType.ReadOnly<BoneTag>()
        );

    }
    [BurstCompile]
    public void OnDestroy( ref SystemState state )
    {
    }
    [BurstCompile]
    public void OnUpdate( ref SystemState state )
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        var boneCount = m_BoneEntityQuery.CalculateEntityCount();
        
        if ( boneCount > 0 )
        {
            var bonesKeyframes = new NativeParallelHashMap < Entity, AnimationKeyframe >(boneCount, Allocator.TempJob);
            
            var dependency = state.Dependency;

            var animationCurveLibrary = SystemAPI.GetSingleton < EntitiesAnimationCurveLibrary >();
            var updateKeyframes = new UpdateBoneKeyframesJob { BonesToKeyframe = bonesKeyframes.AsParallelWriter(), DeltaTime = deltaTime, AnimationCurveLibrary = animationCurveLibrary}.ScheduleParallel( dependency);
            dependency = JobHandle.CombineDependencies( updateKeyframes, dependency );
            
            dependency =
                new UpdateBoneAnimatorsJob { BonesToKeyframe = bonesKeyframes }.ScheduleParallel(dependency);
            
            state.Dependency = JobHandle.CombineDependencies(bonesKeyframes.Dispose(dependency), dependency);
        }
        
        //parallelWriter.Playback( state.EntityManager );
    }

    [BurstCompile]
    public partial struct UpdateBoneKeyframesJob : IJobEntity
    {
        public NativeParallelHashMap < Entity, AnimationKeyframe >.ParallelWriter BonesToKeyframe;
        public float DeltaTime;

        [ReadOnly]
        public EntitiesAnimationCurveLibrary AnimationCurveLibrary;

        [BurstCompile]
        public void Execute(RefRW <AnimatorComponentEntity> animator,DynamicBuffer <BoneAnimatorEntity> bones, DynamicBuffer <BlendParameter> blendParameters,  BoneAnimation.DataComponents.BoneAnimationSetEntity animations)
        {
            if ( ! animator.ValueRW.Enabled ||  animator.ValueRW.CurrentAnimationIndex == -1 )
            {
                return;
            }
            
            animator.ValueRW.UpdateAnimationTime( DeltaTime, AnimationCurveLibrary, animations );


            for ( int to = 0; to <bones.Length; to++ )
            {
                Entity entity = bones[to].BoneEntity;
                BonesToKeyframe.TryAdd( entity, animator.ValueRW.GetKeyframe( to, animations, blendParameters ) );
            }
        }
    }
    [BurstCompile]
    public partial struct UpdateBoneAnimatorsJob : IJobEntity
    {
        [ReadOnly]
        public NativeParallelHashMap < Entity, AnimationKeyframe > BonesToKeyframe;
        
        [BurstCompile]
        public void Execute(Entity entity, RefRW <BoneAnimatorComponent> animator)
        {
            if ( BonesToKeyframe.TryGetValue( entity, out AnimationKeyframe keyframe ) )
            {
                animator.ValueRW.IsActive = true;
                animator.ValueRW.CurrentAnimationKeyframe = BonesToKeyframe[entity];
            }
        }
    }
}

}
