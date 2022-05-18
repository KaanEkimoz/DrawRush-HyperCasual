using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    public Animator enemyAnim;

    private void Awake()
    {
        if (enemyAnim == null)
        {
            enemyAnim = GetComponentInChildren<Animator>();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("EnemyTriggerEnter");
        if (other.gameObject.CompareTag("Player"))
        {
            Attack();
        }
    }

    private void Attack()
    {
        PlayerCombat.healthPoint -= damage;
        GameManager.playerHP.text = PlayerCombat.healthPoint.ToString();
    }
}
