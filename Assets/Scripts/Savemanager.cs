using UnityEngine;

/// <summary>
/// Top End War — Kayit/Yukle Sistemi (Claude)
///
/// UNITY KURULUM:
///   Hierarchy -> Create Empty -> "SaveManager" -> bu scripti ekle.
///   Baska hicbir sey yapma.
///
/// NE KAYDEDER:
///   - En iyi CP (highScore)
///   - En iyi mesafe (highDistance)
///   - Toplam oynama sayisi
///   - Toplam düsman oldurme
///
/// NE KAYDETMEZ (sezon bazli, her oyunda sifirlanir):
///   - Askerler, mevcut CP, tier
///
/// GELECEK:
///   - Equipment/Pet secimi kalici olacak (JSON'a gecince)
///   - Bölge ilerleme haritasi (hangi sehir ele gecirildi)
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // ── Save Anahtarlari ──────────────────────────────────────────────────
    const string KEY_HIGH_CP       = "tew_high_cp";
    const string KEY_HIGH_DIST     = "tew_high_dist";
    const string KEY_TOTAL_RUNS    = "tew_total_runs";
    const string KEY_TOTAL_KILLS   = "tew_total_kills";
    const string KEY_BEST_SOLDIERS = "tew_best_soldiers";

    // ── Mevcut Skor (o anki oyun icin) ───────────────────────────────────
    public int   CurrentRunKills    { get; private set; } = 0;
    public float CurrentRunStartTime{ get; private set; }

    // ── Cached Degerler ───────────────────────────────────────────────────
    public int   HighScoreCP       { get; private set; }
    public float HighScoreDistance { get; private set; }
    public int   TotalRuns         { get; private set; }
    public int   TotalKills        { get; private set; }
    public int   BestSoldierCount  { get; private set; }

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // sahne degisince kaybolmasin

        Load();
        CurrentRunStartTime = Time.time;
        Debug.Log($"[Save] Yukle tamam | En iyi CP: {HighScoreCP:N0} | En iyi mesafe: {HighScoreDistance:N0}m");
    }

    void Start()
    {
        GameEvents.OnGameOver += OnGameOver;
    }

    void OnDestroy()
    {
        GameEvents.OnGameOver -= OnGameOver;
    }

    // ── Kaydet ───────────────────────────────────────────────────────────
    void OnGameOver()
    {
        int   cp   = PlayerStats.Instance?.CP ?? 0;
        float dist = PlayerStats.Instance?.transform.position.z ?? 0f;
        int   soldiers = ArmyManager.Instance?.SoldierCount ?? 0;

        // En iyi skorlari guncelle
        bool newCPRecord   = cp   > HighScoreCP;
        bool newDistRecord = dist > HighScoreDistance;

        if (newCPRecord)       HighScoreCP       = cp;
        if (newDistRecord)     HighScoreDistance = dist;
        if (soldiers > BestSoldierCount) BestSoldierCount = soldiers;

        TotalRuns++;
        TotalKills += CurrentRunKills;

        Save();

        // Event'leri tetikle (GameOverUI bunlari dinleyecek)
        if (newCPRecord)
            GameEvents.OnSynergyFound?.Invoke($"YENİ REKOR: {cp:N0} CP!");

        Debug.Log($"[Save] Run bitti | CP={cp} | Dist={dist:N0}m | Runs={TotalRuns}");
    }

    // ── Dusmanı Say ──────────────────────────────────────────────────────
    public void RegisterKill() => CurrentRunKills++;

    // ── PlayerPrefs IO ───────────────────────────────────────────────────
    public void Save()
    {
        PlayerPrefs.SetInt(KEY_HIGH_CP,       HighScoreCP);
        PlayerPrefs.SetFloat(KEY_HIGH_DIST,   HighScoreDistance);
        PlayerPrefs.SetInt(KEY_TOTAL_RUNS,    TotalRuns);
        PlayerPrefs.SetInt(KEY_TOTAL_KILLS,   TotalKills);
        PlayerPrefs.SetInt(KEY_BEST_SOLDIERS, BestSoldierCount);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        HighScoreCP       = PlayerPrefs.GetInt(KEY_HIGH_CP,       0);
        HighScoreDistance = PlayerPrefs.GetFloat(KEY_HIGH_DIST,   0f);
        TotalRuns         = PlayerPrefs.GetInt(KEY_TOTAL_RUNS,    0);
        TotalKills        = PlayerPrefs.GetInt(KEY_TOTAL_KILLS,   0);
        BestSoldierCount  = PlayerPrefs.GetInt(KEY_BEST_SOLDIERS, 0);
    }

    /// <summary>Tum kayitlari sil (debug/test icin).</summary>
    public void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        Load();
        Debug.Log("[Save] Tum veriler silindi.");
    }
}