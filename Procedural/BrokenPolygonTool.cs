#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace TOBI.Procedural
{
    public class BrokenPolygonTool : MonoBehaviour
    {
        [MenuItem("TOBI/Find Broken Polygons #b", false, 1)]
        private static void StartFromCurrentPosition()
        {
            IEnumerable<ProceduralPolygon> brokenPolygons = FindObjectsOfType<ProceduralPolygon>().Where(p => !string.IsNullOrEmpty(p.generationMessage));
            int count = brokenPolygons.Count();

            if (count == 0)
                Debug.Log("<b>Find Broken Polygons |</b> Nothing broken!");
            else
            {
                Debug.Log("<b>Find Broken Polygons |</b> <color=red>" + count + " found.</color>");
                Selection.activeGameObject = brokenPolygons.First().gameObject;
                SceneView.FrameLastActiveSceneView();
            }
        }
    }
}

#endif