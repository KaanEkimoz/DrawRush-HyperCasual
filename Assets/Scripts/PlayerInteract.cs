using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [HideInInspector] private GameObject previousPart;
     public GameObject trail;
    [HideInInspector] public bool isDrawing;
    [SerializeField] private Material _lineMaterial;
    private bool canDraw;

    private void Awake()
    {
        isDrawing = false;
        previousPart = null;
    }
    private void OnTriggerEnter(Collider other)
    {
        
        if (other.CompareTag("DrawArea"))
        {
            canDraw = true;
            Debug.Log("Player DrawArea Trigger Enter");
        }
        else
        {
            Debug.Log("Player DrawPart Trigger Enter");
            var interactable = other.GetComponent<IInteractable>();
            if (interactable != null && canDraw)
            {
                if (previousPart == null)
                {
                    interactable.Interact();
                    previousPart = other.gameObject;
                }
                else if(isDrawing)
                {
                    interactable.Interact();
                    previousPart.GetComponent<DrawPart>().isDrawCompleted = true;
                    other.gameObject.GetComponent<DrawPart>().isDrawCompleted = true;
                    LineRenderer lineRenderer = previousPart.AddComponent(typeof(LineRenderer)) as LineRenderer;
                    AdjustLineRenderer(lineRenderer,other.gameObject.transform.position,previousPart.transform.position);
                    Destroy(trail);
                    previousPart = null;
                    isDrawing = false;
                    interactable = null;
                }
            }
        }
        
    }
    private void OnTriggerExit(Collider other)
    {
        
        if (other.CompareTag("DrawArea"))
        {
            Debug.Log("Exit from DrawArea");
            Destroy(trail);
            //When it leaves the DrawableArea
            //Can't draw
            canDraw = false;
            //Currently not drawing
            isDrawing = false;
            if (previousPart)
            {
                //Previous and Current DrawPart Resetted
                previousPart.GetComponent<DrawPart>().isPlayerEntered = false;
                previousPart = null;
            }
            
        }
    }
    private void AdjustLineRenderer(LineRenderer lineRenderer,Vector3 startPosition, Vector3 endPosition)
    {
        startPosition.y = 0f;
        endPosition.y = 0f;
        lineRenderer.material = _lineMaterial;
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
    }
}
