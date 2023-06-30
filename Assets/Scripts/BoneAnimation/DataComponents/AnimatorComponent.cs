using System;
using Animations.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BoneAnimation.DataComponents
{

[BurstCompile]
public struct AnimationKeyframe
{
    public float3 Position;
    public quaternion Rotation;
    public float3 Scale;
    public float TimeStamp;

    [BurstCompile]
    public static void BlendTwoKeyframes(
        ref AnimationKeyframe keyframeOne,
        ref AnimationKeyframe keyframeTwo,
        ref AnimationKeyframe resultKeyframe,
        float blendValue )
    {
        if ( blendValue <= 0.0f )
        {
            resultKeyframe = keyframeOne;

            return;
        }

        if ( blendValue >= 1.0f )
        {
            resultKeyframe = keyframeTwo;

            return;
        }

        resultKeyframe = new AnimationKeyframe
        {
            Position = math.lerp( keyframeOne.Position, keyframeTwo.Position, blendValue ),
            Rotation = math.slerp( keyframeOne.Rotation, keyframeTwo.Rotation, blendValue ),
            Scale = math.lerp( keyframeOne.Scale, keyframeTwo.Scale, blendValue )
        };
    }

    public static void WeightKeyframe(
        ref AnimationKeyframe keyframeOne,
        float weigth )
    {
        keyframeOne = new AnimationKeyframe
        {
            Position = keyframeOne.Position * weigth, Rotation = keyframeOne.Rotation, Scale = keyframeOne.Scale
        };
    }
}
[BurstCompile]
public struct BoneAnimationSetEntity : IComponentData
{
    public BlobAssetReference < BoneAnimationSet > AnimationSet;
}
[BurstCompile]
public struct BoneAnimationSet
{
    public BlobArray < FixedString128Bytes > AnimationIdToAnimationName;
    public BlobArray < AnimationKeyframe > Keyframes;
    public BlobArray < int > AnimationIdToBoneIdStart;
    public BlobArray < int2 > BoneIdToKeyframeStartEnd;
    public BlobArray < float > AnimationIdToFps;
    public BlobArray < float > AnimationIdToLength;
    public FixedString128Bytes AnimationSetName;
}
[BurstCompile]
public struct AnimationLibraryEntity : IComponentData
{
    public BlobAssetReference < AnimationLibrary > AnimationLibrary;
}
[BurstCompile]
public struct AnimationLibrary
{
    public BlobArray < BoneAnimationSet > Animations;
}

[BurstCompile]
public struct BlendParameter: IBufferElementData
{
    public FixedString128Bytes ParameterName;
    public float Parameter;

    public BlendParameter( FixedString128Bytes parameterName, float parameter )
    {
        ParameterName = parameterName;
        Parameter = parameter;
    }
}
[BurstCompile]
public struct AnimatorComponentEntity : IComponentData
{
    public BlobAssetReference < AnimatorComponent > Animator;
    public bool Enabled;
    public float AnimationTime;
    public int CurrentAnimationIndex;
    public AnimationType CurrentAnimationType;

    public bool BlendingEnabled;
    public float BlendingAnimationTime;
    public int BlendingAnimationIndex;
    public AnimationType BlendingAnimationType;

    public float BlendingAmount;
    public float BlendingDuration;
    public float BlendingCurrentTime;
    public int BlendingCurveIndex;
    public int CurrentBlendTree1DEntry;
    
    [BurstCompile]
    public readonly AnimationKeyframe GetKeyframe( int animationId, int boneId, float animationTime,
        BoneAnimation.DataComponents.BoneAnimationSetEntity animations  )
    {
        int boneIdStart = animations.AnimationSet.Value.AnimationIdToBoneIdStart[animationId];
     
        int localKeyframeStart = animations.AnimationSet.Value.BoneIdToKeyframeStartEnd[boneIdStart + boneId].x;
        int localKeyframeEnd = animations.AnimationSet.Value.BoneIdToKeyframeStartEnd[boneIdStart + boneId].y;
     
        for ( int index = localKeyframeStart; index <= localKeyframeEnd - 1; index++ )
        {
            if ( animationTime <= animations.AnimationSet.Value.Keyframes[index].TimeStamp )
            {
                AnimationKeyframe animationKeyframeOne = animations.AnimationSet.Value.Keyframes[index];
                AnimationKeyframe animationKeyframeTwo = animations.AnimationSet.Value.Keyframes[index + 1];
     
                if ( index < localKeyframeEnd - 1 )
                {
                    AnimationKeyframe result = new AnimationKeyframe();
     
                    AnimationKeyframe.BlendTwoKeyframes(
                        ref animationKeyframeOne,
                        ref animationKeyframeTwo,
                        ref result,
                        1.0f / animations.AnimationSet.Value.Keyframes[index + 1].TimeStamp * animationTime );
     
                    return result;
                }
     
                return animations.AnimationSet.Value.Keyframes[index];
            }
        }
     
        return animations.AnimationSet.Value.Keyframes[localKeyframeEnd - 1];
    }
    
     [BurstCompile]
    public void UpdateAnimationTime(
        float deltaTime,
        EntitiesAnimationCurveLibrary animationCurveLibrary,
        BoneAnimation.DataComponents.BoneAnimationSetEntity animations )
    {
        if ( BlendingEnabled )
        {
            if ( BlendingCurrentTime >= BlendingDuration )
            {
                BlendingEnabled = false;
                CurrentAnimationIndex = BlendingAnimationIndex;
                AnimationTime = BlendingAnimationTime;
            }
            else
            {
                BlendingCurrentTime += deltaTime;

                BlendingAmount = animationCurveLibrary.EntitiesCurve.Value.GetValueAtTime(
                    BlendingCurveIndex,
                    1.0f / BlendingDuration * BlendingCurrentTime );

                if ( BlendingAnimationTime >= animations.AnimationSet.Value.AnimationIdToLength[BlendingAnimationIndex] )
                {
                    BlendingAnimationTime = 0.0f;
                }

                BlendingAnimationTime += deltaTime;
            }
        }

        if ( CurrentAnimationType == AnimationType.BlendTree1D )
        {
            if ( AnimationTime >=
                 animations.AnimationSet.Value.AnimationIdToLength[Animator.Value.BlendTree1DEntries[CurrentBlendTree1DEntry].AnimationId] )
            {
                AnimationTime = 0.0f;
            }

            AnimationTime += deltaTime * Animator.Value.BlendTree1DEntries[CurrentBlendTree1DEntry].AnimationSpeed;
        }
        else if ( CurrentAnimationType == AnimationType.BlendTree2D )
        {
            if ( AnimationTime >=
                 animations.AnimationSet.Value.AnimationIdToLength[Animator.Value.BlendTree2DEntries[CurrentBlendTree1DEntry].AnimationId] )
            {
                AnimationTime = 0.0f;
            }

            AnimationTime += deltaTime * Animator.Value.BlendTree2DEntries[CurrentBlendTree1DEntry].AnimationSpeed;
        }
        else
        {
            if ( AnimationTime >= animations.AnimationSet.Value.AnimationIdToLength[CurrentAnimationIndex] )
            {
                AnimationTime = 0.0f;
            }

            AnimationTime += deltaTime;
        }
    }

    [BurstCompile]
    public AnimationKeyframe GetKeyframe( int boneId, BoneAnimationSetEntity animations, DynamicBuffer <BlendParameter> blendParameters)
    {
        AnimationKeyframe keyframe;

        switch ( CurrentAnimationType )
        {
            case AnimationType.BlendTree1D:
            {
                int blendTreeId = CurrentAnimationIndex;

                int blendTreeStart = Animator.Value.BlendTree1DIdToBlendTree1DStartEnd[blendTreeId].x;
                int blendTreeEnd = Animator.Value.BlendTree1DIdToBlendTree1DStartEnd[blendTreeId].y;
                float blendParameter = 0.0f;
                for ( int i = 0; i < blendParameters.Length; i++)
                {
                    if ( blendParameters[i].ParameterName == Animator.Value.BlendTree1DIdToBlendParameterName[blendTreeId] )
                    {
                        blendParameter = blendParameters[i].Parameter;
                        break;
                    }
                }
               
                float blendValue = 0.0f;
                int thresholdIndex = blendTreeEnd - 1;

                for ( int i = blendTreeStart; i < blendTreeEnd; i++ )
                {
                    if ( Animator.Value.BlendTree1DEntries[i].Threshold >= blendParameter )
                    {
                        thresholdIndex = i;

                        if ( Animator.Value.BlendTree1DEntries[i].Threshold != 0.0f )
                        {
                            if ( blendParameter != 0.0f )
                            {
                                blendValue = 1 / Animator.Value.BlendTree1DEntries[i].Threshold * blendParameter;
                            }
                        }

                        break;
                    }
                }

                if ( thresholdIndex > 0 )
                {
                    AnimationKeyframe keyframeOne = GetKeyframe(
                        Animator.Value.BlendTree1DEntries[thresholdIndex - 1].AnimationId,
                        boneId,
                        AnimationTime, animations  );

                    AnimationKeyframe keyframeTwo = GetKeyframe(
                        Animator.Value.BlendTree1DEntries[thresholdIndex].AnimationId,
                        boneId,
                        AnimationTime, animations  );

                    AnimationKeyframe result = new AnimationKeyframe();
                    AnimationKeyframe.BlendTwoKeyframes( ref keyframeOne, ref keyframeTwo, ref result, blendValue );
                    keyframe = result;
                }
                else
                {
                    keyframe = GetKeyframe(
                        Animator.Value.BlendTree1DEntries[thresholdIndex].AnimationId,
                        boneId,
                        AnimationTime, animations  );
                }

                CurrentBlendTree1DEntry = thresholdIndex;

                break;
            }

            case AnimationType.BlendTree2D:
            {
                int blendTreeId = CurrentAnimationIndex;

                int blendTreeStart = Animator.Value.BlendTree2DIdToBlendTree2DStartEnd[blendTreeId].x;
                int blendTreeEnd = Animator.Value.BlendTree2DIdToBlendTree2DStartEnd[blendTreeId].y;
                
                float firstBlendParameter = 0.0f;
                for ( int i = 0; i < blendParameters.Length; i++)
                {
                    if ( blendParameters[i].ParameterName == Animator.Value.BlendTree2DIdToFirstBlendParameterName[blendTreeId] )
                    {
                        firstBlendParameter = blendParameters[i].Parameter;
                        break;
                    }
                }
                
                float secondBlendParameter = 0.0f;
                for ( int i = 0; i < blendParameters.Length; i++)
                {
                    if ( blendParameters[i].ParameterName ==  Animator.Value.BlendTree2DIdToSecondBlendParameterName[blendTreeId] )
                    {
                        secondBlendParameter = blendParameters[i].Parameter;
                        break;
                    }
                }

                int length = blendTreeEnd - blendTreeStart;

                NativeArray < float > weights = new NativeArray < float >( length, Allocator.Temp );
                NativeArray < float2 > points = new NativeArray < float2 >( length, Allocator.Temp );
                
                for ( int i = 0; i < length; ++i )
                {
                    points[i] = Animator.Value.BlendTree2DEntries[i + blendTreeStart].MotionPosition;
                }
                
                SampleWeightsCartesian( new float2( firstBlendParameter, secondBlendParameter ), points, ref weights);

                int maxIndex = 0;
                float maxWeight = float.MinValue;
                keyframe = new AnimationKeyframe();
                AnimationKeyframe result = new AnimationKeyframe();

                AnimationKeyframe keyframeStart = GetKeyframe(
                    Animator.Value.BlendTree2DEntries[blendTreeStart].AnimationId,
                    boneId,
                    AnimationTime, animations  );

                AnimationKeyframe keyframe2 = GetKeyframe(
                    Animator.Value.BlendTree2DEntries[blendTreeStart + 1].AnimationId,
                    boneId,
                    AnimationTime, animations  );

                AnimationKeyframe.BlendTwoKeyframes(
                    ref keyframeStart,
                    ref keyframe2,
                    ref result,
                    weights[1] );

                if ( maxWeight < weights[1] )
                {
                    maxWeight = weights[1];
                    maxIndex = 1 + blendTreeStart;
                }

                for ( int i = 2; i < weights.Length; i++ )
                {
                    AnimationKeyframe keyframe1 = GetKeyframe(
                        Animator.Value.BlendTree2DEntries[blendTreeStart + i].AnimationId,
                        boneId,
                        AnimationTime, animations  );

                    AnimationKeyframe.BlendTwoKeyframes(
                        ref result,
                        ref keyframe1,
                        ref result,
                        weights[i] );

                    if ( maxWeight < weights[i] )
                    {
                        maxWeight = weights[i];
                        maxIndex = i + blendTreeStart;
                    }
                }

                AnimationKeyframe.BlendTwoKeyframes(
                    ref result,
                    ref keyframeStart,
                    ref result,
                    weights[0] );

                if ( maxWeight < weights[0] )
                {
                    maxWeight = weights[0];
                    maxIndex = blendTreeStart;
                }

                CurrentBlendTree1DEntry = maxIndex;
                keyframe = result;

                break;
            }

            case AnimationType.Animation:
            default:
                keyframe = GetKeyframe( CurrentAnimationIndex, boneId, AnimationTime, animations  );

                break;
        }

        if ( BlendingEnabled )
        {
            AnimationKeyframe blendKeyframe;

            switch ( BlendingAnimationType )
            {
                case AnimationType.BlendTree1D:
                {
                    int blendTreeId = BlendingAnimationIndex;

                    int blendTreeStart = Animator.Value.BlendTree1DIdToBlendTree1DStartEnd[blendTreeId].x;
                    int blendTreeEnd = Animator.Value.BlendTree1DIdToBlendTree1DStartEnd[blendTreeId].y;
                    
                    float blendParameter = 0.0f;
                    for ( int i = 0; i < blendParameters.Length; i++)
                    {
                        if ( blendParameters[i].ParameterName == Animator.Value.BlendTree1DIdToBlendParameterName[blendTreeId] )
                        {
                            blendParameter = blendParameters[i].Parameter;
                            break;
                        }
                    }
                    float blendValue = 0.0f;
                    int thresholdIndex = blendTreeEnd - 1;

                    for ( int i = blendTreeStart; i < blendTreeEnd; i++ )
                    {
                        if ( Animator.Value.BlendTree1DEntries[i].Threshold >= blendParameter )
                        {
                            thresholdIndex = i;

                            if ( Animator.Value.BlendTree1DEntries[i].Threshold != 0.0f )
                            {
                                if ( blendParameter != 0.0f )
                                {
                                    blendValue = Animator.Value.BlendTree1DEntries[i].Threshold / blendParameter;
                                }
                            }

                            break;
                        }
                    }

                    if ( thresholdIndex > 0 )
                    {
                        AnimationKeyframe keyframeOne = GetKeyframe(
                            Animator.Value.BlendTree1DEntries[thresholdIndex - 1].AnimationId,
                            boneId,
                            AnimationTime, animations  );

                        AnimationKeyframe keyframeTwo = GetKeyframe(
                            Animator.Value.BlendTree1DEntries[thresholdIndex].AnimationId,
                            boneId,
                            AnimationTime, animations  );

                        AnimationKeyframe keyframeResult = new AnimationKeyframe();

                        AnimationKeyframe.BlendTwoKeyframes(
                            ref keyframeOne,
                            ref keyframeTwo,
                            ref keyframeResult,
                            blendValue );

                        blendKeyframe = keyframeResult;
                    }
                    else
                    {
                        blendKeyframe = GetKeyframe(
                            Animator.Value.BlendTree1DEntries[thresholdIndex].AnimationId,
                            boneId,
                            AnimationTime, animations );
                    }

                    break;
                }

                case AnimationType.Animation:
                default:
                    blendKeyframe = GetKeyframe( BlendingAnimationIndex, boneId, AnimationTime, animations  );

                    break;
            }

            AnimationKeyframe newResult = new AnimationKeyframe();
            AnimationKeyframe.BlendTwoKeyframes( ref keyframe, ref blendKeyframe, ref newResult, BlendingAmount );

            return newResult;
        }

        return keyframe;
    }

    private static float SignedAngle( float2 a, float2 b )
    {
        return math.atan2( a.x * b.y - a.y * b.x, a.x * b.x + a.y * b.y );
    }

    private void SampleWeightsCartesian( float2 samplePoint, NativeArray<float2> points, ref NativeArray<float> weights )
    {
        float totalWeight = 0.0f;

        for ( int i = 0; i < points.Length; ++i )
        {
            // Calc vec i -> sample
            float2 pointI = points[i];
            float2 vecIs = samplePoint - pointI;

            float weight = 1.0f;

            for ( int j = 0; j < points.Length; ++j )
            {
                if ( j == i )
                    continue;

                // Calc vec i -> j
                float2 pointJ = points[j];
                float2 vecIj = pointJ - pointI;

                // Calc Weight
                float lensqIj = math.dot( vecIj, vecIj );
                float newWeight = math.dot( vecIs, vecIj ) / lensqIj;
                newWeight = 1.0f - newWeight;
                newWeight = math.clamp( newWeight, 0.0f, 1.0f );

                weight = math.min( weight, newWeight );
            }

            weights[i] = weight;
            totalWeight += weight;
        }

        for ( int i = 0; i < points.Length; ++i )
        {
            weights[i] /= totalWeight;
        }
    }

    private void SampleWeightsPolar( float2 samplePoint, NativeArray<float2> points, ref NativeArray<float> weights )
    {
        const float KDirScale = 2.0f;

        float totalWeight = 0.0f;

        float sampleMag = math.length( samplePoint );

        for ( int i = 0; i < points.Length; ++i )
        {
            float2 pointI = points[i];
            float pointMagI = math.length( pointI );

            float weight = 1.0f;

            for ( int j = 0; j < points.Length; ++j )
            {
                if ( j == i )
                    continue;

                float2 pointJ = points[j];
                float pointMagJ = math.length( pointJ );

                float ijAvgMag = ( pointMagJ + pointMagI ) * 0.5f;

                // Calc angle and mag for i -> sample
                float magIs = ( sampleMag - pointMagI ) / ijAvgMag;
                float angleIs = SignedAngle( pointI, samplePoint );

                // Calc angle and mag for i -> j
                float magIj = ( pointMagJ - pointMagI ) / ijAvgMag;
                float angleIj = SignedAngle( pointI, pointJ );

                // Calc vec for i -> sample
                float2 vecIs;
                vecIs.x = magIs;
                vecIs.y = angleIs * KDirScale;

                // Calc vec for i -> j
                float2 vecIj;
                vecIj.x = magIj;
                vecIj.y = angleIj * KDirScale;

                // Calc weight
                float lensqIj = math.dot( vecIj, vecIj );
                float newWeight = math.dot( vecIs, vecIj ) / lensqIj;
                newWeight = 1.0f - newWeight;
                newWeight = math.clamp( newWeight, 0.0f, 1.0f );

                weight = math.min( newWeight, weight );
            }

            weights[i] = weight;

            totalWeight += weight;
        }

        for ( int i = 0; i < points.Length; ++i )
        {
            weights[i] /= totalWeight;
        }
    }
}
[BurstCompile]
public struct AnimatorComponent
{
   

    public BlobArray < BlendTree1DEntry > BlendTree1DEntries;
    public BlobArray< int2 > BlendTree1DIdToBlendTree1DStartEnd;
    public BlobArray< FixedString128Bytes > BlendTree1DIdToBlendParameterName;

    public BlobArray < BlendTree2DEntry > BlendTree2DEntries;
    public BlobArray < int2 > BlendTree2DIdToBlendTree2DStartEnd;
    public BlobArray < FixedString128Bytes > BlendTree2DIdToFirstBlendParameterName;
    public BlobArray < FixedString128Bytes > BlendTree2DIdToSecondBlendParameterName;
}

public enum AnimationType : byte
{
    Animation,
    BlendTree1D,
    BlendTree2D
}

public struct BlendTree1DEntry
{
    public int AnimationId;
    public float Threshold;
    public float AnimationSpeed;
}

public struct BlendTree2DEntry
{
    public int AnimationId;
    public float2 MotionPosition;
    public float AnimationSpeed;
}

public struct BlendTree1D
{
    public FixedString128Bytes AnimationName;
    public NativeArray < BlendTree1DEntry > Animations;
    public int NumberOfAnimations;
    public float BlendParameter;
}

public struct BoneAnimatorEntity : IBufferElementData
{
    public Entity BoneEntity;
}

public struct BoneAnimatorComponent : IComponentData
{
    public bool IsActive;
    public AnimationKeyframe CurrentAnimationKeyframe;
}

//public struct SharedBoneAnimationData : IComponentData //ISharedComponentData
//{
//    public Animations.Buffer.BoneAnimations Animations;
//}

}
