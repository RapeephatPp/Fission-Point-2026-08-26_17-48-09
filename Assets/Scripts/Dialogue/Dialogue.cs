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
    public float typeSpeed = 0.04f;           
    public float autoAdvanceDelay = 2.0f;     

    private string[] currentLines;
    private int lineIndex;
    private Coroutine dialogCoroutine;

    public System.Action OnDialogFinished;

    private void Start()
    {
        if (dialogPanel != null) dialogPanel.SetActive(false);
    }

    public void StartDialog(string speakerName, string[] lines)
    {
        dialogPanel.SetActive(true);
        if (speakerText != null) speakerText.text = speakerName;
        currentLines = lines;
        lineIndex = 0;

       

        if (dialogCoroutine != null) StopCoroutine(dialogCoroutine);
        dialogCoroutine = StartCoroutine(PlayDialogRoutine());
    }

    private IEnumerator PlayDialogRoutine()
    {
        
        while (lineIndex < currentLines.Length)
        {
            dialogText.text = "";

            
            foreach (char c in currentLines[lineIndex].ToCharArray())
            {
                dialogText.text += c;
                yield return new WaitForSecondsRealtime(typeSpeed);
            }

            
            yield return new WaitForSecondsRealtime(autoAdvanceDelay);

            
            lineIndex++;
        }

        // เมื่อครบทุกบรรทัด จะปิด UI และคืนค่าเวลาให้เกมเดินต่อ
        if (dialogPanel != null) dialogPanel.SetActive(false);
        Time.timeScale = 1f;

        OnDialogFinished?.Invoke(); 
    }
}