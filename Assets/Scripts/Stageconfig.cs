using UnityEngine;

/// <summary>
/// Top End War — Stage Konfigurasyonu v1 (Claude)
///
/// Assets > Create > TopEndWar > StageConfig
///
/// Her stage icin ayri bir SO. StageManager bunlari yukler.
/// HP degerleri Fixed Difficulty prensibine gore sabittir —
/// oyuncunun gucune gore degismez.
///
/// DifficultyGenerator editör araci ile otomatik olusturulabilir
/// (ilerleyen surumlerde — su an manuel doldur).
/// </summary>
[CreateAssetMenu(fileName = "Stage_", menuName = "TopEndWar/StageConfig")]
public class StageConfig : ScriptableObject
{
    [Header("Kimlik")]
    public int    worldID       = 1;
    public int    stageID       = 1;
    [Tooltip("Harita ve HUD'da gosterilecek ad")]
    public string locationName  = "Sivas - Sinir Boyu";

    [Header("Dusman HP (Sabit — oyuncuya gore degismez)")]
    [Tooltip("Normal mob HP. Formul: beklenenDPS x 1.0")]
    public float  mobHP         = 90f;
    [Tooltip("Elite mob HP. Formul: beklenenDPS x 3.0")]
    public float  eliteHP       = 270f;
    [Tooltip("Boss HP. Formul: beklenenDPS x 40 (Phase Shield dahil)")]
    public float  bossHP        = 3600f;

    [Header("Spawn Hizi")]
    [Tooltip("Daha yuksek = daha sik dusman (DifficultyManager carpani yine de uygulanir)")]
    [Range(0.5f, 3f)]
    public float  spawnDensity  = 1f;

    [Header("Odüller")]
    public int    baseGoldReward = 150;
    [Tooltip("Bu stage tamamlaninca saatlik altina eklenen miktar")]
    public int    offlineBoostPerHour = 5;
    [Tooltip("Yolu ortasinda micro-loot dusecek mi?")]
    public bool   hasMidStageLoot = true;
    [Range(0f, 1f)]
    [Tooltip("TechCore dusme sansi (0 = hic, 1 = kesin)")]
    public float  techCoreDropChance = 0.15f;

    [Header("Ozel Isaretler")]
    [Tooltip("Bu stage tutorial kapsaminda mi?")]
    public bool   isTutorial   = false;
    [Tooltip("Bu stage bir boss stage mi? BossManager.StartBoss() tetiklenir.")]
    public bool   isBossStage  = false;

#if UNITY_EDITOR
    // Editor'da stage adi otomatik guncellenir
    void OnValidate()
    {
        if (string.IsNullOrEmpty(name)) return;
        name = $"Stage_W{worldID}_{stageID:D2}";
    }
#endif
}