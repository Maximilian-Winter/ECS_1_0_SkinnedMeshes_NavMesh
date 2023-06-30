using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;
using BoneAnimation.DataComponents;
using UnityEditor;
using UnityEditor.Animations;

namespace Animations.Utilities
{



[Serializable]
public class AnimationKeyframe
{
    public float3 Position;
    public quaternion Rotation;
    public float3 Scale;
    public float TimeStamp;
}

[Serializable]
public class BoneAnimationKeyframes
{
    public int BoneId;
    public string BoneName;
    public List<AnimationKeyframe> Keyframes = new List < AnimationKeyframe >();
}

[Serializable]
public class BoneAnimation
{
    public int AnimationId = -1;
    public string AnimationName = "";
    public List<BoneAnimationKeyframes> BoneKeyframes = new List < BoneAnimationKeyframes >();
   // public BoneToBoneIdDict BoneToBoneId;
    public float Fps;
    public float Length;
    public int FrameCount;
  
}
[Serializable]
public class BoneAnimatorState
{
    public AnimatorState State;
    public List<BlendTree1D> FoundBlendTree1Ds = new List < BlendTree1D >();
    public List<BlendTree2D> FoundBlendTree2Ds = new List < BlendTree2D >();
    public List<AnimationClip> FoundAnimationClips = new List < AnimationClip >();
    public int Layer = 0;
}
[Serializable]
public class BlendTree1DEntry
{
    public int AnimationId;
    public Motion Motion;
    public float Threshold;
    public float AnimationSpeed;
}
[Serializable]
public class BlendTree2DEntry
{
    public int AnimationId;
    public Motion Motion;
    public float2 MotionPosition;
    public float AnimationSpeed;
}

[Serializable]
public class BlendTree1D
{
    public int BlendTree1DId = -1;
    public string BlendTree1DName = "";
    public List<BlendTree1DEntry> BlendTree1DEntries = new List < BlendTree1DEntry >();

    public string BlendTreeParameterName;
}

[Serializable]
public class BlendTree2D
{
    public int BlendTree2DId = -1;
    public string BlendTree2DName = "";
    public List<BlendTree2DEntry> BlendTree2DEntries = new List < BlendTree2DEntry >();
    public string FirstBlendTreeParameterName;
    public string SecondBlendTreeParameterName;
}

[Serializable]
public class AnimationTimeToFrameIndexDict : SerializableDictionary < float, int >
{
    
}

[Serializable]
public class BoneToBoneIdDict : SerializableDictionary < string, int >
{
    
}

[CreateAssetMenu]
public class AnimationConverter :ScriptableObject
{
    public string AnimationSetName;
    public GameObject ModelPrefab;
    public AnimatorController ModelAnimatorController;
    //public List<AnimationClip> FoundAnimationClips = new List < AnimationClip >();
   //public List<BlendTree1D> FoundBlendTree1Ds = new List < BlendTree1D >();
   //public List<BlendTree2D> FoundBlendTree2Ds = new List < BlendTree2D >();
    public List <BoneAnimatorState > FoundStates = new List < BoneAnimatorState >();
    public BoneToBoneIdDict BoneToBoneId = new BoneToBoneIdDict();
    public List < BoneAnimation > Animations = new List < BoneAnimation >();

    public float Fps;
    public float Length;
    public float SampleFPS = 30.0f;
    private BoneAnimatorState m_CurrentState;
    public void Convert()
    {
        Fps = 0.0f;
        //FoundAnimationClips = new List < AnimationClip >();
        //FoundBlendTree1Ds = new List < BlendTree1D >();
        //FoundBlendTree2Ds = new List < BlendTree2D >();
        FoundStates = new List < BoneAnimatorState >();
        Animations = new List < BoneAnimation >();
        BoneToBoneId = new BoneToBoneIdDict();
       
        AnimatorController animatorController = ModelAnimatorController;

        if ( Fps > 1.0f )
        {
            Fps = 0.0f;
        }

        for ( int i = 0; i < animatorController.layers.Length; i++ )
        {
            var rootStateMachine = animatorController.layers[i].stateMachine;
            ChildAnimatorState[] states = rootStateMachine.states;

            for ( int j = 0; j < states.Length; j++ )
            {
                AnimatorState state = states[j].state;
                BoneAnimatorState boneAnimatorState = new BoneAnimatorState();
                m_CurrentState = boneAnimatorState;
                m_CurrentState.State = state;
                m_CurrentState.Layer = i;
                Motion motion = state.motion;
                ProcessMotion( motion );
                FoundStates.Add( m_CurrentState );
            }
        }

        GameObject inst = GameObject.Instantiate(ModelPrefab);
        SkinnedMeshRenderer skinnedMeshRenderer = inst.GetComponent<SkinnedMeshRenderer>();
        if ( skinnedMeshRenderer == null )
        {
            skinnedMeshRenderer = inst.GetComponentInChildren < SkinnedMeshRenderer >();
        }
        Transform[] bones = skinnedMeshRenderer.bones;
        
        for ( int i = 0; i < bones.Length; i++ )
        {
            BoneToBoneId.Add( bones[i].name, i );
        }
        Animator animator = inst.GetComponent < Animator >();
        AnimationMode.StartAnimationMode();

        for ( int g = 0; g < FoundStates.Count; g++ )
        {
            foreach ( BlendTree1D t in FoundStates[g].FoundBlendTree1Ds )
            {
                foreach ( BlendTree1DEntry tBlendTree1DEntry in t.BlendTree1DEntries )
                {
                    float duration = 1.0f;

                    if ( tBlendTree1DEntry.Motion is AnimationClip clip )
                    {
                        duration = clip.length;
                    }
                    else
                    {
                        duration =  tBlendTree1DEntry.Motion.averageDuration;
                    }
                    tBlendTree1DEntry.AnimationId = Animations.Count;
                    BoneAnimation boneAnimation = new BoneAnimation();
                    boneAnimation.AnimationId = Animations.Count;
                    boneAnimation.AnimationName = tBlendTree1DEntry.Motion.name;
                    boneAnimation.Fps = SampleFPS;
                    boneAnimation.Length =  duration;
                    for ( int i = 0; i < bones.Length; i++ )
                    {
                        boneAnimation.BoneKeyframes.Add( new BoneAnimationKeyframes() );
                        boneAnimation.BoneKeyframes[^1].BoneId = i;
                        boneAnimation.BoneKeyframes[^1].BoneName = bones[i].name;
                    }
                    float xOld =animator.GetFloat( t.BlendTreeParameterName );
                    float x = tBlendTree1DEntry.Threshold;
                    animator.SetFloat( t.BlendTreeParameterName, x );
                    int frames = (int)( duration * SampleFPS );
                    boneAnimation.FrameCount = frames;
                    for ( int i = 0; i < frames; i++ )
                    {
                        animator.Play( FoundStates[g].State.name, FoundStates[g].Layer, 1.0f/frames * i  );
                        animator.Update(  1.0f / SampleFPS );

                        UpdateBones( bones, boneAnimation, duration, frames, i );
                    }
                    animator.SetFloat( t.BlendTreeParameterName, xOld );
                    Animations.Add( boneAnimation );
                }
            }
            foreach ( BlendTree2D t in FoundStates[g].FoundBlendTree2Ds )
            {
                foreach ( BlendTree2DEntry tBlendTree2DEntry in t.BlendTree2DEntries )
                {
                    float duration = 1.0f;
                    if ( tBlendTree2DEntry.Motion is AnimationClip clip )
                    {
                        duration = clip.length;
                    }
                    else
                    {
                        duration =  tBlendTree2DEntry.Motion.averageDuration;
                    }
                    tBlendTree2DEntry.AnimationId = Animations.Count;
                    BoneAnimation boneAnimation = new BoneAnimation();
                    boneAnimation.AnimationId = Animations.Count;
                    boneAnimation.AnimationName = tBlendTree2DEntry.Motion.name;
                    boneAnimation.Fps = SampleFPS;
                    boneAnimation.Length =  duration;
                    for ( int i = 0; i < bones.Length; i++ )
                    {
                        boneAnimation.BoneKeyframes.Add( new BoneAnimationKeyframes() );
                        boneAnimation.BoneKeyframes[^1].BoneId = i;
                        boneAnimation.BoneKeyframes[^1].BoneName = bones[i].name;
                    }
                    float xOld =  animator.GetFloat( t.FirstBlendTreeParameterName );
                    float yOld =  animator.GetFloat( t.SecondBlendTreeParameterName );
                    float x = tBlendTree2DEntry.MotionPosition.x;
                    float y = tBlendTree2DEntry.MotionPosition.y;
                    animator.SetFloat( t.FirstBlendTreeParameterName, x );
                    animator.SetFloat( t.SecondBlendTreeParameterName, y );
                    int frames = (int)( duration * SampleFPS );
                    boneAnimation.FrameCount = frames;
                    for ( int i = 0; i < frames; i++ )
                    {
                        animator.Play( FoundStates[g].State.name, FoundStates[g].Layer, 1.0f/frames * i  );
                        animator.Update(  1.0f / SampleFPS );
           
                        UpdateBones( bones, boneAnimation, duration, frames, i );
                    }
                    animator.SetFloat( t.FirstBlendTreeParameterName, xOld );
                    animator.SetFloat( t.SecondBlendTreeParameterName, yOld );
                    Animations.Add( boneAnimation );
                }
            }

            foreach ( AnimationClip foundAnimationClip in FoundStates[g].FoundAnimationClips )
            {
                BoneAnimation boneAnimation = new BoneAnimation();
                boneAnimation.AnimationId = Animations.Count;
                boneAnimation.AnimationName = foundAnimationClip.name;
                boneAnimation.Fps = SampleFPS;
                boneAnimation.Length =  foundAnimationClip.length;
                for ( int i = 0; i < bones.Length; i++ )
                {
                    boneAnimation.BoneKeyframes.Add( new BoneAnimationKeyframes() );
                    boneAnimation.BoneKeyframes[^1].BoneId = i;
                    boneAnimation.BoneKeyframes[^1].BoneName = bones[i].name;
                }
                int frames = (int)( boneAnimation.Length * SampleFPS );
                boneAnimation.FrameCount = frames;
                for ( int i = 0; i < frames; i++ )
                {
                    animator.Play( FoundStates[g].State.name, FoundStates[g].Layer, 1.0f/frames * i  );
                    animator.Update(  1.0f / SampleFPS );
       
                    UpdateBones( bones, boneAnimation, boneAnimation.Length, frames, i );
                }
                Animations.Add( boneAnimation );
            }
        }
        
        AnimationMode.StopAnimationMode();
       

       

/*        int animationIndex = 0;
 AnimationMode.StartAnimationMode();
        foreach(AnimationClip animationClip in FoundAnimationClips)
        {
            BoneAnimation boneAnimation = new BoneAnimation();
            boneAnimation.AnimationId = animationIndex;
            boneAnimation.AnimationName = animationClip.name;
            boneAnimation.BoneToBoneId = BoneToBoneId;
            boneAnimation.Fps = animationClip.frameRate;
            boneAnimation.Length = animationClip.length;

            for ( int i = 0; i < bones.Length; i++ )
            {
                boneAnimation.BoneKeyframes.Add( new BoneAnimationKeyframes() );
                boneAnimation.BoneKeyframes[^1].BoneId = i;
                boneAnimation.BoneKeyframes[^1].BoneName = bones[i].name;
            }
            
            int frames = (int)( animationClip.length * animationClip.frameRate );

            boneAnimation.FrameCount = frames;
            Fps = animationClip.frameRate;
            
            
            for ( int f = 0; f < frames; f++ )
            {
                //AnimationMode.BeginSampling();
                //AnimationMode.SampleAnimationClip( inst, animationClip, (animationClip.length /frames) * f );
                //AnimationMode.EndSampling();
              
                for ( int i = 0; i < bones.Length; i++ )
                {
                    AnimationKeyframe keyframe = new AnimationKeyframe();
                    keyframe.TimeStamp = ( animationClip.length / frames ) * f;
                    keyframe.Position = bones[i].localPosition;
                    keyframe.Rotation = bones[i].localRotation;
                    keyframe.Scale = bones[i].localScale;

                    if ( boneAnimation.BoneKeyframes[i].Keyframes.Count > 0 )
                    {
                        AnimationKeyframe oldKeyframe = boneAnimation.BoneKeyframes[i].Keyframes[^1];
                        
                        bool3 checkOne =
                            keyframe.
                                Position ==
                            oldKeyframe.Position;
                        
                        bool3 checkTwo =
                            keyframe.
                                Scale ==
                            oldKeyframe.Scale;
                        
                        bool4 checkThree = keyframe.
                                           Rotation.value
                                           ==
                                           oldKeyframe.Rotation.value;

                        if ( !(checkOne.x &&
                             checkOne.y &&
                             checkOne.z &&
                             checkTwo.x &&
                             checkTwo.y &&
                             checkTwo.z &&
                             checkThree.x &&
                             checkThree.y &&
                             checkThree.z &&
                             checkThree.w) )
                        {
                            boneAnimation.BoneKeyframes[i].Keyframes.Add( keyframe );
                        }
                    }
                    else
                    {
                        boneAnimation.BoneKeyframes[i].Keyframes.Add( keyframe );
                    }
                   
                }
               
            }
            Animations.Add( boneAnimation );
            animationIndex++;
        }
        
        AnimationMode.StopAnimationMode();*/
    }

    private void UpdateBones( Transform[] bones,BoneAnimation boneAnimation, float duration, int frames, int frameIndex)
    {
        for ( int b = 0; b < bones.Length; b++ )
        {
            AnimationKeyframe keyframe = new AnimationKeyframe();
            keyframe.TimeStamp = ( duration / frames ) * frameIndex;
            keyframe.Position = bones[b].localPosition;
            keyframe.Rotation = bones[b].localRotation;
            keyframe.Scale = bones[b].localScale;

            if ( boneAnimation.BoneKeyframes[b].Keyframes.Count > 0 )
            {
                AnimationKeyframe oldKeyframe = boneAnimation.BoneKeyframes[b].Keyframes[^1];

                if ( !(keyframe.
                       Position.Equals( oldKeyframe.Position )&&
                       keyframe.
                           Scale.Equals( oldKeyframe.Scale ) &&
                       keyframe.
                           Rotation.value.Equals( oldKeyframe.Rotation.value )) )
                {
                    boneAnimation.BoneKeyframes[b].Keyframes.Add( keyframe );
                }
            }
            else
            {
                boneAnimation.BoneKeyframes[b].Keyframes.Add( keyframe );
            }
        }
    }
    private void ProcessMotion( Motion motion )
    {
        if ( motion is AnimationClip clip )
        {
            m_CurrentState.FoundAnimationClips.Add( clip );
        }
                
        if ( motion is BlendTree tree )
        {
            if ( tree.blendType == BlendTreeType.Simple1D )
            {
                BlendTree1D blendTree1D = new BlendTree1D();
                blendTree1D.BlendTree1DId = m_CurrentState.FoundBlendTree1Ds.Count;
                blendTree1D.BlendTree1DName = tree.name;
                blendTree1D.BlendTreeParameterName = tree.blendParameter;
                ChildMotion[] childMotions = tree.children;
                for ( int k = 0; k < childMotions.Length; k++ )
                {
                    BlendTree1DEntry blendTree1DEntry = new BlendTree1DEntry();
                    blendTree1DEntry.Motion = childMotions[k].motion;
                    
                    if ( childMotions[k].motion is AnimationClip )
                    {
                        blendTree1DEntry.AnimationId =  k;
                    }
                    
                    blendTree1DEntry.Threshold = childMotions[k].threshold;
                    blendTree1DEntry.AnimationSpeed = childMotions[k].timeScale;
                    blendTree1D.BlendTree1DEntries.Add( blendTree1DEntry );
                }
                m_CurrentState.FoundBlendTree1Ds.Add( blendTree1D );
            }
            else
            {
                BlendTree2D blendTree2D = new BlendTree2D();
                blendTree2D.BlendTree2DId = m_CurrentState.FoundBlendTree2Ds.Count;
                blendTree2D.BlendTree2DName = tree.name;
                blendTree2D.FirstBlendTreeParameterName = tree.blendParameter;
                blendTree2D.SecondBlendTreeParameterName = tree.blendParameterY;
                ChildMotion[] childMotions = tree.children;
                for ( int k = 0; k < childMotions.Length; k++ )
                {
                    BlendTree2DEntry blendTree2DEntry = new BlendTree2DEntry();
                    blendTree2DEntry.Motion = childMotions[k].motion;

                    if ( childMotions[k].motion is AnimationClip )
                    {
                        blendTree2DEntry.AnimationId = k;
                    }
                    blendTree2DEntry.MotionPosition = childMotions[k].position;
                    blendTree2DEntry.AnimationSpeed = childMotions[k].timeScale;
                    blendTree2D.BlendTree2DEntries.Add( blendTree2DEntry );
                }
                m_CurrentState.FoundBlendTree2Ds.Add( blendTree2D );
            }
        }
    }
}

}
