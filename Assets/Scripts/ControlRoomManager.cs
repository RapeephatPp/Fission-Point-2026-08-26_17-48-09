using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ControlRoomManager : MonoBehaviour
{
    [Header("Dialog System")]
    public Dialogue dialogManager;
    private bool hasPlayedIntro = false;
    private bool hasTakenFirstMeds = false;
    private bool hasEnteredFirstMinigame = false;
    private bool hasLostFirstSanity = false;

    [Header("Debug / Cheat Mode")]
    public bool enableCheatMode = true; 

    [Header("Main UI Container")]
    public RectTransform mainGameElements; 
    public float mainGameSlideOffset = -800f; 

    [Header("Control Room Focus Zoom")]
    public RectTransform controlRoomContainer;
    public float zoomScale = 1.6f;
    public float zoomDuration = 0.35f;

    [Header("End Game Panels (Updated Flow!)")]
    public GameObject gameOverPanel;
    public RectTransform incidentReportTransform;
    public TextMeshProUGUI gameOverStoryText;
    
    public GameObject winPanel;
    public TextMeshProUGUI winStoryText;
    public RectTransform winCheckTransform; 

    [Header("Restart Overlay Panel (Updated!)")]
    public GameObject restartPanel;
    public TextMeshProUGUI restartPromptText;
    public Image restartProgressBar;
    public int restartMashTarget = 10;
    public float storyTypewriterSpeed = 0.035f;
    public float restartTypewriterSpeed = 0.055f;
    public float restartTextJitterIntensity = 2.5f;

    [Header("URP Post-Processing (Juice)")]
    public Volume globalVolume;
    private ChromaticAberration chromaticAberration;
    private Vignette vignette;
    private Bloom bloom;
    private LensDistortion lensDistortion;
    private ColorAdjustments colorAdjustments;
    
    private float defaultBloomIntensity = 0f;
    private float defaultVignetteIntensity = 0.15f;

    [Header("Mini-Game UI")]
    public RectTransform cursor;
    public RectTransform greenZone;
    public RectTransform redZone;
    
    [Header("Event UI")]
    public RectTransform yellowZone;   
    public RectTransform blackoutZone; 
    public Image eventBorderImage;     

    [Header("Radiation HDR Glow Settings")]
    [Tooltip("ระดับความสว่างวาบสูงสุดของสีเหลืองแบบ HDR")]
    public float yellowGlowIntensity = 3.2f;
    [Tooltip("ความเร็วในการกระพริบสว่างวาบของแสงรังสี")]
    public float yellowPulseSpeed = 4.5f;

    // 🟢 ระบบไฟกะพริบเตือนในวันที่มีโอกาสไฟดับ
    [Header("Ambient Light Flickering (New!)")]
    [Tooltip("เปิดใช้งานระบบไฟกะพริบเตือนในวันที่ไฟดับได้ (Day 3 ขึ้นไป)")]
    public bool enableLightFlicker = true;
    [Tooltip("ระยะเวลารอต่ำสุดและสูงสุดระหว่างการกะพริบแต่ละรอบ (วินาที)")]
    public float flickerMinInterval = 8.0f;
    public float flickerMaxInterval = 16.0f;
    [Tooltip("ความมืดตอนไฟกะพริบ")]
    [Range(0.1f, 0.8f)]
    public float flickerDarkness = 0.35f;
    [Tooltip("Image แผ่นมืดสำหรับทำไฟกะพริบ (เว้นว่างได้ ระบบจะใช้ ScreenFader / Post-Processing ให้เองอัตโนมัติ)")]
    public Image lightFlickerOverlay;

    [Header("Game Info UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI sanityText;

    [Header("Sanity Bar UI")]
    [Tooltip("Image หลอด Sanity (ตั้ง Image Type เป็น Filled)")]
    public Image sanityBar;
    [Tooltip("ให้หลอดลด/เพิ่มแบบสมูทนุ่มนวล")]
    public bool smoothSanityBar = true;
    [Tooltip("ความเร็วในการวิ่งของหลอด Sanity")]
    public float sanityBarLerpSpeed = 5.0f;
    public Color sanityHighColor = new Color(0.2f, 0.85f, 0.35f, 1f);
    public Color sanityMidColor = new Color(1f, 0.75f, 0.15f, 1f);
    public Color sanityLowColor = new Color(0.95f, 0.2f, 0.2f, 1f);

    [Header("Notification UI")]
    public TextMeshProUGUI notificationText; 
    public TextMeshProUGUI tipTextTitle; 
    public TextMeshProUGUI tipTextBody; 
    public TextMeshProUGUI tipText; 
    public float notificationDuration = 2.0f; 
    public float typeWriterSpeed = 0.05f; 

    [Header("Minigame System")]
    public GameObject[] minigamePanels; 
    private GameObject currentActiveMinigame; 

    [Header("Settings")]
    public float cursorSpeed = 500f;
    public float startPointX = -400f;
    public float endPointX = 400f;
    public int currentDay = 1;
    public int maxDays = 7; 

    [Header("Sanity Settings")]
    public int maxSanity = 100;
    public int currentSanity;
    public int sanityHeal = 10;
    public int sanityDamage = 20;

    [Header("Difficulty & Spawn Settings")]
    public float redShrinkRate = 25f;      
    public float greenShrinkRate = 10f;    
    public float minGreenWidth = 15f;
    public float minSpawnDistance = 150f;
    public float spawnDuration = 0.5f;
    
    public float redSpawnDelayMin = 2.0f;     
    public float redSpawnDelayMax = 5.0f;     
    public float greenSpawnInterval = 4.0f; 

    [Header("Forgiveness Mechanics")]
    public float greenHitboxMultiplier = 1.5f; 
    public float greenLifeTime = 3.0f;         

    [Header("Juice (Effects)")]
    public Transform shakeTarget;
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 10f;
    public Image damageFlashImage;     
    public float hitPauseDuration = 0.05f; 
    public float cursorBumpScale = 1.5f;   
    public float cursorBumpTime = 0.1f;    

    // --- State Variables ---
    private float timeRemaining;
    private bool isGameActive = true;
    private bool isMinigameActive = false;
    private bool isTutorialPhase = true;

    private float initialGreenWidth;
    private float initialRedWidth;
    private float initialYellowWidth = 60f;
    private float initialBlackoutWidth = 100f;
    private Vector3 originalShakePos;
    private Vector3 originalCursorScale;
    private Vector2 originalMainGamePos; 
    private Vector2 originalContainerPos;
    private Vector3 originalContainerScale;
    private Vector2 originalIncidentReportPos; 
    private Vector2 originalRestartPromptPos;

    private Image yellowZoneImage;
    private Color originalYellowColor = Color.yellow;
    private float targetSanityFill = 1f;

    private bool isGreenSpawning = false;
    private bool isRedSpawning = false;
    private bool isYellowSpawning = false;
    private bool isBlackoutSpawning = false;
    
    private float greenTimer = 0f;
    private float eventTimer = 0f;
    private float nextEventDelay = 12f; 
    private int lastEventId = -1;       
    private bool isMashingBlackout = false;
    private int blackoutMashCount = 0;
    private int requiredBlackoutMash = 5;
    private bool isGlitching = false;
    private bool isZonesMoving = false;
    private bool isDamageFlashing = false;

    // ตัวแปรตัวจับเวลาไฟกะพริบ
    private float flickerTimer = 0f;
    private float nextFlickerDelay = 10f;

    // --- Tracking Stats ---
    private int totalGreenStabilized = 0;
    private int totalMinigamesWon = 0;
    private int totalMinigamesFailed = 0;
    private int totalBlackoutsFixed = 0;
    private int totalRadiationLeaksHit = 0;

    // --- End Game State ---
    private bool isEndGameScreenActive = false;
    private bool canMashRestart = false;
    private int restartCurrentMash = 0;

    private Coroutine greenSpawnCoroutine;
    private Coroutine redSpawnCoroutine;
    private Coroutine yellowSpawnCoroutine;
    private Coroutine blackoutSpawnCoroutine;
    private Coroutine shakeCoroutine;
    private Coroutine cursorBumpCoroutine;
    private Coroutine notificationCoroutine;
    private Coroutine eventBorderCoroutine;
    private Coroutine radiationPulseCoroutine;
    private Coroutine glitchCoroutine;
    private Coroutine movingZonesCoroutine;
    private Coroutine restartTextShakeCoroutine;
    private Coroutine lightFlickerCoroutine;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out chromaticAberration);
            globalVolume.profile.TryGet(out vignette);
            globalVolume.profile.TryGet(out bloom);
            globalVolume.profile.TryGet(out lensDistortion);
            globalVolume.profile.TryGet(out colorAdjustments);
            
            if (bloom != null) defaultBloomIntensity = (float)bloom.intensity.value;
            if (vignette != null) defaultVignetteIntensity = (float)vignette.intensity.value;
        }

        currentSanity = maxSanity;
        timeRemaining = GetTimeForDay(currentDay); 

        initialGreenWidth = greenZone.rect.width;
        initialRedWidth = redZone.rect.width;
        if (yellowZone != null) 
        {
            initialYellowWidth = yellowZone.rect.width;
            yellowZoneImage = yellowZone.GetComponent<Image>();
            if (yellowZoneImage != null) originalYellowColor = yellowZoneImage.color;
        }
        if (blackoutZone != null) initialBlackoutWidth = blackoutZone.rect.width;

        if (shakeTarget != null) originalShakePos = shakeTarget.localPosition;
        if (cursor != null) originalCursorScale = cursor.localScale;
        
        if (mainGameElements != null) originalMainGamePos = mainGameElements.anchoredPosition;

        if (controlRoomContainer != null)
        {
            originalContainerPos = controlRoomContainer.anchoredPosition;
            originalContainerScale = controlRoomContainer.localScale;
        }

        if (incidentReportTransform != null)
        {
            originalIncidentReportPos = incidentReportTransform.anchoredPosition;
            incidentReportTransform.gameObject.SetActive(false);
        }

        if (restartPromptText != null)
        {
            originalRestartPromptPos = restartPromptText.rectTransform.anchoredPosition;
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (restartPanel != null) restartPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (winCheckTransform != null) winCheckTransform.gameObject.SetActive(false);

        if (damageFlashImage != null) { Color c = damageFlashImage.color; c.a = 0f; damageFlashImage.color = c; }
        if (eventBorderImage != null) { Color c = eventBorderImage.color; c.a = 0f; eventBorderImage.color = c; }
        if (lightFlickerOverlay != null) { Color c = lightFlickerOverlay.color; c.a = 0f; lightFlickerOverlay.color = c; }
        
        if (notificationText != null) { Color c = notificationText.color; c.a = 0f; notificationText.color = c; notificationText.gameObject.SetActive(false); }
        if (tipTextTitle != null) { Color c = tipTextTitle.color; c.a = 0f; tipTextTitle.color = c; tipTextTitle.gameObject.SetActive(false); }
        if (tipTextBody != null) { Color c = tipTextBody.color; c.a = 0f; tipTextBody.color = c; tipTextBody.gameObject.SetActive(false); }
        if (tipText != null) { Color c = tipText.color; c.a = 0f; tipText.color = c; tipText.gameObject.SetActive(false); }

        if (yellowZone != null) yellowZone.sizeDelta = new Vector2(0, yellowZone.sizeDelta.y);
        if (blackoutZone != null) blackoutZone.sizeDelta = new Vector2(0, blackoutZone.sizeDelta.y);

        if (sanityBar != null)
        {
            targetSanityFill = Mathf.Clamp01((float)currentSanity / maxSanity);
            sanityBar.fillAmount = targetSanityFill;
            UpdateSanityBarVisual(targetSanityFill);
        }

        UpdateDayUI();
        UpdateSanityUI();

        greenZone.anchoredPosition = new Vector2(startPointX + 100f, greenZone.anchoredPosition.y);
        redZone.anchoredPosition = new Vector2(endPointX - 100f, redZone.anchoredPosition.y);

        TriggerRespawn(greenZone, initialGreenWidth, redZone, 1, true, false);
        TriggerRespawn(redZone, initialRedWidth, greenZone, 2, true, false);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayAmbient("ambientLoop");
        
        ShowNotification("DAY " + currentDay + "\nSURVIVE THE MELTDOWN", "TIP: Press Space on GREEN to heal, RED to engage minigame.");
        PlayDailyIntroDialog(currentDay);
    }

    void Update()
    {
        if (isEndGameScreenActive)
        {
            HandleRestartMashing();
            return;
        }

        if (enableCheatMode) HandleDebugKeys();
        if (!isGameActive) return;

        UpdateTimer();
        UpdateSanityPostProcessing();
        UpdateSanityBarAnimation();
        HandleAmbientLightFlicker();

        if (isMinigameActive) return;

        if (!isMashingBlackout) 
        {
            MoveCursorLoop();
            MoveZones(); 
        }

        ShrinkBoxes();
        HandleGreenLifeTime(); 
        HandleRandomEvents();

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("clickSound");
            TriggerCursorBump(); 
            CheckHitZone();
        }
    }

    // 🟢 ตรวจสอบและรันไฟกะพริบในวันที่มีโอกาสเกิดไฟดับ (Day 3 ขึ้นไป)
    private void HandleAmbientLightFlicker()
    {
        if (!enableLightFlicker || currentDay < 3 || !isGameActive || isMinigameActive || isMashingBlackout) return;

        flickerTimer += Time.deltaTime;
        if (flickerTimer >= nextFlickerDelay)
        {
            flickerTimer = 0f;
            nextFlickerDelay = Random.Range(flickerMinInterval, flickerMaxInterval);
            if (lightFlickerCoroutine != null) StopCoroutine(lightFlickerCoroutine);
            lightFlickerCoroutine = StartCoroutine(SubtleLightFlickerRoutine());
        }
    }

    private IEnumerator SubtleLightFlickerRoutine()
    {
        int flickCount = Random.Range(2, 4);
        for (int i = 0; i < flickCount; i++)
        {
            if (isMashingBlackout || !isGameActive) yield break;

            SetFlickerDarkness(flickerDarkness);
            if (AudioManager.Instance != null && Random.value > 0.5f) AudioManager.Instance.PlaySFX("clickSound");
            yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.08f));

            SetFlickerDarkness(0f);
            yield return new WaitForSecondsRealtime(Random.Range(0.05f, 0.12f));
        }
        SetFlickerDarkness(0f);
        lightFlickerCoroutine = null;
    }

    private void SetFlickerDarkness(float darkness)
    {
        if (lightFlickerOverlay != null)
        {
            Color c = lightFlickerOverlay.color;
            c.a = darkness;
            lightFlickerOverlay.color = c;
        }
        else if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = -darkness * 4.0f;
        }
        else if (ScreenFader.Instance != null && ScreenFader.Instance.fadeImageGroup != null && !isMashingBlackout)
        {
            ScreenFader.Instance.fadeImageGroup.alpha = darkness;
        }
    }

    private void UpdateSanityBarAnimation()
    {
        if (sanityBar == null) return;

        if (smoothSanityBar)
        {
            sanityBar.fillAmount = Mathf.MoveTowards(sanityBar.fillAmount, targetSanityFill, Time.unscaledDeltaTime * sanityBarLerpSpeed);
        }
        else
        {
            sanityBar.fillAmount = targetSanityFill;
        }

        UpdateSanityBarVisual(sanityBar.fillAmount);
    }

    private void UpdateSanityBarVisual(float currentFill)
    {
        if (sanityBar == null) return;

        Color targetColor;
        if (currentFill > 0.5f)
        {
            targetColor = Color.Lerp(sanityMidColor, sanityHighColor, (currentFill - 0.5f) * 2f);
        }
        else
        {
            targetColor = Color.Lerp(sanityLowColor, sanityMidColor, currentFill * 2f);
        }

        if (currentFill <= 0.25f && isGameActive)
        {
            float pulse = Mathf.PingPong(Time.unscaledTime * 7f, 0.4f);
            targetColor = Color.Lerp(targetColor, Color.white, pulse);
        }

        sanityBar.color = targetColor;
    }

    private void HandleRestartMashing()
    {
        if (!canMashRestart) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            restartCurrentMash++;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("clickSound");
            TriggerShake();

            float progress = Mathf.Clamp01((float)restartCurrentMash / restartMashTarget);
            if (restartProgressBar != null) restartProgressBar.fillAmount = progress;
            if (restartPromptText != null) restartPromptText.text = $"REBOOTING CORE... [{restartCurrentMash} / {restartMashTarget}]";

            if (restartCurrentMash >= restartMashTarget)
            {
                canMashRestart = false;
                StartCoroutine(RestartRoutine());
            }
        }
    }

    private IEnumerator RestartRoutine()
    {
        if (restartTextShakeCoroutine != null) StopCoroutine(restartTextShakeCoroutine);
        if (restartPromptText != null) 
        {
            restartPromptText.rectTransform.anchoredPosition = originalRestartPromptPos;
            restartPromptText.text = "SYSTEM REBOOT CONFIRMED";
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("dayChangeSound");

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.fadeDuration = 1.0f;
            yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(1.0f));
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private float GetBaselineCA()
    {
        float sanityLossRatio = 1f - Mathf.Clamp01((float)currentSanity / maxSanity);
        return Mathf.Lerp(0.1f, 0.32f, sanityLossRatio); 
    }

    private float GetBaselineVignette()
    {
        float sanityLossRatio = 1f - Mathf.Clamp01((float)currentSanity / maxSanity);
        return Mathf.Lerp(defaultVignetteIntensity, defaultVignetteIntensity + 0.12f, sanityLossRatio);
    }

    private void UpdateSanityPostProcessing()
    {
        if (!isGlitching && !isDamageFlashing && radiationPulseCoroutine == null && chromaticAberration != null)
        {
            chromaticAberration.intensity.value = Mathf.MoveTowards(chromaticAberration.intensity.value, GetBaselineCA(), Time.deltaTime * 1.2f);
        }

        if (!isMashingBlackout && !isDamageFlashing && vignette != null)
        {
            vignette.intensity.value = Mathf.MoveTowards(vignette.intensity.value, GetBaselineVignette(), Time.deltaTime * 1.2f);
        }

        if (!isZonesMoving && !isGlitching && lensDistortion != null)
        {
            lensDistortion.intensity.value = Mathf.MoveTowards(lensDistortion.intensity.value, 0f, Time.deltaTime * 2.0f);
        }
    }

    private void HandleDebugKeys()
    {
        if (Input.GetKeyDown(KeyCode.F1)) { timeRemaining = 0f; }
        if (Input.GetKeyDown(KeyCode.F2)) { currentSanity = maxSanity; UpdateSanityUI(); }
        if (Input.GetKeyDown(KeyCode.F3)) { if (!isMinigameActive && isGameActive) EnterMinigame(); }
        if (Input.GetKeyDown(KeyCode.F4)) { if (!isMinigameActive && isGameActive && !isMashingBlackout) TriggerRandomEvent(true); }
    }

    private void MoveCursorLoop()
    {
        float speed = cursorSpeed;
        
        if (isGlitching) 
        {
            speed = Random.Range(-cursorSpeed * 0.5f, cursorSpeed * 1.5f);
            if (Random.value > 0.85f) cursor.localScale = originalCursorScale * Random.Range(0.85f, 1.15f);
        }
        else
        {
            if (cursor.localScale != originalCursorScale && cursorBumpCoroutine == null) 
                cursor.localScale = Vector3.Lerp(cursor.localScale, originalCursorScale, Time.deltaTime * 10f);
        }

        cursor.anchoredPosition += new Vector2(speed * Time.deltaTime, 0);
        
        if (cursor.anchoredPosition.x >= endPointX) cursor.anchoredPosition = new Vector2(startPointX, cursor.anchoredPosition.y);
        else if (cursor.anchoredPosition.x <= startPointX) cursor.anchoredPosition = new Vector2(endPointX, cursor.anchoredPosition.y);
    }

    private void MoveZones()
    {
        if (isZonesMoving)
        {
            float offset = Mathf.Sin(Time.time * 5f) * 80f * Time.deltaTime; 
            if (greenZone.rect.width > 0) greenZone.anchoredPosition += new Vector2(offset, 0);
            if (redZone.rect.width > 0) redZone.anchoredPosition -= new Vector2(offset, 0);
            if (yellowZone != null && yellowZone.rect.width > 0) yellowZone.anchoredPosition += new Vector2(offset, 0);
        }
    }

    private void HandleRandomEvents()
    {
        if (isTutorialPhase || currentDay <= 1 || isMashingBlackout) return;

        eventTimer += Time.deltaTime;
        if (eventTimer > nextEventDelay)
        {
            eventTimer = 0f;
            nextEventDelay = Random.Range(12.0f, 18.0f); 
            TriggerRandomEvent(false);
        }
    }

    private void TriggerRandomEvent(bool forceEvent)
    {
        int simulatedDay = forceEvent ? 7 : currentDay; 
        List<int> availableEvents = new List<int>();
        
        if (simulatedDay >= 3 && simulatedDay <= 6 && blackoutZone != null && blackoutZone.rect.width <= 0) availableEvents.Add(1);
        if (simulatedDay >= 4 && yellowZone != null && yellowZone.rect.width <= 0) availableEvents.Add(2);
        availableEvents.Add(3);
        availableEvents.Add(4);

        if (availableEvents.Count > 1 && availableEvents.Contains(lastEventId))
        {
            availableEvents.Remove(lastEventId);
        }

        int chosenEvent = availableEvents[Random.Range(0, availableEvents.Count)];
        lastEventId = chosenEvent; 

        switch (chosenEvent)
        {
            case 1:
                // 🟢 ไฟดับ: ตัด Notification และขอบแดงออก ให้จอดับวูบมืดสนิททันที
                TriggerRespawn(blackoutZone, initialBlackoutWidth, redZone, 4, false, false);
                float oldMag = shakeMagnitude; shakeMagnitude = 18f; TriggerShake(); shakeMagnitude = oldMag;
                
                SetFlickerDarkness(0f); // ล้างค่ากะพริบเดิม
                if (ScreenFader.Instance != null)
                {
                    ScreenFader.Instance.fadeDuration = 0.2f; // ดับวูบอย่างรวดเร็ว
                    StartCoroutine(ScreenFader.Instance.FadeRoutine(0.96f)); // เกือบมืดสนิท
                }
                if (vignette != null) vignette.intensity.value = 0.6f;
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("explosionSound");
                break;

            case 2:
                TriggerRespawn(yellowZone, initialYellowWidth, redZone, 3, false, false);
                ShowEventWarning("WARNING: RADIATION LEAK", "TIP: Do NOT touch the YELLOW zone!");
                if (radiationPulseCoroutine != null) StopCoroutine(radiationPulseCoroutine);
                radiationPulseCoroutine = StartCoroutine(RadiationPulseRoutine());
                break;
            case 3:
                if (glitchCoroutine != null) StopCoroutine(glitchCoroutine);
                glitchCoroutine = StartCoroutine(GlitchRoutine());
                break;
            case 4:
                if (movingZonesCoroutine != null) StopCoroutine(movingZonesCoroutine);
                movingZonesCoroutine = StartCoroutine(MovingZonesRoutine());
                break;
        }
    }

    private IEnumerator RadiationPulseRoutine()
    {
        if (yellowZoneImage == null && yellowZone != null)
            yellowZoneImage = yellowZone.GetComponent<Image>();

        while (yellowZone != null && yellowZone.rect.width > 0)
        {
            if (chromaticAberration != null && !isGlitching && !isDamageFlashing)
            {
                chromaticAberration.intensity.value = GetBaselineCA() + Mathf.PingPong(Time.time * 1.5f, 0.08f);
            }

            if (yellowZoneImage != null)
            {
                float wave = Mathf.PingPong(Time.time * yellowPulseSpeed, 1f);
                float currentIntensity = Mathf.Lerp(1.0f, yellowGlowIntensity, wave);

                yellowZoneImage.color = new Color(
                    originalYellowColor.r * currentIntensity,
                    originalYellowColor.g * currentIntensity,
                    originalYellowColor.b * currentIntensity,
                    originalYellowColor.a
                );
            }

            yield return null;
        }

        if (yellowZoneImage != null)
        {
            yellowZoneImage.color = originalYellowColor;
        }
        radiationPulseCoroutine = null;
    }

    private IEnumerator GlitchRoutine()
    {
        isGlitching = true;
        ShowEventWarning("SYSTEM GLITCH", "TIP: Cursor speed is corrupted. Rely on your reflexes!");

        float duration = 2.0f; 
        float timer = 0f;
        float interval = 0.08f;
        WaitForSeconds waitInterval = new WaitForSeconds(interval);

        while (timer < duration)
        {
            timer += interval;

            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = Random.Range(0.25f, 0.45f);
            }
            if (lensDistortion != null)
            {
                lensDistortion.intensity.value = Random.Range(-0.03f, 0.03f);
            }
            yield return waitInterval;
        }

        StopGlitchImmediately();
    }

    private void StopGlitchImmediately()
    {
        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
            glitchCoroutine = null;
        }
        isGlitching = false;
        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
        if (chromaticAberration != null) chromaticAberration.intensity.value = GetBaselineCA();
        if (cursor != null) cursor.localScale = originalCursorScale;
    }

    private IEnumerator MovingZonesRoutine()
    {
        isZonesMoving = true;
        ShowEventWarning("UNSTABLE PRESSURE", "TIP: Targets are drifting. Anticipate their movement!");
        
        float elapsed = 0f;
        while (elapsed < 0.6f) 
        {
            elapsed += Time.deltaTime;
            if (lensDistortion != null) lensDistortion.intensity.value = Mathf.Lerp(0f, -0.08f, elapsed / 0.6f);
            yield return null;
        }

        yield return new WaitForSeconds(2.5f);

        elapsed = 0f;
        while (elapsed < 0.6f) 
        {
            elapsed += Time.deltaTime;
            if (lensDistortion != null) lensDistortion.intensity.value = Mathf.Lerp(-0.08f, 0f, elapsed / 0.6f);
            yield return null;
        }

        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
        isZonesMoving = false;
        movingZonesCoroutine = null;
    }

    private float GetTimeForDay(int day)
    {
        if (day <= 4) return 60f;        
        if (day == 5) return 90f;        
        if (day == 6) return 105f;       
        if (day >= 7) return 120f;       
        return 60f;
    }

    private void UpdateTimer()
    {
        if (isTutorialPhase)
        {
            if (timerText != null) timerText.text = "Time: " + Mathf.Ceil(timeRemaining).ToString("00");
            return; 
        }

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            if (timerText != null) timerText.text = "Time: " + Mathf.Ceil(timeRemaining).ToString("00");
        }
        else
        {
            AdvanceToNextDay();
        }
    }

    private void AdvanceToNextDay()
    {
        if (currentDay >= maxDays) { WinGame(); return; }
        StartCoroutine(DayTransitionRoutine());
    }

    private IEnumerator DayTransitionRoutine()
    {
        isGameActive = false;
        
        if (ScreenFader.Instance != null) 
        {
            ScreenFader.Instance.fadeDuration = 1.5f; 
            yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(1f));
        }

        isMashingBlackout = false;
        StopGlitchImmediately();

        if (lightFlickerCoroutine != null) { StopCoroutine(lightFlickerCoroutine); lightFlickerCoroutine = null; }
        SetFlickerDarkness(0f);

        if (movingZonesCoroutine != null) { StopCoroutine(movingZonesCoroutine); movingZonesCoroutine = null; }
        isZonesMoving = false;
        if (radiationPulseCoroutine != null) { StopCoroutine(radiationPulseCoroutine); radiationPulseCoroutine = null; }

        if (yellowZoneImage != null) yellowZoneImage.color = originalYellowColor;
        if (yellowZone != null) yellowZone.sizeDelta = new Vector2(0, yellowZone.sizeDelta.y);
        if (blackoutZone != null) blackoutZone.sizeDelta = new Vector2(0, blackoutZone.sizeDelta.y);
        if (eventBorderImage != null) { Color cb = eventBorderImage.color; cb.a = 0f; eventBorderImage.color = cb; }
        
        if (chromaticAberration != null) chromaticAberration.intensity.value = GetBaselineCA();
        if (vignette != null) vignette.intensity.value = GetBaselineVignette();
        if (lensDistortion != null) lensDistortion.intensity.value = 0f;

        if (controlRoomContainer != null)
        {
            controlRoomContainer.anchoredPosition = originalContainerPos;
            controlRoomContainer.localScale = originalContainerScale;
        }

        currentDay++;
        timeRemaining = GetTimeForDay(currentDay);
        
        cursorSpeed += 25f;
        redSpawnDelayMin = Mathf.Max(1.0f, redSpawnDelayMin - 0.15f);
        redSpawnDelayMax = Mathf.Max(2.5f, redSpawnDelayMax - 0.35f);

        UpdateDayUI();
        
        TriggerRespawn(greenZone, initialGreenWidth, redZone, 1, true, false);
        TriggerRespawn(redZone, initialRedWidth, greenZone, 2, true, false);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("dayChangeSound");

        yield return new WaitForSecondsRealtime(1.0f);

        if (ScreenFader.Instance != null) 
        {
            ScreenFader.Instance.fadeDuration = 0.5f; 
            yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(0f));
        }

        isGameActive = true;
        ShowNotification("DAY " + currentDay, "TIP: The system is getting faster. Stay focused.");
        PlayDailyIntroDialog(currentDay);
    }

    private void UpdateDayUI() { if (dayText != null) dayText.text = "Day: " + currentDay + "/" + maxDays; }

    private void UpdateSanityUI() 
    { 
        if (sanityText != null) sanityText.text = "Sanity: " + currentSanity + "/" + maxSanity; 
        targetSanityFill = Mathf.Clamp01((float)currentSanity / maxSanity);

        if (!smoothSanityBar && sanityBar != null)
        {
            sanityBar.fillAmount = targetSanityFill;
            UpdateSanityBarVisual(targetSanityFill);
        }
    }

    private void HandleGreenLifeTime()
    {
        if (isGreenSpawning || greenZone.rect.width <= 0) return;
        greenTimer += Time.deltaTime;
        
        float lifeTime = isTutorialPhase ? greenLifeTime * 2f : greenLifeTime;
        if (greenTimer >= lifeTime) TriggerRespawn(greenZone, initialGreenWidth, redZone, 1, false, true);
    }

    private void ShrinkBoxes()
    {
        float currentGreenShrink = isTutorialPhase ? greenShrinkRate * 0.3f : greenShrinkRate;
        float currentRedShrink = isTutorialPhase ? redShrinkRate * 0.3f : redShrinkRate;

        if (!isGreenSpawning && greenZone.rect.width > minGreenWidth)
        {
            greenZone.sizeDelta = new Vector2(Mathf.Max(greenZone.rect.width - (currentGreenShrink * Time.deltaTime), minGreenWidth), greenZone.sizeDelta.y);
        }

        if (!isRedSpawning && redZone.rect.width > 0)
        {
            float newRedWidth = redZone.rect.width - (currentRedShrink * Time.deltaTime);
            if (newRedWidth <= 0) OnRedZoneDisappeared();
            else redZone.sizeDelta = new Vector2(newRedWidth, redZone.sizeDelta.y);
        }

        if (!isYellowSpawning && yellowZone != null && yellowZone.rect.width > 0)
        {
            float newYelWidth = yellowZone.rect.width - (currentGreenShrink * Time.deltaTime);
            if (newYelWidth <= 0) 
            {
                StartCoroutine(FadeOutAndHideRoutine(yellowZone, 3)); 
            }
            else yellowZone.sizeDelta = new Vector2(newYelWidth, yellowZone.sizeDelta.y);
        }

        if (!isBlackoutSpawning && blackoutZone != null && blackoutZone.rect.width > 0)
        {
            float newBlkWidth = blackoutZone.rect.width - (currentRedShrink * 0.7f * Time.deltaTime);
            if (newBlkWidth <= 0)
            {
                isMashingBlackout = false;
                currentSanity -= sanityDamage;
                TriggerShake();
                StartCoroutine(FlashDamageScreen()); 
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("explosionSound");
                
                PlaySanityLossDialog();
                StartCoroutine(FadeOutAndHideRoutine(blackoutZone, 4)); 
                if (ScreenFader.Instance != null)
                {
                    ScreenFader.Instance.fadeDuration = 0.5f;
                    StartCoroutine(ScreenFader.Instance.FadeRoutine(0f)); 
                }
                UpdateSanityUI();
                CheckGameOver();
            }
            else blackoutZone.sizeDelta = new Vector2(newBlkWidth, blackoutZone.sizeDelta.y);
        }
    }

    private void OnRedZoneDisappeared()
    {
        currentSanity -= isTutorialPhase ? 1 : sanityDamage;
        totalMinigamesFailed++;
        TriggerShake();
        StartCoroutine(FlashDamageScreen()); 
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("explosionSound");

        PlaySanityLossDialog();
        UpdateSanityUI();
        CheckGameOver();
        if (isGameActive) TriggerRespawn(redZone, initialRedWidth, greenZone, 2, false, true);
    }

    private void TriggerRespawn(RectTransform zoneToSpawn, float targetWidth, RectTransform otherZone, int zoneType, bool spawnImmediately, bool doFadeOut)
    {
        if (zoneType == 1 && greenSpawnCoroutine != null) StopCoroutine(greenSpawnCoroutine);
        else if (zoneType == 2 && redSpawnCoroutine != null) StopCoroutine(redSpawnCoroutine);
        else if (zoneType == 3 && yellowSpawnCoroutine != null) StopCoroutine(yellowSpawnCoroutine);
        else if (zoneType == 4 && blackoutSpawnCoroutine != null) StopCoroutine(blackoutSpawnCoroutine);

        Coroutine newRoutine = StartCoroutine(GradualSpawnRoutine(zoneToSpawn, targetWidth, otherZone, zoneType, spawnImmediately, doFadeOut));

        if (zoneType == 1) greenSpawnCoroutine = newRoutine;
        else if (zoneType == 2) redSpawnCoroutine = newRoutine;
        else if (zoneType == 3) yellowSpawnCoroutine = newRoutine;
        else if (zoneType == 4) blackoutSpawnCoroutine = newRoutine;
    }

    private IEnumerator GradualSpawnRoutine(RectTransform zone, float targetWidth, RectTransform otherZone, int zoneType, bool spawnImmediately, bool doFadeOut)
    {
        if (zoneType == 1) { isGreenSpawning = true; greenTimer = 0f; }
        else if (zoneType == 2) isRedSpawning = true;
        else if (zoneType == 3) isYellowSpawning = true;
        else if (zoneType == 4) isBlackoutSpawning = true;

        CanvasGroup cg = zone.GetComponent<CanvasGroup>();
        if (cg == null) cg = zone.gameObject.AddComponent<CanvasGroup>();

        if (doFadeOut && zone.rect.width > 0)
        {
            float fadeOutDur = 0.2f;
            float elapsedOut = 0f;
            float startWidth = zone.rect.width;
            
            while(elapsedOut < fadeOutDur)
            {
                if (!isMinigameActive)
                {
                    elapsedOut += Time.deltaTime;
                    float t = elapsedOut / fadeOutDur;
                    zone.sizeDelta = new Vector2(Mathf.Lerp(startWidth, 0, t), zone.sizeDelta.y);
                    cg.alpha = Mathf.Lerp(1f, 0f, t);
                }
                yield return null;
            }
        }

        zone.sizeDelta = new Vector2(0, zone.sizeDelta.y);
        cg.alpha = 0f;

        if (!spawnImmediately)
        {
            float waitTime = (zoneType == 1) ? greenSpawnInterval : Random.Range(redSpawnDelayMin, redSpawnDelayMax);
            float currentWaitTimer = 0f;
            while (currentWaitTimer < waitTime)
            {
                if (!isMinigameActive) currentWaitTimer += Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitUntil(() => !isMinigameActive);

        float halfWidth = targetWidth / 2f;
        float newX = 0f;
        for (int i = 0; i < 15; i++)
        {
            newX = Random.Range(startPointX + halfWidth, endPointX - halfWidth);
            if (Mathf.Abs(newX - otherZone.anchoredPosition.x) >= minSpawnDistance) break;
        }

        zone.anchoredPosition = new Vector2(newX, zone.anchoredPosition.y);

        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            if (!isMinigameActive && !isMashingBlackout)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / spawnDuration;
                zone.sizeDelta = new Vector2(Mathf.Lerp(0, targetWidth, t), zone.sizeDelta.y);
                cg.alpha = Mathf.Lerp(0f, 1f, t);
            }
            yield return null;
        }

        zone.sizeDelta = new Vector2(targetWidth, zone.sizeDelta.y);
        cg.alpha = 1f;

        if (zoneType == 1) isGreenSpawning = false;
        else if (zoneType == 2) isRedSpawning = false;
        else if (zoneType == 3) isYellowSpawning = false;
        else if (zoneType == 4) isBlackoutSpawning = false;
    }

    private IEnumerator FadeOutAndHideRoutine(RectTransform zone, int zoneType)
    {
        if (zoneType == 3) 
        {
            isYellowSpawning = true;
            if (radiationPulseCoroutine != null) { StopCoroutine(radiationPulseCoroutine); radiationPulseCoroutine = null; }
            if (yellowZoneImage != null) yellowZoneImage.color = originalYellowColor;
        }
        else if (zoneType == 4) 
        {
            isBlackoutSpawning = true;
        }

        CanvasGroup cg = zone.GetComponent<CanvasGroup>();
        if (cg == null) cg = zone.gameObject.AddComponent<CanvasGroup>();

        float fadeOutDur = 0.2f;
        float elapsedOut = 0f;
        float startWidth = zone.rect.width;
        
        while(elapsedOut < fadeOutDur)
        {
            if (!isMinigameActive)
            {
                elapsedOut += Time.deltaTime;
                float t = elapsedOut / fadeOutDur;
                zone.sizeDelta = new Vector2(Mathf.Lerp(startWidth, 0, t), zone.sizeDelta.y);
                cg.alpha = Mathf.Lerp(1f, 0f, t);
            }
            yield return null;
        }

        zone.sizeDelta = new Vector2(0, zone.sizeDelta.y);
        cg.alpha = 0f;

        if (zoneType == 3) isYellowSpawning = false;
        else if (zoneType == 4) isBlackoutSpawning = false;
    }

    private void CheckHitZone()
    {
        float cursorX = cursor.anchoredPosition.x;
        StartCoroutine(HitPauseRoutine()); 

        if (isMashingBlackout)
        {
            blackoutMashCount++;
            float mashProgress = (float)blackoutMashCount / requiredBlackoutMash;
            
            if (ScreenFader.Instance != null) 
                StartCoroutine(ScreenFader.Instance.FadeRoutine(0.96f - (0.96f * mashProgress)));
            
            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(0.6f, GetBaselineVignette(), mashProgress);
            
            if (blackoutMashCount >= requiredBlackoutMash)
            {
                isMashingBlackout = false;
                totalBlackoutsFixed++;
                StartCoroutine(FadeOutAndHideRoutine(blackoutZone, 4));
                currentSanity += sanityHeal;
                if (currentSanity > maxSanity) currentSanity = maxSanity;
                
                ShowNotification("POWER RESTORED", "TIP: Great job! Stay alert.");
                if (ScreenFader.Instance != null)
                {
                    ScreenFader.Instance.fadeDuration = 0.5f;
                    StartCoroutine(ScreenFader.Instance.FadeRoutine(0f));
                }
            }
            return; 
        }

        if (blackoutZone != null && blackoutZone.rect.width > 0 && IsInsideZone(cursorX, blackoutZone, 1.2f))
        {
            isMashingBlackout = true;
            blackoutMashCount = 1;
            return;
        }

        if (yellowZone != null && yellowZone.rect.width > 0 && IsInsideZone(cursorX, yellowZone, 1.0f))
        {
            totalRadiationLeaksHit++;
            currentSanity -= sanityDamage;
            TriggerShake();
            StartCoroutine(FlashDamageScreen()); 
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("missSound");
            PlaySanityLossDialog();
            StartCoroutine(FadeOutAndHideRoutine(yellowZone, 3));
            UpdateSanityUI();
            CheckGameOver();
            return;
        }

        if (IsInsideZone(cursorX, greenZone, greenHitboxMultiplier))
        {
            totalGreenStabilized++;
            currentSanity += isTutorialPhase ? 1 : sanityHeal;
            if (currentSanity > maxSanity) currentSanity = maxSanity;
            if (isTutorialPhase) { isTutorialPhase = false; ShowNotification("SYSTEM ONLINE", "TIP: Maintain stability until the end of the shift."); }
            
            TriggerRespawn(greenZone, initialGreenWidth, redZone, 1, false, true);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("hitSound");

            if (dialogManager != null)
            {
                if (!hasTakenFirstMeds)
                {
                    dialogManager.StartDialog("Me", new string[] { "Hello, my trusty old pills. It's been a while." });
                    hasTakenFirstMeds = true;
                }
                else
                {
                    string[] genericMeds = {
                        "Still tastes like crap.",
                        "That helps a lot.",
                        "Much better.",
                        "My mind feels so much clearer now."
                    };

                    dialogManager.StartDialog("Me", new string[] { genericMeds[Random.Range(0, genericMeds.Length)] });
                }
            }
        }
        else if (IsInsideZone(cursorX, redZone, 1.0f)) 
        {
            if (isTutorialPhase) { isTutorialPhase = false; }
            TriggerRespawn(redZone, initialRedWidth, greenZone, 2, false, true);
            EnterMinigame();
        }
        else
        {
            currentSanity -= isTutorialPhase ? 1 : (sanityDamage / 4);
            TriggerShake();
            StartCoroutine(FlashDamageScreen()); 
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("missSound");
            PlaySanityLossDialog();
        }

        UpdateSanityUI();
        CheckGameOver();
    }

    private void EnterMinigame()
    {
        if (!hasEnteredFirstMinigame && dialogManager != null)
        {
            dialogManager.StartDialog("Me", new string[] { "Here we go. Let's see what we're dealing with." });
            hasEnteredFirstMinigame = true;
        }
        isMinigameActive = true;
        StopGlitchImmediately();

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("minigameTransitionInSound");

        if (minigamePanels != null && minigamePanels.Length > 0)
        {
            currentActiveMinigame = minigamePanels[Random.Range(0, minigamePanels.Length)];
            StartCoroutine(SwitchToMinigameAnimation());
        }
    }

    private IEnumerator SwitchToMinigameAnimation()
    {
        float elapsed = 0f;
        float duration = zoomDuration;

        Vector2 targetContainerPos = originalContainerPos;
        Vector3 targetContainerScale = originalContainerScale * zoomScale;

        if (controlRoomContainer != null && currentActiveMinigame != null)
        {
            Vector3 targetLocalPos = controlRoomContainer.InverseTransformPoint(currentActiveMinigame.transform.position);
            targetContainerPos = originalContainerPos - (Vector2)targetLocalPos * zoomScale;
        }

        Vector2 startMainPos = mainGameElements != null ? mainGameElements.anchoredPosition : Vector2.zero;
        Vector2 targetMainPos = originalMainGamePos + new Vector2(0, mainGameSlideOffset);

        Vector2 startContainerPos = controlRoomContainer != null ? controlRoomContainer.anchoredPosition : Vector2.zero;
        Vector3 startContainerScale = controlRoomContainer != null ? controlRoomContainer.localScale : Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);

            if (mainGameElements != null)
            {
                mainGameElements.anchoredPosition = Vector2.Lerp(startMainPos, targetMainPos, t);
            }

            if (controlRoomContainer != null)
            {
                controlRoomContainer.anchoredPosition = Vector2.Lerp(startContainerPos, targetContainerPos, t);
                controlRoomContainer.localScale = Vector3.Lerp(startContainerScale, targetContainerScale, t);
            }

            yield return null;
        }

        if (mainGameElements != null) mainGameElements.anchoredPosition = targetMainPos;
        if (controlRoomContainer != null)
        {
            controlRoomContainer.anchoredPosition = targetContainerPos;
            controlRoomContainer.localScale = targetContainerScale;
        }

        if (currentActiveMinigame != null)
        {
            currentActiveMinigame.BroadcastMessage("StartMinigame", SendMessageOptions.DontRequireReceiver);
        }
    }

    public void FinishMinigame(bool isSuccess)
    {
        if (isSuccess) totalMinigamesWon++;
        else totalMinigamesFailed++;

        StartCoroutine(SwitchBackToMainAnimation(isSuccess));
    }

    private IEnumerator SwitchBackToMainAnimation(bool isSuccess)
    {
        float elapsed = 0f;
        float duration = zoomDuration;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(isSuccess ? "minigameWinSound" : "minigameLoseSound");

        if (currentActiveMinigame != null)
        {
            currentActiveMinigame.BroadcastMessage("IdleMinigame", SendMessageOptions.DontRequireReceiver);
            currentActiveMinigame = null;
        }

        Vector2 startMainPos = mainGameElements != null ? mainGameElements.anchoredPosition : Vector2.zero;
        Vector2 startContainerPos = controlRoomContainer != null ? controlRoomContainer.anchoredPosition : Vector2.zero;
        Vector3 startContainerScale = controlRoomContainer != null ? controlRoomContainer.localScale : Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);

            if (mainGameElements != null)
            {
                mainGameElements.anchoredPosition = Vector2.Lerp(startMainPos, originalMainGamePos, t);
            }

            if (controlRoomContainer != null)
            {
                controlRoomContainer.anchoredPosition = Vector2.Lerp(startContainerPos, originalContainerPos, t);
                controlRoomContainer.localScale = Vector3.Lerp(startContainerScale, originalContainerScale, t);
            }

            yield return null;
        }

        if (mainGameElements != null) mainGameElements.anchoredPosition = originalMainGamePos;
        if (controlRoomContainer != null)
        {
            controlRoomContainer.anchoredPosition = originalContainerPos;
            controlRoomContainer.localScale = originalContainerScale;
        }

        if (!isSuccess)
        {
            currentSanity -= isTutorialPhase ? 1 : sanityDamage;
            TriggerShake();
            StartCoroutine(FlashDamageScreen());

            PlaySanityLossDialog();
        }
        
        UpdateSanityUI();
        CheckGameOver();
        isMinigameActive = false; 
    }

    private void CheckGameOver()
    {
        if (currentSanity <= 0 && isGameActive)
        {
            isGameActive = false;
            isEndGameScreenActive = true;
            if (AudioManager.Instance != null) 
            { 
                AudioManager.Instance.StopAmbient(); 
                AudioManager.Instance.PlaySFX("gameOverSound"); 
            }
            StartCoroutine(GameOverSequence());
        }
    }

    private IEnumerator GameOverSequence()
    {
        yield return new WaitForSeconds(0.2f);

        if (mainGameElements != null) mainGameElements.gameObject.SetActive(false);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            CanvasGroup bgCg = gameOverPanel.GetComponent<CanvasGroup>();
            if (bgCg == null) bgCg = gameOverPanel.AddComponent<CanvasGroup>();
            bgCg.alpha = 0f;

            float fadeDuration = 0.5f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                bgCg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            bgCg.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(0.15f);

        if (incidentReportTransform != null)
        {
            incidentReportTransform.gameObject.SetActive(true);
            Vector2 startPos = originalIncidentReportPos + new Vector2(0, -1200f);
            incidentReportTransform.anchoredPosition = startPos;

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("clickSound");

            float slideDur = 0.45f;
            float elapsed = 0f;
            while (elapsed < slideDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / slideDur;
                t = 1f - Mathf.Pow(1f - t, 3f);
                incidentReportTransform.anchoredPosition = Vector2.Lerp(startPos, originalIncidentReportPos, t);
                yield return null;
            }
            incidentReportTransform.anchoredPosition = originalIncidentReportPos;
            TriggerShake();
        }

        string reportStory = GenerateGameOverStory();
        if (gameOverStoryText != null)
        {
            yield return StartCoroutine(TypewriteStory(gameOverStoryText, reportStory, storyTypewriterSpeed));
        }

        yield return StartCoroutine(WaitForClickOrTimeout(5.0f));
        yield return StartCoroutine(ShowRestartPanelRoutine());
    }

    private void WinGame()
    {
        if (!isGameActive) return;
        isGameActive = false;
        isEndGameScreenActive = true;
        timerText.text = "Time: 00";

        StartCoroutine(WinSequence());
    }

    private IEnumerator WinSequence()
    {
        yield return new WaitForSeconds(0.4f);

        if (mainGameElements != null) mainGameElements.gameObject.SetActive(false);
        if (winPanel != null) winPanel.SetActive(true);
        if (winCheckTransform != null) winCheckTransform.gameObject.SetActive(false);

        string story = GenerateWinStory();
        if (winStoryText != null)
        {
            yield return StartCoroutine(TypewriteStory(winStoryText, story, storyTypewriterSpeed));
        }

        if (restartPromptText != null) restartPromptText.text = "PRESS [SPACE] OR [CLICK] TO CLAIM REWARD";
        yield return new WaitForSecondsRealtime(0.25f);

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0));

        if (winCheckTransform != null)
        {
            yield return StartCoroutine(PopCheckInRoutine());
        }

        yield return StartCoroutine(WaitForClickOrTimeout(5.0f));
        yield return StartCoroutine(ShowRestartPanelRoutine());
    }

    private IEnumerator WaitForClickOrTimeout(float timeoutDuration)
    {
        yield return new WaitForSecondsRealtime(0.2f); 
        float timer = 0f;
        while (timer < timeoutDuration)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                yield break;
            }
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator ShowRestartPanelRoutine()
    {
        if (restartPanel != null)
        {
            restartPanel.SetActive(true);
            CanvasGroup rstCg = restartPanel.GetComponent<CanvasGroup>();
            if (rstCg == null) rstCg = restartPanel.AddComponent<CanvasGroup>();

            rstCg.alpha = 0f;
            float rstFade = 0.35f;
            float elapsed = 0f;
            while (elapsed < rstFade)
            {
                elapsed += Time.unscaledDeltaTime;
                rstCg.alpha = Mathf.Lerp(0f, 1f, elapsed / rstFade);
                yield return null;
            }
            rstCg.alpha = 1f;
        }

        if (restartPromptText != null)
        {
            if (restartTextShakeCoroutine != null) StopCoroutine(restartTextShakeCoroutine);
            restartTextShakeCoroutine = StartCoroutine(JitterTextRoutine(restartPromptText, originalRestartPromptPos, restartTextJitterIntensity));

            string prompt = "MASH [SPACE] OR [CLICK] TO REBOOT";
            yield return StartCoroutine(TypewriteStory(restartPromptText, prompt, restartTypewriterSpeed));
        }

        canMashRestart = true;
    }

    private IEnumerator JitterTextRoutine(TextMeshProUGUI textElement, Vector2 originalPos, float intensity)
    {
        while (textElement != null && textElement.gameObject.activeInHierarchy)
        {
            Vector2 jitter = Random.insideUnitCircle * intensity;
            textElement.rectTransform.anchoredPosition = originalPos + jitter;
            yield return new WaitForSecondsRealtime(0.03f);
        }
        if (textElement != null) textElement.rectTransform.anchoredPosition = originalPos;
    }

    private IEnumerator PopCheckInRoutine()
    {
        winCheckTransform.gameObject.SetActive(true);
        winCheckTransform.localScale = Vector3.zero;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("hitSound");

        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float scale = Mathf.LerpUnclamped(0f, 1f, t * t * (2.70158f * t - 1.70158f) + 1f);
            winCheckTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        winCheckTransform.localScale = Vector3.one;
        TriggerShake();
    }

    private string GenerateGameOverStory()
    {
        return $"[INCIDENT REPORT - REACTOR FAILURE]\n\n" +
               $"Location: Core Containment Sector\n" +
               $"Day of Failure: Day {currentDay} / {maxDays}\n\n" +
               $"Description:\n" +
               $"The operator experienced acute cognitive exhaustion. " +
               $"Core pressure calibrations failed {totalMinigamesFailed} times. " +
               $"Radiation shielding sustained {totalRadiationLeaksHit} direct breaches. " +
               $"Power grid collapsed following {totalBlackoutsFixed} recovered generators.\n\n" +
               $"Conclusion: Containment breached. Reactor core lost to complete thermal meltdown.";
    }

    private string GenerateWinStory()
    {
        return $"[PAYROLL & VALOR COMMENDATION]\n\n" +
               $"Seven consecutive days of core stabilization successfully completed.\n\n" +
               $"• Pressure Cycles Kept: {totalGreenStabilized}\n" +
               $"• Systems Repaired: {totalMinigamesWon}\n" +
               $"• Blackouts Countered: {totalBlackoutsFixed}\n" +
               $"• Operator Mental Reserve: {currentSanity}%\n\n" +
               $"Containment intact. Hazard bonus wired directly from Illutio Co.";
    }

    private IEnumerator TypewriteStory(TextMeshProUGUI targetText, string fullText, float speed)
    {
        targetText.text = fullText;
        targetText.maxVisibleCharacters = 0;

        for (int i = 0; i <= fullText.Length; i++)
        {
            targetText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(speed);
        }
    }

    private bool IsInsideZone(float xPos, RectTransform zone, float hitboxMultiplier)
    {
        if (zone.rect.width <= 0) return false;
        float halfWidth = (zone.rect.width * hitboxMultiplier) / 2f; 
        return xPos >= (zone.anchoredPosition.x - halfWidth) && xPos <= (zone.anchoredPosition.x + halfWidth);
    }

    private void ShowEventWarning(string message, string tip = "")
    {
        ShowNotification(message, tip);
        if (eventBorderImage != null) StartCoroutine(PulseEventBorder());
    }

    // 🟢 ฟังก์ชัน PulseEventBorder สำหรับกะพริบขอบแดงในอีเวนต์อื่น
    private IEnumerator PulseEventBorder()
    {
        if (eventBorderImage == null) yield break;
        Color c = eventBorderImage.color;
        float elapsed = 0f;
        while(elapsed < 2.5f) 
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.PingPong(elapsed * 4f, 0.7f); 
            eventBorderImage.color = c;
            yield return null;
        }
        c.a = 0f;
        eventBorderImage.color = c;
    }

    private void ShowNotification(string message, string tip = "")
    {
        ParseTipText(tip, out string title, out string body);
        ShowNotification(message, title, body);
    }

    private void ShowNotification(string message, string title, string body)
    {
        if (notificationCoroutine != null) StopCoroutine(notificationCoroutine);
        notificationCoroutine = StartCoroutine(NotificationRoutine(message, title, body));
    }

    private void ParseTipText(string rawTip, out string title, out string body)
    {
        if (string.IsNullOrEmpty(rawTip))
        {
            title = "";
            body = "";
            return;
        }

        int colonIndex = rawTip.IndexOf(':');
        if (colonIndex >= 0)
        {
            title = rawTip.Substring(0, colonIndex).Trim();
            body = rawTip.Substring(colonIndex + 1).Trim();
        }
        else
        {
            title = "TIP";
            body = rawTip.Trim();
        }
    }

    private IEnumerator NotificationRoutine(string message, string title, string body)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.maxVisibleCharacters = 0;
            notificationText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            Color c = notificationText.color; c.a = 1f; notificationText.color = c;
        }

        if (tipTextTitle != null)
        {
            tipTextTitle.text = title;
            tipTextTitle.maxVisibleCharacters = 0;
            tipTextTitle.gameObject.SetActive(!string.IsNullOrEmpty(title));
            Color c = tipTextTitle.color; c.a = 1f; tipTextTitle.color = c;
        }

        if (tipTextBody != null)
        {
            tipTextBody.text = body;
            tipTextBody.maxVisibleCharacters = 0;
            tipTextBody.gameObject.SetActive(!string.IsNullOrEmpty(body));
            Color c = tipTextBody.color; c.a = 1f; tipTextBody.color = c;
        }

        if (tipText != null)
        {
            string combined = string.IsNullOrEmpty(title) ? body : $"{title}: {body}";
            tipText.text = combined;
            tipText.maxVisibleCharacters = 0;
            tipText.gameObject.SetActive(!string.IsNullOrEmpty(combined));
            Color c = tipText.color; c.a = 1f; tipText.color = c;
        }

        if (notificationText != null && !string.IsNullOrEmpty(message))
        {
            for (int i = 0; i <= message.Length; i++)
            {
                notificationText.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(typeWriterSpeed);
            }
        }

        if (tipTextTitle != null && !string.IsNullOrEmpty(title))
        {
            yield return new WaitForSecondsRealtime(0.12f);
            for (int i = 0; i <= title.Length; i++)
            {
                tipTextTitle.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(typeWriterSpeed * 0.6f);
            }
        }

        if (tipTextBody != null && !string.IsNullOrEmpty(body))
        {
            yield return new WaitForSecondsRealtime(0.08f);
            for (int i = 0; i <= body.Length; i++)
            {
                tipTextBody.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(typeWriterSpeed * 0.5f);
            }
        }

        if (tipText != null && (tipTextTitle == null && tipTextBody == null) && !string.IsNullOrEmpty(tipText.text))
        {
            yield return new WaitForSecondsRealtime(0.15f);
            for (int i = 0; i <= tipText.text.Length; i++)
            {
                tipText.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(typeWriterSpeed * 0.5f);
            }
        }

        yield return new WaitForSecondsRealtime(notificationDuration);

        float fadeTime = 0.3f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            
            if (notificationText != null) { Color c = notificationText.color; c.a = alpha; notificationText.color = c; }
            if (tipTextTitle != null) { Color c = tipTextTitle.color; c.a = alpha; tipTextTitle.color = c; }
            if (tipTextBody != null) { Color c = tipTextBody.color; c.a = alpha; tipTextBody.color = c; }
            if (tipText != null) { Color c = tipText.color; c.a = alpha; tipText.color = c; }
            yield return null;
        }
        
        if (notificationText != null) notificationText.gameObject.SetActive(false);
        if (tipTextTitle != null) tipTextTitle.gameObject.SetActive(false);
        if (tipTextBody != null) tipTextBody.gameObject.SetActive(false);
        if (tipText != null) tipText.gameObject.SetActive(false);
    }

    private void TriggerShake()
    {
        if (shakeTarget != null) { if (shakeCoroutine != null) StopCoroutine(shakeCoroutine); shakeCoroutine = StartCoroutine(ShakeEffect()); }
    }

    private IEnumerator ShakeEffect()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            shakeTarget.localPosition = originalShakePos + new Vector3(x, y, 0);
            elapsed += Time.unscaledDeltaTime; 
            yield return null;
        }
        shakeTarget.localPosition = originalShakePos;
    }

    private void PlayDailyIntroDialog(int day)
    {
        if (dialogManager == null) return;

        string[] lines = null;

        switch (day)
        {
            case 1:
                if (!hasPlayedIntro)
                {
                    lines = new string[] {
                        "Dammit. I ended up coming back here again.",
                        "Even though I swore I'd never step foot in this place again.",
                        "What choice do I have? I need the cash.",
                        "I swear, this is the last time. Alright... just 7 days."
                    };
                    hasPlayedIntro = true;
                }
                break;
            case 2:
                lines = new string[] {
                    "Day 2. My head is pounding.",
                    "Let's just get this over with."
                };
                break;
            case 3:
                lines = new string[] {
                    "Third day.",
                    "The system is getting more unstable. Or maybe it's just me."
                };
                break;
            case 4:
                lines = new string[] {
                    "Day 4. Halfway there.",
                    "Just keep the core from melting. Simple, right?"
                };
                break;
            case 5:
                lines = new string[] {
                    "Day 5. Is the AC broken, or is the core actually melting?",
                    "It's getting damn hot in here.",
                    "My hands won't stop shaking. Where are those pills...",
                    "Just swallow it down and focus. Two more days."
                };
                break;
            case 6:
                lines = new string[] {
                    "Day 6. The console is literally burning my fingers.",
                    "Alarms ringing non-stop. I took a double dose today, but my head is still splitting.",
                    "Hold it together... Don't lose your mind now."
                };
                break;
            case 7:
                lines = new string[] {
                    "Day 7. The last day.",
                    "I don't care anymore! Just survive this shift and get the hell out of here!"
                };
                break;
        }

        if (lines != null)
        {
            dialogManager.StartDialog("Me", lines);
        }
    }

    private void PlaySanityLossDialog()
    {
        if (dialogManager == null) return;

        if (!hasLostFirstSanity)
        {       
            dialogManager.StartDialog("Me", new string[] { "Ugh... my head hurts" });
            hasLostFirstSanity = true;
        }
        else
        {
            string[] sanityLossLines = {
                "Focus, damn it!", 
                "These alarms are driving me crazy.", 
                "This is bad...", 
                "I can't take this!" 
            };
            dialogManager.StartDialog("Me", new string[] { sanityLossLines[Random.Range(0, sanityLossLines.Length)] });
        }
    }

    private IEnumerator HitPauseRoutine()
    {
        Time.timeScale = 0f; 
        yield return new WaitForSecondsRealtime(hitPauseDuration);
        Time.timeScale = 1f; 
    }

    private void TriggerCursorBump()
    {
        if (cursor != null) { if (cursorBumpCoroutine != null) StopCoroutine(cursorBumpCoroutine); cursorBumpCoroutine = StartCoroutine(CursorBumpRoutine()); }
    }

    private IEnumerator CursorBumpRoutine()
    {
        cursor.localScale = originalCursorScale * cursorBumpScale;
        float elapsed = 0f;
        while (elapsed < cursorBumpTime)
        {
            elapsed += Time.unscaledDeltaTime;
            cursor.localScale = Vector3.Lerp(originalCursorScale * cursorBumpScale, originalCursorScale, elapsed / cursorBumpTime);
            yield return null;
        }
        cursor.localScale = originalCursorScale;
    }

    private IEnumerator FlashDamageScreen()
    {
        isDamageFlashing = true;

        float targetBaseCA = GetBaselineCA();
        float targetBaseVig = GetBaselineVignette();

        if (vignette != null) vignette.intensity.value = Mathf.Max(targetBaseVig + 0.12f, 0.35f);
        if (chromaticAberration != null) chromaticAberration.intensity.value = Mathf.Max(targetBaseCA + 0.15f, 0.4f);

        if (damageFlashImage != null) 
        {
            Color c = damageFlashImage.color;
            c.a = 0.35f; 
            damageFlashImage.color = c;
        }

        float flashDuration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / flashDuration;
            
            if (damageFlashImage != null)
            {
                Color c = damageFlashImage.color;
                c.a = Mathf.Lerp(0.35f, 0f, t);
                damageFlashImage.color = c;
            }
            
            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(Mathf.Max(targetBaseVig + 0.12f, 0.35f), targetBaseVig, t);
            }
            
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = Mathf.Lerp(Mathf.Max(targetBaseCA + 0.15f, 0.4f), targetBaseCA, t);
            }
            
            yield return null;
        }

        isDamageFlashing = false;
    }
}