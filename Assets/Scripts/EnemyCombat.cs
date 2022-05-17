using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public Animator enemyAnim;
    private int damage = 1;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("EnemyTriggerEnter");
        if (other.gameObject.CompareTag("Player"))
        {
            //Deal damage to Enemy
        }
    }
}
