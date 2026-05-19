using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct Tag
{
    [SerializeField] private string _value;

    public string Value => _value;

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Tag))]
    private class PropertyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            SerializedProperty valueProperty = property.FindPropertyRelative(nameof(_value));
            string value = EditorGUI.TagField(position, label, valueProperty.stringValue);
            if (EditorGUI.EndChangeCheck()) valueProperty.stringValue = value;
        }
    }
#endif
}