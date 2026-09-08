using UnityEngine;
using System.Collections;
using TMPro;

public class Dialogue : MonoBehaviour
{
   [Header("UI Elements")]
    public GameObject dialogPanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogText;

    [Header("Settings")]
    public float typeSpeed = 0.03f;

    private string[] currentLines;
    private int lineIndex;
    private bool isTyping;
    private Coroutine typingCoroutine;

    public System.Action OnDialogFinished;

    private void Start()
    {
        if (dialogPanel != null) dialogPanel.SetActive(false);
    }

    private void Update()
    {
        if (!dialogPanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                StopCoroutine(typingCoroutine);
                dialogText.text = currentLines[lineIndex];
                isTyping = false;
            }
            else
            {
                NextLine();
            }
        }
    }

    public void StartDialog(string speakerName, string[] lines)
    {
        dialogPanel.SetActive(true);
        speakerText.text = speakerName;
        currentLines = lines;
        lineIndex = 0;
        
        Time.timeScale = 0f; 
        
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLine());
    }

    private IEnumerator TypeLine()
    {
        isTyping = true;
        dialogText.text = "";

        foreach (char c in currentLines[lineIndex].ToCharArray())
        {
            dialogText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed); 
        }

        isTyping = false;
    }

    private void NextLine()
    {
        lineIndex++;
        if (lineIndex < currentLines.Length)
        {
            typingCoroutine = StartCoroutine(TypeLine());
        }
        else
        {
            dialogPanel.SetActive(false);
            Time.timeScale = 1f; 
            OnDialogFinished?.Invoke();
        }
    }
}