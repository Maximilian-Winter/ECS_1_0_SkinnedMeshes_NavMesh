using BoneAnimation.DataComponents;
using Unity.Entities;
using UnityEngine;

namespace Animations.Systems
{

[RequireMatchingQueriesForUpdate]
[UpdateBefore( typeof( PrepareBoneAnimationSystem ) )]
public partial struct TestBlendTreeParametersSystem : ISystem
{
    public void OnCreate( ref SystemState state )
    {
    }

    public void OnDestroy( ref SystemState state )
    {
    }

    public void OnUpdate( ref SystemState state )
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        foreach ( var animator in SystemAPI.Query<RefRW<AnimatorComponentEntity>, PlayerComponent>() )
        {
          /*  if ( Input.GetKey( KeyCode.Y ) )
            {
                animator.Item1.ValueRW.BlendParameters["Turn"] +=0.15f * deltaTime;
            }
            if ( Input.GetKey( KeyCode.X ) )
            {
                animator.Item1.ValueRW.BlendParameters["Turn"] -= 0.15f * deltaTime;
            }
            
            if ( Input.GetKey( KeyCode.C ) )
            {
                animator.Item1.ValueRW.BlendParameters["Forward"] += 0.15f * deltaTime;
            }
            if ( Input.GetKey( KeyCode.V ) )
            {
                animator.Item1.ValueRW.BlendParameters["Forward"] -= 0.15f * deltaTime;
            }
            
          

            if ( animator.Item1.ValueRW.BlendParameters["Forward"] > 1.0f )
            {
                animator.Item1.ValueRW.BlendParameters["Forward"] = 1.0f;
            }
            
            if ( animator.Item1.ValueRW.BlendParameters["Forward"] < 0.0f )
            {
                animator.Item1.ValueRW.BlendParameters["Forward"] = 0.0f;
            }
            
            if ( animator.Item1.ValueRW.BlendParameters["Turn"] > 1.0f )
            {
                animator.Item1.ValueRW.BlendParameters["Turn"] = 1.0f;
            }
            
            if ( animator.Item1.ValueRW.BlendParameters["Turn"] < -1.0f )
            {
                animator.Item1.ValueRW.BlendParameters["Turn"] = -1.0f;
            }
            //Debug.Log(  animator.Item1.ValueRW.BlendParameters["Speed"] + " " +  animator.Item1.ValueRW.BlendParameters["Turn"] );*/
        }
    }
}

}
