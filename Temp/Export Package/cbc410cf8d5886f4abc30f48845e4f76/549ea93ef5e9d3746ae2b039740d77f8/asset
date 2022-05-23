using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyFollow : MonoBehaviour
{
    private NavMeshAgent _enemyNavMeshAgent;
    private Transform _playerTransform;
    private void Start()
    {
        _enemyNavMeshAgent = GetComponent<NavMeshAgent>();
        _playerTransform = GameObject.FindWithTag("Player").GetComponent<Transform>();
    }

    private void Update()
    {
        if (_playerTransform)
        {
            _enemyNavMeshAgent.SetDestination(_playerTransform.position);
        }
        
    }
}
