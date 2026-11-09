using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChristmasRemoteConfigManager))]
public class ChristmasRemoteConfigManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ChristmasRemoteConfigManager manager = (ChristmasRemoteConfigManager)target;

        EditorGUILayout.Space(5);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("🎮 UGS Remote Config & Game Overrides Manager", headerStyle);
        EditorGUILayout.Space(5);

        // กล่องควบคุม Remote Config สด (เชื่อมโยงกับ Dashboard / Remote Config Window)
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("☁️ Live Remote Config", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
        if (GUILayout.Button("🔄 Fetch / Apply Live Remote Config Now", GUILayout.Height(32)))
        {
            manager.configMode = ChristmasRemoteConfigManager.ConfigMode.LiveUgsRemoteConfig;
            manager.ApplyCurrentConfig();
            EditorUtility.SetDirty(manager);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // กล่องปุ่มควบคุมสำหรับการถ่ายคลิปวิดีโอส่งงาน
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Demo Control", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.95f, 0.5f);
        if (GUILayout.Button(" Test Christmas", GUILayout.Height(30)))
        {
            manager.configMode = ChristmasRemoteConfigManager.ConfigMode.ForceChristmasOn;
            manager.ApplyCurrentConfig();
            EditorUtility.SetDirty(manager);
        }

        GUI.backgroundColor = new Color(1f, 0.6f, 0.1f);
        if (GUILayout.Button(" Test Halloween", GUILayout.Height(30)))
        {
            manager.configMode = ChristmasRemoteConfigManager.ConfigMode.ForceHalloweenOn;
            manager.ApplyCurrentConfig();
            EditorUtility.SetDirty(manager);
        }

        GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
        if (GUILayout.Button("🛑 Base Config (หยุดหิมะ)", GUILayout.Height(30)))
        {
            manager.configMode = ChristmasRemoteConfigManager.ConfigMode.ForceFestivalOff;
            manager.ApplyCurrentConfig();
            EditorUtility.SetDirty(manager);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // ปุ่มจำลองวันที่สำหรับสาธิต Game Overrides Schedule Rule
        EditorGUILayout.LabelField("📅 สาธิต Game Overrides (ตั้งกฎวันเวลาตามเทศกาล):", EditorStyles.miniBoldLabel);
        GUI.backgroundColor = new Color(0.35f, 0.75f, 1f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📅 25 Dec (Christmas)", GUILayout.Height(25)))
        {
            manager.configMode = ChristmasRemoteConfigManager.ConfigMode.SimulateGameOverridesDate;
            manager.simulatedMonth = 12;
            manager.simulatedDay = 25;
            manager.ApplyCurrentConfig();
            EditorUtility.SetDirty(manager);
        }

        if (GUILayout.Button("📅 31 Oct (Halloween)", GUILayout.Height(25)))
        {
            manager.configMode = ChristmasRemoteConfigManager.ConfigMode.SimulateGameOverridesDate;
            manager.simulatedMonth = 10;
            manager.simulatedDay = 31;
            manager.ApplyCurrentConfig();
            EditorUtility.SetDirty(manager);
        }

        if (GUILayout.Button("📅 15 July (Normal Day)", GUILayout.Height(25)))
        {
            manager.configMode = ChristmasRemoteConfigManager.ConfigMode.SimulateGameOverridesDate;
            manager.simulatedMonth = 7;
            manager.simulatedDay = 15;
            manager.ApplyCurrentConfig();
            EditorUtility.SetDirty(manager);
        }
        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        DrawDefaultInspector();

        EditorGUILayout.Space(8);
    }
}
