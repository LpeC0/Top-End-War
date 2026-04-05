using UnityEngine;

/// <summary>
/// Top End War — Stage Yoneticisi v1 (Claude)
///
/// GOREV:
///   - Aktif World ve Stage'i tutar
///   - StageConfig SO'yu yukleyerek BossManager ve SpawnManager'a HP/yogunluk iletir
///   - Stage tamamlaninca EconomyManager'a offline boost ekler
///   - Dunya tamamlaninca WorldConfig'deki komuTanı acar + BiomeManager'i gunceller
///
/// KURULUM:
///   Hierarchy > Create Empty > "StageManager" > bu scripti ekle
///   Inspector'dan worlds[] dizisini doldur (WorldConfig SO'lari sur)
///   Resources/Stages/ klasoru olustur, StageConfig SO'lari oraya koy
///   (veya Inspector'dan stageConfigs[] dizisini doldur)
///
/// KULLANIM:
///   StageManager.Instance.LoadStage(worldID: 1, stageID: 3);
///   StageManager.Instance.OnStageComplete();  // Stage bittikten sonra
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Dunya Listesi (siralı — World 1, 2, 3...)")]
    public WorldConfig[] worlds;

    [Header("Stage Verileri (Resources/Stages/ yoksa buraya sur)")]
    [Tooltip("Bos birakılırsa Resources/Stages/Stage_W{w}_{s:D2} yolundan yuklenir")]
    public StageConfig[] stageConfigs;

    [Header("Debug (Salt Okunur)")]
    [SerializeField] int _currentWorldID = 1;
    [SerializeField] int _currentStageID = 1;

    StageConfig  _activeStage;
    WorldConfig  _activeWorld;

    // ── Yasamdongüsü ──────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Ilk stage'i yukle
        LoadStage(_currentWorldID, _currentStageID);
    }

    // ── Stage Yukle ───────────────────────────────────────────────────────
    public void LoadStage(int worldID, int stageID)
    {
        _currentWorldID = worldID;
        _currentStageID = stageID;

        // WorldConfig bul
        _activeWorld = FindWorld(worldID);
        if (_activeWorld == null)
            Debug.LogWarning($"[StageManager] World {worldID} bulunamadi!");

        // StageConfig bul
        _activeStage = FindStage(worldID, stageID);
        if (_activeStage == null)
        {
            Debug.LogWarning($"[StageManager] Stage W{worldID}-{stageID} bulunamadi! Varsayilan kullaniliyor.");
            return;
        }

        // Biyomu guncelle
        if (_activeWorld != null && BiomeManager.Instance != null)
            BiomeManager.Instance.SetBiome(_activeWorld.biome);

        // Boss HP'sini BossManager'a ilet
        if (_activeStage.isBossStage && BossManager.Instance != null)
            BossManager.Instance.bossMaxHP = Mathf.RoundToInt(_activeStage.bossHP);

        GameEvents.OnStageChanged?.Invoke(worldID, stageID);
        Debug.Log($"[StageManager] W{worldID}-{stageID} yuklendi: {_activeStage.locationName}");
    }

    // ── Stage Tamamlandi ─────────────────────────────────────────────────
    public void OnStageComplete()
    {
        if (_activeStage == null) return;

        // Offline boost ekle
        EconomyManager.Instance?.AddOfflineRate(_activeStage.offlineBoostPerHour);

        // Temel altin odulu
        EconomyManager.Instance?.AddGold(_activeStage.baseGoldReward);

        // Dunya bitti mi?
        bool worldComplete = _activeStage.isBossStage;
        if (worldComplete) OnWorldComplete();
        else
        {
            // Sonraki stage
            LoadStage(_currentWorldID, _currentStageID + 1);
        }

        Debug.Log($"[StageManager] Stage tamamlandi: W{_currentWorldID}-{_currentStageID}");
    }

    // ── Dunya Tamamlandi ─────────────────────────────────────────────────
    void OnWorldComplete()
    {
        if (_activeWorld == null) return;

        // Offline boost (dunya seviyesinde)
        EconomyManager.Instance?.AddOfflineRate(_activeWorld.offlineIncomeBoost);

        // Komutan ac
        if (_activeWorld.unlockedCommander != null)
            Debug.Log($"[StageManager] Komutan acildi: {_activeWorld.unlockedCommander.commanderName}");
            // TODO: Komutan unlock UI bildirimi

        GameEvents.OnWorldChanged?.Invoke(_currentWorldID);

        // Sonraki dunya
        LoadStage(_currentWorldID + 1, stageID: 1);
    }

    // ── Yardimcilar ───────────────────────────────────────────────────────
    WorldConfig FindWorld(int id)
    {
        if (worlds == null) return null;
        foreach (var w in worlds)
            if (w != null && w.worldID == id) return w;
        return null;
    }

    StageConfig FindStage(int worldID, int stageID)
    {
        // Once Inspector dizisine bak
        if (stageConfigs != null)
            foreach (var s in stageConfigs)
                if (s != null && s.worldID == worldID && s.stageID == stageID) return s;

        // Sonra Resources klasorune bak
        string path = $"Stages/Stage_W{worldID}_{stageID:D2}";
        return Resources.Load<StageConfig>(path);
    }

    // ── Getter'lar ────────────────────────────────────────────────────────
    public StageConfig ActiveStage  => _activeStage;
    public WorldConfig ActiveWorld  => _activeWorld;
    public int CurrentWorldID       => _currentWorldID;
    public int CurrentStageID       => _currentStageID;

    public float GetActiveMobHP()
        => _activeStage != null ? _activeStage.mobHP : 1100f;

    public float GetActiveBossHP()
        => _activeStage != null ? _activeStage.bossHP : 41000f;
}