using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTrapCreater : MonoBehaviour
{
    [Header("Player Grid")]
    [SerializeField] private int gridWidth;
    [SerializeField] private int gridHeight;
    [SerializeField] private float cellSize;
    [SerializeField] private Vector3 originPosition;
    
    private Grid playerGrid;
    void Start()
    {
        playerGrid = new Grid(gridWidth, gridHeight,cellSize, originPosition);
    }

    private void Update()
    {
        if (ThirdPersonMovement.Moving)
        {
            playerGrid.SetValue(transform.position,1);
        }
    }

    void SendASignal()
    {
        //Send a signal from a "1" value cell.
    }

    void Pathfinding()
    {
        //After signal try to find a path
    }
}
