#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Game.Components.Editor
{
    [CustomPropertyDrawer(typeof(@bool))]
    public class BooleanDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.Next(true))
            {
                property.intValue = EditorGUI.Toggle(position, label, property.intValue != 0) ? byte.MaxValue : 0;
            }

            if (GUI.changed)
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}

#endif