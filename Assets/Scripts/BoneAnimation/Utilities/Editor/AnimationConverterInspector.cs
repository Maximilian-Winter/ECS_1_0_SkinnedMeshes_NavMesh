
using UnityEditor;
using UnityEngine;

namespace Animations.Utilities
{
[CustomEditor(typeof(AnimationConverter))]
public class AnimationConverterInspector : Editor
{
    private AnimationConverter m_AnimationConverter = null;
    
    void OnEnable()
    {
        m_AnimationConverter = target as AnimationConverter;
    }
    
    public override void OnInspectorGUI()
		{
			serializedObject.Update();

			InputGUI();
			BakeGUI();

			serializedObject.ApplyModifiedProperties();
		}

		private void InputGUI()
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("ModelPrefab"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("ModelAnimatorController"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("FoundStates"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("Animations"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("SampleFPS"));
			//EditorGUILayout.PropertyField(serializedObject.FindProperty("Fps"));
		}

		private void BakeGUI()
		{
			if (GUILayout.Button("Convert", GUILayout.Height(32)))
			{
				m_AnimationConverter.Convert();
			}
		}
}

}
