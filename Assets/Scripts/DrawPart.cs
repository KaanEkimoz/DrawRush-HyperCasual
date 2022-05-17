using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawPart : MonoBehaviour, IInteractable
{
    [HideInInspector] public bool isPlayerEntered;
    [SerializeField] private GameObject _trailPrefab;
    private GameObject _playerRef;
    private PlayerInteract _playerInteract;
    private GameObject _currTrail;
    private Transform _trailPoint;
    public bool isDrawCompleted;

    private bool isGoingToPlayer;
    private bool isReachedToPlayer;

    private void ResetDrawPart()
    {
        isPlayerEntered = false;
        isDrawCompleted = false;
        isGoingToPlayer = false;
        isReachedToPlayer = false;
    }
    private void Awake()
    {
        ResetDrawPart();
        if (_currTrail == null)
        {
            CreateTrail();
        }
        if (_playerRef == null)
        {
            _playerRef = GameObject.FindWithTag("Player");
        }
        _playerInteract = _playerRef.GetComponent<PlayerInteract>();
        _trailPoint = _playerRef.transform.GetChild(0).transform;
    }
    private void Update()
    {
        if (!isDrawCompleted)
        {
            if (_currTrail == null)
            {
                CreateTrail();
            }
            if (Math.Abs(_currTrail.transform.position.z - _trailPoint.transform.position.z) > 0.001 && isGoingToPlayer)
            {
                Debug.Log("Completing the Border Line");
                GoToPlayer();
            }
            else if (isGoingToPlayer)
            {
                isReachedToPlayer = true;
                isGoingToPlayer = false;
                Interact();
            }
        }
    }
    public void Interact()
    {
        if (_currTrail == null)
        {
            CreateTrail();
        }
        if (!isDrawCompleted && !isPlayerEntered)
        {
            if (isReachedToPlayer)
            {
                if (!(_playerInteract.isDrawing))
                {
                    Debug.Log("Started to Drawing");
                    isPlayerEntered = true;
                    _currTrail.transform.parent = _trailPoint;
                    _currTrail.transform.position = _trailPoint.position;
                    _currTrail.SetActive(true);
                    _playerInteract.trail = _currTrail;
                    _playerInteract.isDrawing = true;
                }
                else
                {
                    CompleteDraw();
                }
            }
            else
            {
                isGoingToPlayer = true;
            }
        }
    }
    private void CompleteDraw()
    {
        _playerInteract.isDrawing = false;
        _playerInteract.trail.transform.parent = gameObject.transform;
        foreach (Transform child in gameObject.transform)
        {
            Destroy(child.gameObject);
        }
        _playerInteract.trail = null;
        isDrawCompleted = true;
        isPlayerEntered = false;
    }
    private void CreateTrail()
    {
        _currTrail = Instantiate(_trailPrefab,gameObject.transform.position - new Vector3(0,-0.25f,0),Quaternion.Euler(91,40,38), gameObject.transform);
        _currTrail.SetActive(true);
    }
    
    private void GoToPlayer()
    {
        _currTrail.transform.position = new Vector3(Mathf.Lerp(_currTrail.transform.position.x, _trailPoint.position.x, 100* Time.deltaTime),
            Mathf.Lerp(_currTrail.transform.position.y, _trailPoint.position.y, 100 * Time.deltaTime), 
            Mathf.Lerp(_currTrail.transform.position.z, _trailPoint.position.z, 100 * Time.deltaTime));
    }
}
