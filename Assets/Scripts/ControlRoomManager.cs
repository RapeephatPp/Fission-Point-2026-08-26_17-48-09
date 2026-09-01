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

    [Header("Game Info UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI sanityText;

    [Header("Notification UI (New!)")]
    public TextMeshProUGUI notificationText; 
    public float notificationDuration = 2.0f; 
    public float typeWriterSpeed = 0.05f; // เพิ่มความเร็วในการพิมพ์ต่อ 1 ตัวอักษร (วินาที)

    [Header("Minigame System")]
    public GameObject[] minigamePanels; // เปลี่ยนเป็น Array ให้ใส่ได้หลายๆ หน้าจอ
    private GameObject currentActiveMinigame; // เอาไว้จำว่าตอนนี้เปิดอันไหนอยู่ จะได้ปิดถูก

    [Header("Settings")]
    public float cursorSpeed = 500f;
    public float startPointX = -400f;
    public float endPointX = 400f;
    public int currentDay = 1;
    public int maxDays = 7; // กำหนดวันสูงสุดที่ 7 วัน

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

    [Header("Forgiveness Mechanics (Design)")]
    public float greenHitboxMultiplier = 1.5f; 
    public float greenLifeTime = 3.0f;         

    [Header("Juice (Effects)")]
    public Transform shakeTarget;
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 10f;
    
    [Header("Extra Juice")]
    public Image damageFlashImage;     
    public float hitPauseDuration = 0.05f; 
    public float cursorBumpScale = 1.5f;   
    public float cursorBumpTime = 0.1f;    

    private float timeRemaining;
    private bool isGameActive = true;
    private bool isMinigameActive = false;
    
    private bool isTutorialPhase = true;

    private float initialGreenWidth;
    private float initialRedWidth;
    private Vector3 originalShakePos;
    private Vector3 originalCursorScale;

    private bool isGreenSpawning = false;
    private bool isRedSpawning = false;
    
    private float greenTimer = 0f;

    private Coroutine greenSpawnCoroutine;
    private Coroutine redSpawnCoroutine;
    private Coroutine shakeCoroutine;
    private Coroutine cursorBumpCoroutine;
    private Coroutine notificationCoroutine;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        currentSanity = maxSanity;
        timeRemaining = GetTimeForDay(currentDay); // ดึงเวลาตามวันที่

        initialGreenWidth = greenZone.rect.width;
        initialRedWidth = redZone.rect.width;

        if (shakeTarget != null) originalShakePos = shakeTarget.localPosition;
        if (cursor != null) originalCursorScale = cursor.localScale;

        if (damageFlashImage != null)
        {
            Color c = damageFlashImage.color;
            c.a = 0f;
            damageFlashImage.color = c;
        }

        if (notificationText != null)
        {
            Color c = notificationText.color;
            c.a = 0f;
            notificationText.color = c;
            notificationText.gameObject.SetActive(false);
        }

        if (minigamePanels != null)
        {
            foreach (GameObject panel in minigamePanels)
            {
                if (panel != null) panel.SetActive(false);
            }
        }

        UpdateDayUI();
        UpdateSanityUI();

        greenZone.anchoredPosition = new Vector2(startPointX + 100f, greenZone.anchoredPosition.y);
        redZone.anchoredPosition = new Vector2(endPointX - 100f, redZone.anchoredPosition.y);

        TriggerRespawn(greenZone, initialGreenWidth, redZone, true, true);
        TriggerRespawn(redZone, initialRedWidth, greenZone, false, true);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayAmbient("ambientLoop");
        
        // แจ้งเตือนเริ่มเกม
        ShowNotification("DAY " + currentDay + "\nSURVIVE THE MELTDOWN");
    }

    void Update()
    {
        if (!isGameActive) return;

        UpdateTimer();
        if (isMinigameActive) return;

        MoveCursorLoop();
        ShrinkBoxes();
        HandleGreenLifeTime(); 

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("clickSound");
            TriggerCursorBump(); 
            CheckHitZone();
        }
    }

    private void MoveCursorLoop()
    {
        cursor.anchoredPosition += new Vector2(cursorSpeed * Time.deltaTime, 0);
        if (cursor.anchoredPosition.x >= endPointX)
        {
            cursor.anchoredPosition = new Vector2(startPointX, cursor.anchoredPosition.y);
        }
    }

    // ฟังก์ชันคำนวณเวลาของแต่ละวัน
    private float GetTimeForDay(int day)
    {
        if (day <= 4) return 60f;        // วันที่ 1-4 = 60 วิ
        if (day == 5) return 90f;        // วันที่ 5 = 90 วิ
        if (day == 6) return 105f;       // วันที่ 6 = 105 วิ
        if (day >= 7) return 120f;       // วันที่ 7 = 120 วิ
        return 60f;
    }

    private void UpdateTimer()
    {
        // 🟢 ถ้ายังอยู่ในโหมดฝึกสอน (ยังกดไม่โดนเลยสักครั้ง) ให้หยุดการนับเวลาไว้
        if (isTutorialPhase)
        {
            // โชว์ตัวเลขเวลาค้างไว้ที่ค่าเริ่มต้น (เช่น 60s)
            if (timerText != null) timerText.text = "Time: " + Mathf.Ceil(timeRemaining).ToString("00");
            return; // จบการทำงานฟังก์ชันนี้ทันทีโดยไม่ลดค่า Time.deltaTime
        }

        // 🟢 ถ้าหลุดโหมดฝึกสอนแล้ว ให้นับเวลาถอยหลังตามปกติ
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            if (timerText != null) timerText.text = "Time: " + Mathf.Ceil(timeRemaining).ToString("00");
        }
        else
        {
            // เปลี่ยนวันโดยใช้ Coroutine ที่เราทำไว้
            StartCoroutine(DayTransitionRoutine());
        }
    }

    private void AdvanceToNextDay()
    {
        if (currentDay >= maxDays)
        {
            WinGame();
            return;
        }

        // เปลี่ยนจากการอัปเดตทันที เป็นการเรียก Coroutine เพื่อทำแอนิเมชันจอดำ
        StartCoroutine(DayTransitionRoutine());
    }

    private IEnumerator DayTransitionRoutine()
    {
        // 1. หยุดเกมหลักชั่วคราว (สคริปต์จะไม่รับ Input และเส้นจะไม่วิ่ง)
        isGameActive = false;
        
        // 2. สั่งเฟดจอให้มืดสนิท (targetAlpha = 1f) แล้วรอจนกว่าจะเฟดเสร็จ
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(1f));
        }

        // 3. แอบรีเซ็ตข้อมูลและเพิ่มความยากตอนที่หน้าจอกำลังดำสนิท
        currentDay++;
        timeRemaining = GetTimeForDay(currentDay);
        
        cursorSpeed += 25f;
        redSpawnDelayMin = Mathf.Max(0.5f, redSpawnDelayMin - 0.3f);
        redSpawnDelayMax = Mathf.Max(1.5f, redSpawnDelayMax - 0.6f);

        UpdateDayUI();
        
        // บังคับสุ่มกล่องใหม่แบบเกิดทันที (spawnImmediately = true)
        TriggerRespawn(greenZone, initialGreenWidth, redZone, true, true);
        TriggerRespawn(redZone, initialRedWidth, greenZone, false, true);

        if (AudioManager.Instance != null) 
        {
            AudioManager.Instance.PlaySFX("dayChangeSound");
        }

        // 4. สั่งเฟดจอให้กลับมาสว่าง (targetAlpha = 0f) แล้วรอจนเฟดเสร็จ
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(0f));
        }

        // 5. ปลดล็อคให้เกมกลับมาเล่นต่อได้ พร้อมโชว์แจ้งเตือน
        isGameActive = true;
        ShowNotification("DAY " + currentDay);
    }

    private void UpdateDayUI()
    {
        if (dayText != null) dayText.text = "Day: " + currentDay + "/" + maxDays;
    }

    private void UpdateSanityUI()
    {
        if (sanityText != null) sanityText.text = "Sanity: " + currentSanity + "/" + maxSanity;
    }

    private void HandleGreenLifeTime()
    {
        if (isGreenSpawning || greenZone.rect.width <= 0) return;

        greenTimer += Time.deltaTime;
        if (greenTimer >= greenLifeTime)
        {
            TriggerRespawn(greenZone, initialGreenWidth, redZone, true, false);
        }
    }

    private void ShrinkBoxes()
    {
        if (!isGreenSpawning && greenZone.rect.width > minGreenWidth)
        {
            float newWidth = greenZone.rect.width - (greenShrinkRate * Time.deltaTime);
            greenZone.sizeDelta = new Vector2(Mathf.Max(newWidth, minGreenWidth), greenZone.sizeDelta.y);
        }

        if (!isRedSpawning && redZone.rect.width > 0)
        {
            float newRedWidth = redZone.rect.width - (redShrinkRate * Time.deltaTime);
            if (newRedWidth <= 0)
            {
                OnRedZoneDisappeared();
            }
            else
            {
                redZone.sizeDelta = new Vector2(newRedWidth, redZone.sizeDelta.y);
            }
        }
    }

    private void OnRedZoneDisappeared()
    {
        int actualDamage = isTutorialPhase ? 1 : sanityDamage;
        currentSanity -= actualDamage;

        TriggerShake();
        StartCoroutine(FlashDamageScreen()); 
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("explosionSound");
        UpdateSanityUI();
        CheckGameOver();

        if (isGameActive)
        {
            TriggerRespawn(redZone, initialRedWidth, greenZone, false, false);
        }
    }

    private void TriggerRespawn(RectTransform zoneToSpawn, float targetWidth, RectTransform otherZone, bool isGreen, bool spawnImmediately)
    {
        if (isGreen && greenSpawnCoroutine != null) StopCoroutine(greenSpawnCoroutine);
        if (!isGreen && redSpawnCoroutine != null) StopCoroutine(redSpawnCoroutine);

        Coroutine newRoutine = StartCoroutine(GradualSpawnRoutine(zoneToSpawn, targetWidth, otherZone, isGreen, spawnImmediately));

        if (isGreen) greenSpawnCoroutine = newRoutine;
        else redSpawnCoroutine = newRoutine;
    }

    private IEnumerator GradualSpawnRoutine(RectTransform zoneToSpawn, float targetWidth, RectTransform otherZone, bool isGreen, bool spawnImmediately)
    {
        if (isGreen) 
        {
            isGreenSpawning = true;
            greenTimer = 0f; 
        }
        else 
        {
            isRedSpawning = true;
        }

        zoneToSpawn.sizeDelta = new Vector2(0, zoneToSpawn.sizeDelta.y);

        if (!spawnImmediately)
        {
            float waitTime = isGreen ? greenSpawnInterval : Random.Range(redSpawnDelayMin, redSpawnDelayMax);
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
            if (!isMinigameActive)
            {
                elapsed += Time.deltaTime;
                float currentWidth = Mathf.Lerp(0, targetWidth, elapsed / spawnDuration);
                zoneToSpawn.sizeDelta = new Vector2(currentWidth, zoneToSpawn.sizeDelta.y);
            }
            yield return null;
        }

        zoneToSpawn.sizeDelta = new Vector2(targetWidth, zoneToSpawn.sizeDelta.y);

        if (isGreen) isGreenSpawning = false;
        else isRedSpawning = false;
    }

    private void CheckHitZone()
    {
        float cursorX = cursor.anchoredPosition.x;
        StartCoroutine(HitPauseRoutine()); 

        if (IsInsideZone(cursorX, greenZone, greenHitboxMultiplier))
        {
            int actualHeal = isTutorialPhase ? 1 : sanityHeal;
            currentSanity += actualHeal;
            if (currentSanity > maxSanity) currentSanity = maxSanity;
            
            if (isTutorialPhase)
            {
                isTutorialPhase = false; 
                /*ShowNotification("TUTORIAL CLEARED\nReal Meltdown Begins!");*/
            }
            
            TriggerRespawn(greenZone, initialGreenWidth, redZone, true, false);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("hitSound");
        }
        else if (IsInsideZone(cursorX, redZone, 1.0f)) 
        {
            if (isTutorialPhase)
            {
                isTutorialPhase = false;
                /*ShowNotification("TUTORIAL CLEARED\nReal Meltdown Begins!");*/
            }

            TriggerRespawn(redZone, initialRedWidth, greenZone, false, false);
            EnterMinigame();
        }
        else
        {
            int actualDamage = isTutorialPhase ? 1 : (sanityDamage / 4);
            currentSanity -= actualDamage;

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
        
        // สุ่มเลือก 1 มินิเกมจาก Array
        if (minigamePanels != null && minigamePanels.Length > 0)
        {
            int randomIndex = Random.Range(0, minigamePanels.Length);
            currentActiveMinigame = minigamePanels[randomIndex];
            currentActiveMinigame.SetActive(true);
        }
        
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("minigameTransitionInSound");
    }

    public void FinishMinigame(bool isSuccess)
    {
        isMinigameActive = false;
        if (currentActiveMinigame != null) 
        {
            currentActiveMinigame.SetActive(false);
            currentActiveMinigame = null; // เคลียร์ความจำ
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(isSuccess ? "minigameWinSound" : "minigameLoseSound");

        if (!isSuccess)
        {
            int actualDamage = isTutorialPhase ? 1 : sanityDamage;
            currentSanity -= actualDamage;

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

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopAmbient();
                AudioManager.Instance.PlaySFX("gameOverSound");
            }
        }
    }

    private void WinGame()
    {
        isGameActive = false;
        timerText.text = "Time: 00";
        ShowNotification("SURVIVED!\nSYSTEM STABILIZED");
        
        // TODO: สามารถเพิ่มการหน่วงเวลาเพื่อเรียกหน้าฉากจบ (Victory Screen) ได้ตรงนี้
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
        // ตั้งค่าข้อความ และซ่อนตัวอักษรทั้งหมดไว้ก่อน (ให้เหลือ 0 ตัว)
        notificationText.text = message;
        notificationText.maxVisibleCharacters = 0;
        notificationText.gameObject.SetActive(true);
        
        // รีเซ็ตค่า Alpha ให้เป็นสีทึบ 100% ตั้งแต่แรก (เผื่อของเก่าเฟดจางไว้)
        Color c = notificationText.color;
        c.a = 1f;
        notificationText.color = c;

        // 1. Typewriter Effect (ค่อยๆ โชว์ทีละตัว)
        for (int i = 0; i <= message.Length; i++)
        {
            notificationText.maxVisibleCharacters = i;
            
            // ใช้ Realtime เผื่อกรณีเกมโดน Pause ไว้จะได้พิมพ์ต่อได้
            yield return new WaitForSecondsRealtime(typeWriterSpeed);
        }

        // 2. Show (ค้างข้อความไว้ให้ผู้เล่นอ่าน)
        yield return new WaitForSecondsRealtime(notificationDuration);

        // 3. Fade Out (ค่อยๆ จางหายไปแบบสมูท)
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
        if (shakeTarget != null)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeEffect());
        }
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
        if (cursor != null)
        {
            if (cursorBumpCoroutine != null) StopCoroutine(cursorBumpCoroutine);
            cursorBumpCoroutine = StartCoroutine(CursorBumpRoutine());
        }
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