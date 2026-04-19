using UnityEngine;

/// <summary>
/// Top End War — Silah Arketip Konfigurasyonu v2.1
///
/// v2 → v2.1 Delta (Faz 2 / Localization Foundation):
///   • Localization Header eklendi: weaponNameKey, descriptionKey, roleKey, tag1Key, tag2Key
///   • DisplayWeaponName, DisplayDescription, DisplayRole, DisplayTag1, DisplayTag2 property'leri eklendi
///   • Mevcut weaponName ve tüm combat alanları DOKUNULMADI
///
/// Eski alanlar:
///   weaponName → hâlâ okunabilir, fallback olarak çalışır.
///
/// ASSETS: Create > TopEndWar > WeaponArchetypeConfig
/// </summary>
[CreateAssetMenu(fileName = "Weapon_", menuName = "TopEndWar/WeaponArchetypeConfig")]
public class WeaponArchetypeConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string weaponId = "assault";
    public string weaponName = "Assault Rifle";
    public WeaponFamily family = WeaponFamily.Assault;

    // ── Localization Keys ──────────────────────────────────────────────────
    // Lokalizasyon sistemi hazır olduğunda bu alanlar kullanılır.
    // Şimdilik boş bırakılabilir; Display property'leri fallback olarak weaponName vb. döner.
    [Header("Localization Keys  (Boş = fallback display string kullan)")]
    [Tooltip("Silah adı lokalizasyon anahtarı  ör: weapon_assault_name")]
    public string weaponNameKey  = "";
    [Tooltip("Kısa açıklama / flavor text anahtarı  ör: weapon_assault_desc")]
    public string descriptionKey = "";
    [Tooltip("Silahın rolünü tanımlayan anahtar  ör: weapon_assault_role  →  'Orta Menzil Genel Amaç'")]
    public string roleKey        = "";
    [Tooltip("UI alt satır sol tag anahtarı  ör: weapon_assault_tag1  →  'HIZLI'")]
    public string tag1Key        = "";
    [Tooltip("UI alt satır sağ tag anahtarı  ör: weapon_assault_tag2  →  'DENGE'")]
    public string tag2Key        = "";

    // ── Display Properties (Localization-ready fallback) ───────────────────
    public string DisplayWeaponName  => string.IsNullOrEmpty(weaponNameKey)  ? weaponName : weaponNameKey;
    public string DisplayDescription => string.IsNullOrEmpty(descriptionKey) ? ""          : descriptionKey;
    public string DisplayRole        => string.IsNullOrEmpty(roleKey)        ? ""          : roleKey;
    public string DisplayTag1        => string.IsNullOrEmpty(tag1Key)        ? ""          : tag1Key;
    public string DisplayTag2        => string.IsNullOrEmpty(tag2Key)        ? ""          : tag2Key;

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