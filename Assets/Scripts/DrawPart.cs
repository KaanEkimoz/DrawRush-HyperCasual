using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawPart : MonoBehaviour, IInteractable
{
    [HideInInspector] public bool playerEntered;
    [SerializeField] private GameObject _trailPrefab;
    private GameObject _playerRef;
    private PlayerInteract _playerInteract;
    private GameObject _currTrail;
    private Transform _trailPoint;
    public bool isDrawCompleted;

    private void Awake()
    {
        playerEntered = false;
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
        if (!isDrawCompleted && !playerEntered)
        {
            playerEntered = true;
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
                foreach (Transform child in gameObject.transform)
                {
                    Destroy(child.gameObject);
                }
                _playerInteract.trail = null;
                isDrawCompleted = true;
                _playerInteract.isDrawing = false;
                playerEntered = false;
            }
        }
    }
    private void CreateTrail()
    {
        _currTrail = Instantiate(_trailPrefab,gameObject.transform.position - new Vector3(0,-0.25f,0),Quaternion.Euler(91,40,38), gameObject.transform);
        _currTrail.SetActive(false);
    }
}
