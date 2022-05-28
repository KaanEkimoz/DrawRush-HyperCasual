using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Line Variables")]
    [SerializeField] private float destroyAfterSeconds = 2.0f;
    
    [SerializeField] private Material lineMaterial;
    [HideInInspector] public bool isDrawing;
    [HideInInspector] public GameObject trail = null;
    private GameObject _previousPart = null;
    private bool _canDraw;
    private void Awake()
    {
        isDrawing = false;
        _previousPart = null;
    }
    private void OnTriggerEnter(Collider other)
    {
        
        if (other.CompareTag("DrawArea"))
        {
            _canDraw = true;
        }
        else
        {
            var interactable = other.GetComponent<IInteractable>();
            if (interactable != null && _canDraw)
            {
                if (_previousPart == null)
                {
                    interactable.Interact();
                    _previousPart = other.gameObject;
                }
                else if(isDrawing)
                {
                    if (_previousPart.gameObject != other.gameObject)
                    {
                        interactable.Interact();
                        _previousPart.GetComponent<DrawPart>().isDrawCompleted = true;
                        other.gameObject.GetComponent<DrawPart>().isDrawCompleted = true;
                        LineRenderer lineRenderer = _previousPart.AddComponent(typeof(LineRenderer)) as LineRenderer;
                        AdjustLineRenderer(lineRenderer,other.gameObject.transform.position,_previousPart.transform.position);
                        Destroy(trail);
                        _previousPart = null;
                        isDrawing = false;
                        interactable = null; 
                    }
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
            _canDraw = false;
            //Currently not drawing
            isDrawing = false;
            if (_previousPart)
            {
                //Previous and Current DrawPart Resetted
                _previousPart.GetComponent<DrawPart>().isPlayerEntered = false;
                _previousPart = null;
            }
        }
    }
    
    //Changing given lineRenderer's variables
    private void AdjustLineRenderer(LineRenderer lineRenderer,Vector3 startPosition, Vector3 endPosition)
    {
        startPosition.y = 0f;
        endPosition.y = 0f;
        lineRenderer.material = lineMaterial;
        lineRenderer.startWidth = 0.4f;
        lineRenderer.endWidth = 0.4f;
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
        StartCoroutine(DestroyTheLine(lineRenderer));
    }
    private IEnumerator DestroyTheLine(LineRenderer lineRenderer)
    {
        yield return new WaitForSeconds(destroyAfterSeconds);
        Destroy(lineRenderer);
    }
}
