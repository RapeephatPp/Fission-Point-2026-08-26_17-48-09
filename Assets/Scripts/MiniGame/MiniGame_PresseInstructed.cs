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

    [Header("UI Visuals & Juice")]
    public RectTransform buttonVisual; 
    public float pressedScale = 0.85f;
    public float pressDuration = 0.15f; 
    public Color successColor = Color.green;
    public Color failColor = Color.red;

    private Image buttonImage; 
    private Color originalButtonColor;

    [Header("Minigame Settings")]
    public int minRounds = 1;                 
    public int maxRounds = 5;                 
    public int minPresses = 1;                
    public int maxPresses = 9;                
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

    private Vector3 originalButtonScale;
    private Coroutine pressCoroutine;

    private void Awake()
    {
        if (buttonVisual != null)
        {
            originalButtonScale = buttonVisual.localScale;

            buttonImage = buttonVisual.GetComponent<Image>();
            if (buttonImage != null) originalButtonColor = buttonImage.color;
        }
    }

    private void OnEnable()
    {
        if (buttonVisual != null)
        {
            buttonVisual.localScale = originalButtonScale;
            if (buttonImage != null) buttonImage.color = originalButtonColor;
        }
        StartMinigame();
    }

    public void StartMinigame()
    {
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
        if (buttonImage != null) buttonImage.color = originalButtonColor;

        if (currentRound > 1 && Random.value <= sameAsLastChance)
        {
            targetPresses = previousTargetPresses;
            instructionText.text = "Press the same number as last time..";
        }
        else
        {
            targetPresses = Random.Range(minPresses, maxPresses + 1);
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
            currentPresses++;
            timeSinceLastPress = 0f;

            if (buttonVisual != null)
            {
                if (pressCoroutine != null) StopCoroutine(pressCoroutine);
                pressCoroutine = StartCoroutine(AnimateButtonPress());
            }

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

    private IEnumerator AnimateButtonPress()
    {
        buttonVisual.localScale = originalButtonScale * pressedScale; 

        float elapsed = 0f;
        while (elapsed < pressDuration)
        {
            elapsed += Time.deltaTime;
            buttonVisual.localScale = Vector3.Lerp(originalButtonScale * pressedScale, originalButtonScale, elapsed / pressDuration);
            yield return null;
        }

        buttonVisual.localScale = originalButtonScale; 
    }

    private void CheckRoundResult()
    {
        isRoundActive = false;
        timerText.text = "0s";
        if (timerBar != null) timerBar.fillAmount = 0f;

        if (currentPresses == targetPresses)
        {
            if (buttonImage != null) buttonImage.color = successColor;

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
            if (buttonImage != null) buttonImage.color = failColor;

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