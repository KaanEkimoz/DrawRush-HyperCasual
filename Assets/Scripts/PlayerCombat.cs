using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public static int healthPoint = 3;

    private void Update()
    {
        if (healthPoint <= 0)
        {
            Destroy(gameObject);
        }
    }
}