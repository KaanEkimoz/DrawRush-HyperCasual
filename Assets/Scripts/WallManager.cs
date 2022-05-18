using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    private List<DrawPart> _drawParts;
    void Start()
    {
        _drawParts = new List<DrawPart>();
        _drawParts = FindObjectsOfType<DrawPart>().ToList();
    }
    void Update()
    {
        int trueCounter = 0;
        foreach (var part in _drawParts)
        {
            if (part.isDrawCompleted)
            {
                trueCounter++;
                if (trueCounter == _drawParts.Count)
                {
                    GameManager.isGameWon = true;
                }
            }
        }
    }
    IEnumerator WaitForAnim()
    {
        yield return new WaitForSeconds(1.5f);
    }
}
