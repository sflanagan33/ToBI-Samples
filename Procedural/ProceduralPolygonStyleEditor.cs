#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace TOBI.Procedural
{
    [CustomEditor(typeof(ProceduralPolygonStyle))]
    public class ProceduralPolygonStyleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }

    [CustomPropertyDrawer(typeof(ProceduralPolygonStrip))]
    public class ProceduralPolygonStripDrawer : PropertyDrawer
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
            SerializedProperty uShift = property.FindPropertyRelative("uShift");
            SerializedProperty vBehavior = property.FindPropertyRelative("vBehavior");
            SerializedProperty materialIndex = property.FindPropertyRelative("materialIndex");
            SerializedProperty overrideTextureSize = property.FindPropertyRelative("overrideTextureSize");

            EditorGUI.PropertyField(contentPosition, points, true);
            contentPosition.y += EditorGUI.GetPropertyHeight(points);

            EditorGUI.PropertyField(contentPosition, uShift, true);
            contentPosition.y += EditorGUI.GetPropertyHeight(uShift);

            EditorGUI.PropertyField(contentPosition, vBehavior, true);
            contentPosition.y += EditorGUI.GetPropertyHeight(vBehavior);

            EditorGUI.PropertyField(contentPosition, materialIndex, true);
            contentPosition.y += EditorGUI.GetPropertyHeight(materialIndex);

            contentPosition.height = EditorGUI.GetPropertyHeight(overrideTextureSize);
            EditorGUI.PropertyField(contentPosition, overrideTextureSize);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = BUFFER;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("points"));
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("uShift"));
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("vBehavior"));
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("materialIndex"));
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("overrideTextureSize"));
            return height;
        }
    }

    [CustomPropertyDrawer(typeof(ProceduralPolygonStripPoint))]
    public class ProceduralPolygonStripPointDrawer : PropertyDrawer
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
            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("anchor"), new GUIContent("A", "Anchor"));

            contentPosition.x += offset;
            EditorGUIUtility.labelWidth = 12f;
            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("distance"), new GUIContent("R", "Radial Distance from Anchor"));

            contentPosition.x += offset;
            EditorGUIUtility.labelWidth = 12f;
            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("height"), new GUIContent("H", "Height from Anchor"));

            EditorGUI.EndProperty();
            EditorGUI.indentLevel = oldIndent;
        }
    }
}

#endif