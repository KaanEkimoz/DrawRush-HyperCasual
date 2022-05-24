using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    [Header("UI Panel Elements"), Space] 
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject gameUI;
    [Header("Others")]
   
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private GameObject _particles;
    
    private TextMeshProUGUI playerHP;
    public static bool isGameWon;
    private GameObject _player;
    private int _level = 1;
    private bool waitEnds;
    private static List<int> randomLevelList = new List<int>{1,2,3};

    private void Awake()
    {
        Time.timeScale = 0.0f;
    }

    void Start()
    {
        gameUI.SetActive(true);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        _level = PlayerPrefs.GetInt("Level", 1);
        isGameWon = false;
        _player = GameObject.FindWithTag("Player");
        _levelText.text = "Level " + _level;
    }
    void Update()
    {
        if (!_player)
        {
            Time.timeScale = 0.0f;
            losePanel.SetActive(true);
        }
        if (isGameWon)
        {
            EnemyCombat[] enemies = GameObject.FindObjectsOfType<EnemyCombat>();
            TrailRenderer[] trails = GameObject.FindObjectsOfType<TrailRenderer>();
            
            foreach (var enemy in enemies)
            {
                enemy.enemyAnim.SetTrigger("t_die");
            }
            foreach (var trail in trails)
            {
                Destroy(trail);
            }
            LineRenderer[] lines = GameObject.FindObjectsOfType<LineRenderer>();
            foreach (var line in lines)
            {
                Destroy(line);
            }
            _particles.SetActive(true);
            Invoke(nameof(GameWon),3.0f);
        }
    }

    public void StartTheGame()
    {
        Time.timeScale = 1.0f;
    }

    private void GameWon()
    {
        winPanel.SetActive(true);
    }
    
    #region SceneManagement
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextLevel()
    {
        PlayerPrefs.SetInt("Level", PlayerPrefs.GetInt("Level") + 1);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    public void LoadRandomLevel()
    {
        PlayerPrefs.SetInt("Level", PlayerPrefs.GetInt("Level") + 1);
        int randomInt = Random.Range(1, 5);
        while (randomInt == SceneManager.GetActiveScene().buildIndex)
        {
            randomInt = Random.Range(1, 5);
        }

        if (randomLevelList.Count != 0)
        {
            randomLevelList.Remove(randomInt);
        }
        else
        {
            randomLevelList.Add(1);
            randomLevelList.Add(2);
            randomLevelList.Add(3);
        }
        SceneManager.LoadScene(randomInt);
    }
    #endregion
}
