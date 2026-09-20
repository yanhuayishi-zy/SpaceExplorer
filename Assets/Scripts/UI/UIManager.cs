using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    
    [Header("游戏界面")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI waveText;
    public Image[] healthImages;
    
    [Header("暂停界面")]
    public GameObject pausePanel;
    
    [Header("游戏结束界面")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalHighScoreText;
    
    [Header("胜利界面")]
    public GameObject victoryPanel;
    
    [Header("波次信息")]
    public GameObject waveInfoPanel;
    public TextMeshProUGUI waveInfoText;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 初始化UI
        UpdateScore(0);
        int savedHigh = GameManager.Instance != null
            ? GameManager.Instance.highScore
            : PlayerPrefs.GetInt(ZodiacLevels.IsCampaign ? "HighScore_Campaign" : "HighScore_Endless", 0);
        UpdateHighScore(savedHigh);
        
        // 隐藏所有面板
        HideAllPanels();
    }
    
    void Update()
    {
        // 更新分数显示
        if (GameManager.Instance != null)
        {
            UpdateScore(GameManager.Instance.score);
        }
    }
    
    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = "分数: " + score.ToString();
        }
    }
    
    public void UpdateHighScore(int highScore)
    {
        if (highScoreText != null)
        {
            highScoreText.text = "最高分: " + highScore.ToString();
        }
    }
    
    public void UpdateWave(int waveNumber)
    {
        if (waveText != null)
        {
            waveText.text = "波次: " + waveNumber.ToString();
        }
    }
    
    public void ShowPauseMenu(bool show)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(show);
        }
    }
    
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (finalScoreText != null)
            {
                finalScoreText.text = "最终分数: " + GameManager.Instance.score.ToString();
            }
            
            if (finalHighScoreText != null)
            {
                finalHighScoreText.text = "最高分: " + GameManager.Instance.highScore.ToString();
            }
        }
    }
    
    public void ShowVictory()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
    }
    
    public void ShowWaveInfo(string waveName)
    {
        if (waveInfoPanel != null)
        {
            waveInfoPanel.SetActive(true);
            waveInfoText.text = waveName;
            
            // 3秒后隐藏
            Invoke("HideWaveInfo", 3f);
        }
    }
    
    void HideWaveInfo()
    {
        if (waveInfoPanel != null)
        {
            waveInfoPanel.SetActive(false);
        }
    }
    
    void HideAllPanels()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (waveInfoPanel != null) waveInfoPanel.SetActive(false);
    }
    
    // 按钮回调方法
    public void OnResumeButton()
    {
        GameManager.Instance.PauseGame();
    }
    
    public void OnRestartButton()
    {
        GameManager.Instance.RestartGame();
    }
    
    public void OnMainMenuButton()
    {
        GameManager.Instance.ReturnToMainMenu();
    }
    
    public void OnNextLevelButton()
    {
        GameManager.Instance.NextLevel();
    }
    
    public void OnQuitButton()
    {
        GameManager.Instance.QuitGame();
    }
}
