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

    // --- State Variables ---
    private float currentGauge = 0f;
    private float currentError = 0f;
    private float timer = 0f;

    private float targetMin = 0f;
    private float targetMax = 0f;
    private float targetCenter = 0f;

    private bool isGameActive = false;
    private bool isZoneMoving = false;
    private float randomTimeOffset = 0f;

    private Vector3 originalButtonScale;
    private Image targetZoneImage; 
    
    private void Awake()
    {
        if (buttonVisual != null)
        {
            originalButtonScale = buttonVisual.localScale;
        }

        if (targetZone != null)
        {
            targetZoneImage = targetZone.GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        if (buttonVisual != null) buttonVisual.localScale = originalButtonScale;
        IdleMinigame(); 
    }
    
    // 🟢 อัปเดตโหมด Idle ให้เคลียร์หลอดต่างๆ ให้เป็น 0 ด้วย จะได้ดูเหมือนปิดเครื่องอยู่
    public void IdleMinigame()
    {
        isGameActive = false;
        if (instructionText != null) instructionText.text = "";
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

        int day = gameManager.currentDay;
        currentGauge = 0f;
        currentError = 0f;
        timer = surviveTime;
        isZoneMoving = false;

        if (day >= 3 && day < 6)
        {
            if (Random.value <= movingChance)
            {
                errorPenalty = 0.4f;
                isZoneMoving = true;               
                randomTimeOffset = Random.Range(0f, 100f);
            }
        }

        if (day >= 6)
        {
            errorPenalty = 0.3f;
            moveSpeed = 0.5f;
            isZoneMoving = true;               
            randomTimeOffset = Random.Range(0f, 100f);           
        }

        if (!isZoneMoving)
        {
            errorPenalty = 0.5f;
            targetMin = Random.Range(0f, 1f - zoneSize);
            UpdateZoneUI();
        }
     
        if (instructionText != null) instructionText.text = "Hold in gauge..";
        if (errorBar != null) errorBar.fillAmount = 0f;

        isGameActive = true;
    }

    private void Update()
    {
        if (!isGameActive) return;

        if (isZoneMoving)
        {
            targetMin = Mathf.PingPong((Time.time + randomTimeOffset) * moveSpeed, 1f - zoneSize);
            UpdateZoneUI();
        }

        timer -= Time.deltaTime;
        if (timerText != null) timerText.text = Mathf.Ceil(timer).ToString() + "s";

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
        if (timerText != null) timerText.text = "0s";
        if (instructionText != null) instructionText.text = "success.. in this time.";

        if (buttonVisual != null) buttonVisual.localScale = originalButtonScale;
        StartCoroutine(EndMinigameRoutine(true));
    }

    private void LoseGame()
    {
        isGameActive = false;
        if (instructionText != null) instructionText.text = "You failed.";

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