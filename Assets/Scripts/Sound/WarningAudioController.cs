using UnityEngine;
using UnityEngine.UI;

public class WarningAudioController : MonoBehaviour
{
    private Image warningImage;
    private AudioSource audioSource;
    private bool isCurrentlyVisible;
    private bool isInitialized;

    private void Awake()
    {
        warningImage = GetComponent<Image>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (warningImage != null)
        {
            // บันทึกสถานะเริ่มต้น เพื่อข้ามไม่ให้เสียงดังตอนกดเริ่มเกม
            isCurrentlyVisible = warningImage.enabled && warningImage.color.a > 0.1f;
        }
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || warningImage == null || audioSource == null) return;

        // ตรวจจับว่ารูปภาพโชว์สีแดงขึ้นมาบนจอจริงหรือไม่
        bool visibleNow = warningImage.enabled && warningImage.color.a > 0.1f;

        // ภาพเปลี่ยนจาก "ซ่อน" เป็น "แสดงผล" -> เล่นเสียง
        if (visibleNow && !isCurrentlyVisible)
        {
            audioSource.Play();
            isCurrentlyVisible = true;
        }
        // ภาพเปลี่ยนจาก "แสดงผล" เป็น "ซ่อน" -> หยุดเสียง
        else if (!visibleNow && isCurrentlyVisible)
        {
            audioSource.Stop();
            isCurrentlyVisible = false;
        }
    }

    private void OnDisable()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}