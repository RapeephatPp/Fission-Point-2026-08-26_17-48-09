using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.RemoteConfig;

[DisallowMultipleComponent]
public class ChristmasRemoteConfigManager : MonoBehaviour
{
    public struct userAttributes {}
    public struct appAttributes {}

    public enum FestivalType
    {
        None,
        Christmas,
        Halloween
    }

    public enum ConfigMode
    {
        LiveUgsRemoteConfig,         // ดึงข้อมูลจริงจาก Unity Gaming Services (UGS) Remote Config
        SimulateGameOverridesDate,   // จำลองการตั้งกฎช่วงวันเวลาของ Game Overrides (ตรวจเช็ควันที่)
        ForceChristmasOn,            // บังคับเปิดอีเวนต์คริสต์มาส (สำหรับทดสอบทันที)
        ForceHalloweenOn,            // บังคับเปิดอีเวนต์ฮาโลวีน (สำหรับทดสอบกฎ Override เทศกาลอื่น)
        ForceFestivalOff             // บังคับปิดเทศกาล (กลับสู่ Base Config)
    }

    [Header("Operation Mode")]
    [Tooltip("เลือกรูปแบบการทำงาน: Live UGS, จำลองกฎ Game Overrides ตามวันที่, หรือทดสอบเปิด/ปิด")]
    public ConfigMode configMode = ConfigMode.LiveUgsRemoteConfig;

    [Header("Target References")]
    [Tooltip("ตัวควบคุมเอฟเฟกต์อนุภาค (หิมะ / สะเก็ดไฟฮาโลวีน)")]
    public SnowParticleController particleController;
    [Tooltip("ตัวควบคุมห้องควบคุม (สำหรับส่ง Notification)")]
    public ControlRoomManager controlRoomManager;

    [Header("Game Overrides Scheduling Simulation")]
    [Tooltip("ใช้วันเวลาจริงของระบบ หรือใช้วันเวลาจำลองด้านล่าง")]
    public bool useSystemClock = false;
    [Range(1, 12)] public int simulatedMonth = 12;
    [Range(1, 31)] public int simulatedDay = 25;

    [Header("Current Applied Values (Read-Only)")]
    [SerializeField] private FestivalType currentActiveFestival = FestivalType.None;
    [SerializeField] private float activeParticleRate = 0f;
    [SerializeField] private string activeGreetingText = "";
    [SerializeField] private string activeSource = "Not Initialized";

    [Header("Base Config Values (วันปกติ)")]
    public float baseParticleRate = 0f;
    public string baseGreeting = "";

    [Header("Christmas Override Values (24 - 26 ธ.ค.)")]
    [Range(2f, 60f)] public float christmasParticleRate = 18f;
    public string christmasGreeting = "SPECIAL PROTOCOL: MERRY CHRISTMAS OPERATOR!";
    public Color christmasColorMin = new Color(0.9f, 0.95f, 1f, 0.4f);
    public Color christmasColorMax = new Color(1f, 1f, 1f, 0.7f);

    [Header("Halloween Override Values (25 - 31 ต.ค.)")]
    [Range(2f, 60f)] public float halloweenParticleRate = 15f;
    public string halloweenGreeting = "SPOOKY PROTOCOL: HAPPY HALLOWEEN OPERATOR!";
    public Color halloweenColorMin = new Color(1f, 0.5f, 0.1f, 0.45f);
    public Color halloweenColorMax = new Color(1f, 0.2f, 0.05f, 0.75f);

    private bool hasShownGreeting = false;
    private bool isFetchingUgs = false;

    private void Awake()
    {
        if (particleController == null)
        {
            particleController = FindAnyObjectByType<SnowParticleController>();
        }

        if (controlRoomManager == null)
        {
            controlRoomManager = FindAnyObjectByType<ControlRoomManager>();
        }
    }

    private async void Start()
    {
        if (configMode == ConfigMode.LiveUgsRemoteConfig)
        {
            // เริ่มต้นด้วย Base Config (ปิดหิมะ) ก่อนดึง Remote Config สำเร็จ
            ApplyFestival(FestivalType.None, 0f, "", Color.white, Color.white, "Base Config (Waiting for Remote Config...)");
            await FetchLiveRemoteConfigAsync();
        }
        else
        {
            ApplyCurrentConfig();
        }
    }

    /// <summary>
    /// สั่งประเมินและใช้งาน Configuration ตามโหมดที่เลือก
    /// </summary>
    [ContextMenu("Apply Config Now")]
    public void ApplyCurrentConfig()
    {
        switch (configMode)
        {
            case ConfigMode.ForceChristmasOn:
                ApplyFestival(FestivalType.Christmas, christmasParticleRate, christmasGreeting, christmasColorMin, christmasColorMax, "Force Override: Christmas");
                break;

            case ConfigMode.ForceHalloweenOn:
                ApplyFestival(FestivalType.Halloween, halloweenParticleRate, halloweenGreeting, halloweenColorMin, halloweenColorMax, "Force Override: Halloween");
                break;

            case ConfigMode.ForceFestivalOff:
                ApplyFestival(FestivalType.None, baseParticleRate, baseGreeting, Color.white, Color.white, "Base Config: Normal Day (Off)");
                break;

            case ConfigMode.SimulateGameOverridesDate:
                EvaluateDateOverride();
                break;

            case ConfigMode.LiveUgsRemoteConfig:
                if (Application.isPlaying)
                {
                    _ = FetchLiveRemoteConfigAsync();
                }
                else
                {
                    ReadFromAppConfig("Inspector Evaluation");
                }
                break;
        }
    }

    /// <summary>
    /// เชื่อมต่อ UGS และดึงค่า Remote Config จาก Cloud
    /// </summary>
    public async Task FetchLiveRemoteConfigAsync()
    {
        if (isFetchingUgs) return;
        isFetchingUgs = true;

        Debug.Log("<color=#00d4ff>[RemoteConfig]</color> Initializing Unity Gaming Services & Remote Config...");

        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (AuthenticationService.Instance != null && !AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            RemoteConfigService.Instance.FetchCompleted += OnRemoteConfigFetched;
            await RemoteConfigService.Instance.FetchConfigsAsync(new userAttributes(), new appAttributes());
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[RemoteConfig] Remote Config fetch notice: {ex.Message}. Checking cached config or fallback...");
            ReadFromAppConfig("Local Cached / Fallback Config");
        }
        finally
        {
            isFetchingUgs = false;
        }
    }

    private void OnRemoteConfigFetched(ConfigResponse response)
    {
        Debug.Log($"<color=#00d4ff>[RemoteConfig]</color> Remote Config fetch completed! Origin: {response.requestOrigin}");
        ReadFromAppConfig($"Live Remote Config ({response.requestOrigin})");
    }

    /// <summary>
    /// อ่านค่าคีย์ต่างๆ เช่น eventSettings_snow, active_festival จาก Remote Config และนำมาปรับใช้ในเกม
    /// </summary>
    public void ReadFromAppConfig(string source)
    {
        bool isSnow = false;
        string festStr = "none";
        float pRate = 0f;
        string greetStr = "";

        if (RemoteConfigService.Instance != null && RemoteConfigService.Instance.appConfig != null)
        {
            isSnow = RemoteConfigService.Instance.appConfig.GetBool("eventSettings_snow", false);
            festStr = RemoteConfigService.Instance.appConfig.GetString("active_festival", "none");
            pRate = RemoteConfigService.Instance.appConfig.GetFloat("particle_intensity", 0f);
            greetStr = RemoteConfigService.Instance.appConfig.GetString("festival_greeting", "");
        }

        Debug.Log($"<color=#00d4ff>[RemoteConfig]</color> Evaluated Values: eventSettings_snow={isSnow}, active_festival='{festStr}', particle_intensity={pRate}, greeting='{greetStr}'");

        if (isSnow || string.Equals(festStr, "christmas", StringComparison.OrdinalIgnoreCase))
        {
            if (pRate <= 0f) pRate = christmasParticleRate;
            if (string.IsNullOrEmpty(greetStr)) greetStr = christmasGreeting;
            ApplyFestival(FestivalType.Christmas, pRate, greetStr, christmasColorMin, christmasColorMax, $"{source} (eventSettings_snow: True)");
        }
        else if (string.Equals(festStr, "halloween", StringComparison.OrdinalIgnoreCase))
        {
            if (pRate <= 0f) pRate = halloweenParticleRate;
            if (string.IsNullOrEmpty(greetStr)) greetStr = halloweenGreeting;
            ApplyFestival(FestivalType.Halloween, pRate, greetStr, halloweenColorMin, halloweenColorMax, $"{source} (Halloween)");
        }
        else
        {
            // หิมะปิด (eventSettings_snow = False)
            ApplyFestival(FestivalType.None, 0f, "", Color.white, Color.white, $"{source} (eventSettings_snow: False)");
        }
    }

    /// <summary>
    /// จำลองการคำนวณเงื่อนไข Game Overrides Schedule (Target Date Range)
    /// </summary>
    public void EvaluateDateOverride()
    {
        int evalMonth = useSystemClock ? DateTime.Now.Month : simulatedMonth;
        int evalDay = useSystemClock ? DateTime.Now.Day : simulatedDay;

        // 1. ตรวจสอบเงื่อนไข Christmas Override (24 - 26 ธ.ค.)
        if (evalMonth == 12 && evalDay >= 24 && evalDay <= 26)
        {
            string source = $"Game Override [Christmas Campaign] Date: {evalDay}/{evalMonth}";
            Debug.Log($"<color=#00ff88>[RemoteConfig]</color> MATCHED Game Override rule: 'Christmas Campaign' for date {evalDay}/{evalMonth}!");
            ApplyFestival(FestivalType.Christmas, christmasParticleRate, christmasGreeting, christmasColorMin, christmasColorMax, source);
            return;
        }

        // 2. ตรวจสอบเงื่อนไข Halloween Override (25 - 31 ต.ค.)
        if (evalMonth == 10 && evalDay >= 25 && evalDay <= 31)
        {
            string source = $"Game Override [Halloween Campaign] Date: {evalDay}/{evalMonth}";
            Debug.Log($"<color=#ff8800>[RemoteConfig]</color> MATCHED Game Override rule: 'Halloween Campaign' for date {evalDay}/{evalMonth}!");
            ApplyFestival(FestivalType.Halloween, halloweenParticleRate, halloweenGreeting, halloweenColorMin, halloweenColorMax, source);
            return;
        }

        // 3. วันปกติ (Base Config)
        string baseSource = $"Base Config (No event active for date: {evalDay}/{evalMonth})";
        Debug.Log($"<color=#aaaaaa>[RemoteConfig]</color> No Game Override matched for date {evalDay}/{evalMonth} -> Applying Base Config.");
        ApplyFestival(FestivalType.None, baseParticleRate, baseGreeting, Color.white, Color.white, baseSource);
    }

    /// <summary>
    /// สั่งการระบบ Particle และระบบข้อความในเกม
    /// </summary>
    public void ApplyFestival(FestivalType festival, float particleRate, string greeting, Color minCol, Color maxCol, string source)
    {
        bool festivalChanged = (currentActiveFestival != festival);
        currentActiveFestival = festival;
        activeParticleRate = particleRate;
        activeGreetingText = greeting;
        activeSource = source;

        // 1. ควบคุม Particle
        if (particleController != null)
        {
            bool hasParticles = (festival != FestivalType.None && particleRate > 0f);
            particleController.SetParticleTheme(minCol, maxCol);
            particleController.SetSnowActive(hasParticles, particleRate);
        }

        // 2. แสดงข้อความทักทาย
        if (festival != FestivalType.None && !string.IsNullOrEmpty(greeting))
        {
            if (festivalChanged || !hasShownGreeting)
            {
                hasShownGreeting = true;
                if (controlRoomManager != null)
                {
                    string tipHeader = (festival == FestivalType.Christmas) ? "FESTIVE OVERRIDE: Christmas Mode Active" : "FESTIVE OVERRIDE: Halloween Spooky Mode Active";
                    controlRoomManager.ShowNotification(greeting, tipHeader);
                }
            }
        }
        else
        {
            hasShownGreeting = false;
        }

        Debug.Log($"<color=#00ff88>[RemoteConfig]</color> Applied '{source}' -> Festival: {currentActiveFestival}, ParticleRate: {activeParticleRate}");
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyCurrentConfig();
        }
    }
}
