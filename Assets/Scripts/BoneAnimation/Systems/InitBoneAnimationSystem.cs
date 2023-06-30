using BoneAnimation.DataComponents;
using Unity.Collections;
using Unity.Entities;

namespace Animations.Systems
{

[RequireMatchingQueriesForUpdate]
[UpdateBefore( typeof( PrepareBoneAnimationSystem ) )]
public partial struct InitBoneAnimationSystem : ISystem
{
    public void OnCreate( ref SystemState state )
    {
    }

    public void OnDestroy( ref SystemState state )
    {
    }

    public void OnUpdate( ref SystemState state )
    {
        EntityCommandBuffer ecb =new EntityCommandBuffer(Allocator.Temp);
        foreach ( var (initTag, rootEntity, boneEntities, entity) in SystemAPI.Query<InitBonesTag, RootEntity,  DynamicBuffer<BoneEntity>>().WithEntityAccess() )
        {
            // ecb.AddComponent<LocalTransform>(rootEntity.Value);
            BoneAnimatorComponent rootAnimatorComponent = new BoneAnimatorComponent { IsActive = false};
            ecb.AddComponent < RootTag >( rootEntity.Value );
            ecb.AddComponent<BoneAnimatorComponent>( rootEntity.Value, rootAnimatorComponent );
            for ( int i = 0; i < boneEntities.Length; i++ )
            {
                BoneAnimatorComponent boneAnimatorComponent = new BoneAnimatorComponent { IsActive = false};
                ecb.AddComponent<BoneTag>( boneEntities[i].Value );
                ecb.AddComponent<BoneAnimatorComponent>( boneEntities[i].Value, boneAnimatorComponent );
            }
            
            ecb.RemoveComponent<InitBonesTag>( entity );
        }
        ecb.Playback( state.EntityManager );
    }
}

}
