using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class MiniGame_PresseInstructed : MonoBehaviour
{
    [Header("References")]
    public ControlRoomManager gameManager;    
    public TextMeshProUGUI instructionText;   
    public TextMeshProUGUI timerText;
    public Image timerBar;

    [Header("Grid UI Setup")]
    [Tooltip("ลากปุ่มสี่เหลี่ยมเล็กๆ ทั้งหมดในแผงมาใส่เรียงตามลำดับ (0, 1, 2...)")]
    public Image[] gridButtons; 

    [Header("Sprites (สลับภาพตาม Assets)")]
    public Sprite normalWhiteSprite;   
    public Sprite normalYellowSprite;  
    public Sprite glowingWhiteSprite;  
    public Sprite glowingYellowSprite; 
    public Sprite redFailSprite;       

    [Header("Juice Animation")]
    [Tooltip("ความเด้งตอนกด (คูณจากสเกลเดิม เช่น 1.1 เท่าของ 0.26)")]
    public float popScaleMultiplier = 1.12f; 
    public float popDuration = 0.12f;

    [Header("Timing & Difficulty Settings")]
    [Tooltip("ตัวคูณเวลาต่อรอบ (เพิ่มเป็น 1.2 หรือ 1.5 ได้ถ้าต้องการให้นับช้าลง)")]
    public float timerSpeedMultiplier = 1.0f;
    public int minRounds = 1;                 
    public int maxRounds = 4;                 
    public int minPresses = 1;                
    public int maxPresses = 16; 
    public float delayBetweenRounds = 0.4f;

    [Header("Vanishing Prompt Twists (Day 3+)")]
    [Tooltip("เปิดใช้งานโหมดข้อความบอกจำนวนโผล่แค่ช่วงแรก แล้วกะพริบดับหายไปให้ผู้เล่นจำเอง")]
    public bool enableVanishingPrompt = true;
    public int vanishPromptStartDay = 3;
    [Tooltip("เวลาที่แสดงข้อความให้เห็นนิ่งๆ ช่วงเริ่มรอบ (วินาที)")]
    public float promptVisibleDuration = 0.9f;
    [Tooltip("ระยะเวลาช่วงกะพริบเร่งจังหวะและค่อยๆ จางดับ (วินาที)")]
    public float promptFadeDuration = 0.45f;

    [Header("Blind Mode Twists (Day 5+)")]
    [Tooltip("เปิดใช้งานโหมด Blind (ปุ่มที่กดจะกะพริบไปมาก่อนดับ ไม่ค้างไฟไว้ให้นับ)")]
    public bool enableBlindMode = true;
    public int blindModeStartDay = 5;
    [Range(0f, 1f)] public float blindModeChance = 0.7f;
    [Tooltip("จำนวนครั้งที่ปุ่มกะพริบไปมาก่อนดับใน Blind Mode")]
    public int blindButtonBlinkCount = 2;
    [Tooltip("จังหวะกะพริบของปุ่มก่อนดับ (วินาที)")]
    public float blindButtonBlinkInterval = 0.08f;

    [Header("Auto Submit Settings")]
    [Tooltip("เวลารอยืนยันหลังกดครบเป้าหมายแล้ว ไม่ต้องรอเวลาหมด")]
    public float autoSubmitDelay = 0.35f;

    private int totalRounds;
    private int currentRound;
    private int targetPresses;
    private int currentPresses;

    private float timer;
    private float timePerRound;
    private float timeSinceLastPress;
    private bool isRoundActive = false;
    private bool isBlindModeRound = false;

    private Color originalInstructionColor = Color.white;
    private Coroutine promptFadeCoroutine;
    private List<Coroutine> activeButtonCoroutines = new List<Coroutine>();

    // เก็บทั้ง Scale (0.26), SizeDelta (87.52, 71.88) และ Sprite เดิมไว้
    private Vector3[] originalScales;
    private Vector2[] originalSizes;
    private Sprite[] roundNormalSprites;

    public Dialogue dialogManager;
    private static bool hasSeenThisMinigame = false;

    private void Awake()
    {
        CacheOriginalTransforms();
        if (instructionText != null)
        {
            originalInstructionColor = instructionText.color;
        }
    }

    private void CacheOriginalTransforms()
    {
        if (gridButtons != null && gridButtons.Length > 0)
        {
            originalScales = new Vector3[gridButtons.Length];
            originalSizes = new Vector2[gridButtons.Length];
            roundNormalSprites = new Sprite[gridButtons.Length];

            for (int i = 0; i < gridButtons.Length; i++)
            {
                if (gridButtons[i] != null)
                {
                    originalScales[i] = gridButtons[i].rectTransform.localScale;
                    originalSizes[i] = gridButtons[i].rectTransform.sizeDelta;
                }
            }
        }
    }

    private void OnEnable()
    {
        IdleMinigame(); 
    }

    public void IdleMinigame()
    {
        isRoundActive = false;
        isBlindModeRound = false;
        StopTwistCoroutines();
        StopAllCoroutines();
        ResetGridVisuals();
        if (instructionText != null)
        {
            instructionText.text = "";
            instructionText.color = originalInstructionColor;
        }
        if (timerText != null) timerText.text = "";
        if (timerBar != null) timerBar.fillAmount = 0f;
    }

    private void StopTwistCoroutines()
    {
        if (promptFadeCoroutine != null)
        {
            StopCoroutine(promptFadeCoroutine);
            promptFadeCoroutine = null;
        }

        for (int i = 0; i < activeButtonCoroutines.Count; i++)
        {
            if (activeButtonCoroutines[i] != null)
            {
                StopCoroutine(activeButtonCoroutines[i]);
            }
        }
        activeButtonCoroutines.Clear();
    }

    public void StartMinigame()
    {
        if (gameManager == null)
        {
            Debug.LogError("ยังไม่ได้ใส่ Game Manager ในหน้าต่าง Inspector ของมินิเกม!");
            return; 
        }

        if (!hasSeenThisMinigame && dialogManager != null)
        {
            dialogManager.StartDialog("Me", new string[] { "This one... just press the exact number on the screen, right? Heh, my memory's still sharp." });
            hasSeenThisMinigame = true;
        }

        int day = gameManager.currentDay;

        if (day <= 2)
        {
            totalRounds = Random.Range(minRounds, 3);
            currentRound = 1;
            timePerRound = 12f * timerSpeedMultiplier;
        }
        else if (day >= 3 && day <= 5)
        {
            totalRounds = Random.Range(2, maxRounds + 1);
            currentRound = 1;
            timePerRound = 10f * timerSpeedMultiplier;
        }
        else if (day >= 6)
        {
            totalRounds = 3;
            currentRound = 1;
            timePerRound = 8f * timerSpeedMultiplier;
        }

        StartRound();
    }

    private void StartRound()
    {
        StopTwistCoroutines();

        currentPresses = 0;
        timer = timePerRound;
        timeSinceLastPress = 0f;

        if (timerBar != null) timerBar.fillAmount = 1f;
        
        ResetGridVisuals();

        int day = gameManager != null ? gameManager.currentDay : 1;
        isBlindModeRound = enableBlindMode && day >= blindModeStartDay && Random.value <= blindModeChance;

        int actualMax = Mathf.Min(maxPresses, gridButtons.Length);
        targetPresses = Random.Range(minPresses, actualMax + 1);

        if (instructionText != null)
        {
            instructionText.color = originalInstructionColor;
            if (isBlindModeRound)
            {
                instructionText.text = $"Press {targetPresses} Times..";
            }
            else
            {
                instructionText.text = $"Press {targetPresses} Times..";
            }
        }

        isRoundActive = true;

        // หากเปิดใช้โหมดข้อความหาย (Day 3+ หรือเมื่ออยู่ใน Blind Mode) ให้โผล่ช่วงแรกแล้วกะพริบดับไป
        bool shouldVanishPrompt = (enableVanishingPrompt && day >= vanishPromptStartDay) || isBlindModeRound;
        if (shouldVanishPrompt)
        {
            promptFadeCoroutine = StartCoroutine(VanishingPromptRoutine());
        }
    }

    private IEnumerator VanishingPromptRoutine()
    {
        if (instructionText == null) yield break;

        Color baseCol = originalInstructionColor;
        baseCol.a = 1f;
        instructionText.color = baseCol;

        // 1. แสดงข้อความนิ่งๆ ช่วงแรกให้ผู้เล่นอ่านและจำ
        yield return new WaitForSeconds(promptVisibleDuration);
        if (!isRoundActive) yield break;

        // 2. กะพริบถี่ขึ้นเรื่อยๆ พร้อมค่อยๆ หรี่จางจนดับหายไป
        float elapsed = 0f;
        float dur = Mathf.Max(promptFadeDuration, 0.05f);
        while (elapsed < dur)
        {
            if (!isRoundActive) yield break;
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / dur);
            float pulse = 0.5f + 0.5f * Mathf.Cos(p * p * Mathf.PI * 6f);
            baseCol.a = (1f - p) * (0.25f + 0.75f * pulse);
            instructionText.color = baseCol;
            yield return null;
        }

        if (!isRoundActive) yield break;

        // 3. ดับหายไป ผู้เล่นต้องจำตัวเลขให้ได้
        instructionText.text = "";
        instructionText.color = originalInstructionColor;
        promptFadeCoroutine = null;
    }

    private void Update()
    {
        if (!isRoundActive) return;

        timer -= Time.deltaTime;
        if (timerText != null) timerText.text = Mathf.Ceil(Mathf.Max(timer, 0f)).ToString() + "s";
        if (timerBar != null) timerBar.fillAmount = Mathf.Clamp01(timer / timePerRound);

        if (currentPresses > 0)
        {
            timeSinceLastPress += Time.deltaTime;
        }

        // กดครบตามเป้า -> รอเวลาสั้นๆ แล้วตัดผ่านทันที
        if (currentPresses == targetPresses)
        {
            if (timeSinceLastPress >= autoSubmitDelay)
            {
                CheckRoundResult();
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("tileClick");

            if (currentPresses < gridButtons.Length)
            {
                int idx = currentPresses;
                Image targetButton = gridButtons[idx];
                if (targetButton != null)
                {
                    Sprite normSpr = (roundNormalSprites != null && idx < roundNormalSprites.Length)
                        ? roundNormalSprites[idx]
                        : normalWhiteSprite;
                    Sprite glowSpr = (normSpr == normalYellowSprite) ? glowingYellowSprite : glowingWhiteSprite;

                    if (isBlindModeRound)
                    {
                        Coroutine c = StartCoroutine(BlinkAndDarkenButtonRoutine(targetButton, normSpr, glowSpr));
                        activeButtonCoroutines.Add(c);
                    }
                    else
                    {
                        targetButton.sprite = glowSpr;
                    }

                    Vector3 baseScale = (originalScales != null && idx < originalScales.Length) 
                        ? originalScales[idx] 
                        : targetButton.rectTransform.localScale;

                    StartCoroutine(PopButtonRoutine(targetButton.rectTransform, baseScale));
                }
            }

            currentPresses++;
            timeSinceLastPress = 0f;

            if (currentPresses > targetPresses)
            {
                CheckRoundResult();
                return;
            }
        }
        
        if (timer <= 0)
        {
            CheckRoundResult();
        }
    }

    private IEnumerator BlinkAndDarkenButtonRoutine(Image btn, Sprite normalSpr, Sprite glowSpr)
    {
        if (btn == null) yield break;

        for (int i = 0; i < blindButtonBlinkCount; i++)
        {
            if (!isRoundActive) yield break;
            btn.sprite = glowSpr;
            yield return new WaitForSeconds(blindButtonBlinkInterval);

            if (!isRoundActive) yield break;
            btn.sprite = normalSpr;
            yield return new WaitForSeconds(blindButtonBlinkInterval);
        }

        if (isRoundActive && btn != null)
        {
            btn.sprite = normalSpr;
        }
    }

    private void ResetGridVisuals()
    {
        if (gridButtons == null) return;
        if (originalScales == null || originalScales.Length == 0 || roundNormalSprites == null || roundNormalSprites.Length != gridButtons.Length)
        {
            CacheOriginalTransforms();
        }
        
        for (int i = 0; i < gridButtons.Length; i++)
        {
            Image btn = gridButtons[i];
            if (btn != null)
            {
                Sprite chosenNormal = Random.value > 0.5f ? normalWhiteSprite : normalYellowSprite;
                btn.sprite = chosenNormal;
                if (roundNormalSprites != null && i < roundNormalSprites.Length)
                {
                    roundNormalSprites[i] = chosenNormal;
                }

                // คืนค่า Scale เดิม (0.26) และ SizeDelta (87.52, 71.88) ตามหน้า Editor ไม่ขยายบวมเป็น 1.0
                if (originalScales != null && i < originalScales.Length)
                    btn.rectTransform.localScale = originalScales[i];

                if (originalSizes != null && i < originalSizes.Length)
                    btn.rectTransform.sizeDelta = originalSizes[i];
            }
        }
    }

    // แอนิเมชันเด้งอิงตาม Base Scale เดิม (0.26)
    private IEnumerator PopButtonRoutine(RectTransform btnRect, Vector3 baseScale)
    {
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            float scaleMultiplier = Mathf.Lerp(1f, popScaleMultiplier, Mathf.PingPong(t * 2f, 1f));
            btnRect.localScale = baseScale * scaleMultiplier;
            yield return null;
        }
        btnRect.localScale = baseScale;
    }

    private void CheckRoundResult()
    {
        isRoundActive = false;
        StopTwistCoroutines();

        if (instructionText != null) instructionText.color = originalInstructionColor;
        if (timerText != null) timerText.text = "0s";
        if (timerBar != null) timerBar.fillAmount = 0f;

        if (currentPresses == targetPresses)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("hitSound");

            if (currentRound >= totalRounds)
            {
                if (instructionText != null) instructionText.text = "PASS";
                StartCoroutine(EndMinigameRoutine(true));
            }
            else
            {
                if (instructionText != null) instructionText.text = "OK...";
                currentRound++;
                StartCoroutine(WaitAndStartNextRound());
            }
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("missSound");

            for (int i = 0; i < currentPresses; i++)
            {
                if (i < gridButtons.Length && gridButtons[i] != null) 
                    gridButtons[i].sprite = redFailSprite;
            }

            if (instructionText != null)
            {
                if (currentPresses > targetPresses)
                    instructionText.text = $"OVERLOAD! ({currentPresses}/{targetPresses})";
                else
                    instructionText.text = $"TIME OUT! ({currentPresses}/{targetPresses})";
            }

            StartCoroutine(EndMinigameRoutine(false)); 
        }
    }

    private IEnumerator WaitAndStartNextRound()
    {
        yield return new WaitForSeconds(delayBetweenRounds); 
        StartRound(); 
    }

    private IEnumerator EndMinigameRoutine(bool isSuccess)
    {
        yield return new WaitForSeconds(0.6f);

        if (gameManager != null)
        {
            gameManager.FinishMinigame(isSuccess);
        }
    }
}
