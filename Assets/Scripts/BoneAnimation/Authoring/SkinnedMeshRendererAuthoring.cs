using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Adding this component will trigger a conversion system that adds
// components which will calculate the SkinMatrices based on transform data.
public class SkinnedMeshRendererAuthoring : MonoBehaviour
{
    
}

public struct InitBonesTag : IComponentData
{
    
}

public class SkinnedMeshRendererAuthoringBaker : Baker<SkinnedMeshRendererAuthoring>
{
    public override void Bake(SkinnedMeshRendererAuthoring authoring)
    {
        var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>(authoring);
        if (skinnedMeshRenderer == null)
            return;
        var entity = GetEntity( TransformUsageFlags.Dynamic );
        AddComponent<InitBonesTag>(entity);
        // Only execute this if we have a valid skinning setup
        DependsOn(skinnedMeshRenderer.sharedMesh);
        var hasSkinning = skinnedMeshRenderer.bones.Length > 0 && skinnedMeshRenderer.sharedMesh.bindposes.Length > 0;
        if (hasSkinning)
        {
            // Setup reference to the root bone
            var rootTransform = skinnedMeshRenderer.rootBone ? skinnedMeshRenderer.rootBone : skinnedMeshRenderer.transform;
            var rootEntity = GetEntity(rootTransform, TransformUsageFlags.Dynamic);
            AddComponent(entity, new RootEntity {Value = rootEntity});

            // Setup reference to the other bones
            var boneEntityArray = AddBuffer<BoneEntity>(entity);
            boneEntityArray.ResizeUninitialized(skinnedMeshRenderer.bones.Length);

            for (int boneIndex = 0; boneIndex < skinnedMeshRenderer.bones.Length; ++boneIndex)
            {
                var bone = skinnedMeshRenderer.bones[boneIndex];
                var boneEntity = GetEntity(bone, TransformUsageFlags.Dynamic);
                boneEntityArray[boneIndex] = new BoneEntity {Value = boneEntity};
            }

            
            // Store the bindpose for each bone
            var bindPoseArray = AddBuffer<BindPose>(entity);
            bindPoseArray.ResizeUninitialized(skinnedMeshRenderer.bones.Length);

            for (int boneIndex = 0; boneIndex != skinnedMeshRenderer.bones.Length; ++boneIndex)
            {
                var bindPose = skinnedMeshRenderer.sharedMesh.bindposes[boneIndex];
                bindPoseArray[boneIndex] = new BindPose { Value = bindPose };
            }
        }
    }
}
