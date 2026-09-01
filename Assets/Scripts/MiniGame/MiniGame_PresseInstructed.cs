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

    [Header("Sprites (สลับภาพตาม Assets ใหม่)")]
    public Sprite normalWhiteSprite;   
    public Sprite normalYellowSprite;  
    public Sprite glowingWhiteSprite;  
    public Sprite glowingYellowSprite; 
    public Sprite redFailSprite;       

    [Header("Juice")]
    public float popDuration = 0.15f; 
    public float popScale = 1.2f;

    [Header("Minigame Settings")]
    public int minRounds = 1;                 
    public int maxRounds = 5;                 
    public int minPresses = 1;                
    public int maxPresses = 20; 
    public float timePerRound;           
    public float delayBetweenRounds = 0.5f;

    [Range(0f, 1f)]
    public float sameAsLastChance = 0.3f;     

    [Header("Fast Check Settings")]
    public float autoSubmitDelay = 0.5f;

    private int totalRounds;
    private int currentRound;
    private int targetPresses;
    private int currentPresses;
    private int previousTargetPresses;

    private float timer;
    private float timeSinceLastPress;
    private bool isRoundActive = false;

    private void OnEnable()
    {
        ResetGridVisuals();
        StartMinigame();
    }

    public void StartMinigame()
    {
        if (gameManager == null)
        {
            Debug.LogError("ยังไม่ได้ใส่ Game Manager ในหน้าต่าง Inspector ของมินิเกม กด!");
            return; 
        }

        int day = gameManager.currentDay;

        if (day == 1)
        {
            totalRounds = 1;
            currentRound = 1;
            previousTargetPresses = 0;
            timePerRound = 10f;
        }
        else if (day >= 2 && day <= 5)
        {
            totalRounds = Random.Range(minRounds, maxRounds + 1);
            currentRound = 1;
            previousTargetPresses = 0;
            timePerRound = 8f;
        }
        else if (day >= 6)
        {
            totalRounds = 4;
            currentRound = 1;
            previousTargetPresses = 0;
            timePerRound = 5f;
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

        if (currentRound > 1 && Random.value <= sameAsLastChance)
        {
            targetPresses = previousTargetPresses;
            instructionText.text = "Press the same number as last time..";
        }
        else
        {
            int actualMax = Mathf.Min(maxPresses, gridButtons.Length);
            targetPresses = Random.Range(minPresses, actualMax + 1);
            instructionText.text = "Press " + targetPresses + " Times..";
        }

        isRoundActive = true;
    }

    private void Update()
    {
        if (!isRoundActive) return;

        timer -= Time.deltaTime;
        timerText.text = Mathf.Ceil(timer).ToString() + "s";

        if (timerBar != null) timerBar.fillAmount = timer / timePerRound;

        if (currentPresses > 0)
        {
            timeSinceLastPress += Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (currentPresses < gridButtons.Length)
            {
                Image targetButton = gridButtons[currentPresses];
                
                // สลับแค่ Sprite อย่างเดียว ไม่ยุ่งกับขนาด
                if (targetButton.sprite == normalWhiteSprite)
                {
                    targetButton.sprite = glowingWhiteSprite;
                }
                else if (targetButton.sprite == normalYellowSprite)
                {
                    targetButton.sprite = glowingYellowSprite;
                }
                
                StartCoroutine(PopButtonRoutine(targetButton.rectTransform));
            }

            currentPresses++;
            timeSinceLastPress = 0f;

            if (currentPresses > targetPresses)
            {
                CheckRoundResult();
                return;
            }
        }

        if (currentPresses == targetPresses && timeSinceLastPress > autoSubmitDelay)
        {
            CheckRoundResult();
        }

        if (timer <= 0)
        {
            CheckRoundResult();
        }
    }

    private void ResetGridVisuals()
    {
        if (gridButtons == null) return;
        
        foreach (Image btn in gridButtons)
        {
            if (btn != null)
            {
                btn.sprite = Random.value > 0.5f ? normalWhiteSprite : normalYellowSprite;
                btn.rectTransform.localScale = Vector3.one; 
            }
        }
    }

    private IEnumerator PopButtonRoutine(RectTransform btnRect)
    {
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            float currentScale = Mathf.Lerp(1f, popScale, Mathf.PingPong(t * 2f, 1f));
            btnRect.localScale = new Vector3(currentScale, currentScale, 1f);
            yield return null;
        }
        btnRect.localScale = Vector3.one;
    }

    private void CheckRoundResult()
    {
        isRoundActive = false;
        timerText.text = "0s";
        if (timerBar != null) timerBar.fillAmount = 0f;

        if (currentPresses == targetPresses)
        {
            if (currentRound >= totalRounds)
            {
                instructionText.text = "Pass";
                StartCoroutine(EndMinigameRoutine(true));
            }
            else
            {
                instructionText.text = "....";
                previousTargetPresses = targetPresses;
                currentRound++;
                StartCoroutine(WaitAndStartNextRound());
            }
        }
        else
        {
            for (int i = 0; i < currentPresses; i++)
            {
                if (i < gridButtons.Length) gridButtons[i].sprite = redFailSprite;
            }

            if (currentPresses > targetPresses)
                instructionText.text = "OVERLOAD! You pressed " + currentPresses + " / " + targetPresses;
            else
                instructionText.text = "FAIL! You pressed " + currentPresses + " / " + targetPresses;

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
        yield return new WaitForSeconds(0.8f);

        if (gameManager != null)
        {
            gameManager.FinishMinigame(isSuccess);
        }

        gameObject.SetActive(false);
    }
}