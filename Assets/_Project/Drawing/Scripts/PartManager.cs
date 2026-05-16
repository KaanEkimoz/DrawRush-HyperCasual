using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PartManager : MonoBehaviour
{
    [SerializeField] private GameObject wall;
    private List<DrawPart> _childParts;
    private int _trueCounter = 0;
    void Start()
    {
        _childParts = GetComponentsInChildren<DrawPart>().ToList();
    }
    void Update()
    {
        foreach (var part in _childParts)
        {
            if (part.isDrawCompleted)
            {
                _trueCounter++;
                if (_trueCounter == _childParts.Count)
                {
                    CreateTheWall();
                }
            }
        }
    }

    private void CreateTheWall()
    {
        wall.SetActive(true);
    }
}
