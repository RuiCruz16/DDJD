using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Player : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;

    [Header("Hunger")]
    public int maxHunger = 100;
    public int currentHunger;
    public HungerBar hungerBar;

    [Header("Thirst")]
    public int maxThirst = 100;
    public int currentThirst;
    public ThirstBar thirstBar;

    [Header("Drain Settings")]
    public int healthDrainAmount = 7; // health lost per tick
    public float healthDrainInterval = 0.8f; // interval between ticks
    private float drainTimer = 0f;

    [Header("Death Settings")]
    public Animator characterAnimator;
    public float deathDelay = 3.0f; 
    private bool isDead = false;

    [Header("Passive Decay Settings")]
    [Tooltip("How many seconds it takes to lose 1 Hunger point")]
    public float hungerDecayRate = 4f; 
    [Tooltip("How many seconds it takes to lose 1 Thirst point")]
    public float thirstDecayRate = 3f; 
    private float hungerTimer = 0f;
    private float thirstTimer = 0f;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public GameObject gameplayHUDPanel;

    [Header("Controls UI")]
    public GameObject controlsPanel;

    [Header("Visual Effects")]
    public GameObject gameOverBlurVolume;
    public CanvasGroup damageFlashGroup;
    public Volume dangerVolume;
    public float flashDuration = 0.5f;
    public float pulseSpeed = 1.5f;

    private Vignette vignetteEffect;
    private Coroutine currentFlashCoroutine;
    private PlayerDamageFMOD damageFMOD;
    private GameAmbienceFMOD gameAmbienceFMOD;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);

        currentHunger = maxHunger;
        hungerBar.SetMaxHunger(maxHunger);

        currentThirst = maxThirst;
        thirstBar.SetMaxThirst(maxThirst);

        Time.timeScale = 1f;
        if (gameOverBlurVolume != null)
            gameOverBlurVolume.SetActive(false);

        if (damageFlashGroup != null) 
            damageFlashGroup.alpha = 0f;

        if (dangerVolume != null && dangerVolume.profile.TryGet<Vignette>(out Vignette foundVignette))
        {
            vignetteEffect = foundVignette;
        }

        if (dangerVolume != null) 
            dangerVolume.weight = 0f;

        gameAmbienceFMOD = FindFirstObjectByType<GameAmbienceFMOD>();
        damageFMOD = GetComponent<PlayerDamageFMOD>();
    }

    void Update()
    {
        if (isDead) 
        {
            if (dangerVolume != null) dangerVolume.weight = 1f; 
                return; 
        }

        bool isStarving = currentHunger <= (maxHunger * 0.15f);
        bool isDehydrated = currentThirst <= (maxThirst * 0.15f);
        bool isDying = currentHealth <= (maxHealth * 0.25f);

        if ((isStarving || isDehydrated || isDying) && Time.timeScale > 0f)
        {
            if (dangerVolume != null && vignetteEffect != null)
            {
                dangerVolume.weight = Mathf.PingPong(Time.time * pulseSpeed, 1f);

                if (isDying)
                {
                    vignetteEffect.color.Override(new Color(0.6f, 0f, 0f)); 
                }
                else if (isDehydrated)
                {
                    vignetteEffect.color.Override(new Color(0.65f, 0.55f, 0.35f)); 
                }
                else if (isStarving)
                {
                    vignetteEffect.color.Override(new Color(0.25f, 0.3f, 0.25f)); 
                }
            }
        }
        else
        {
            if (dangerVolume != null)
            {
                dangerVolume.weight = 0f;
            }
        }

        // fast testing
        if (Input.GetKeyDown(KeyCode.M)) 
        {
            TakeHunger(10);
        }
        if (Input.GetKeyDown(KeyCode.N)) 
        {
            TakeThirst(10);
        }

        HandlePassiveDecay();

        if (currentHunger <= 0 || currentThirst <= 0)
        {
            drainTimer += Time.deltaTime;

            if (drainTimer >= healthDrainInterval)
            {
                TakeDamage(healthDrainAmount, false); 
                drainTimer = 0f; 
            }
        }
        else
        {
            drainTimer = 0f;
        }
    }

    private void HandlePassiveDecay()
    {
        hungerTimer += Time.deltaTime;
        if (hungerTimer >= hungerDecayRate)
        {
            TakeHunger(1);
            hungerTimer = 0f;
        }

        thirstTimer += Time.deltaTime;
        if (thirstTimer >= thirstDecayRate)
        {
            TakeThirst(1);
            thirstTimer = 0f;
        }
    }

    public void TakeDamage(int damage, bool playFlash = true)
    {
        if (isDead) return;

        currentHealth -= damage; 
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        healthBar.SetHealth(currentHealth);

        if (playFlash)
        {
            if (currentFlashCoroutine != null) StopCoroutine(currentFlashCoroutine);
            currentFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
        }

        // Play damage sound
        if (damageFMOD != null)
        {
            damageFMOD.HandleDamage();
        }

        // 

        // player died / game over
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlashCoroutine()
    {
        if (damageFlashGroup == null) yield break;

        damageFlashGroup.alpha = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            damageFlashGroup.alpha = Mathf.Lerp(0.5f, 0f, elapsedTime / flashDuration);
            yield return null;
        }

        damageFlashGroup.alpha = 0f;
    }

    public void RestoreHunger(int amount)
    {
        currentHunger += amount;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
        hungerBar.SetHunger(currentHunger);
    }

    public void RestoreThirst(int amount)
    {
        currentThirst += amount;
        currentThirst = Mathf.Clamp(currentThirst, 0, maxThirst);
        thirstBar.SetThirst(currentThirst);
    }

    void TakeHunger(int hunger)
    {
        currentHunger -= hunger;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
        hungerBar.SetHunger(currentHunger);
    }

    void TakeThirst(int thirst)
    {
        currentThirst -= thirst;
        currentThirst = Mathf.Clamp(currentThirst, 0, maxThirst);
        thirstBar.SetThirst(currentThirst);
    }

    private void Die() 
    {
        if (isDead) return;
        isDead = true;

        ThirdPersonMovement movementScript = GetComponent<ThirdPersonMovement>();
        if (movementScript != null)
        {
            movementScript.isDead = true;
        }

        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("DieTrigger");
        }

        StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        yield return new WaitForSeconds(deathDelay);

        if (currentFlashCoroutine != null) StopCoroutine(currentFlashCoroutine);
        if (damageFlashGroup != null) damageFlashGroup.alpha = 0f;

        if (gameOverBlurVolume != null) gameOverBlurVolume.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        
        if (gameAmbienceFMOD != null) gameAmbienceFMOD.StopAmbience();

        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene"); 
    }

    public void QuitGame()
    {
        Debug.Log("Quit button pressed."); 
        Application.Quit(); // does not work in unity, only after
    }
}
