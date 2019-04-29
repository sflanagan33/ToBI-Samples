using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace TOBI.World
{
    public class WorldAreaManager : MonoBehaviour
    {
        private const string LOAD_PATH = "World/";
        
        private void Awake()
        {
            foreach (GameObject prefab in Resources.LoadAll<GameObject>(LOAD_PATH))
            {
                GameObject g = Instantiate(prefab);
                g.transform.SetParent(transform);
                g.name = prefab.name;
            }
        }
    }
}