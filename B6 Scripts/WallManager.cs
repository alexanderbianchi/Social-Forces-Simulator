using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    private static HashSet<GameObject> wallObjs = new HashSet<GameObject>();

    void Start()
    {
        GameObject[] walls;
        walls = GameObject.FindGameObjectsWithTag("Wall");
        foreach(GameObject wall in walls){
            wallObjs.Add(wall);
        }
    }

    #region Public Functions

    public static bool IsWall(GameObject obj)
    {
        return wallObjs.Contains(obj);
    }

    #endregion
}
