using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

using TOBI.Core;

namespace TOBI.World
{
    public class WorldArea : MonoBehaviour
    {
        public static readonly float LOAD_BOUNDARY = 75;
        public static readonly float UNLOAD_BOUNDARY = 100;
        
        [HideInInspector] public string scenePath;
        [HideInInspector] public MeshRenderer lod;
        [HideInInspector] public Vector3 center;
        [HideInInspector] public Vector3 size;

        private Bounds loadBoundary;
        private Bounds unloadBoundary;
        private WorldAreaLoadState state;

        public bool IsLoadingOrUnloading()
        {
            return state == WorldAreaLoadState.Busy;
        }

        private void Start()
        {
            loadBoundary = new Bounds(center, size + LOAD_BOUNDARY * Vector3.one);
            unloadBoundary = new Bounds(center, size + UNLOAD_BOUNDARY * Vector3.one);
        }

        private void Update()
        {
            // Figure out if this area should be loaded based on the position of the camera

            if (state == WorldAreaLoadState.Unloaded)
            {
                if (loadBoundary.Contains(Game.objects.cam.LoadPosition))
                    StartCoroutine(LoadRoutine());
            }

            else if (state == WorldAreaLoadState.Loaded)
            {
                if (!unloadBoundary.Contains(Game.objects.cam.LoadPosition))
                    StartCoroutine(UnloadRoutine());
            }
        }

        // ====================================================================================================================

        private IEnumerator LoadRoutine()
        {
            state = WorldAreaLoadState.Busy;

            // Load the scene asynchronously

            AsyncOperation a = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            while (!a.isDone)
                yield return null;

            // Turn off the LOD

            lod.enabled = false;

            state = WorldAreaLoadState.Loaded;
        }

        private IEnumerator UnloadRoutine()
        {
            state = WorldAreaLoadState.Busy;
            
            AsyncOperation a = SceneManager.UnloadSceneAsync(scenePath);
            while (!a.isDone)
                yield return null;
            
            lod.enabled = true;

            state = WorldAreaLoadState.Unloaded;
        }
    }

    public enum WorldAreaLoadState
    {
        Unloaded, Loaded, Busy
    }
}