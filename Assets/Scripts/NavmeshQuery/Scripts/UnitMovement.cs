using System.Runtime.CompilerServices;
using BoneAnimation.DataComponents;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

public static class MathematicsExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static quaternion RotateTowards(
        quaternion from,
        quaternion to,
        float maxDegreesDelta)
    {
        float num = Angle(from, to);
        return num < float.Epsilon ? to : math.slerp(from, to, math.min(1f, maxDegreesDelta / num));
    }
     
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Angle(this quaternion q1, quaternion q2)
    {
        var dot    = math.dot(q1, q2);
        return !(dot > 0.999998986721039) ? (float) (math.acos(math.min(math.abs(dot), 1f)) * 2.0) : 0.0f;
    }
}

[BurstCompile]
[UpdateAfter(typeof(NavAgent_System))]
public partial struct UnitMovement : ISystem
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
        float deltaTime = SystemAPI.Time.DeltaTime;
        var unitMovement = new UnitMovmentJob { DeltaTime = deltaTime }.ScheduleParallel(state.Dependency);

        state.Dependency = JobHandle.CombineDependencies( unitMovement, state.Dependency );
    }
    [BurstCompile]
    public partial struct UnitMovmentJob: IJobEntity
    {
        public float DeltaTime;
        [BurstCompile]
        public void Execute(ref LocalTransform trans, ref UnitComponentData uc, ref AnimatorComponentEntity animatorComponent, RefRW <NavAgent_Component> nc, in DynamicBuffer<NavAgent_Buffer> nb, DynamicBuffer<BlendParameter> bps)
        {
            if (nc.ValueRO.routed && nb.Length>0 && !uc.Reached)
            {
                if ( math.distance(trans.Position, nb[uc.CurrentBufferIndex].wayPoints) <= uc.MinDistance)
                {
                    if ( uc.CurrentBufferIndex < nb.Length - 1 )
                    {
                        uc.CurrentBufferIndex++;
                    }
                    else
                    {
                        float3 newTarget = trans.Position + uc.Rand.NextFloat3( new float3( -100 ), new float3( 100 ) );
                        newTarget.y = 0.0f;
                        nc.ValueRW.fromLocation = trans.Position;
                        nc.ValueRW.toLocation = newTarget;
                        uc.CurrentBufferIndex = 0;
                        // uc.reached = true;
                        nc.ValueRW.routed = false;
                    }
                }
                else
                {
                    uc.CurrentWaypoint = math.normalize((nb[uc.CurrentBufferIndex].wayPoints- trans.Position) );
                    float3 wantedChange = (math.normalize(uc.CurrentWaypoint)) * uc.Speed * DeltaTime;

                    trans.Position += wantedChange;
                
                    var newRot = MathematicsExtension.RotateTowards(
                        trans.Rotation,
                        quaternion.LookRotation( uc.CurrentWaypoint, new float3( 0.0f, 1.0f, 0.0f ) ),
                        DeltaTime * 190.0f );
                    trans.Rotation = math.slerp( trans.Rotation, newRot, DeltaTime * 10.25f );

                    float speed = math.length( uc.CurrentWaypoint );

                    for ( int i = 0; i < bps.Length; i++ )
                    {
                        if ( bps[i].ParameterName == "Forward" )
                        {
                            BlendParameter blendParameter;
                            blendParameter.Parameter = speed;
                            blendParameter.ParameterName = "Forward";
                            bps[i] = blendParameter;
                            break;
                        }
                    }
                }
            }
        }
    }
}
