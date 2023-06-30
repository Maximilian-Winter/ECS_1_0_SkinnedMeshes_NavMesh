using System.Collections.Generic;
using Animations.Utilities;
using BoneAnimation.DataComponents;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using AnimationKeyframe = BoneAnimation.DataComponents.AnimationKeyframe;
using BlendTree1D = Animations.Utilities.BlendTree1D;
using BlendTree1DEntry =  BoneAnimation.DataComponents.BlendTree1DEntry;
using BlendTree2DEntry =  BoneAnimation.DataComponents.BlendTree2DEntry;
namespace Animations.Authoring
{

public class BoneAnimationAuthoring : MonoBehaviour
{
    public AnimationConverter AnimationConverter;

    public int DefaultAnimationIndex;
    
    public bool InitWithBlendTree1D;
    public bool InitWithBlendTree2D;
}

public class BoneAnimationBaker : Baker < BoneAnimationAuthoring >
{
    public override void Bake( BoneAnimationAuthoring authoring )
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        int totalFrameCount = 0;
        List < int2 > boneIdToKeyframe = new List < int2 >();
        List < int > animationIdToKeyframeStartBoneIdStart = new List < int >();
        List < float > fps = new List < float >();
        List < float > length = new List < float >();
        List < FixedString128Bytes > animationNames = new List < FixedString128Bytes >();

        List < BoneAnimation.DataComponents.AnimationKeyframe > keyframes =
            new List < BoneAnimation.DataComponents.AnimationKeyframe >();
        
        foreach ( Utilities.BoneAnimation t in authoring.AnimationConverter.Animations )
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

        foreach ( Utilities.BoneAnimation t in authoring.AnimationConverter.Animations )
        {
            fps.Add( t.Fps );
            length.Add( t.Length );
            animationNames.Add( t.AnimationName );

            for ( int j = 0; j < t.BoneKeyframes.Count; j++ )
            {
                foreach ( Utilities.AnimationKeyframe boneAnimationKeyframe in t.BoneKeyframes[j].Keyframes )
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

        BoneAnimation.DataComponents.BoneAnimationSetEntity boneAnimationSet =
            new BoneAnimation.DataComponents.BoneAnimationSetEntity();
        using ( BlobBuilder blobBuilder = new BlobBuilder( Allocator.Temp ) )
        {
            ref BoneAnimationSet animator = ref blobBuilder.ConstructRoot < BoneAnimationSet >();
            BlobBuilderArray<AnimationKeyframe> blend1dArray = blobBuilder.Allocate( ref animator.Keyframes, keyframes.Count  );
            BlobBuilderArray<float> blend2dArray = blobBuilder.Allocate( ref animator.AnimationIdToFps, fps.Count  );
            BlobBuilderArray<float> blend1dStartArray = blobBuilder.Allocate( ref animator.AnimationIdToLength, length.Count  );
            BlobBuilderArray<FixedString128Bytes> blend2dStartArray = blobBuilder.Allocate( ref animator.AnimationIdToAnimationName, animationNames.Count  );
            BlobBuilderArray<int> blend1dParaArray = blobBuilder.Allocate( ref animator.AnimationIdToBoneIdStart, animationIdToKeyframeStartBoneIdStart.Count  );
            BlobBuilderArray<int2> blend2dParaXArray = blobBuilder.Allocate( ref animator.BoneIdToKeyframeStartEnd, boneIdToKeyframe.Count  );

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

            boneAnimationSet.AnimationSet = blobBuilder.CreateBlobAssetReference < BoneAnimationSet >( Allocator.Persistent );
        }

        AddComponent < BoneAnimationSetEntity >( entity, boneAnimationSet );

        var skinnedMeshRenderer = authoring.gameObject.GetComponentInChildren < SkinnedMeshRenderer >();

        // Setup reference to the other bones
        var boneEntityArray = AddBuffer < BoneAnimatorEntity >(entity);
        boneEntityArray.ResizeUninitialized( skinnedMeshRenderer.bones.Length );

        for ( int boneIndex = 0; boneIndex < skinnedMeshRenderer.bones.Length; ++boneIndex )
        {
            var bone = skinnedMeshRenderer.bones[boneIndex];
            var boneEntity = GetEntity(  bone, TransformUsageFlags.Dynamic );

            //buffer.Add( new BoneAnimatorEntity{BoneEntity = boneEntity});
            boneEntityArray[boneIndex] = new BoneAnimatorEntity { BoneEntity = boneEntity };
        }

        List < BlendTree1DEntry > blendTree1DEntries = new List < BlendTree1DEntry >();
        List < int2 > blendTree1DIdToStartEnd = new List < int2 >();
        List < FixedString128Bytes > blendParameterName = new List < FixedString128Bytes >();

        List < BlendTree2DEntry > blendTree2DEntries = new List < BlendTree2DEntry >();
        List < int2 > blendTree2DIdToStartEnd = new List < int2 >();
        List < FixedString128Bytes > firstBlendParameterName = new List < FixedString128Bytes >();
        List < FixedString128Bytes > secondBlendParameterName = new List < FixedString128Bytes >();

        foreach ( BoneAnimatorState animationConverterFoundState in authoring.AnimationConverter.FoundStates )
        {
            foreach ( BlendTree1D foundBlendTree1D in animationConverterFoundState.FoundBlendTree1Ds )
            {
                int2 blendTreeStartEnd = new int2( blendTree1DEntries.Count, 0 );
                foreach ( Animations.Utilities.BlendTree1DEntry tree1DEntry in foundBlendTree1D.BlendTree1DEntries )
                {
                    BlendTree1DEntry blendTree1DEntry = new BlendTree1DEntry();
                    blendTree1DEntry.Threshold = tree1DEntry.Threshold;
                    blendTree1DEntry.AnimationId = tree1DEntry.AnimationId;
                    blendTree1DEntry.AnimationSpeed = tree1DEntry.AnimationSpeed;
                    blendTree1DEntries.Add( blendTree1DEntry );
                }

                blendTreeStartEnd.y = blendTree1DEntries.Count;
                blendParameterName.Add( foundBlendTree1D.BlendTreeParameterName );
                blendTree1DIdToStartEnd.Add( blendTreeStartEnd );
            }

     
        
            foreach ( BlendTree2D foundBlendTree2D in animationConverterFoundState.FoundBlendTree2Ds )
            {
                int2 blendTreeStartEnd = new int2( blendTree1DEntries.Count, 0 );
                foreach ( Animations.Utilities.BlendTree2DEntry tree1DEntry in foundBlendTree2D.BlendTree2DEntries )
                {
                    BlendTree2DEntry blendTree2DEntry = new BlendTree2DEntry();
                    blendTree2DEntry.MotionPosition = tree1DEntry.MotionPosition;
                    blendTree2DEntry.AnimationId = tree1DEntry.AnimationId;
                    blendTree2DEntry.AnimationSpeed = tree1DEntry.AnimationSpeed;
                    blendTree2DEntries.Add( blendTree2DEntry );
                }

                blendTreeStartEnd.y = blendTree2DEntries.Count;
                firstBlendParameterName.Add( foundBlendTree2D.FirstBlendTreeParameterName );
                secondBlendParameterName.Add( foundBlendTree2D.SecondBlendTreeParameterName );
                blendTree2DIdToStartEnd.Add( blendTreeStartEnd );
            }
        }
        AnimatorComponentEntity componentEntity = new AnimatorComponentEntity();

        if ( authoring.InitWithBlendTree1D )
        {
            componentEntity.CurrentAnimationType = AnimationType.BlendTree1D;
        }
        
        if ( authoring.InitWithBlendTree2D )
        {
            componentEntity.CurrentAnimationType = AnimationType.BlendTree2D;
        }
        componentEntity.CurrentAnimationIndex = authoring.DefaultAnimationIndex;
        var blendParameters = AddBuffer < BlendParameter >( entity );
        using ( BlobBuilder blobBuilder = new BlobBuilder( Allocator.Temp ) )
        {
            ref AnimatorComponent animator = ref blobBuilder.ConstructRoot < AnimatorComponent >();
            BlobBuilderArray<BlendTree1DEntry> blend1dArray = blobBuilder.Allocate( ref animator.BlendTree1DEntries, blendTree1DEntries.Count  );
            BlobBuilderArray<BlendTree2DEntry> blend2dArray = blobBuilder.Allocate( ref animator.BlendTree2DEntries, blendTree2DEntries.Count  );
            BlobBuilderArray<int2> blend1dStartArray = blobBuilder.Allocate( ref animator.BlendTree1DIdToBlendTree1DStartEnd, blendTree1DIdToStartEnd.Count  );
            BlobBuilderArray<int2> blend2dStartArray = blobBuilder.Allocate( ref animator.BlendTree2DIdToBlendTree2DStartEnd, blendTree2DIdToStartEnd.Count  );
            BlobBuilderArray<FixedString128Bytes> blend1dParaArray = blobBuilder.Allocate( ref animator.BlendTree1DIdToBlendParameterName, blendParameterName.Count  );
            BlobBuilderArray<FixedString128Bytes> blend2dParaXArray = blobBuilder.Allocate( ref animator.BlendTree2DIdToFirstBlendParameterName, firstBlendParameterName.Count  );
            BlobBuilderArray<FixedString128Bytes> blend2dParaYArray = blobBuilder.Allocate( ref animator.BlendTree2DIdToSecondBlendParameterName, secondBlendParameterName.Count  );

            int tii = 0;
            foreach ( BlendTree1DEntry blendTree1DEntry in blendTree1DEntries )
            {
                blend1dArray[tii] = blendTree1DEntry;
                tii++;
            }
            
            tii = 0;
            foreach ( BlendTree2DEntry blendTree1DEntry in blendTree2DEntries )
            {
                blend2dArray[tii] = blendTree1DEntry;
                tii++;
            }
            
            
            tii = 0;
            foreach ( int2 blendTree1DEntry in blendTree1DIdToStartEnd )
            {
                blend1dStartArray[tii] = blendTree1DEntry;
                tii++;
            }
            
            tii = 0;
            foreach ( int2 blendTree1DEntry in blendTree2DIdToStartEnd )
            {
                blend2dStartArray[tii] = blendTree1DEntry;
                tii++;
            }
            
            tii = 0;
            foreach ( FixedString128Bytes blendTree1DEntry in blendParameterName )
            {
                blend1dParaArray[tii] = blendTree1DEntry;
                tii++;
            }
            
            tii = 0;
            foreach ( FixedString128Bytes blendTree1DEntry in firstBlendParameterName )
            {
                blend2dParaXArray[tii] = blendTree1DEntry;
                tii++;
            }
            
            tii = 0;
            foreach ( FixedString128Bytes blendTree1DEntry in secondBlendParameterName )
            {
                blend2dParaYArray[tii] = blendTree1DEntry;
                tii++;
            }
            
            foreach ( FixedString128Bytes fixedString128Bytes in blendParameterName )
            {
                blendParameters.Add( new BlendParameter( fixedString128Bytes, 0.0f ));
            }
        
            foreach ( FixedString128Bytes fixedString128Bytes in firstBlendParameterName )
            {
                blendParameters.Add( new BlendParameter( fixedString128Bytes, 0.0f ));

            }
        
            foreach ( FixedString128Bytes fixedString128Bytes in secondBlendParameterName )
            {
                blendParameters.Add(new BlendParameter( fixedString128Bytes, 0.0f ));
            }
            
            
            componentEntity.Animator = blobBuilder.CreateBlobAssetReference < AnimatorComponent >( Allocator.Persistent );
        }

        componentEntity.Enabled = true;
        AddComponent < AnimatorComponentEntity >( entity, componentEntity );
      
    }
}

}
