using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    private List<WallTrail> _wallParts;

    void Start()
    {
        _wallParts = new List<WallTrail>();
        _wallParts = FindObjectsOfType<WallTrail>().ToList();
        Debug.Log(_wallParts.Count);
    }
    void Update()
    {
        int trueCounter = 0;
        foreach (var wallPart in _wallParts)
        {
            if (wallPart.isDrawed)
            {
                trueCounter++;
                if (trueCounter == _wallParts.Count)
                {
                    Debug.Log("Win");
                    GameManager.isGameOver = true;
                }
            }
        }
    }
}
