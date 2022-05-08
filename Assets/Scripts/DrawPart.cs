using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawPart : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject _trailPrefab;
    private GameObject _playerRef;
    private PlayerInteract _playerInteract;
    private GameObject _currTrail;
    private Transform _trailPoint;
    [HideInInspector]public bool isDrawCompleted;

    private void Awake()
    {
        isDrawCompleted = false;
        if (_playerRef == null)
        {
            _playerRef = GameObject.FindWithTag("Player");
        }
        _playerInteract = _playerRef.GetComponent<PlayerInteract>();
        _trailPoint = _playerRef.transform.GetChild(0).transform;
    }

    public void Interact()
    {
        Debug.Log("DrawPart Trigger");
        if (!isDrawCompleted)
        {
            if (_currTrail == null)
            {
                CreateTrail();
            }
            
            if (!(_playerInteract.isDrawing))
            {
                _currTrail.transform.parent = _trailPoint;
                _playerInteract.trail = _currTrail;
                _currTrail.transform.position = _trailPoint.position;
                _currTrail.SetActive(true);
                _playerInteract.isDrawing = true;
            }
            else
            {
                _playerInteract.trail.transform.parent = gameObject.transform;
                _playerInteract.trail = null;
                isDrawCompleted = true;
                _playerInteract.isDrawing = false;
            }
        }
    }
    private void CreateTrail()
    {
        _currTrail = Instantiate(_trailPrefab,gameObject.transform.position - new Vector3(0,-0.25f,0),Quaternion.Euler(91,40,38), gameObject.transform);/*
        _currTrail.transform.rotation = Quaternion.Euler(-91, 40, 38);
        _currTrail.transform.transform.position = new Vector3(_currTrail.transform.position.x, 0f, _trailPoint.position.z);
        _currTrail.transform.position = new Vector3(_currTrail.transform.position.x, 0.2f, _trailPoint.position.z);*/
        _currTrail.SetActive(false);
    }
}
