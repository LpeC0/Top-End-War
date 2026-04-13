using UnityEngine;

/// <summary>
/// Top End War — Dusman Arketip Konfigurasyonu v2
///
/// Runtime baglantisi ileride WaveConfig/SpawnEntry tarafindan verilir.
/// Simdilik guvenli data asset'i olarak clamp'lenir.
/// </summary>
[CreateAssetMenu(fileName = "Enemy_", menuName = "TopEndWar/EnemyArchetypeConfig")]
public class EnemyArchetypeConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string enemyId = "trooper";
    public string enemyName = "Trooper";
    public EnemyClass enemyClass = EnemyClass.Normal;

    [Header("Stat Faktörleri")]
    [Tooltip("HP = StageConfig.targetDps * hpFactor")]
    public float hpFactor = 0.90f;

    [Tooltip("Zirh degeri. Silah ArmorPen bu degerden dusulur.")]
    public int armor = 0;

    [Tooltip("Hareket hizi (birim/saniye).")]
    public float moveSpeed = 4.5f;

    [Tooltip("Temas hasari")]
    public int contactDamage = 30;

    [Header("Davranis")]
    public EnemyThreatType threatType = EnemyThreatType.Standard;
    public bool canSpawnInRunner = true;
    public bool canSpawnInAnchor = true;

    [Header("Odül")]
    [Tooltip("CP reward = targetDps * cpRewardFactor")]
    public float cpRewardFactor = 0.06f;

    [Header("Gorsel / Ses")]
    public Sprite icon;
    public RuntimeAnimatorController animatorOverride;

    public int GetHP(float targetDps)
        => Mathf.Max(1, Mathf.RoundToInt(targetDps * hpFactor));

    public int GetCPReward(float targetDps)
        => Mathf.Max(1, Mathf.RoundToInt(targetDps * cpRewardFactor));

    // DEĞİŞİKLİK
    public bool IsEliteLike =>
        enemyClass == EnemyClass.Elite ||
        threatType == EnemyThreatType.ElitePressure;

#if UNITY_EDITOR
    void OnValidate()
    {
        hpFactor = Mathf.Max(0.1f, hpFactor);
        armor = Mathf.Max(0, armor);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        contactDamage = Mathf.Max(0, contactDamage);
        cpRewardFactor = Mathf.Max(0f, cpRewardFactor);

        if (!string.IsNullOrEmpty(enemyId))
            name = $"Enemy_{enemyClass}_{enemyId}";
    }
#endif
}

public enum EnemyClass
{
    Normal,
    Elite,
    MiniBoss,
    BossSupport,
}

public enum EnemyThreatType
{
    Standard,
    PackPressure,
    Priority,
    Durable,
    ElitePressure,
    Backline,
}