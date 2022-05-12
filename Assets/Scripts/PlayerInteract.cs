using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [HideInInspector] private GameObject previousPart;
    [HideInInspector] public GameObject trail;
    [HideInInspector] public bool isDrawing;
    [SerializeField] private Material _lineMaterial;
    private bool canDraw;

    private void Awake()
    {
        isDrawing = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Player Trigger Enter");
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
                    LineRenderer lineRenderer = previousPart.AddComponent(typeof(LineRenderer)) as LineRenderer;
                    lineRenderer.material = _lineMaterial;
                    lineRenderer.SetPosition(0, previousPart.transform.position);
                    lineRenderer.SetPosition(1, other.gameObject.transform.position);
                    
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
            previousPart.GetComponent<DrawPart>().playerEntered = false;
            previousPart = null;
            Destroy(trail);
        }
        
    }
}
