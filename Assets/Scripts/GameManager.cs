using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static bool isGameWon;
    [SerializeField] private GameObject _winPanel, _losePanel, _gameUI;
    private GameObject _player;
    private int _level = 1;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private GameObject _particles;
    private bool waitEnds;

    void Start()
    {
        _gameUI.SetActive(true);
        _winPanel.SetActive(false);
        _losePanel.SetActive(false);
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
            _losePanel.SetActive(true);
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
            Invoke("GameWon",3.0f);
        }
    }

    private void GameWon()
    {
        _winPanel.SetActive(true);
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
    #endregion
}
