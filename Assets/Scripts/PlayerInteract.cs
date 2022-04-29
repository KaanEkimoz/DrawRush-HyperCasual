using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private GameObject _trail;
    void Start()
    {
        
    }
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        var interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            interactable.Interact();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        Debug.Log("TriggerStay");
        if (other.gameObject.CompareTag("WallTrail"))
        {
            _trail.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _trail.SetActive(false);
    }
}
