using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static bool isGameWon;
    [SerializeField] private GameObject _winPanel, _losePanel;
    private GameObject _player;
    private int _level = 1;
    [SerializeField] private TextMeshProUGUI _levelText;
    void Start()
    {
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
            _winPanel.SetActive(true);
        }
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
