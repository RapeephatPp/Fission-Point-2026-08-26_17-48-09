using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MiniGameHold : MonoBehaviour
{
    [Header("References")]
    public ControlRoomManager gameManager;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI timerText;

    [Header("UI Elements (Updated for New Assets)")]
    public RectTransform playerIndicator; 
    public RectTransform targetZone;      
    public Image errorBar;                

    [Header("UI Movement Bounds (ปรับตำแหน่งขึ้น-ลง)")]
    [Tooltip("ตำแหน่ง Y ต่ำสุดของกรอบ (ลองเลื่อน UI ลงล่างสุดแล้วเอาค่า Pos Y มาใส่)")]
    public float minYPos = -180f;
    [Tooltip("ตำแหน่ง Y สูงสุดของกรอบ (ลองเลื่อน UI ขึ้นบนสุดแล้วเอาค่า Pos Y มาใส่)")]
    public float maxYPos = 180f;

    [Header("Button Visual (EF Botton)")]
    public RectTransform buttonVisual;       
    public float pressedScale = 0.9f;        
    public float buttonLerpSpeed = 15f;

    [Header("Game Settings")]
    public float surviveTime = 10f;    
    public float gaugeUpSpeed = 1.5f;  
    public float gaugeDownSpeed = 2f;  

    [Header("Zone & Penalty Settings")]
    [Tooltip("ความยาก (0.0 - 1.0) ขนาดเป้าหมายในการคำนวณหลังบ้าน")]
    public float zoneSize = 0.15f;       
    public float errorPenalty;    
    public float errorRecover = 0.2f;

    [Header("Moving Zone")] 
    [Range(0f, 1f)]
    public float movingChance = 0.7f;      
    public float moveSpeed = 0.3f;

    [Header("Dynamic Zone Twists (Day 3+)")]
    public bool enableDynamicZone = true;
    public int dynamicZoneStartDay = 3;
    [Range(0f, 1f)] public float zoneStutterChance = 0.45f;

    [Header("Pressure Surge Twists (Day 5+)")]
    public bool enablePressureSurge = true;
    public int pressureSurgeStartDay = 5;
    public float surgeWarningDuration = 0.7f;
    public float surgeActiveDuration = 1.4f;
    public float surgeForce = 0.65f;

    // --- State Variables ---
    private float currentGauge = 0f;
    private float currentError = 0f;
    private float timer = 0f;

    private float targetMin = 0f;
    private float targetMax = 0f;
    private float targetCenter = 0f;

    private bool isGameActive = false;
    private bool isZoneMoving = false;
    private float zoneMovePhase = 0f;
    private float currentMoveSpeed = 0.3f;
    private float baseMoveSpeed = 0.3f;
    private float nextZoneShiftTimer = 0f;
    private bool isZonePaused = false;

    private float activeSurgeForce = 0f;
    private Coroutine twistCoroutine;

    private Vector3 originalButtonScale;
    private Color originalInstructionColor = Color.white;
    private Image targetZoneImage; 
    
    private void Awake()
    {
        baseMoveSpeed = moveSpeed;

        if (buttonVisual != null)
        {
            originalButtonScale = buttonVisual.localScale;
        }

        if (targetZone != null)
        {
            targetZoneImage = targetZone.GetComponent<Image>();
        }

        if (instructionText != null)
        {
            originalInstructionColor = instructionText.color;
        }
    }

    private void OnEnable()
    {
        if (buttonVisual != null) buttonVisual.localScale = originalButtonScale;
        IdleMinigame(); 
    }
    
    // อัปเดตโหมด Idle ให้เคลียร์หลอดต่างๆ ให้เป็น 0 ด้วย จะได้ดูเหมือนปิดเครื่องอยู่
    public void IdleMinigame()
    {
        isGameActive = false;
        activeSurgeForce = 0f;
        twistCoroutine = null;
        StopAllCoroutines();

        if (instructionText != null)
        {
            instructionText.text = "";
            instructionText.color = originalInstructionColor;
        }
        if (timerText != null) timerText.text = "";
        
        if (errorBar != null) 
        {
            errorBar.fillAmount = 0f;
            errorBar.color = Color.red;
        }
        
        // ดึงแท่งสีส้มกลับลงมาล่างสุด
        if (playerIndicator != null)
        {
            playerIndicator.anchoredPosition = new Vector2(playerIndicator.anchoredPosition.x, minYPos);
        }
        
        // ดึงกล่องสีฟ้ากลับลงมาล่างสุด และเปลี่ยนสีเป็นปกติ
        if (targetZone != null)
        {
            targetZone.anchoredPosition = new Vector2(targetZone.anchoredPosition.x, minYPos);
        }
        if (targetZoneImage != null)
        {
            targetZoneImage.color = new Color(0.2f, 0.6f, 1f); 
        }
    }

    public void StartMinigame()
    {
        if (gameManager == null)
        {
            Debug.LogError("ยังไม่ได้ใส่ Game Manager ในหน้าต่าง Inspector ของมินิเกม Hold! ไปลากมาใส่ด้วย");
            return; 
        }

        if (twistCoroutine != null)
        {
            StopCoroutine(twistCoroutine);
            twistCoroutine = null;
        }

        int day = gameManager.currentDay;
        currentGauge = 0f;
        currentError = 0f;
        timer = surviveTime;
        isZoneMoving = false;
        isZonePaused = false;
        activeSurgeForce = 0f;
        currentMoveSpeed = baseMoveSpeed;

        if (day >= 3 && day < 6)
        {
            if (Random.value <= movingChance)
            {
                errorPenalty = 0.4f;
                isZoneMoving = true;               
                zoneMovePhase = Random.Range(0f, 100f);
                currentMoveSpeed = baseMoveSpeed;
            }
        }

        if (day >= 6)
        {
            errorPenalty = 0.3f;
            currentMoveSpeed = Mathf.Max(baseMoveSpeed, 0.5f);
            isZoneMoving = true;               
            zoneMovePhase = Random.Range(0f, 100f);           
        }

        nextZoneShiftTimer = Random.Range(1.2f, 2.2f);

        if (!isZoneMoving)
        {
            errorPenalty = 0.5f;
            targetMin = Random.Range(0f, 1f - zoneSize);
            UpdateZoneUI();
        }
     
        if (instructionText != null)
        {
            instructionText.text = "Hold in gauge..";
            instructionText.color = originalInstructionColor;
        }
        if (errorBar != null) errorBar.fillAmount = 0f;

        isGameActive = true;

        if (enablePressureSurge && day >= pressureSurgeStartDay)
        {
            twistCoroutine = StartCoroutine(PressureSurgeRoutine());
        }
    }

    private IEnumerator PressureSurgeRoutine()
    {
        // หน่วงเวลาให้ผู้เล่นตั้งหลักในช่วงแรก
        yield return new WaitForSeconds(Random.Range(2.2f, 3.2f));

        while (isGameActive && timer > 2.2f)
        {
            bool pushUp = Random.value > 0.5f;

            // ช่วงแจ้งเตือนล่วงหน้า (Pure ASCII Text, No Emoji)
            if (instructionText != null)
            {
                instructionText.text = "WARNING: PRESSURE SURGE!";
                instructionText.color = new Color(1f, 0.65f, 0f);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("clickSound");

            yield return new WaitForSeconds(surgeWarningDuration);

            if (!isGameActive) yield break;

            // ช่วงแรงดันกระชากทำงาน
            activeSurgeForce = pushUp ? surgeForce : -surgeForce;
            if (instructionText != null)
            {
                instructionText.text = pushUp ? "PRESSURE SURGE UP!" : "PRESSURE DROP!";
                instructionText.color = new Color(1f, 0.25f, 0.25f);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("missSound");

            yield return new WaitForSeconds(surgeActiveDuration);

            activeSurgeForce = 0f;
            if (!isGameActive) yield break;

            if (instructionText != null)
            {
                instructionText.text = "Hold in gauge..";
                instructionText.color = originalInstructionColor;
            }

            yield return new WaitForSeconds(Random.Range(2.5f, 3.5f));
        }
    }

    private void Update()
    {
        if (!isGameActive) return;

        int day = gameManager != null ? gameManager.currentDay : 1;

        if (isZoneMoving)
        {
            if (enableDynamicZone && day >= dynamicZoneStartDay)
            {
                nextZoneShiftTimer -= Time.deltaTime;
                if (nextZoneShiftTimer <= 0f)
                {
                    if (!isZonePaused && Random.value <= zoneStutterChance)
                    {
                        isZonePaused = true;
                        nextZoneShiftTimer = Random.Range(0.35f, 0.65f);
                    }
                    else
                    {
                        isZonePaused = false;
                        float dayBase = day >= 6 ? Mathf.Max(baseMoveSpeed, 0.5f) : baseMoveSpeed;
                        currentMoveSpeed = dayBase * Random.Range(0.75f, 1.45f);
                        nextZoneShiftTimer = Random.Range(1.0f, 2.0f);
                    }
                }
            }

            if (!isZonePaused)
            {
                zoneMovePhase += Time.deltaTime * currentMoveSpeed;
            }

            targetMin = Mathf.PingPong(zoneMovePhase, 1f - zoneSize);
            UpdateZoneUI();
        }

        timer -= Time.deltaTime;
        if (timerText != null) timerText.text = Mathf.Ceil(timer).ToString() + "";

        if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
        {
            currentGauge += gaugeUpSpeed * Time.deltaTime;
            if (buttonVisual != null)
                buttonVisual.localScale = Vector3.Lerp(buttonVisual.localScale, originalButtonScale * pressedScale, Time.deltaTime * buttonLerpSpeed);
        }
        else
        {
            currentGauge -= gaugeDownSpeed * Time.deltaTime;
            if (buttonVisual != null)
                buttonVisual.localScale = Vector3.Lerp(buttonVisual.localScale, originalButtonScale, Time.deltaTime * buttonLerpSpeed);
        }

        if (activeSurgeForce != 0f)
        {
            currentGauge += activeSurgeForce * Time.deltaTime;
        }

        currentGauge = Mathf.Clamp01(currentGauge);

        if (playerIndicator != null)
        {
            float visualY = Mathf.Lerp(minYPos, maxYPos, currentGauge);
            playerIndicator.anchoredPosition = new Vector2(playerIndicator.anchoredPosition.x, visualY);
        }

        bool isInZone = currentGauge >= targetMin && currentGauge <= targetMax;

        if (targetZoneImage != null)
        {
            Color baseBlue = new Color(0.2f, 0.6f, 1f); 
            targetZoneImage.color = isInZone ? Color.green : baseBlue;
        }

        if (!isInZone)
        {
            currentError += errorPenalty * Time.deltaTime;
        }
        else
        {
            currentError -= errorRecover * Time.deltaTime;
        }

        currentError = Mathf.Clamp01(currentError);

        if (errorBar != null)
        {
            errorBar.fillAmount = currentError;

            if (currentError > 0.7f)
            {
                errorBar.color = Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.time * 15f, 1f));
            }
            else
            {
                errorBar.color = Color.red;
            }
        }
    
        if (currentError >= 1f)
        {
            LoseGame();
        }
        else if (timer <= 0)
        {
            WinGame();
        }
    }

    private void UpdateZoneUI()
    {
        targetMax = targetMin + zoneSize;
        targetCenter = targetMin + (zoneSize / 2f);

        if (targetZone != null)
        {
            float targetVisualY = Mathf.Lerp(minYPos, maxYPos, targetCenter);
            targetZone.anchoredPosition = new Vector2(targetZone.anchoredPosition.x, targetVisualY);
        }
    }

    private void WinGame()
    {
        isGameActive = false;
        activeSurgeForce = 0f;
        if (twistCoroutine != null)
        {
            StopCoroutine(twistCoroutine);
            twistCoroutine = null;
        }
        if (timerText != null) timerText.text = "0";
        if (instructionText != null)
        {
            instructionText.text = "Success";
            instructionText.color = originalInstructionColor;
        }

        if (buttonVisual != null) buttonVisual.localScale = originalButtonScale;
        StartCoroutine(EndMinigameRoutine(true));
    }

    private void LoseGame()
    {
        isGameActive = false;
        activeSurgeForce = 0f;
        if (twistCoroutine != null)
        {
            StopCoroutine(twistCoroutine);
            twistCoroutine = null;
        }
        if (instructionText != null)
        {
            instructionText.text = "You failed";
            instructionText.color = originalInstructionColor;
        }

        if (buttonVisual != null) buttonVisual.localScale = originalButtonScale;
        StartCoroutine(EndMinigameRoutine(false));
    }

    private IEnumerator EndMinigameRoutine(bool isSuccess)
    {
        yield return new WaitForSeconds(1.5f);

        if (gameManager != null)
        {
            gameManager.FinishMinigame(isSuccess);
        }

    }
}
