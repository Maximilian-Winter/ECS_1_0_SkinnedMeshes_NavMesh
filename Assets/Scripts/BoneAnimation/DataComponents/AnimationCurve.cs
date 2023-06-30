using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BoneAnimation
{
public static class AnimationCurveExtention
{
    [Serializable]
    public struct UnityAnimationCurve
    {
        public AnimationCurve AnimationCurve;
        public int NumberOfSamples;
    }
    public static float[] GenerateCurveArray(this UnityEngine.AnimationCurve self, int numberOfSamples)
    {
        float[] returnArray = new float[numberOfSamples];
        for (int j = 0; j < numberOfSamples; j++)
        {
            returnArray[j] = self.Evaluate((float)j / numberOfSamples);   
            //Debug.Log( returnArray[j] );
        }
        
        return returnArray;
    }
    
    public static EntitiesAnimationCurveLibrary LoadUnityAnimationCurveIntoEntitiesAnimationCurve(List<UnityAnimationCurve> animationCurves)
    {
        EntitiesAnimationCurveLibrary component;
        int totalSamples = 0;;
        foreach (UnityAnimationCurve curve in animationCurves )
        {
            totalSamples += curve.NumberOfSamples;
        }
        
        
        using ( BlobBuilder blobBuilder = new BlobBuilder( Allocator.Temp ) )
        {
            ref EntitiesAnimationCurves animationCurveEcs = ref blobBuilder.ConstructRoot < EntitiesAnimationCurves >();
            BlobBuilderArray<float> curveSamples = blobBuilder.Allocate( ref animationCurveEcs.SampledPoints, totalSamples  );
            BlobBuilderArray<int2> samplePointStartNumberOfSamples = blobBuilder.Allocate( ref animationCurveEcs.SamplePointsStartNumberOfSamples, animationCurves.Count  );
            int samplePointIndex = 0;
            int animationIndex = 0;
            foreach (UnityAnimationCurve curve in animationCurves )
            {
                float[] samplePoints = GenerateCurveArray( curve.AnimationCurve, curve.NumberOfSamples );
                samplePointStartNumberOfSamples[animationIndex] = new int2( samplePointIndex,  curve.NumberOfSamples );
                for (int i = 0; i <  curve.NumberOfSamples; i++)
                {
                    // Copy data.
                    curveSamples[samplePointIndex] = samplePoints[i];
                    samplePointIndex++;
                }

                animationIndex++;
            }
            
            

            component.EntitiesCurve = blobBuilder.CreateBlobAssetReference < EntitiesAnimationCurves >( Allocator.Persistent );
        }
        return component;
    }
}
[Serializable]
public struct EntitiesAnimationCurves
{
    public BlobArray < float > SampledPoints;
    public  BlobArray < int2 > SamplePointsStartNumberOfSamples;
    
    public float GetValueAtTime(int index, float time)
    {
        int numberOfSamples = SamplePointsStartNumberOfSamples[index].y;
        var approxSampleIndex = (numberOfSamples - 1) * time;
        var sampleIndexBelow = (int)math.floor(approxSampleIndex);

        if (sampleIndexBelow >= numberOfSamples - 1 )
        {
            return SampledPoints[numberOfSamples - 1+ SamplePointsStartNumberOfSamples[index].x];
        }
        var indexRemainder = approxSampleIndex - sampleIndexBelow;
        return math.lerp(SampledPoints[sampleIndexBelow+ SamplePointsStartNumberOfSamples[index].x], SampledPoints[sampleIndexBelow + 1+ SamplePointsStartNumberOfSamples[index].x], indexRemainder);
    }
    

}

[Serializable]
public struct EntitiesAnimationCurveLibrary : IComponentData
{
    public BlobAssetReference<EntitiesAnimationCurves> EntitiesCurve;
}


public struct SampledAnimationCurve : System.IDisposable
{
    NativeArray<float> sampledFloat;
    /// <param name="samples">Must be 2 or higher</param>
    public SampledAnimationCurve(UnityEngine.AnimationCurve ac, int samples)
    {
        sampledFloat = new NativeArray<float>(samples, Allocator.Persistent);
        float timeFrom = ac.keys[0].time;
        float timeTo = ac.keys[ac.keys.Length - 1].time;
        float timeStep = (timeTo - timeFrom) / (samples - 1);
 
        for (int i = 0; i < samples; i++)
        {
            sampledFloat[i] = ac.Evaluate(timeFrom + (i * timeStep));
        }
    }
 
    public void Dispose()
    {
        sampledFloat.Dispose();
    }
 
    /// <param name="time">Must be from 0 to 1</param>
    public float EvaluateLerp(float time)
    {
        int len = sampledFloat.Length - 1;
        float clamp01 = time < 0 ? 0 : (time > 1 ? 1 : time);
        float floatIndex = (clamp01 * len);
        int floorIndex = (int)math.floor(floatIndex);
        if (floorIndex == len)
        {
            return sampledFloat[len];
        }
 
        float lowerValue = sampledFloat[floorIndex];
        float higherValue = sampledFloat[floorIndex + 1];
        return math.lerp(lowerValue, higherValue, math.frac(floatIndex));
    }
}
}
