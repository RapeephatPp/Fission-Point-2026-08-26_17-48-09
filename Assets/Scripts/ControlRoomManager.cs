using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlRoomManager : MonoBehaviour
{
    [Header("Debug / Cheat Mode")]
    public bool enableCheatMode = true; 

    [Header("Mini-Game UI")]
    public RectTransform cursor;
    public RectTransform greenZone;
    public RectTransform redZone;
    
    [Header("Event UI (New Mechanics)")]
    public RectTransform yellowZone;   
    public RectTransform blackoutZone; 
    public Image eventBorderImage;     

    [Header("Game Info UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI sanityText;

    [Header("Notification UI")]
    public TextMeshProUGUI notificationText; 
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

    private bool isGreenSpawning = false;
    private bool isRedSpawning = false;
    private bool isYellowSpawning = false;
    private bool isBlackoutSpawning = false;
    
    private float greenTimer = 0f;
    
    // --- Random Event Variables ---
    private float eventTimer = 0f;
    private float nextEventDelay = 12f; 
    private int lastEventId = -1;       
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
        if (tipText != null) { Color c = tipText.color; c.a = 0f; tipText.color = c; tipText.gameObject.SetActive(false); }

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

        TriggerRespawn(greenZone, initialGreenWidth, redZone, 1, true, false);
        TriggerRespawn(redZone, initialRedWidth, greenZone, 2, true, false);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayAmbient("ambientLoop");
        
        ShowNotification("DAY " + currentDay + "\nSURVIVE THE MELTDOWN", "TIP: Press Space on GREEN to heal, RED to engage minigame.");
    }

    void Update()
    {
        if (enableCheatMode) HandleDebugKeys();

        if (!isGameActive) return;

        UpdateTimer();
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
            if (Random.value > 0.8f) cursor.localScale = originalCursorScale * Random.Range(0.6f, 1.8f);
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
                TriggerRespawn(blackoutZone, initialBlackoutWidth, redZone, 4, false, false);
                ShowEventWarning("CRITICAL: POWER FAILURE", "TIP: MASH Spacebar to restart the generator!");
                float oldMag = shakeMagnitude; shakeMagnitude = 20f; TriggerShake(); shakeMagnitude = oldMag;
                if (ScreenFader.Instance != null) StartCoroutine(ScreenFader.Instance.FadeRoutine(0.85f));
                break;
            case 2:
                TriggerRespawn(yellowZone, initialYellowWidth, redZone, 3, false, false);
                ShowEventWarning("WARNING: RADIATION LEAK", "TIP: Do NOT touch the YELLOW zone!");
                break;
            case 3:
                StartCoroutine(GlitchRoutine());
                break;
            case 4:
                StartCoroutine(MovingZonesRoutine());
                break;
        }
    }

    private IEnumerator GlitchRoutine()
    {
        isGlitching = true;
        ShowEventWarning("SYSTEM GLITCH", "TIP: Cursor speed is corrupted. Rely on your reflexes!");
        yield return new WaitForSeconds(3.5f);
        isGlitching = false;
        cursor.localScale = originalCursorScale; 
    }

    private IEnumerator MovingZonesRoutine()
    {
        isZonesMoving = true;
        ShowEventWarning("UNSTABLE PRESSURE", "TIP: Targets are drifting. Anticipate their movement!");
        yield return new WaitForSeconds(5.0f);
        isZonesMoving = false;
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
        isGlitching = false;
        isZonesMoving = false;
        if (yellowZone != null) yellowZone.sizeDelta = new Vector2(0, yellowZone.sizeDelta.y);
        if (blackoutZone != null) blackoutZone.sizeDelta = new Vector2(0, blackoutZone.sizeDelta.y);
        if (eventBorderImage != null) { Color cb = eventBorderImage.color; cb.a = 0f; eventBorderImage.color = cb; }

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
            yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(0f));
            ScreenFader.Instance.fadeDuration = 0.5f; 
        }

        isGameActive = true;
        ShowNotification("DAY " + currentDay, "TIP: The system is getting faster. Stay focused.");
    }

    private void UpdateDayUI() { if (dayText != null) dayText.text = "Day: " + currentDay + "/" + maxDays; }
    private void UpdateSanityUI() { if (sanityText != null) sanityText.text = "Sanity: " + currentSanity + "/" + maxSanity; }

    private void HandleGreenLifeTime()
    {
        if (isGreenSpawning || greenZone.rect.width <= 0) return;
        greenTimer += Time.deltaTime;
        
        // 🟢 เพิ่มอายุขัยของกล่องเขียวในโหมดสอนเล่น
        float lifeTime = isTutorialPhase ? greenLifeTime * 2f : greenLifeTime;
        
        if (greenTimer >= lifeTime) TriggerRespawn(greenZone, initialGreenWidth, redZone, 1, false, true);
    }

    private void ShrinkBoxes()
    {
        // 🟢 ปรับให้ช่วงสอนเล่น กล่องหดช้าลงเหลือ 30% ของความเร็วปกติ
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
                StartCoroutine(FadeOutAndHideRoutine(yellowZone, 3)); // หายไปแบบสมูท
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
                
                StartCoroutine(FadeOutAndHideRoutine(blackoutZone, 4)); // เฟดทิ้ง
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
        if (isGameActive) TriggerRespawn(redZone, initialRedWidth, greenZone, 2, false, true);
    }

    // 🟢 เพิ่มพารามิเตอร์ doFadeOut เข้ามาควบคุมการเฟด
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

        // ดึง CanvasGroup มาใช้ ถ้าไม่มีจะสร้างให้เองอัตโนมัติ
        CanvasGroup cg = zone.GetComponent<CanvasGroup>();
        if (cg == null) cg = zone.gameObject.AddComponent<CanvasGroup>();

        // 🟢 จังหวะ Fade Out ทิ้งกล่องเดิม
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

        // 🟢 จังหวะค่อยๆ Fade In กลับมา
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

    // 🟢 สำหรับการสั่งเฟดทิ้งแบบหายไปเลย (ไม่เกิดใหม่)
    private IEnumerator FadeOutAndHideRoutine(RectTransform zone, int zoneType)
    {
        if (zoneType == 3) isYellowSpawning = true;
        else if (zoneType == 4) isBlackoutSpawning = true;

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
            if (ScreenFader.Instance != null) StartCoroutine(ScreenFader.Instance.FadeRoutine(0.85f - (0.85f * (blackoutMashCount / (float)requiredBlackoutMash))));
            
            if (blackoutMashCount >= requiredBlackoutMash)
            {
                isMashingBlackout = false;
                StartCoroutine(FadeOutAndHideRoutine(blackoutZone, 4));
                currentSanity += sanityHeal;
                if (currentSanity > maxSanity) currentSanity = maxSanity;
                
                ShowNotification("POWER RESTORED", "TIP: Great job! Stay alert.");
                if (ScreenFader.Instance != null) StartCoroutine(ScreenFader.Instance.FadeRoutine(0f));
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
            currentSanity -= sanityDamage;
            TriggerShake();
            StartCoroutine(FlashDamageScreen()); 
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("missSound");
            StartCoroutine(FadeOutAndHideRoutine(yellowZone, 3));
            UpdateSanityUI();
            CheckGameOver();
            return;
        }

        if (IsInsideZone(cursorX, greenZone, greenHitboxMultiplier))
        {
            currentSanity += isTutorialPhase ? 1 : sanityHeal;
            if (currentSanity > maxSanity) currentSanity = maxSanity;
            if (isTutorialPhase) { isTutorialPhase = false; ShowNotification("SYSTEM ONLINE", "TIP: Maintain stability until the end of the shift."); }
            
            TriggerRespawn(greenZone, initialGreenWidth, redZone, 1, false, true);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("hitSound");
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
            ShowNotification("MELTDOWN!\nSYSTEM FAILURE", "");
            if (AudioManager.Instance != null) { AudioManager.Instance.StopAmbient(); AudioManager.Instance.PlaySFX("gameOverSound"); }
        }
    }

    private void WinGame()
    {
        isGameActive = false;
        timerText.text = "Time: 00";
        ShowNotification("SURVIVED!\nSYSTEM STABILIZED", "TIP: You've mastered the reactor control.");
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
    
    private void ShowEventWarning(string message, string tip = "")
    {
        ShowNotification(message, tip);
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
            c.a = Mathf.PingPong(elapsed * 4f, 0.7f); 
            eventBorderImage.color = c;
            yield return null;
        }
        c.a = 0f;
        eventBorderImage.color = c;
    }

    private void ShowNotification(string message, string tip = "")
    {
        if (notificationText != null)
        {
            if (notificationCoroutine != null) StopCoroutine(notificationCoroutine);
            notificationCoroutine = StartCoroutine(NotificationRoutine(message, tip));
        }
    }

    private IEnumerator NotificationRoutine(string message, string tip)
    {
        notificationText.text = message;
        notificationText.maxVisibleCharacters = 0;
        notificationText.gameObject.SetActive(true);
        
        Color c = notificationText.color;
        c.a = 1f;
        notificationText.color = c;

        if (tipText != null)
        {
            tipText.text = tip;
            tipText.maxVisibleCharacters = 0;
            tipText.gameObject.SetActive(true);
            Color tipC = tipText.color;
            tipC.a = 1f;
            tipText.color = tipC;
        }

        for (int i = 0; i <= message.Length; i++)
        {
            notificationText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(typeWriterSpeed);
        }

        if (tipText != null && !string.IsNullOrEmpty(tip))
        {
            yield return new WaitForSecondsRealtime(0.2f); 
            for (int i = 0; i <= tip.Length; i++)
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
            
            c.a = alpha;
            notificationText.color = c;
            
            if (tipText != null) {
                Color tipC = tipText.color;
                tipC.a = alpha;
                tipText.color = tipC;
            }
            
            yield return null;
        }
        
        notificationText.gameObject.SetActive(false);
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