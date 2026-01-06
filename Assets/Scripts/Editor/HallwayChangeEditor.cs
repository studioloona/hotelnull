using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(HallwayChange))]
public class HallwayChangeEditor : Editor
{
    private SerializedProperty changeConfigurationsProperty;
    private SerializedProperty targetObjectProperty;

    void OnEnable()
    {
        changeConfigurationsProperty = serializedObject.FindProperty("changeConfigurations");
        targetObjectProperty = serializedObject.FindProperty("targetObject");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw default header
        EditorGUILayout.LabelField("Change Configuration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetObjectProperty);

        EditorGUILayout.Space();

        // Draw the list size
        EditorGUILayout.PropertyField(changeConfigurationsProperty, new GUIContent("Change Configurations"), false);

        if (changeConfigurationsProperty.isExpanded)
        {
            EditorGUI.indentLevel++;

            // Draw array size
            EditorGUILayout.PropertyField(changeConfigurationsProperty.FindPropertyRelative("Array.size"));

            // Draw each element
            for (int i = 0; i < changeConfigurationsProperty.arraySize; i++)
            {
                EditorGUILayout.Space();
                SerializedProperty element = changeConfigurationsProperty.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(element, new GUIContent($"Configuration {i}"), false);

                if (element.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    // Get the change type
                    SerializedProperty changeTypeProperty = element.FindPropertyRelative("changeType");
                    EditorGUILayout.PropertyField(changeTypeProperty);

                    ChangeType changeType = (ChangeType)changeTypeProperty.enumValueIndex;

                    // Show relevant fields based on change type
                    EditorGUILayout.Space(5);

                    switch (changeType)
                    {
                        case ChangeType.ChangeColor:
                            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("newColor"), new GUIContent("New Color"));
                            break;

                        case ChangeType.ChangePosition:
                            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("positionOffset"), new GUIContent("Position Offset"));
                            break;

                        case ChangeType.ChangeRotation:
                            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("rotationOffset"), new GUIContent("Rotation Offset"));
                            break;

                        case ChangeType.ChangeScale:
                            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("scaleMultiplier"), new GUIContent("Scale Multiplier"));
                            break;

                        case ChangeType.SwapObject:
                            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("swapWithObject"), new GUIContent("Swap With Object"));
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("flipSwappedObject"), new GUIContent("Flip 180° (Y axis)"));
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("swapPositionOffset"), new GUIContent("Position Offset"));
                            break;

                        case ChangeType.PlayCreepySound:
                        case ChangeType.PlayWhisper:
                        case ChangeType.PlayDistortion:
                        case ChangeType.WeirdAmbience:
                            EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("loopAudio"), new GUIContent("Loop Audio"));
                            break;

                        case ChangeType.DisableObject:
                        case ChangeType.EnableObject:
                            EditorGUILayout.HelpBox("No additional settings required. This will affect the Target Object.", MessageType.Info);
                            break;

                        case ChangeType.StopAmbience:
                            EditorGUILayout.HelpBox("No additional settings required. Audio will play when anomaly is triggered.", MessageType.Info);
                            break;
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
