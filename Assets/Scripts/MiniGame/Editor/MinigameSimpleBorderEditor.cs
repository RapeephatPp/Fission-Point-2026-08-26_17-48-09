using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MinigameSimpleBorder))]
public class MinigameSimpleBorderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MinigameSimpleBorder border = (MinigameSimpleBorder)target;
        RectTransform rt = border.GetComponent<RectTransform>();

        EditorGUILayout.Space(6);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("🔲 Minigame Shader Border (Box SDF)", headerStyle);
        EditorGUILayout.Space(4);

        // 1. กล่องควบคุม Live Preview ใน Edit Mode
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("👁️ Scene & Game View Live Preview (Edit Mode)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("กดปุ่มด้านล่างเพื่อเลือกดูและปรับขนาดกรอบของมินิเกมนั้นๆ ได้ทันทีใน Scene View!", MessageType.Info);

        if (border.profiles != null && border.profiles.Length > 0)
        {
            int columns = 2;
            for (int i = 0; i < border.profiles.Length; i += columns)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < columns; c++)
                {
                    int index = i + c;
                    if (index < border.profiles.Length)
                    {
                        var p = border.profiles[index];
                        string label = string.IsNullOrEmpty(p.minigameName) ? $"Profile {index}" : p.minigameName;

                        bool isSelected = (border.ActiveProfileIndex == index) && border.gameObject.activeInHierarchy;
                        GUI.backgroundColor = isSelected ? new Color(0.3f, 1.0f, 0.4f) : new Color(1.0f, 0.85f, 0.2f);

                        if (GUILayout.Button($"🔍 {label}", GUILayout.Height(28)))
                        {
                            Undo.RecordObject(border, $"Select Profile {index}");
                            border.gameObject.SetActive(true);
                            CanvasGroup cg = border.GetComponent<CanvasGroup>();
                            if (cg != null) cg.alpha = 1.0f;

                            border.ApplyProfile(index);
                            border.UpdateShaderProperties();
                            EditorUtility.SetDirty(border);
                            SceneView.RepaintAll();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(1.0f, 0.4f, 0.4f);
            if (GUILayout.Button("❌ ปิดการแสดงกรอบ (Hide Border)", GUILayout.Height(26)))
            {
                Undo.RecordObject(border, "Hide Border");
                CanvasGroup cg = border.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0f;
                border.gameObject.SetActive(false);
                EditorUtility.SetDirty(border);
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 2. กล่อง Quick Adjust สำหรับ Profile ที่กำลัง Active อยู่
        int activeIdx = border.ActiveProfileIndex;
        if (activeIdx >= 0 && border.profiles != null && activeIdx < border.profiles.Length && border.gameObject.activeInHierarchy)
        {
            var p = border.profiles[activeIdx];
            EditorGUILayout.BeginVertical("box");
            GUIStyle subHeader = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.9f, 0.75f, 0.1f) } };
            EditorGUILayout.LabelField($"✏️ กำลังปรับแต่ง: [{p.minigameName}]", subHeader);

            EditorGUI.BeginChangeCheck();
            Vector2 newPos = EditorGUILayout.Vector2Field("Anchored Position (X, Y)", rt.anchoredPosition);
            Vector2 newSize = EditorGUILayout.Vector2Field("Size (Width, Height)", rt.sizeDelta);
            Color newCol = EditorGUILayout.ColorField("Border Color", p.borderColor);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new Object[] { border, rt }, "Adjust Border Transform");
                rt.anchoredPosition = newPos;
                rt.sizeDelta = newSize;
                p.anchoredPosition = newPos;
                p.size = newSize;
                p.borderColor = newCol;
                border.SetBorderColor(newCol);
                border.UpdateShaderProperties();
                EditorUtility.SetDirty(border);
                EditorUtility.SetDirty(rt);
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1.0f);
            if (GUILayout.Button("💾 ซิงก์ค่าจาก RectTransform ปัจจุบัน", GUILayout.Height(24)))
            {
                Undo.RecordObject(border, "Sync From RectTransform");
                border.SaveCurrentRectToActiveProfile();
                border.UpdateShaderProperties();
                EditorUtility.SetDirty(border);
                SceneView.RepaintAll();
            }

            if (p.referenceTransform != null)
            {
                GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
                if (GUILayout.Button($"📐 Snap จาก {p.referenceTransform.name}", GUILayout.Height(24)))
                {
                    Undo.RecordObjects(new Object[] { border, rt }, $"Snap {p.minigameName}");
                    border.SnapProfileFromReference(activeIdx);
                    EditorUtility.SetDirty(border);
                    EditorUtility.SetDirty(rt);
                    SceneView.RepaintAll();
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);
        }

        // 3. วาด Inspector ปกติสำหรับแก้ค่า Shader พารามิเตอร์ และ Profiles ทั้งหมด
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            if (activeIdx >= 0 && border.profiles != null && activeIdx < border.profiles.Length)
            {
                var p = border.profiles[activeIdx];
                rt.anchoredPosition = p.anchoredPosition;
                rt.sizeDelta = p.size;
            }
            border.UpdateShaderProperties();
            EditorUtility.SetDirty(border);
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        MinigameSimpleBorder border = (MinigameSimpleBorder)target;
        if (border == null || !border.gameObject.activeInHierarchy) return;

        RectTransform rt = border.GetComponent<RectTransform>();
        if (rt != null)
        {
            // ตรวจสอบว่าผู้ใช้ขยับ RectTransform ใน Scene View หรือไม่
            if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.Used)
            {
                border.SaveCurrentRectToActiveProfile();
                border.UpdateShaderProperties();
            }

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Handles.color = new Color(1.0f, 0.85f, 0.1f, 0.7f);
            Handles.DrawLine(corners[0], corners[1]);
            Handles.DrawLine(corners[1], corners[2]);
            Handles.DrawLine(corners[2], corners[3]);
            Handles.DrawLine(corners[3], corners[0]);

            Vector3 center = (corners[0] + corners[2]) * 0.5f;
            Handles.Label(center + Vector3.up * 16f, "[Minigame Box SDF Border]", EditorStyles.boldLabel);
        }
    }
}
