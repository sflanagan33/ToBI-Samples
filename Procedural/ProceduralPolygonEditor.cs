#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace TOBI.Procedural
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProceduralPolygon))]
    public class ProceduralPolygonEditor : Editor
    {
        private const float MIN_RADIUS = 0.1f;
        private const float MAX_RADIUS = 10f;
        private const float MIN_HEIGHT = 0.25f;
        private const float MAX_HEIGHT = 100f;
        private const float POINT_SNAP = 1f;
        private const float HEIGHT_SNAP = 2f;
        private const float SCALE_MODIFIER = 0.1f;
        private const float SCALE_SPEED_CHANGE_THRESHOLD = 0.05f;

        private SerializedProperty style;
        private SerializedProperty hasTop;
        private SerializedProperty hasBottom;
        private SerializedProperty radius;
        private SerializedProperty height;

        private static ProceduralPolygon newPointOwner = null;
        private static int newPointIndex = -1;
        private static Vector3 scaleVector = new Vector3(1f, 1f, 1f);
        private static Vector3 lastScaleVector = new Vector3(1f, 1f, 1f);
        
        private static bool scaleToolEnabled = false;
        private static Tool lastTool;

        private void OnEnable()
        {
            style = serializedObject.FindProperty("style");
            hasTop = serializedObject.FindProperty("hasTop");
            hasBottom = serializedObject.FindProperty("hasBottom");
            radius = serializedObject.FindProperty("radius");
            height = serializedObject.FindProperty("height");

            foreach (Object o in targets)
                Undo.undoRedoPerformed += ((ProceduralPolygon)o).Regenerate;
        }

        private void OnDisable()
        {
            // Cleanly close out of the custom scale tool.

            if (scaleToolEnabled)
            {
                Tools.current = lastTool;
                lastTool = Tool.None;
                scaleToolEnabled = false;
            }

            foreach (Object o in targets)
                Undo.undoRedoPerformed -= ((ProceduralPolygon)o).Regenerate;
        }

        [MenuItem("TOBI/Set Lighting Parameters for All Polygons", false, 13)]
        public static void SetLighting()
        {
            ProceduralPolygon[] polys = FindObjectsOfType<ProceduralPolygon>();
            Undo.RecordObjects(polys, "Set lighting parameters");

            foreach (ProceduralPolygon p in polys)
            {
                StaticEditorFlags flags = p.style.lightmapStatic ? StaticEditorFlags.LightmapStatic : 0;
                GameObjectUtility.SetStaticEditorFlags(p.gameObject, flags);
            }

            Debug.Log("Set lighting parameters for " + polys.Length + " polygons");
        }

        [MenuItem("TOBI/Select All Polygons Using Selected Style", false, 14)]
        public static void SelectAll()
        {
            if (Selection.activeObject is ProceduralPolygonStyle)
            {
                ProceduralPolygonStyle s = (ProceduralPolygonStyle) Selection.activeObject;
                Selection.objects = FindObjectsOfType<ProceduralPolygon>().Where(p => p.style == s).Select(p => p.gameObject).ToArray();
            }

            Debug.Log("No style selected");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Get an array of undoable objects (all the currently edited polygons and their mesh renderers / transforms)

            ProceduralPolygon[] polygons = System.Array.ConvertAll(targets, p => (ProceduralPolygon)p);

            List<Object> u = new List<Object>(polygons);

            foreach (ProceduralPolygon p in polygons)
            {
                u.Add(p.meshRenderer);
                u.Add(p.transform);
            }

            Object[] undoables = u.ToArray();

            // Using tools

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Reset Shape"))
            {
                Undo.RecordObjects(undoables, "Use Procedural Polygon Tool");

                foreach (ProceduralPolygon p in polygons)
                    p.ResetShape();
            }

            bool anyNullStyles = polygons.Any(p => p.style == null);
            EditorGUI.BeginDisabledGroup(anyNullStyles);

            if (GUILayout.Button("Reset Materials"))
            {
                Undo.RecordObjects(undoables, "Use Procedural Polygon Tool");

                foreach (ProceduralPolygon p in polygons)
                    p.ResetMaterials();
            }

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Center Pivot"))
            {
                Undo.RecordObjects(undoables, "Use Procedural Polygon Tool");

                foreach (ProceduralPolygon p in polygons)
                    p.CenterPivot();
            }

            if (GUILayout.Button("Regenerate"))
            {
                Undo.RecordObjects(undoables, "Use Procedural Polygon Tool");

                foreach (ProceduralPolygon p in polygons)
                    p.Regenerate();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Flip X"))
            {
                Undo.RecordObjects(undoables, "Use Procedural Polygon Tool");

                foreach (ProceduralPolygon p in polygons)
                    p.Flip('X');
            }

            if (GUILayout.Button("Flip Z"))
            {
                Undo.RecordObjects(undoables, "Use Procedural Polygon Tool");

                foreach (ProceduralPolygon p in polygons)
                    p.Flip('Z');
            }
            EditorGUILayout.EndHorizontal();

            // Changing the style

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(style);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(undoables, "Apply Procedural Polygon Style");

                serializedObject.ApplyModifiedProperties();

                ProceduralPolygonStyle s = (ProceduralPolygonStyle)style.objectReferenceValue;
                float newMinRadius = GetMinimumRadius(s);
                float newMinHeight = GetMinimumHeight(s);

                foreach (ProceduralPolygon p in polygons)
                {
                    p.radius = Mathf.Max(p.radius, newMinRadius);
                    p.height = Mathf.Max(p.height, newMinHeight);
                    p.ResetMaterials();
                    p.Regenerate();
                }
            }

            // Changing the settings

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(hasTop, new GUIContent("Has Top"));
            EditorGUILayout.PropertyField(hasBottom, new GUIContent("Has Bottom"));

            if (!style.hasMultipleDifferentValues)
            {
                ProceduralPolygonStyle s = (ProceduralPolygonStyle)style.objectReferenceValue;
                EditorGUILayout.Slider(radius, GetMinimumRadius(s), MAX_RADIUS, "Radius");
                EditorGUILayout.Slider(height, GetMinimumHeight(s), MAX_HEIGHT, "Height");
            }

            else
                EditorGUILayout.HelpBox("Can't change radius or height for two polygons of different styles!", MessageType.Warning);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(undoables, "Edit Procedural Polygon");

                serializedObject.ApplyModifiedProperties();

                foreach (ProceduralPolygon p in polygons)
                    p.Regenerate();
            }

            // Display generation error messages if one polygon is selected.

            if (polygons.Length == 1 && polygons[0].generationMessage != "")
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Generation failed! " + polygons[0].generationMessage, MessageType.Error);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        public void OnSceneGUI()
        {
            // Setup. Get the procedural polygon we're editing, work in its local matrix, and start listening for changes.

            ProceduralPolygon p = (ProceduralPolygon)target;
            Handles.matrix = p.transform.localToWorldMatrix;
            EditorGUI.BeginChangeCheck();


            // Draw dotted lines around the shape.

            int n = p.points.Count;
            for (int i = 0; i < n; i++)
            {
                Point a = p.points[i];
                Point b = p.points[(i + 1) % n];
                Vector3 up = Vector3.up * p.height;

                Handles.color = new Color(1, 1, 1, 1);
                Handles.DrawDottedLine(a, b, 5f);
                Handles.color = new Color(1, 1, 1, 0.5f);
                Handles.DrawDottedLine(a + up, b + up, 5f);
                Handles.DrawDottedLine(a, a + up, 5f);
            }

            // Point editing. Create a unique control ID and handle for every point and let the user drag them around, with optional snapping.

            List<int> editControls = new List<int>(n);
            List<Point> newPoints = new List<Point>(n);

            for (int i = 0; i < n; i++)
            {
                editControls.Add(GUIUtility.GetControlID(FocusType.Passive));

                if (newPointIndex == i && newPointOwner == p)
                {
                    GUIUtility.hotControl = editControls[i];
                    newPointIndex = -1;
                    newPointOwner = null;
                }

                Handles.color = new Color(1, 1, editControls[i] == GUIUtility.hotControl ? 0 : 1, 0.5f);
                Handles.DrawSolidDisc(p.points[i], Vector3.up, 1f);
                Handles.color = new Color(1, 1, 1, 1);
                Vector3 e = Handles.Slider2D(editControls[i], p.points[i], Vector3.up, Vector3.right, Vector3.forward, 1f, Handles.CircleHandleCap, Vector2.one * POINT_SNAP, false);

                if (Event.current.control && editControls[i] == GUIUtility.hotControl)
                {
                    e.x = Mathf.Round(e.x / POINT_SNAP) * POINT_SNAP;
                    e.z = Mathf.Round(e.z / POINT_SNAP) * POINT_SNAP;
                }

                newPoints.Add(e);
            }

            // Point addition. Create a handle on every line segment and add a new point if the user clicks on the handle. Seamlessly drag the new point.

            for (int i = 0; i < n; i++)
            {
                int addControl = GUIUtility.GetControlID(FocusType.Passive);

                Point addPoint = (p.points[i] + p.points[(i + 1) % n]) * 0.5f;

                Handles.color = new Color(0, 0.8f, 1, 0.5f);
                Handles.DrawSolidDisc(addPoint, Vector3.up, 0.5f);
                Handles.color = new Color(0, 0.8f, 1, 1);
                Handles.Slider2D(addControl, addPoint, Vector3.up, Vector3.right, Vector3.forward, 0.5f, Handles.CircleHandleCap, Vector2.one * POINT_SNAP, false);

                if (addControl == GUIUtility.hotControl)
                {
                    newPoints.Insert(i + 1, addPoint + Random.insideUnitCircle.normalized * 0.01f);
                    newPointIndex = i + 1;
                    newPointOwner = p;

                    GUI.changed = true;
                    GUIUtility.hotControl = 0;
                }
            }

            // Point deletion. While the user is dragging a point, they can press Delete to remove it from the list.

            if (n > 3)
            {
                switch (Event.current.type)
                {
                    case EventType.MouseDown:
                    case EventType.KeyDown:

                        if (Event.current.keyCode == KeyCode.Delete || Event.current.button == 1)
                        {
                            int deletedPoint = editControls.FindIndex(i => i == GUIUtility.hotControl);

                            if (deletedPoint != -1)
                            {
                                newPoints.RemoveAt(deletedPoint);

                                GUI.changed = true;
                                GUIUtility.hotControl = 0;

                                Event.current.Use();
                            }
                        }

                        break;
                }
            }
            
            // Find center for use in drawing tools.

            n = newPoints.Count;
            Point center = new Point();
            for (int i = 0; i < n; i++)
                center += newPoints[i];
            center /= newPoints.Count;
            
            // Scaling override.

            // If scaling tool is selected, change to having no tool selected.

            if (Tools.current == Tool.Scale)
            {
                lastTool = Tools.current;
                Tools.current = Tool.None;
                scaleToolEnabled = true;
            }

            else if (scaleToolEnabled && Tools.current != Tool.None)
            {
                lastTool = Tool.None;
                scaleToolEnabled = false;
            }

            // Draw and use custom scale tool.

            if (scaleToolEnabled)
            {
                if (GUIUtility.hotControl == 0)
                    scaleVector = new Vector3(1f, 1f, 1f);

                scaleVector = Handles.ScaleHandle(scaleVector, center, Quaternion.identity, HandleUtility.GetHandleSize(center));

                float addScaleX = scaleVector.x - lastScaleVector.x;
                float addScaleZ = scaleVector.z - lastScaleVector.z;

                for (int i = 0; i < n; i++)
                {
                    Point addPoint = newPoints[i];
                    addPoint.x *= 1 + ((addScaleX) * SCALE_MODIFIER);
                    addPoint.y *= 1 + ((addScaleZ) * SCALE_MODIFIER);

                    newPoints[i] = addPoint;
                }

                if (Mathf.Abs(scaleVector.x - lastScaleVector.x) > SCALE_SPEED_CHANGE_THRESHOLD
                 || Mathf.Abs(scaleVector.z - lastScaleVector.z) > SCALE_SPEED_CHANGE_THRESHOLD)
                    lastScaleVector = scaleVector;
            }

            // Height editing.

            Handles.color = new Color(1, 1, 1, 1);
            int heightControl = GUIUtility.GetControlID(FocusType.Passive);
            float newHeight = Handles.Slider(heightControl, center + Vector3.up * (p.height + 1), Vector3.up, 2f, Handles.ConeHandleCap, HEIGHT_SNAP).y - 1;

            if (Event.current.control && heightControl == GUIUtility.hotControl)
                newHeight = Mathf.Round(newHeight / HEIGHT_SNAP) * HEIGHT_SNAP;
            newHeight = Mathf.Clamp(newHeight, GetMinimumHeight(p.style), MAX_HEIGHT);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Edit Procedural Polygon");
                p.points = newPoints;
                p.height = newHeight;
                p.Regenerate();
            }
        }

        private float GetMinimumRadius(ProceduralPolygonStyle style)
        {
            if (style == null)
                return MIN_RADIUS;
            else
                return Mathf.Max(MIN_RADIUS, style.highpolySilhouette.Width);
        }

        private float GetMinimumHeight(ProceduralPolygonStyle style)
        {
            if (style == null)
                return MIN_HEIGHT;
            else
                return Mathf.Max(MIN_HEIGHT, style.highpolySilhouette.Height);
        }
    }
}

#endif