using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlRoomManager : MonoBehaviour
{
    [Header("Mini-Game UI")]
    public RectTransform cursor;
    public RectTransform greenZone;
    public RectTransform redZone;
    
    [Header("Event UI (New Mechanics)")]
    public RectTransform yellowZone;   // กล่องรังสี (ห้ามกด)
    public RectTransform blackoutZone; // กล่องปั่นไฟ (กดรัวๆ)
    public Image eventBorderImage;     // ขอบจอเตือนภัยสีแดง

    [Header("Game Info UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI sanityText;

    [Header("Notification UI")]
    public TextMeshProUGUI notificationText; 
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

    private bool isGreenSpawning = false;
    private bool isRedSpawning = false;
    private bool isYellowSpawning = false;
    private bool isBlackoutSpawning = false;
    
    private float greenTimer = 0f;
    
    // --- Random Event Variables ---
    private float eventTimer = 0f;
    private bool isMashingBlackout = false;
    private int blackoutMashCount = 0;
    private int requiredBlackoutMash = 5;
    private bool isGlitching = false;
    private bool isZonesMoving = false;

    // --- Coroutines ---
    private Coroutine greenSpawnCoroutine;
    private Coroutine redSpawnCoroutine;
    private Coroutine yellowSpawnCoroutine;
    private Coroutine blackoutSpawnCoroutine;
    private Coroutine shakeCoroutine;
    private Coroutine cursorBumpCoroutine;
    private Coroutine notificationCoroutine;
    private Coroutine eventBorderCoroutine;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        currentSanity = maxSanity;
        timeRemaining = GetTimeForDay(currentDay); 

        initialGreenWidth = greenZone.rect.width;
        initialRedWidth = redZone.rect.width;
        if (yellowZone != null) initialYellowWidth = yellowZone.rect.width;
        if (blackoutZone != null) initialBlackoutWidth = blackoutZone.rect.width;

        if (shakeTarget != null) originalShakePos = shakeTarget.localPosition;
        if (cursor != null) originalCursorScale = cursor.localScale;

        if (damageFlashImage != null) { Color c = damageFlashImage.color; c.a = 0f; damageFlashImage.color = c; }
        if (eventBorderImage != null) { Color c = eventBorderImage.color; c.a = 0f; eventBorderImage.color = c; }
        if (notificationText != null) { Color c = notificationText.color; c.a = 0f; notificationText.color = c; notificationText.gameObject.SetActive(false); }
        if (yellowZone != null) yellowZone.sizeDelta = new Vector2(0, yellowZone.sizeDelta.y);
        if (blackoutZone != null) blackoutZone.sizeDelta = new Vector2(0, blackoutZone.sizeDelta.y);

        if (minigamePanels != null)
        {
            foreach (GameObject panel in minigamePanels) { if (panel != null) panel.SetActive(false); }
        }

        UpdateDayUI();
        UpdateSanityUI();

        greenZone.anchoredPosition = new Vector2(startPointX + 100f, greenZone.anchoredPosition.y);
        redZone.anchoredPosition = new Vector2(endPointX - 100f, redZone.anchoredPosition.y);

        TriggerRespawn(greenZone, initialGreenWidth, redZone, 1, true);
        TriggerRespawn(redZone, initialRedWidth, greenZone, 2, true);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayAmbient("ambientLoop");
        
        ShowNotification("DAY " + currentDay + "\nSURVIVE THE MELTDOWN");
    }

    void Update()
    {
        if (!isGameActive) return;

        UpdateTimer();
        if (isMinigameActive) return;

        // ถ้ารัวปุ่ม Blackout อยู่ เส้นจะหยุดวิ่ง
        if (!isMashingBlackout) 
        {
            MoveCursorLoop();
            MoveZones(); // อัปเดตกล่องดิ้น (ถ้ามี Event)
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

    private void MoveCursorLoop()
    {
        float speed = cursorSpeed;
        
        // Event: ระบบรวน ความเร็วเส้นจะสวิงไปมา
        if (isGlitching) 
        {
            speed = Random.Range(-cursorSpeed * 0.5f, cursorSpeed * 1.5f);
        }

        cursor.anchoredPosition += new Vector2(speed * Time.deltaTime, 0);
        
        if (cursor.anchoredPosition.x >= endPointX) cursor.anchoredPosition = new Vector2(startPointX, cursor.anchoredPosition.y);
        else if (cursor.anchoredPosition.x <= startPointX) cursor.anchoredPosition = new Vector2(endPointX, cursor.anchoredPosition.y);
    }

    private void MoveZones()
    {
        // Event: แรงดันไม่เสถียร กล่องจะไหลซ้ายขวา
        if (isZonesMoving)
        {
            float offset = Mathf.Sin(Time.time * 5f) * 80f * Time.deltaTime; 
            if (greenZone.rect.width > 0) greenZone.anchoredPosition += new Vector2(offset, 0);
            if (redZone.rect.width > 0) redZone.anchoredPosition -= new Vector2(offset, 0);
            if (yellowZone != null && yellowZone.rect.width > 0) yellowZone.anchoredPosition += new Vector2(offset, 0);
        }
    }

    // ==========================================
    // RANDOM EVENT SYSTEM (NEW!)
    // ==========================================
    private void HandleRandomEvents()
    {
        if (isTutorialPhase || currentDay <= 1 || isMashingBlackout) return;

        eventTimer += Time.deltaTime;
        if (eventTimer > 8.0f) // สุ่มทุกๆ 8 วินาที
        {
            eventTimer = 0f;
            TriggerRandomEvent();
        }
    }

    private void TriggerRandomEvent()
    {
        float rand = Random.value;

        // 1. Blackout (Day 3-6)
        if (currentDay >= 3 && currentDay <= 6 && rand < 0.3f && blackoutZone != null && blackoutZone.rect.width <= 0)
        {
            TriggerRespawn(blackoutZone, initialBlackoutWidth, redZone, 4, false);
            ShowEventWarning("CRITICAL: POWER FAILURE");
            if (ScreenFader.Instance != null) StartCoroutine(ScreenFader.Instance.FadeRoutine(0.85f));
        }
        // 2. Yellow Radiation (Day 4-7)
        else if (currentDay >= 4 && rand >= 0.3f && rand < 0.6f && yellowZone != null && yellowZone.rect.width <= 0)
        {
            TriggerRespawn(yellowZone, initialYellowWidth, redZone, 3, false);
            ShowEventWarning("WARNING: RADIATION LEAK");
        }
        // 3. Glitch / Moving Zones (Day 2-7)
        else 
        {
            if (Random.value > 0.5f) StartCoroutine(GlitchRoutine());
            else StartCoroutine(MovingZonesRoutine());
        }
    }

    private IEnumerator GlitchRoutine()
    {
        isGlitching = true;
        ShowEventWarning("SYSTEM GLITCH");
        yield return new WaitForSeconds(3.5f);
        isGlitching = false;
    }

    private IEnumerator MovingZonesRoutine()
    {
        isZonesMoving = true;
        ShowEventWarning("UNSTABLE PRESSURE");
        yield return new WaitForSeconds(5.0f);
        isZonesMoving = false;
    }

    // ==========================================
    // PROGRESSION & SPAWNING
    // ==========================================
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
        if (currentDay >= maxDays)
        {
            WinGame();
            return;
        }
        StartCoroutine(DayTransitionRoutine());
    }

    private IEnumerator DayTransitionRoutine()
    {
        isGameActive = false;
        
        if (ScreenFader.Instance != null) yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(1f));

        // Clear Event States
        isMashingBlackout = false;
        isGlitching = false;
        isZonesMoving = false;
        if (yellowZone != null) yellowZone.sizeDelta = new Vector2(0, yellowZone.sizeDelta.y);
        if (blackoutZone != null) blackoutZone.sizeDelta = new Vector2(0, blackoutZone.sizeDelta.y);
        if (eventBorderImage != null) { Color cb = eventBorderImage.color; cb.a = 0f; eventBorderImage.color = cb; }

        currentDay++;
        timeRemaining = GetTimeForDay(currentDay);
        
        cursorSpeed += 25f;
        redSpawnDelayMin = Mathf.Max(0.5f, redSpawnDelayMin - 0.3f);
        redSpawnDelayMax = Mathf.Max(1.5f, redSpawnDelayMax - 0.6f);

        UpdateDayUI();
        
        TriggerRespawn(greenZone, initialGreenWidth, redZone, 1, true);
        TriggerRespawn(redZone, initialRedWidth, greenZone, 2, true);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("dayChangeSound");

        if (ScreenFader.Instance != null) yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(0f));

        isGameActive = true;
        ShowNotification("DAY " + currentDay);
    }

    private void UpdateDayUI() { if (dayText != null) dayText.text = "Day: " + currentDay + "/" + maxDays; }
    private void UpdateSanityUI() { if (sanityText != null) sanityText.text = "Sanity: " + currentSanity + "/" + maxSanity; }

    private void HandleGreenLifeTime()
    {
        if (isGreenSpawning || greenZone.rect.width <= 0) return;
        greenTimer += Time.deltaTime;
        if (greenTimer >= greenLifeTime) TriggerRespawn(greenZone, initialGreenWidth, redZone, 1, false);
    }

    private void ShrinkBoxes()
    {
        if (!isGreenSpawning && greenZone.rect.width > minGreenWidth)
        {
            greenZone.sizeDelta = new Vector2(Mathf.Max(greenZone.rect.width - (greenShrinkRate * Time.deltaTime), minGreenWidth), greenZone.sizeDelta.y);
        }

        if (!isRedSpawning && redZone.rect.width > 0)
        {
            float newRedWidth = redZone.rect.width - (redShrinkRate * Time.deltaTime);
            if (newRedWidth <= 0) OnRedZoneDisappeared();
            else redZone.sizeDelta = new Vector2(newRedWidth, redZone.sizeDelta.y);
        }

        // กล่องเหลืองหดช้าๆ ปล่อยทิ้งได้ไม่เป็นไร
        if (!isYellowSpawning && yellowZone != null && yellowZone.rect.width > 0)
        {
            float newYelWidth = yellowZone.rect.width - (greenShrinkRate * Time.deltaTime);
            yellowZone.sizeDelta = new Vector2(Mathf.Max(newYelWidth, 0), yellowZone.sizeDelta.y);
        }

        // กล่อง Blackout ถ้าหดจนหมดก่อนปั่นไฟเสร็จ = บึ้ม!
        if (!isBlackoutSpawning && blackoutZone != null && blackoutZone.rect.width > 0)
        {
            float newBlkWidth = blackoutZone.rect.width - (redShrinkRate * 0.7f * Time.deltaTime);
            if (newBlkWidth <= 0)
            {
                isMashingBlackout = false;
                currentSanity -= sanityDamage;
                TriggerShake();
                StartCoroutine(FlashDamageScreen()); 
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("explosionSound");
                blackoutZone.sizeDelta = new Vector2(0, blackoutZone.sizeDelta.y);
                if (ScreenFader.Instance != null) StartCoroutine(ScreenFader.Instance.FadeRoutine(0f)); 
                UpdateSanityUI();
                CheckGameOver();
            }
            else blackoutZone.sizeDelta = new Vector2(newBlkWidth, blackoutZone.sizeDelta.y);
        }
    }

    private void OnRedZoneDisappeared()
    {
        currentSanity -= isTutorialPhase ? 1 : sanityDamage;
        TriggerShake();
        StartCoroutine(FlashDamageScreen()); 
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("explosionSound");
        UpdateSanityUI();
        CheckGameOver();
        if (isGameActive) TriggerRespawn(redZone, initialRedWidth, greenZone, 2, false);
    }

    // zoneType: 1=Green, 2=Red, 3=Yellow, 4=Blackout
    private void TriggerRespawn(RectTransform zoneToSpawn, float targetWidth, RectTransform otherZone, int zoneType, bool spawnImmediately)
    {
        if (zoneType == 1 && greenSpawnCoroutine != null) StopCoroutine(greenSpawnCoroutine);
        else if (zoneType == 2 && redSpawnCoroutine != null) StopCoroutine(redSpawnCoroutine);
        else if (zoneType == 3 && yellowSpawnCoroutine != null) StopCoroutine(yellowSpawnCoroutine);
        else if (zoneType == 4 && blackoutSpawnCoroutine != null) StopCoroutine(blackoutSpawnCoroutine);

        Coroutine newRoutine = StartCoroutine(GradualSpawnRoutine(zoneToSpawn, targetWidth, otherZone, zoneType, spawnImmediately));

        if (zoneType == 1) greenSpawnCoroutine = newRoutine;
        else if (zoneType == 2) redSpawnCoroutine = newRoutine;
        else if (zoneType == 3) yellowSpawnCoroutine = newRoutine;
        else if (zoneType == 4) blackoutSpawnCoroutine = newRoutine;
    }

    private IEnumerator GradualSpawnRoutine(RectTransform zoneToSpawn, float targetWidth, RectTransform otherZone, int zoneType, bool spawnImmediately)
    {
        if (zoneType == 1) { isGreenSpawning = true; greenTimer = 0f; }
        else if (zoneType == 2) isRedSpawning = true;
        else if (zoneType == 3) isYellowSpawning = true;
        else if (zoneType == 4) isBlackoutSpawning = true;

        zoneToSpawn.sizeDelta = new Vector2(0, zoneToSpawn.sizeDelta.y);

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

        zoneToSpawn.anchoredPosition = new Vector2(newX, zoneToSpawn.anchoredPosition.y);

        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            if (!isMinigameActive && !isMashingBlackout)
            {
                elapsed += Time.deltaTime;
                float currentWidth = Mathf.Lerp(0, targetWidth, elapsed / spawnDuration);
                zoneToSpawn.sizeDelta = new Vector2(currentWidth, zoneToSpawn.sizeDelta.y);
            }
            yield return null;
        }

        zoneToSpawn.sizeDelta = new Vector2(targetWidth, zoneToSpawn.sizeDelta.y);

        if (zoneType == 1) isGreenSpawning = false;
        else if (zoneType == 2) isRedSpawning = false;
        else if (zoneType == 3) isYellowSpawning = false;
        else if (zoneType == 4) isBlackoutSpawning = false;
    }

    private void CheckHitZone()
    {
        float cursorX = cursor.anchoredPosition.x;
        StartCoroutine(HitPauseRoutine()); 

        // 1. ถ้ากำลังรัวปุ่มไฟตก (Blackout Priority)
        if (isMashingBlackout)
        {
            blackoutMashCount++;
            if (ScreenFader.Instance != null) StartCoroutine(ScreenFader.Instance.FadeRoutine(0.85f - (0.85f * (blackoutMashCount / (float)requiredBlackoutMash))));
            
            if (blackoutMashCount >= requiredBlackoutMash)
            {
                isMashingBlackout = false;
                blackoutZone.sizeDelta = new Vector2(0, blackoutZone.sizeDelta.y);
                currentSanity += sanityHeal;
                if (currentSanity > maxSanity) currentSanity = maxSanity;
                
                ShowNotification("POWER RESTORED");
                if (ScreenFader.Instance != null) StartCoroutine(ScreenFader.Instance.FadeRoutine(0f));
            }
            return; 
        }

        // 2. ถ้ากดติดกล่องไฟตก (ล็อค Cursor เข้าสู่การปั่นไฟ)
        if (blackoutZone != null && blackoutZone.rect.width > 0 && IsInsideZone(cursorX, blackoutZone, 1.2f))
        {
            isMashingBlackout = true;
            blackoutMashCount = 1;
            return;
        }

        // 3. ถ้ากดโดนกล่องรังสี (ห้ามกด!)
        if (yellowZone != null && yellowZone.rect.width > 0 && IsInsideZone(cursorX, yellowZone, 1.0f))
        {
            currentSanity -= sanityDamage;
            TriggerShake();
            StartCoroutine(FlashDamageScreen()); 
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("missSound");
            yellowZone.sizeDelta = new Vector2(0, yellowZone.sizeDelta.y);
            UpdateSanityUI();
            CheckGameOver();
            return;
        }

        // 4. เช็คกล่องปกติ (เขียว / แดง)
        if (IsInsideZone(cursorX, greenZone, greenHitboxMultiplier))
        {
            currentSanity += isTutorialPhase ? 1 : sanityHeal;
            if (currentSanity > maxSanity) currentSanity = maxSanity;
            if (isTutorialPhase) { isTutorialPhase = false; ShowNotification("TUTORIAL CLEARED"); }
            
            TriggerRespawn(greenZone, initialGreenWidth, redZone, 1, false);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("hitSound");
        }
        else if (IsInsideZone(cursorX, redZone, 1.0f)) 
        {
            if (isTutorialPhase) { isTutorialPhase = false; ShowNotification("TUTORIAL CLEARED"); }
            TriggerRespawn(redZone, initialRedWidth, greenZone, 2, false);
            EnterMinigame();
        }
        else
        {
            currentSanity -= isTutorialPhase ? 1 : (sanityDamage / 4);
            TriggerShake();
            StartCoroutine(FlashDamageScreen()); 
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("missSound");
        }

        UpdateSanityUI();
        CheckGameOver();
    }

    private void EnterMinigame()
    {
        isMinigameActive = true;
        if (minigamePanels != null && minigamePanels.Length > 0)
        {
            currentActiveMinigame = minigamePanels[Random.Range(0, minigamePanels.Length)];
            currentActiveMinigame.SetActive(true);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("minigameTransitionInSound");
    }

    public void FinishMinigame(bool isSuccess)
    {
        isMinigameActive = false;
        if (currentActiveMinigame != null) { currentActiveMinigame.SetActive(false); currentActiveMinigame = null; }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(isSuccess ? "minigameWinSound" : "minigameLoseSound");

        if (!isSuccess)
        {
            currentSanity -= isTutorialPhase ? 1 : sanityDamage;
            TriggerShake();
            StartCoroutine(FlashDamageScreen()); 
        }
        UpdateSanityUI();
        CheckGameOver();
    }

    private void CheckGameOver()
    {
        if (currentSanity <= 0)
        {
            isGameActive = false;
            ShowNotification("MELTDOWN!\nSYSTEM FAILURE");
            if (AudioManager.Instance != null) { AudioManager.Instance.StopAmbient(); AudioManager.Instance.PlaySFX("gameOverSound"); }
        }
    }

    private void WinGame()
    {
        isGameActive = false;
        timerText.text = "Time: 00";
        ShowNotification("SURVIVED!\nSYSTEM STABILIZED");
    }

    private bool IsInsideZone(float xPos, RectTransform zone, float hitboxMultiplier)
    {
        if (zone.rect.width <= 0) return false;
        float halfWidth = (zone.rect.width * hitboxMultiplier) / 2f; 
        return xPos >= (zone.anchoredPosition.x - halfWidth) && xPos <= (zone.anchoredPosition.x + halfWidth);
    }

    // ==========================================
    // NOTIFICATION & JUICE METHODS
    // ==========================================
    private void ShowEventWarning(string message)
    {
        ShowNotification(message);
        if (eventBorderImage != null) StartCoroutine(PulseEventBorder());
    }

    private IEnumerator PulseEventBorder()
    {
        if (eventBorderImage == null) yield break;
        Color c = eventBorderImage.color;
        float elapsed = 0f;
        while(elapsed < 2.5f) 
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.PingPong(elapsed * 2f, 0.6f); 
            eventBorderImage.color = c;
            yield return null;
        }
        c.a = 0f;
        eventBorderImage.color = c;
    }

    private void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            if (notificationCoroutine != null) StopCoroutine(notificationCoroutine);
            notificationCoroutine = StartCoroutine(NotificationRoutine(message));
        }
    }

    private IEnumerator NotificationRoutine(string message)
    {
        notificationText.text = message;
        notificationText.maxVisibleCharacters = 0;
        notificationText.gameObject.SetActive(true);
        Color c = notificationText.color;
        c.a = 1f;
        notificationText.color = c;

        for (int i = 0; i <= message.Length; i++)
        {
            notificationText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(typeWriterSpeed);
        }

        yield return new WaitForSecondsRealtime(notificationDuration);

        float fadeTime = 0.3f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            notificationText.color = c;
            yield return null;
        }
        notificationText.gameObject.SetActive(false);
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
        if (damageFlashImage == null) yield break;
        Color c = damageFlashImage.color;
        c.a = 0.5f; 
        damageFlashImage.color = c;
        float flashDuration = 0.2f;
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0.5f, 0f, elapsed / flashDuration);
            damageFlashImage.color = c;
            yield return null;
        }
    }
}