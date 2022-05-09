using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static bool isGameOver;
    [SerializeField] private GameObject _gameOverPanel;
    void Start()
    {
        isGameOver = false;
    }
    void Update()
    {
        if (isGameOver)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            TrailRenderer[] trails = GameObject.FindObjectsOfType<TrailRenderer>();
            foreach (var enemy in enemies)
            {
                Destroy(enemy);
            }

            foreach (var trail in trails)
            {
                Destroy(trail);
            }
            _gameOverPanel.SetActive(true);
        }
    }
}
