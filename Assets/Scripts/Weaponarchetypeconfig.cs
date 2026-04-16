using UnityEngine;

/// <summary>
/// Top End War — Silah Arketip Konfigurasyonu v2
///
/// Bu dosya SILAH AILESININ TEMEL DOGASIDIR.
/// Runtime state veya item rarity tasimaz.
///
/// Ornek:
///   - Assault nasil ates eder?
///   - SMG kac menzilden vurur?
///   - Sniper hangi hedefi sever?
///
/// Item bonuslari EquipmentData tarafinda kalir.
/// </summary>
[CreateAssetMenu(fileName = "Weapon_", menuName = "TopEndWar/WeaponArchetypeConfig")]
public class WeaponArchetypeConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string weaponId = "assault";
    public string weaponName = "Assault Rifle";
    public WeaponFamily family = WeaponFamily.Assault;

    [Header("Combat Kimligi")]
    public TargetProfile defaultTargetProfile = TargetProfile.Balanced;

    [Tooltip("Tek mermi taban hasari")]
    public float baseDamage = 14f;

    [Tooltip("Saniyede atis sayisi")]
    public float fireRate = 3.6f;

    [Tooltip("Etkili menzil")]
    public float attackRange = 20f;

    [Tooltip("Mermi/proje hizi")]
    public float projectileSpeed = 38f;

    [Tooltip("Zirh delme degeri")]
    public int armorPen = 6;

    [Tooltip("Ayni atista cikan mermi sayisi (1 = tek mermi, 6 = shotgun pellet)")]
    public int projectileCount = 1;

    [Tooltip("Delme: 0 = delmez, 1 = ek 1 dusmani deler")]
    public int pierceCount = 0;

    [Tooltip("Sekme: 0 = sekmez")]
    public int bounceCount = 0;

    [Tooltip("Patlama yaricapi (0 = yok)")]
    public float splashRadius = 0f;

    [Header("Denge Skoru (Tasarim referansi)")]
    [Range(0f, 2f)]
    public float packFactor = 1.05f;

    [Range(0, 10)] public int runnerScore = 8;
    [Range(0, 10)] public int bossScore = 7;

    [Header("Gorsel / Ses")]
    public Sprite icon;
    public GameObject modelPrefab;

    public float RawSingleDPS => baseDamage * fireRate;

    public float GetArmorDamageMultiplier(int targetArmor)
    {
        int effectiveArmor = Mathf.Max(0, targetArmor - armorPen);
        return 100f / (100f + effectiveArmor);
    }

    public float GetEffectiveDPS(int targetArmor)
        => RawSingleDPS * GetArmorDamageMultiplier(targetArmor);

#if UNITY_EDITOR
    void OnValidate()
    {
        baseDamage = Mathf.Max(1f, baseDamage);
        fireRate = Mathf.Max(0.05f, fireRate);
        attackRange = Mathf.Max(1f, attackRange);
        projectileSpeed = Mathf.Max(1f, projectileSpeed);
        armorPen = Mathf.Max(0, armorPen);
        projectileCount = Mathf.Max(1, projectileCount);
        pierceCount = Mathf.Max(0, pierceCount);
        bounceCount = Mathf.Max(0, bounceCount);
        splashRadius = Mathf.Max(0f, splashRadius);

        if (!string.IsNullOrEmpty(weaponId))
            name = $"Weapon_{family}";
    }
#endif
}

public enum WeaponFamily
{
    Assault,
    SMG,
    Sniper,
    Shotgun,
    Launcher,
    Beam
}

public enum TargetProfile
{
    Balanced,
    NearestThreat,
    EliteHunter,
    Finisher,
    ClusterFocus
}