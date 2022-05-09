using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [SerializeField] private Animator _enemyAnim;
    private int damage = 1;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("EnemyTriggerEnter");
        if (other.gameObject.CompareTag("Player"))
        {
            _enemyAnim.SetTrigger("t_attack");
            PlayerCombat.healthPoint -= damage;
        }
    }
}
