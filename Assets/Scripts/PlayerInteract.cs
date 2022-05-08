using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private GameObject previousPart;
    public GameObject trail;
    [HideInInspector] public bool isDrawing;
    private bool canDraw;

    private void Awake()
    {
        isDrawing = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DrawArea"))
        {
            canDraw = true;
        }
        var interactable = other.GetComponent<IInteractable>();
        if (interactable != null && canDraw)
        {
            interactable.Interact();
            isDrawing = true;
            if (previousPart == null)
            {
                previousPart = other.gameObject;
            }
            else
            {
                previousPart.GetComponent<DrawPart>().isDrawCompleted = true;
                other.gameObject.GetComponent<DrawPart>().isDrawCompleted = true;
                previousPart = null;
                isDrawing = false;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("DrawArea"))
        {
            canDraw = false;
            isDrawing = false;
            previousPart = null;
            Destroy(trail);
        }
        
    }
}
