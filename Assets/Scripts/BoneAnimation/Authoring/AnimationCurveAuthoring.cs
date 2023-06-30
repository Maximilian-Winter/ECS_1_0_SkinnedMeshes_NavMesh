using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BoneAnimation
{

public class AnimationCurveAuthoring : MonoBehaviour
{
    public List < AnimationCurveExtention.UnityAnimationCurve > CurvesToConvert;
}


public class AnimationCurveAuthoringBaker : Baker <AnimationCurveAuthoring>
{
    public override void Bake( AnimationCurveAuthoring authoring )
    {
        EntitiesAnimationCurveLibrary curveLibrary =
            AnimationCurveExtention.LoadUnityAnimationCurveIntoEntitiesAnimationCurve( authoring.CurvesToConvert );

        var entity = GetEntity( TransformUsageFlags.Dynamic );
        AddComponent( entity, curveLibrary );
    }
}
}
