using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    private int healthPoint = 3;
    [SerializeField] private TextMeshProUGUI playerHp;

    private void Start()
    {
        healthPoint = 3;
    }

    private void Update()
    {
        if (healthPoint <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        healthPoint += damage;
        playerHp.text = healthPoint.ToString();
    }
}
