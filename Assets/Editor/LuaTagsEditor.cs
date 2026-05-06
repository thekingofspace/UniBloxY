using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LuaTags))]
[CanEditMultipleObjects]
public class LuaTagsEditor : Editor
{
    private SerializedProperty entriesProp;

    private void OnEnable()
    {
        entriesProp = serializedObject.FindProperty("entries");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "These tags transfer onto the LuaInstance as attributes when the " +
            "GameObject is wrapped via ObjectAsInstance, ConvertToInstance, " +
            "or ImportScene.",
            MessageType.None);

        for (int i = 0; i < entriesProp.arraySize; i++)
        {
            var entry = entriesProp.GetArrayElementAtIndex(i);
            var keyProp = entry.FindPropertyRelative("Key");
            var valProp = entry.FindPropertyRelative("Value");

            using (new EditorGUILayout.HorizontalScope())
            {
                keyProp.stringValue = EditorGUILayout.TextField(keyProp.stringValue, GUILayout.MinWidth(80));
                EditorGUILayout.LabelField("=", GUILayout.Width(12));
                valProp.stringValue = EditorGUILayout.TextField(valProp.stringValue);
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    entriesProp.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
            }
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Add Tag"))
        {
            entriesProp.arraySize++;
            var added = entriesProp.GetArrayElementAtIndex(entriesProp.arraySize - 1);
            added.FindPropertyRelative("Key").stringValue = "";
            added.FindPropertyRelative("Value").stringValue = "";
        }

        serializedObject.ApplyModifiedProperties();
    }
}
