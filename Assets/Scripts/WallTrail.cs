using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallTrail : MonoBehaviour, IInteractable
{
    [HideInInspector] public bool isDrawed;

    private void Start()
    {
        isDrawed = false;
    }

    public void Interact()
    {
        isDrawed = true;
    }
}
