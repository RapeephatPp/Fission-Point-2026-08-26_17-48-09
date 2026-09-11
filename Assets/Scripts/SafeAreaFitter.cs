using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private Vector2 lastScreenSize = new Vector2(0, 0);
    private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        // อัปเดตทันทีเมื่อมีการหมุนจอ หรือเปลี่ยน Device ใน Simulator
        if (lastSafeArea != Screen.safeArea || 
            lastScreenSize.x != Screen.width || 
            lastScreenSize.y != Screen.height || 
            lastOrientation != Screen.orientation)
        {
            ApplySafeArea();
        }
    }

    public void ApplySafeArea()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        Rect safeArea = Screen.safeArea;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        // คำนวณ Anchor Min / Max ตามสัดส่วน Safe Area จริงของเครื่อง
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= screenSize.x;
        anchorMin.y /= screenSize.y;
        anchorMax.x /= screenSize.x;
        anchorMax.y /= screenSize.y;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;
        lastOrientation = Screen.orientation;
    }
}