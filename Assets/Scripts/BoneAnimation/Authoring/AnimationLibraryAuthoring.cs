using System.Collections.Generic;
using Animations.Utilities;
using BoneAnimation.DataComponents;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using AnimationKeyframe = Animations.Utilities.AnimationKeyframe;

namespace BoneAnimation
{

public class AnimationLibraryAuthoring : MonoBehaviour
{
    public List < AnimationConverter > AnimationsToConvert;
}

public class AnimationLibraryAuthoringBaker : Baker < AnimationLibraryAuthoring >
{
    public override void Bake( AnimationLibraryAuthoring authoring )
    {
        AnimationLibraryEntity component = new AnimationLibraryEntity();

        using ( BlobBuilder blobBuilder1 = new BlobBuilder( Allocator.Temp ) )
        {
            ref AnimationLibrary ani = ref blobBuilder1.ConstructRoot < AnimationLibrary >();

            BlobBuilderArray < BoneAnimationSet > anis = blobBuilder1.Allocate(
                ref ani.Animations,
                authoring.AnimationsToConvert.Count );

            int c = 0;
            foreach ( var variable in authoring.AnimationsToConvert )
            {
                int totalFrameCount = 0;
                List < int2 > boneIdToKeyframe = new List < int2 >();
                List < int > animationIdToKeyframeStartBoneIdStart = new List < int >();
                List < float > fps = new List < float >();
                List < float > length = new List < float >();
                List < FixedString128Bytes > animationNames = new List < FixedString128Bytes >();

                List < BoneAnimation.DataComponents.AnimationKeyframe > keyframes =
                    new List < BoneAnimation.DataComponents.AnimationKeyframe >();

                foreach ( var t in  variable.Animations )
                {
                    int keyframeCount = 0;
                    animationIdToKeyframeStartBoneIdStart.Add( boneIdToKeyframe.Count );

                    foreach ( BoneAnimationKeyframes boneAnimationKeyframes in t.BoneKeyframes )
                    {
                        int2 keyframeStartEnd;
                        keyframeStartEnd.x = keyframeCount + totalFrameCount;
                        keyframeCount += boneAnimationKeyframes.Keyframes.Count;
                        keyframeStartEnd.y = keyframeCount + totalFrameCount;
                        boneIdToKeyframe.Add( keyframeStartEnd );
                    }

                    totalFrameCount += keyframeCount;
                }

                foreach ( var t in variable.Animations )
                {
                    fps.Add( t.Fps );
                    length.Add( t.Length );
                    animationNames.Add( t.AnimationName );

                    for ( int j = 0; j < t.BoneKeyframes.Count; j++ )
                    {
                        foreach ( var boneAnimationKeyframe in t.BoneKeyframes[j].Keyframes )
                        {
                            BoneAnimation.DataComponents.AnimationKeyframe animationKeyframe;
                            animationKeyframe.Position = boneAnimationKeyframe.Position;
                            animationKeyframe.Rotation = boneAnimationKeyframe.Rotation;
                            animationKeyframe.Scale = boneAnimationKeyframe.Scale;
                            animationKeyframe.TimeStamp = boneAnimationKeyframe.TimeStamp;
                            keyframes.Add( animationKeyframe );
                        }
                    }
                }
                

                using ( BlobBuilder blobBuilder = new BlobBuilder( Allocator.Temp ) )
                {
                    ref BoneAnimationSet animator = ref blobBuilder.ConstructRoot < BoneAnimationSet >();

                    BlobBuilderArray < DataComponents.AnimationKeyframe > blend1dArray = blobBuilder.Allocate(
                        ref animator.Keyframes,
                        keyframes.Count );

                    BlobBuilderArray < float > blend2dArray = blobBuilder.Allocate(
                        ref animator.AnimationIdToFps,
                        fps.Count );

                    BlobBuilderArray < float > blend1dStartArray = blobBuilder.Allocate(
                        ref animator.AnimationIdToLength,
                        length.Count );

                    BlobBuilderArray < FixedString128Bytes > blend2dStartArray = blobBuilder.Allocate(
                        ref animator.AnimationIdToAnimationName,
                        animationNames.Count );

                    BlobBuilderArray < int > blend1dParaArray = blobBuilder.Allocate(
                        ref animator.AnimationIdToBoneIdStart,
                        animationIdToKeyframeStartBoneIdStart.Count );

                    BlobBuilderArray < int2 > blend2dParaXArray = blobBuilder.Allocate(
                        ref animator.BoneIdToKeyframeStartEnd,
                        boneIdToKeyframe.Count );

                    int tii = 0;

                    foreach ( var blendTree1DEntry in keyframes )
                    {
                        blend1dArray[tii] = blendTree1DEntry;
                        tii++;
                    }

                    tii = 0;

                    foreach ( float blendTree1DEntry in fps )
                    {
                        blend2dArray[tii] = blendTree1DEntry;
                        tii++;
                    }

                    tii = 0;

                    foreach ( float blendTree1DEntry in length )
                    {
                        blend1dStartArray[tii] = blendTree1DEntry;
                        tii++;
                    }

                    tii = 0;

                    foreach ( var blendTree1DEntry in animationNames )
                    {
                        blend2dStartArray[tii] = blendTree1DEntry;
                        tii++;
                    }

                    tii = 0;

                    foreach ( var blendTree1DEntry in animationIdToKeyframeStartBoneIdStart )
                    {
                        blend1dParaArray[tii] = blendTree1DEntry;
                        tii++;
                    }

                    tii = 0;

                    foreach ( var blendTree1DEntry in boneIdToKeyframe )
                    {
                        blend2dParaXArray[tii] = blendTree1DEntry;
                        tii++;
                    }

                    anis[c] = animator;
                }

                c++;
            }

            component.AnimationLibrary =
                blobBuilder1.CreateBlobAssetReference < AnimationLibrary >( Allocator.Persistent );
        }

        var entity = GetEntity( TransformUsageFlags.Dynamic );
        AddComponent( entity, component );
    }
}

}
