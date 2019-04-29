#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace TOBI.Procedural
{
    [CustomEditor(typeof(ProceduralSplineStyle))]
    public class ProceduralSplineStyleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }

    [CustomPropertyDrawer(typeof(ProceduralSplineStrip))]
    public class ProceduralSplineStripDrawer : PropertyDrawer
    {
        private static Color COLOR = new Color(0.549f, 0.576f, 0.604f, 0.5f);
        private static int BUFFER = 5;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            Rect contentPosition = EditorGUI.PrefixLabel(position, GUIContent.none);
            contentPosition.height -= BUFFER;

            EditorGUI.DrawRect(contentPosition, COLOR);

            SerializedProperty points = property.FindPropertyRelative("points");
            SerializedProperty materialIndex = property.FindPropertyRelative("materialIndex");

            EditorGUI.PropertyField(contentPosition, points, true);
            contentPosition.y += EditorGUI.GetPropertyHeight(points);

            contentPosition.height = EditorGUI.GetPropertyHeight(materialIndex);
            EditorGUI.PropertyField(contentPosition, materialIndex);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = BUFFER;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("points"));
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("materialIndex"));
            return height;
        }
    }

    [CustomPropertyDrawer(typeof(ProceduralSplineStripPoint))]
    public class ProceduralSplineStripPointDrawer : PropertyDrawer
    {
        private static Color COLOR = new Color(0.549f, 0.576f, 0.604f);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            Rect contentPosition = EditorGUI.PrefixLabel(position, GUIContent.none);

            EditorGUI.DrawRect(contentPosition, COLOR);
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float offset = contentPosition.width / 3;
            contentPosition.width /= 3.5f;

            EditorGUIUtility.labelWidth = 12f;
            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("x"), new GUIContent("X", "Horizontal Position"));

            contentPosition.x += offset;
            EditorGUIUtility.labelWidth = 12f;
            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("y"), new GUIContent("Y", "Vertical Position"));

            contentPosition.x += offset;
            EditorGUIUtility.labelWidth = 12f;
            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("v"), new GUIContent("V", "Vertical Texcoord"));

            EditorGUI.EndProperty();
            EditorGUI.indentLevel = oldIndent;
        }
    }
}

#endif