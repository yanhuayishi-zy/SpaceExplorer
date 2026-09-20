using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("游戏状态")]
    public bool isGameOver = false;
    public bool isPaused = false;
    
    [Header("分数设置")]
    public int score = 0;
    public int highScore = 0;
    public int killCount = 0;
    
    [Header("关卡设置")]
    public int currentLevel = 1;
    public int maxLevel = 3;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        LoadHighScore();
    }
    
    void Update()
    {
        // 暂停功能
        if (!MobileGameShell.HandlesPauseHotkey
            && !StoryUI.IsPlaying
            && Input.GetKeyDown(KeyCode.Escape)
            && !isGameOver)
        {
            PauseGame();
        }
    }
    
    public void AddScore(int scoreAmount)
    {
        score += scoreAmount;
        UpdateHighScore();
    }

    /// 新一局开始：清空分数/击杀（最高分保留）
    public void ResetRunStats()
    {
        score = 0;
        killCount = 0;
        LoadHighScore();
        isGameOver = false;
        isPaused = false;
        if (ComboBombSystem.Instance != null) ComboBombSystem.Instance.BreakCombo();
        Time.timeScale = 1f;
    }

    public void AddKill(int amount = 1)
    {
        killCount += amount;
    }
    
    void UpdateHighScore()
    {
        if (score > highScore)
        {
            highScore = score;
            SaveHighScore();
        }
    }
    
    void SaveHighScore()
    {
        PlayerPrefs.SetInt(HighScoreKey, highScore);
        PlayerPrefs.Save();
    }
    
    void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    string HighScoreKey => ZodiacLevels.IsCampaign ? "HighScore_Campaign" : "HighScore_Endless";
    
    public void PauseGame()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0 : 1;
        
        // 更新UI
        if (UIManager.Instance != null) UIManager.Instance.ShowPauseMenu(isPaused);
    }
    
    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        Time.timeScale = 0f;

        // 只弹可点的结算板，避免结束动画挡住按钮
        MobileGameShell.ShowGameOverBoard();
    }
    
    public void RestartGame()
    {
        isGameOver = false;
        isPaused = false;
        score = 0;
        killCount = 0;
        if (ComboBombSystem.Instance != null) ComboBombSystem.Instance.BreakCombo();
        Time.timeScale = 1;
        AutoAudio.ResetBgmVolume();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void LoadLevel(int levelIndex)
    {
        currentLevel = levelIndex;
        SceneManager.LoadScene("Level_" + levelIndex);
    }
    
    public void NextLevel()
    {
        if (currentLevel < maxLevel)
        {
            LoadLevel(currentLevel + 1);
        }
        else
        {
            // 游戏通关
            UIManager.Instance.ShowVictory();
        }
    }
    
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
