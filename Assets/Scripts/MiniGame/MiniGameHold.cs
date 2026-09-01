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

    [Header("UI Elements")]
    public Image playerGauge;          
    public RectTransform targetZone;   
    public Image errorBar;

    [Header("Button Visual (EF Botton)")]
    public RectTransform buttonVisual;       
    public float pressedScale = 0.9f;        
    public float buttonLerpSpeed = 15f;

    [Header("Game Settings")]
    public float surviveTime = 10f;    
    public float gaugeUpSpeed = 1.5f;  
    public float gaugeDownSpeed = 2f;  

    [Header("Zone & Penalty Settings")]
    public float zoneSize = 0.25f;       
    public float errorPenalty ;    
    public float errorRecover = 0.2f;

    [Header("Moving Zone")] 
    [Range(0f, 1f)]
    public float movingChance = 0.7f;      
    public float moveSpeed = 0.3f;

    private float currentGauge = 0f;
    private float currentError = 0f;
    private float timer = 0f;

    private float targetMin = 0f;
    private float targetMax = 0f;

    private bool isGameActive = false;
    private bool isZoneMoving = false;
    private float randomTimeOffset = 0f;

    private Vector3 originalButtonScale;
    
    private void Awake()
    {
        if (buttonVisual != null)
        {
            originalButtonScale = buttonVisual.localScale;
        }
    }

    private void OnEnable()
    {
        if (buttonVisual != null)
        {
            buttonVisual.localScale = originalButtonScale;
        }
        StartMinigame();
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
        if (playerGauge != null) playerGauge.fillAmount = 0f;
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
            {
                buttonVisual.localScale = Vector3.Lerp(buttonVisual.localScale, originalButtonScale * pressedScale, Time.deltaTime * buttonLerpSpeed);
            }
        }
        else
        {
            currentGauge -= gaugeDownSpeed * Time.deltaTime;
            if (buttonVisual != null)
            {
                buttonVisual.localScale = Vector3.Lerp(buttonVisual.localScale, originalButtonScale, Time.deltaTime * buttonLerpSpeed);
            }
        }

        currentGauge = Mathf.Clamp01(currentGauge);
        if (playerGauge != null) playerGauge.fillAmount = currentGauge;

        
        bool isInZone = currentGauge >= targetMin && currentGauge <= targetMax;

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

        if (targetZone != null)
        {
            targetZone.anchorMin = new Vector2(0, targetMin);
            targetZone.anchorMax = new Vector2(1, targetMax);
            targetZone.offsetMin = Vector2.zero;
            targetZone.offsetMax = Vector2.zero;
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

        gameObject.SetActive(false);
    }
}
