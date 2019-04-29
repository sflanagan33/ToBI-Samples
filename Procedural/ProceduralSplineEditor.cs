#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace TOBI.Procedural
{
    [CustomEditor(typeof(ProceduralSpline))]
    public class ProceduralSplineEditor : Editor
    {
        private const float POINT_SNAP = 1f;
        private const int MIN_PRECISION = 1;
        private const int MAX_PRECISION = 20;
        private const float MIN_TORSION = 1;
        private const float MAX_TORSION = 100;

        private SerializedProperty style;
        private SerializedProperty closed;
        private SerializedProperty precision;
        private SerializedProperty orientation;

        private static ProceduralSpline lastTarget;
        private static int currentSelection = -1;

        private void OnEnable()
        {
            style = serializedObject.FindProperty("style");
            closed = serializedObject.FindProperty("closed");
            precision = serializedObject.FindProperty("precision");
            orientation = serializedObject.FindProperty("orientation");

            ProceduralSpline ps = (ProceduralSpline) target;
            Undo.undoRedoPerformed += ps.Regenerate;

            if (ps != lastTarget)
            {
                lastTarget = ps;
                currentSelection = -1;
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= ((ProceduralSpline) target).Regenerate;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Get an array of undoable objects (the currently edited spline and its mesh renderer / transform)

            ProceduralSpline ps = (ProceduralSpline) target;

            Object[] undoables = new Object[3];
            undoables[0] = ps;
            undoables[1] = ps.meshRenderer;
            undoables[2] = ps.transform;

            // Using node tools

            bool selected = currentSelection != -1;
            SplineNode selectedNode = selected ? ps.nodes[currentSelection] : new SplineNode();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spline Node Tools", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(!selected);

            EditorGUI.BeginChangeCheck();
            Vector3 p = EditorGUILayout.Vector3Field("Position", selectedNode.position);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(undoables, "Use Procedural Spline Node Tool");

                selectedNode.position = p;
                ps.nodes[currentSelection] = selectedNode;
                ps.Regenerate();
            }

            EditorGUI.BeginChangeCheck();
            Vector3 r = EditorGUILayout.Vector3Field("Rotation", selectedNode.rotation.eulerAngles);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(undoables, "Use Procedural Spline Node Tool");

                selectedNode.rotation = Quaternion.Euler(r);
                ps.nodes[currentSelection] = selectedNode;
                ps.Regenerate();
            }

            EditorGUI.BeginChangeCheck();
            float t = EditorGUILayout.Slider("Torsion", selectedNode.torsion, MIN_TORSION, MAX_TORSION);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(undoables, "Use Procedural Spline Node Tool");

                selectedNode.torsion = t;
                ps.nodes[currentSelection] = selectedNode;
                ps.Regenerate();
            }

            EditorGUI.BeginDisabledGroup(ps.nodes.Count == 2);

            if (GUILayout.Button("Delete Node"))
            {
                Undo.RecordObjects(undoables, "Use Procedural Spline Node Tool");

                ps.nodes.RemoveAt(currentSelection);
                ps.Regenerate();

                currentSelection = -1;
                Repaint();
            }

            EditorGUI.EndDisabledGroup();
            
            EditorGUI.EndDisabledGroup();

            // Using spline tools

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spline Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Reset Shape"))
            {
                Undo.RecordObjects(undoables, "Use Procedural Spline Tool");

                ps.ResetShape();
            }
            
            EditorGUI.BeginDisabledGroup(ps.style == null);

            if (GUILayout.Button("Reset Materials"))
            {
                Undo.RecordObjects(undoables, "Use Procedural Spline Tool");

                ps.ResetMaterials();
            }

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Center Pivot"))
            {
                Undo.RecordObjects(undoables, "Use Procedural Spline Tool");

                ps.CenterPivot();
            }

            if (GUILayout.Button("Regenerate"))
            {
                Undo.RecordObjects(undoables, "Use Procedural Spline Tool");

                ps.Regenerate();
            }

            // Changing the style

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(style);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(undoables, "Apply Procedural Spline Style");

                serializedObject.ApplyModifiedProperties();
                
                ps.ResetMaterials();
                ps.Regenerate();
            }

            // Changing the settings

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(closed, new GUIContent("Closed"));
            EditorGUILayout.IntSlider(precision, MIN_PRECISION, MAX_PRECISION, "Precision");
            EditorGUILayout.PropertyField(orientation, new GUIContent("Orientation"));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(undoables, "Edit Procedural Spline");

                serializedObject.ApplyModifiedProperties();

                ps.Regenerate();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        public void OnSceneGUI()
        {
            // Setup. Get the procedural spline we're editing and start listening for changes.

            ProceduralSpline s = (ProceduralSpline) target;
            EditorGUI.BeginChangeCheck();

            Spline temp = new Spline(s.nodes, s.precision, s.closed, s.orientation);

            // Draw a dotted line along the spline.

            Vector3[] worldPositions = new Vector3[temp.count];
            for (int i = 0; i < temp.count; i++)
                worldPositions[i] = s.transform.TransformPoint(temp.vertices[i]);
            Handles.DrawAAPolyLine(3, worldPositions);

            // Node editing.

            List<SplineNode> editNodes = new List<SplineNode>(s.nodes);

            int n = s.nodes.Count;

            for (int i = 0; i < n; i++)
            {
                bool selected = i == currentSelection;
                Handles.color = new Color(1, 1, selected ? 0 : 1);

                Vector3 worldPos = s.transform.TransformPoint(s.nodes[i].position);
                float size = HandleUtility.GetHandleSize(worldPos) * (selected ? 0.5f : 1);

                if (Handles.Button(worldPos, Quaternion.identity, 0.3f * size, 0.4f * size, Handles.SphereHandleCap))
                {
                    currentSelection = i;
                    Repaint();
                }
            }

            if (currentSelection != -1)
            {
                int cs = currentSelection;
                Vector3 worldPos = s.transform.TransformPoint(s.nodes[cs].position);

                if (Tools.current == Tool.Move)
                {
                    Quaternion reference = Tools.pivotRotation == PivotRotation.Local ? s.nodes[cs].rotation * s.transform.rotation : Quaternion.identity;

                    Vector3 p = s.transform.InverseTransformPoint(Handles.PositionHandle(worldPos, reference));

                    editNodes[cs] = new SplineNode(p, s.nodes[cs].rotation, s.nodes[cs].torsion);
                }

                else if (Tools.current == Tool.Rotate)
                {
                    Quaternion r = Handles.RotationHandle(s.nodes[cs].rotation, worldPos);
                    editNodes[cs] = new SplineNode(s.nodes[cs].position, r, s.nodes[cs].torsion);
                }

                else if (Tools.current == Tool.Scale)
                {
                    float t = Handles.ScaleSlider(s.nodes[cs].torsion, worldPos, s.nodes[cs].rotation * Vector3.forward, s.nodes[cs].rotation, 4f, 1f);
                    t = Mathf.Min(Mathf.Max(t, MIN_TORSION), MAX_TORSION);

                    editNodes[cs] = new SplineNode(s.nodes[cs].position, s.nodes[cs].rotation, t);
                }
            }
            
            // Node addition.

            Handles.color = new Color(0, 0.8f, 1, 1);

            int? insertIndex = null;
            SplineNode insertNode = new SplineNode();

            int m = s.nodes.Count - (s.closed ? 0 : 1);
            for (int i = 0; i < m; i++)
            {
                int a = Mathf.FloorToInt((i + 0.5f) * s.precision);
                int b = (a + (1 - temp.count % 2)) % temp.count;

                Vector3 p = Vector3.Lerp(temp.vertices[a], temp.vertices[b], 0.5f);
                Vector3 worldPos = s.transform.TransformPoint(p);
                Quaternion r = Quaternion.Slerp(temp.rotations[a], temp.rotations[b], 0.5f);

                float size = HandleUtility.GetHandleSize(worldPos);

                if (Handles.Button(worldPos, Quaternion.identity, 0.1f * size, 0.2f * size, Handles.SphereHandleCap))
                {
                    insertIndex = i + 1;
                    insertNode = new SplineNode(p, r, 10);

                    currentSelection = (int) insertIndex;
                    Repaint();

                    GUI.changed = true;
                    break;
                }
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(s, "Edit Procedural Spline");

                s.nodes = editNodes;

                if (insertIndex != null)
                    s.nodes.Insert((int) insertIndex, insertNode);

                s.Regenerate();
            }
        }
    }
}

#endif