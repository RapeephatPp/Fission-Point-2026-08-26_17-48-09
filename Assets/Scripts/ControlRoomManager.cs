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

    [Header("Minigame System")]
    public GameObject minigamePanel;   

    [Header("Settings")]
    public float cursorSpeed = 500f;   
    public float startPointX = -400f;  
    public float endPointX = 400f;     
    public float gameTime = 30f;       
    public int currentDay = 1;         

    [Header("Sanity Settings")]
    public int maxSanity = 100;
    public int currentSanity;
    public int sanityHeal = 10;        
    public int sanityDamage = 20;      

    [Header("Difficulty & Spawn Settings")]
    public float shrinkRate = 20f;     
    public float minGreenWidth = 15f;  
    public float minSpawnDistance = 150f; 
    public float spawnDuration = 0.5f; 
    public float minSpawnDelay = 1.0f; 
    public float maxSpawnDelay = 3.0f; 

    [Header("Juice (Effects)")]
    public Transform shakeTarget;      
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 10f;
    
    [Header("Extra Juice (New!)")]
    public Image damageFlashImage;     // ลาก UI Image สีแดงแบบเต็มจอมาใส่
    public float hitPauseDuration = 0.05f; // เวลาที่เกมจะกระตุกหยุด (วินาที)
    public float cursorBumpScale = 1.5f;   // ขนาดที่เส้นจะเด้งขยาย
    public float cursorBumpTime = 0.1f;    // เวลาที่เส้นเด้ง

    private float timeRemaining;
    private bool isGameActive = true;
    private bool isMinigameActive = false; 
    
    private float initialGreenWidth;
    private float initialRedWidth;
    private Vector3 originalShakePos;
    private Vector3 originalCursorScale;

    private bool isGreenSpawning = false;
    private bool isRedSpawning = false;
    
    private Coroutine greenSpawnCoroutine;
    private Coroutine redSpawnCoroutine;
    private Coroutine shakeCoroutine; 
    private Coroutine cursorBumpCoroutine;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        timeRemaining = gameTime;
        currentSanity = maxSanity;
        
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
        
        if (minigamePanel != null) minigamePanel.SetActive(false);

        UpdateDayUI();
        UpdateSanityUI();
        
        greenZone.anchoredPosition = new Vector2(startPointX + 100f, greenZone.anchoredPosition.y);
        redZone.anchoredPosition = new Vector2(endPointX - 100f, redZone.anchoredPosition.y);
        
        TriggerRespawn(greenZone, initialGreenWidth, redZone, true, true);
        TriggerRespawn(redZone, initialRedWidth, greenZone, false, true);
    }

    void Update()
    {
        if (!isGameActive) return; 

        UpdateTimer(); 
        if (isMinigameActive) return; 

        MoveCursorLoop();
        ShrinkBoxes(); 

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            TriggerCursorBump(); // เด้งเส้นวิ่งทุกครั้งที่กด
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

    private void UpdateTimer()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            timerText.text = "Time: " + Mathf.Ceil(timeRemaining).ToString("00"); 
        }
        else
        {
            AdvanceToNextDay();
        }
    }

    private void AdvanceToNextDay()
    {
        currentDay++;
        timeRemaining = gameTime;
        UpdateDayUI();
        TriggerRespawn(greenZone, initialGreenWidth, redZone, true, true);
        TriggerRespawn(redZone, initialRedWidth, greenZone, false, true);
    }

    private void UpdateDayUI()
    {
        if (dayText != null) dayText.text = "Day: " + currentDay;
    }

    private void UpdateSanityUI()
    {
        if (sanityText != null) sanityText.text = "Sanity: " + currentSanity + "/" + maxSanity;
    }

    private void ShrinkBoxes()
    {
        if (!isGreenSpawning && greenZone.rect.width > minGreenWidth)
        {
            float newWidth = greenZone.rect.width - (shrinkRate * Time.deltaTime);
            greenZone.sizeDelta = new Vector2(Mathf.Max(newWidth, minGreenWidth), greenZone.sizeDelta.y);
        }

        if (!isRedSpawning && redZone.rect.width > 0)
        {
            float newRedWidth = redZone.rect.width - (shrinkRate * Time.deltaTime);
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
        currentSanity -= sanityDamage;
        TriggerShake(); 
        StartCoroutine(FlashDamageScreen()); // จอแดงเมื่อกล่องหาย
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
        if (isGreen) isGreenSpawning = true;
        else isRedSpawning = true;

        zoneToSpawn.sizeDelta = new Vector2(0, zoneToSpawn.sizeDelta.y);
        
        if (!spawnImmediately)
        {
            float waitTime = Random.Range(minSpawnDelay, maxSpawnDelay);
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
        StartCoroutine(HitPauseRoutine()); // หยุดเวลาสั้นๆ ทุกครั้งที่ประมวลผลการกด

        if (IsInsideZone(cursorX, greenZone))
        {
            currentSanity += sanityHeal;
            if (currentSanity > maxSanity) currentSanity = maxSanity;
            TriggerRespawn(greenZone, initialGreenWidth, redZone, true, false);
        }
        else if (IsInsideZone(cursorX, redZone))
        {
            TriggerRespawn(redZone, initialRedWidth, greenZone, false, false); 
            EnterMinigame(); 
        }
        else
        {
            currentSanity -= (sanityDamage / 4);
            TriggerShake(); 
            StartCoroutine(FlashDamageScreen()); // จอแดงเมื่อวืด
        }

        UpdateSanityUI();
        CheckGameOver();
    }

    private void EnterMinigame()
    {
        isMinigameActive = true; 
        if (minigamePanel != null) minigamePanel.SetActive(true); 
    }

    public void FinishMinigame(bool isSuccess)
    {
        isMinigameActive = false; 
        if (minigamePanel != null) minigamePanel.SetActive(false); 

        if (!isSuccess)
        {
            currentSanity -= sanityDamage;
            TriggerShake();
            StartCoroutine(FlashDamageScreen()); // จอแดงเมื่อแพ้มินิเกม
        }

        UpdateSanityUI();
        CheckGameOver();
    }

    private void CheckGameOver()
    {
        if (currentSanity <= 0)
        {
            isGameActive = false;
        }
    }

    private bool IsInsideZone(float xPos, RectTransform zone)
    {
        if (zone.rect.width <= 0) return false;
        float halfWidth = zone.rect.width / 2f;
        return xPos >= (zone.anchoredPosition.x - halfWidth) && xPos <= (zone.anchoredPosition.x + halfWidth);
    }

    // ==========================================
    // EXTRA JUICE METHODS
    // ==========================================

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
            elapsed += Time.unscaledDeltaTime; // ใช้ unscaled เผื่อติด Hit Pause
            yield return null;
        }
        shakeTarget.localPosition = originalShakePos; 
    }

    private IEnumerator HitPauseRoutine()
    {
        Time.timeScale = 0f; // หยุดเวลาทั้งเกม
        yield return new WaitForSecondsRealtime(hitPauseDuration);
        Time.timeScale = 1f; // คืนค่าเวลา
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
        c.a = 0.5f; // ความทึบของสีแดงตอนโผล่มา (ปรับเพิ่มลดได้)
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