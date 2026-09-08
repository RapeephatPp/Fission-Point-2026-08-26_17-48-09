using System.Collections;
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

    [Header("Timing & Difficulty Settings (เล่นง่ายขึ้น)")]
    [Tooltip("ตัวคูณเวลาต่อรอบ (เพิ่มเป็น 1.2 หรือ 1.5 ได้ถ้าต้องการให้นับช้าลง)")]
    public float timerSpeedMultiplier = 1.0f;
    public int minRounds = 1;                 
    public int maxRounds = 4;                 
    public int minPresses = 1;                
    public int maxPresses = 16; 
    public float delayBetweenRounds = 0.4f;

    [Range(0f, 1f)]
    public float sameAsLastChance = 0.25f;     

    [Header("Auto Submit Settings")]
    [Tooltip("เวลารอยืนยันหลังกดครบเป้าหมายแล้ว ไม่ต้องรอเวลาหมด")]
    public float autoSubmitDelay = 0.35f;

    private int totalRounds;
    private int currentRound;
    private int targetPresses;
    private int currentPresses;
    private int previousTargetPresses;

    private float timer;
    private float timePerRound;
    private float timeSinceLastPress;
    private bool isRoundActive = false;

    // 🟢 เก็บทั้ง Scale (0.26) และ SizeDelta (87.52, 71.88) เดิมไว้
    private Vector3[] originalScales;
    private Vector2[] originalSizes;

    public Dialogue dialogManager;
    private static bool hasSeenThisMinigame = false;

    private void Awake()
    {
        CacheOriginalTransforms();
    }

    private void CacheOriginalTransforms()
    {
        if (gridButtons != null && gridButtons.Length > 0)
        {
            originalScales = new Vector3[gridButtons.Length];
            originalSizes = new Vector2[gridButtons.Length];

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
        ResetGridVisuals();
        if (instructionText != null) instructionText.text = "";
        if (timerText != null) timerText.text = "";
        if (timerBar != null) timerBar.fillAmount = 0f;
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

        // ปรับเวลาให้เล่นสบายขึ้น
        if (day <= 2)
        {
            totalRounds = Random.Range(minRounds, 3);
            currentRound = 1;
            previousTargetPresses = 0;
            timePerRound = 12f * timerSpeedMultiplier;
        }
        else if (day >= 3 && day <= 5)
        {
            totalRounds = Random.Range(2, maxRounds + 1);
            currentRound = 1;
            previousTargetPresses = 0;
            timePerRound = 10f * timerSpeedMultiplier;
        }
        else if (day >= 6)
        {
            totalRounds = 3;
            currentRound = 1;
            previousTargetPresses = 0;
            timePerRound = 8f * timerSpeedMultiplier;
        }

        StartRound();
    }

    private void StartRound()
    {
        currentPresses = 0;
        timer = timePerRound;
        timeSinceLastPress = 0f;

        if (timerBar != null) timerBar.fillAmount = 1f;
        
        ResetGridVisuals(); 

        if (currentRound > 1 && Random.value <= sameAsLastChance && previousTargetPresses > 0)
        {
            targetPresses = previousTargetPresses;
            if (instructionText != null) instructionText.text = $"Press the same as last ({targetPresses})..";
        }
        else
        {
            int actualMax = Mathf.Min(maxPresses, gridButtons.Length);
            targetPresses = Random.Range(minPresses, actualMax + 1);
            if (instructionText != null) instructionText.text = $"Press {targetPresses} Times..";
        }

        isRoundActive = true;
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
            if (currentPresses < gridButtons.Length)
            {
                Image targetButton = gridButtons[currentPresses];
                if (targetButton != null)
                {
                    if (targetButton.sprite == normalWhiteSprite)
                        targetButton.sprite = glowingWhiteSprite;
                    else if (targetButton.sprite == normalYellowSprite)
                        targetButton.sprite = glowingYellowSprite;

                    Vector3 baseScale = (originalScales != null && currentPresses < originalScales.Length) 
                        ? originalScales[currentPresses] 
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

    private void ResetGridVisuals()
    {
        if (gridButtons == null) return;
        if (originalScales == null || originalScales.Length == 0) CacheOriginalTransforms();
        
        for (int i = 0; i < gridButtons.Length; i++)
        {
            Image btn = gridButtons[i];
            if (btn != null)
            {
                btn.sprite = Random.value > 0.5f ? normalWhiteSprite : normalYellowSprite;

                // 🟢 คืนค่า Scale เดิม (0.26) และ SizeDelta (87.52, 71.88) ตามหน้า Editor ไม่ขยายบวมเป็น 1.0
                if (originalScales != null && i < originalScales.Length)
                    btn.rectTransform.localScale = originalScales[i];

                if (originalSizes != null && i < originalSizes.Length)
                    btn.rectTransform.sizeDelta = originalSizes[i];
            }
        }
    }

    // 🟢 แอนิเมชันเด้งอิงตาม Base Scale เดิม (0.26)
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
        if (timerText != null) timerText.text = "0s";
        if (timerBar != null) timerBar.fillAmount = 0f;

        if (currentPresses == targetPresses)
        {
            if (currentRound >= totalRounds)
            {
                if (instructionText != null) instructionText.text = "PASS";
                StartCoroutine(EndMinigameRoutine(true));
            }
            else
            {
                if (instructionText != null) instructionText.text = "OK...";
                previousTargetPresses = targetPresses;
                currentRound++;
                StartCoroutine(WaitAndStartNextRound());
            }
        }
        else
        {
            for (int i = 0; i < currentPresses; i++)
            {
                if (i < gridButtons.Length && gridButtons[i] != null) 
                    gridButtons[i].sprite = redFailSprite;
            }

            if (currentPresses > targetPresses)
                instructionText.text = $"OVERLOAD! ({currentPresses}/{targetPresses})";
            else
                instructionText.text = $"TIME OUT! ({currentPresses}/{targetPresses})";

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