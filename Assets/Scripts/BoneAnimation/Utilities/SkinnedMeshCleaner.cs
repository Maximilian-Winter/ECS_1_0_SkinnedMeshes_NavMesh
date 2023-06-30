using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

using Mesh = UnityEngine.Mesh;

#if UNITY_EDITOR
public class SkinnedMeshCleaner : MonoBehaviour
{
	[Range(0.0f, 1.0f)]
	public float BakeQuality;
  [ContextMenu("ClearRenderers")]
  public void ClearSkinnedMeshRenderers()
  {
    SkinnedMeshRenderer[] skins = GetComponentsInChildren < SkinnedMeshRenderer >(true);

    for ( int i = 0; i < skins.Length; i++ )
    {
      if ( !skins[i].gameObject.activeSelf )
      {
        DestroyImmediate( skins[i].gameObject );

      }
    }
  }
  [ContextMenu("CombineRenderers")]
  public void CombineSkinnedMeshRenderers()
  {
	  SkinnedMeshRenderer[] skins = GetComponentsInChildren < SkinnedMeshRenderer >(false);

	  GameObject o = gameObject;
	  Vector3 oldPos = o.transform.position;
	  Quaternion oldRotation = o.transform.rotation;
	  Vector3 oldScale = o.transform.localScale;
	
	  
	  GameObject go = SkinnedMeshCombiner.Combine(skins.ToList(),o.name, oldPos, oldRotation, oldScale);

	  go.name = gameObject.name;
	  PrefabUtility.SaveAsPrefabAsset( go, $"Assets/CombinedSkinnedMeshRendererPrefabs/{go.name}.prefab" );

	  
	  GameObject combinedSkinnedMeshGO = new GameObject( name );
	  
	  //DestroyImmediate( go );
  }
  
  [ContextMenu("CombineRenderersStatic")]
  public void CombineSkinnedMeshRenderersToStatic()
  {
	  SkinnedMeshRenderer[] skins = GetComponentsInChildren < SkinnedMeshRenderer >(false);

	  GameObject o = gameObject;
	  Vector3 oldPos = o.transform.position;
	  Quaternion oldRotation = o.transform.rotation;
	  Vector3 oldScale = o.transform.localScale;
	
	  
	  GameObject go = SkinnedMeshCombiner.Combine(skins.ToList(),o.name, oldPos, oldRotation, oldScale);

	  go.name = gameObject.name;
	 

	  
	  GameObject combinedStatic = new GameObject(go.name + "_Static_Model_" + DateTime.Now.ToLocalTime().Day +"_"
	                                             + DateTime.Now.ToLocalTime().Month +"_" + DateTime.Now.ToLocalTime().Year +"_"+
	                                             DateTime.Now.ToLocalTime().Hour +"_"+ DateTime.Now.ToLocalTime().Minute +"_"+ DateTime.Now.ToLocalTime().Second );

	  Mesh mesh = go.GetComponent < SkinnedMeshRenderer >().sharedMesh;

	  MeshRenderer meshRenderer = combinedStatic.AddComponent < MeshRenderer >();
	  meshRenderer.sharedMaterial = go.GetComponent< SkinnedMeshRenderer >().sharedMaterial;
	  MeshFilter meshFilter = combinedStatic.AddComponent < MeshFilter >();
	  meshFilter.sharedMesh = mesh;
	  AssetDatabase.CreateAsset(  mesh, $"Assets/{combinedStatic.name}.asset" );
	  PrefabUtility.SaveAsPrefabAsset( combinedStatic, $"Assets/{combinedStatic.name}.prefab" );

	  AssetDatabase.SaveAssets();
	  

	  DestroyImmediate( go );
  }
  
}
public static class SkinnedMeshCombiner
{
	public static GameObject Combine(this SkinnedMeshRenderer target, List<SkinnedMeshRenderer> skinnedMeshRenderers, string name, Vector3 position, Quaternion rotation, Vector3 scale)
	{

		List<BoneWeight> boneWeights = new List<BoneWeight>();
		List<Transform> bones = new List<Transform>();
		List<CombineInstance> combineInstances = new List<CombineInstance>();
		Material sharedMaterial = skinnedMeshRenderers[0].sharedMaterial;
		Bounds newBounds = skinnedMeshRenderers[0].bounds;
		int num = 0;
		for( int i = 0; i < skinnedMeshRenderers.Count; ++i )
		{
			SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[i];
			BoneWeight[] bws = skinnedMeshRenderer.sharedMesh.boneWeights;
			Transform[] bs = skinnedMeshRenderer.bones;

			for( int bwIndex = 0; bwIndex < bws.Length; ++bwIndex )
			{
				BoneWeight boneWeight = bws[bwIndex];
				boneWeight.boneIndex0 += num;
				boneWeight.boneIndex1 += num;
				boneWeight.boneIndex2 += num;
				boneWeight.boneIndex3 += num;

				boneWeights.Add( boneWeight );
			}
			num += bs.Length;

			for( int boneIndex = 0; boneIndex < bs.Length; ++boneIndex )
			{
				bones.Add( bs[boneIndex] );
			}

			CombineInstance combineInstance = new CombineInstance()
			{
				mesh = skinnedMeshRenderer.sharedMesh,
				transform = skinnedMeshRenderer.transform.localToWorldMatrix
			};
			combineInstances.Add( combineInstance );

			if ( i > 0 )
			{
				newBounds.Encapsulate( skinnedMeshRenderers[i].bounds );
			}
			//skinnedMeshRenderer.enabled = false;
		}

		List<Matrix4x4> bindposes = new List<Matrix4x4>();
		for( int i = 0; i < bones.Count; ++i )
		{
			Transform bone = bones[i];
			bindposes.Add( bone.worldToLocalMatrix * target.transform.worldToLocalMatrix );
			
		}

		SkinnedMeshRenderer combinedSkinnedMeshRenderer = target;
		combinedSkinnedMeshRenderer.updateWhenOffscreen = false;

		combinedSkinnedMeshRenderer.sharedMesh = new Mesh();
		combinedSkinnedMeshRenderer.sharedMesh.indexFormat = IndexFormat.UInt32;

		if ( combineInstances.Count == 1 )
		{
			combinedSkinnedMeshRenderer.sharedMesh = combineInstances[0].mesh;
		}
		else
		{
			combinedSkinnedMeshRenderer.sharedMesh.CombineMeshes( combineInstances.ToArray(), true, true );
		}

		foreach ( CombineInstance combineInstance in combineInstances )
		{
			combinedSkinnedMeshRenderer.sharedMesh.subMeshCount += combineInstance.mesh.subMeshCount;
		}

		combinedSkinnedMeshRenderer.sharedMaterials = new Material[combinedSkinnedMeshRenderer.sharedMesh.subMeshCount];
		for ( int i = 0; i < combinedSkinnedMeshRenderer.sharedMesh.subMeshCount; i++ )
		{
			combinedSkinnedMeshRenderer.sharedMaterials[i] = sharedMaterial;
		}
		//combinedSkinnedMeshRenderer.sharedMaterial = sharedMaterial;
		combinedSkinnedMeshRenderer.bones = bones.ToArray();
		combinedSkinnedMeshRenderer.sharedMesh.boneWeights = boneWeights.ToArray();
		combinedSkinnedMeshRenderer.sharedMesh.bindposes = bindposes.ToArray();
		combinedSkinnedMeshRenderer.sharedMesh.RecalculateBounds();
		//combinedSkinnedMeshRenderer.localBounds = new Bounds( new Vector3( 0.0f, 1.0f, 0.0f ), new Vector3( 0.5f, 1.0f, 0.5f ) );
		//AssetDatabase.CreateAsset( combinedSkinnedMeshRenderer.sharedMesh, $"Assets/CombinedSkinnedMeshRendererPrefabs/{name}(Mesh{System.DateTime.Now:MM_dd_yyyy-H_mm}).asset" );
		//AssetDatabase.SaveAssets();
		return target.gameObject;
	}
	
	public static GameObject Combine( List<SkinnedMeshRenderer> skinnedMeshRenderers, string name, Vector3 position, Quaternion rotation, Vector3 scale)
	{
		GameObject combinedSkinnedMeshGO = new GameObject( name );

		combinedSkinnedMeshGO.transform.position = position;
		combinedSkinnedMeshGO.transform.rotation = rotation;
		combinedSkinnedMeshGO.transform.localScale = scale;
		if( skinnedMeshRenderers.Count == 0 )
			return combinedSkinnedMeshGO;

		List<BoneWeight> boneWeights = new List<BoneWeight>();
		List<Transform> bones = new List<Transform>();
		List<CombineInstance> combineInstances = new List<CombineInstance>();
		Material sharedMaterial = skinnedMeshRenderers[0].sharedMaterial;
		Bounds newBounds = skinnedMeshRenderers[0].bounds;
		int num = 0;
		for( int i = 0; i < skinnedMeshRenderers.Count; ++i )
		{
			SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[i];
			BoneWeight[] bws = skinnedMeshRenderer.sharedMesh.boneWeights;
			Transform[] bs = skinnedMeshRenderer.bones;

			for( int bwIndex = 0; bwIndex < bws.Length; ++bwIndex )
			{
				BoneWeight boneWeight = bws[bwIndex];
				boneWeight.boneIndex0 += num;
				boneWeight.boneIndex1 += num;
				boneWeight.boneIndex2 += num;
				boneWeight.boneIndex3 += num;

				boneWeights.Add( boneWeight );
			}
			num += bs.Length;

			for( int boneIndex = 0; boneIndex < bs.Length; ++boneIndex )
			{
				bones.Add( bs[boneIndex] );
			}

			CombineInstance combineInstance = new CombineInstance()
			{
				mesh = skinnedMeshRenderer.sharedMesh,
				transform = skinnedMeshRenderer.transform.localToWorldMatrix
			};
			combineInstances.Add( combineInstance );

			if ( i > 0 )
			{
				newBounds.Encapsulate( skinnedMeshRenderers[i].bounds );
			}
			//skinnedMeshRenderer.enabled = false;
		}

		List<Matrix4x4> bindposes = new List<Matrix4x4>();
		for( int i = 0; i < bones.Count; ++i )
		{
			Transform bone = bones[i];
			bindposes.Add( bone.worldToLocalMatrix * combinedSkinnedMeshGO.transform.worldToLocalMatrix );
			
		}

		SkinnedMeshRenderer combinedSkinnedMeshRenderer = combinedSkinnedMeshGO.AddComponent<SkinnedMeshRenderer>();
		combinedSkinnedMeshRenderer.updateWhenOffscreen = false;

		combinedSkinnedMeshRenderer.sharedMesh = new Mesh();
		combinedSkinnedMeshRenderer.sharedMesh.indexFormat = IndexFormat.UInt32;
		//combinedSkinnedMeshRenderer.sharedMesh.subMeshCount = 3;
		
		
		combinedSkinnedMeshRenderer.sharedMesh.CombineMeshes( combineInstances.ToArray(), true, true );
		combinedSkinnedMeshRenderer.sharedMaterial = sharedMaterial;
		combinedSkinnedMeshRenderer.bones = bones.ToArray();
		//combinedSkinnedMeshRenderer.sharedMesh.boneWeights = boneWeights.ToArray();
		combinedSkinnedMeshRenderer.sharedMesh.bindposes = bindposes.ToArray();
		combinedSkinnedMeshRenderer.sharedMesh.RecalculateBounds();
		//combinedSkinnedMeshRenderer.localBounds = new Bounds( new Vector3( 0.0f, 1.0f, 0.0f ), new Vector3( 0.5f, 1.0f, 0.5f ) );
		AssetDatabase.CreateAsset( combinedSkinnedMeshRenderer.sharedMesh, $"Assets/CombinedSkinnedMeshRendererPrefabs/{name}(Mesh).asset" );
		AssetDatabase.SaveAssets();
		return combinedSkinnedMeshGO;
	}
}
#endif