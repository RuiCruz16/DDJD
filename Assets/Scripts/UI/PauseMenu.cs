using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pauseMenuPanel;
    public GameObject gameplayHUDPanel;
    public GameObject controlsCanvas;

    [Header("Visual Effects")]
    public GameObject blurVolume;

    private GameAmbienceFMOD gameAmbienceFMOD;

    public static bool isPaused = false; 

    private void Start()
    {
        gameAmbienceFMOD = FindFirstObjectByType<GameAmbienceFMOD>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Time.timeScale == 0f && !isPaused) 
                return;

            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        pauseMenuPanel.SetActive(true);
        
        if (blurVolume != null) 
            blurVolume.SetActive(true); 

        if (gameAmbienceFMOD != null)
        {
            gameAmbienceFMOD.PauseAmbience();
        }

        if (gameplayHUDPanel != null) 
        {
            gameplayHUDPanel.SetActive(false);
        }

        if (controlsCanvas != null) 
        {
            controlsCanvas.SetActive(false);
        }

        Time.timeScale = 0f; 
        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (gameAmbienceFMOD != null)
        {
            gameAmbienceFMOD.ResumeAmbience();
        }

        pauseMenuPanel.SetActive(false);
        
        if (blurVolume != null) 
            blurVolume.SetActive(false); 
        
        if (gameplayHUDPanel != null) 
        {
            gameplayHUDPanel.SetActive(true);
        }

        if (controlsCanvas != null) 
        {
            controlsCanvas.SetActive(true);
        }

        Time.timeScale = 1f; 
        isPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f; 
        isPaused = false;
        SceneManager.LoadScene("MainMenuScene"); 
    }
}