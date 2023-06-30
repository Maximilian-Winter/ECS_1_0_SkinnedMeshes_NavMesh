using BoneAnimation.DataComponents;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Animations.Systems
{

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct BoneAnimationSystem : ISystem
{
    public void OnCreate( ref SystemState state )
    {
    }

    public void OnDestroy( ref SystemState state )
    {
    }
    [BurstCompile]
    public void OnUpdate( ref SystemState state )
    {
        float t = SystemAPI.Time.DeltaTime;
        int index = 0;
        foreach ( (RefRW < LocalTransform >, RefRO < BoneAnimatorComponent >) entity in SystemAPI.Query<RefRW <LocalTransform>, RefRO <BoneAnimatorComponent> >() )
        {
            if ( entity.Item2.ValueRO.IsActive )
            {
                entity.Item1.ValueRW = LocalTransform.FromPositionRotationScale(  math.lerp( entity.Item1.ValueRW.Position, entity.Item2.ValueRO.CurrentAnimationKeyframe.Position, t * 7.5f), math.slerp(entity.Item1.ValueRW.Rotation, entity.Item2.ValueRO.CurrentAnimationKeyframe.Rotation,t* 7.5f), math.lerp(entity.Item1.ValueRW.Scale, entity.Item2.ValueRO.CurrentAnimationKeyframe.Scale.x, t*  7.5f) );
                index++;
            }
        }
    }
    
    
}

}
