using UnityEngine;

/// <summary>
/// Top End War — Silah Arketip Konfigurasyonu v1 (Claude)
///
/// Bir silah ailesinin tasarim verisi. Runtime state icermez.
/// PlayerStats bu SO'yu okur; silah ekipman slotuna takilinca
/// GetTotalDPS() ve GetBaseFireRate() buradan beslenir.
///
/// SLICE DEGERLERI (Final Pack v1):
///   Assault: damage=14, rate=3.6, armorPen=6
///   SMG:     damage=5.2, rate=9.6, armorPen=2
///   Sniper:  damage=52,  rate=0.95, armorPen=18
///
/// ASSETS: Create > TopEndWar > WeaponArchetypeConfig
/// </summary>
[CreateAssetMenu(fileName = "Weapon_", menuName = "TopEndWar/WeaponArchetypeConfig")]
public class WeaponArchetypeConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string weaponId    = "assault";
    public string weaponName  = "Assault Rifle";
    public WeaponFamily family = WeaponFamily.Assault;

    [Header("Temel Statlar (Tier 1 baslangic)")]
    [Tooltip("Tek mermi hasari")]
    public float baseDamage   = 14f;

    [Tooltip("Saniyede atis sayisi")]
    public float fireRate     = 3.6f;

    [Tooltip("Zirh delme degeri. Dusmanin armor degerinden bu dusuldukten sonra hasar hesaplanir.")]
    public int armorPen       = 6;

    [Tooltip("Mermi sayisi (1 = tek mermi, 6 = shotgun pellet)")]
    public int projectileCount = 1;

    [Tooltip("Delme: 0 = delmez, 1 = ek 1 dusmanı deler")]
    public int pierceCount    = 0;

    [Tooltip("Sekme: 0 = sekmez")]
    public int bounceCount    = 0;

    [Tooltip("Patlama yariçapi (0 = yok)")]
    public float splashRadius = 0f;

    [Header("Denge Skoru (Tasarim referansi, oyuncuya gosterilmez)")]
    [Range(0f, 2f)]
    [Tooltip("Suru/coklu hedef verimlilik carpani. 1.28 = SMG gibi.")]
    public float packFactor   = 1.05f;

    [Range(0, 10)]
    public int runnerScore    = 8;
    [Range(0, 10)]
    public int bossScore      = 7;

    [Header("Gorsel / Ses")]
    public Sprite icon;
    public GameObject modelPrefab;

    // ── Hesaplamalar ──────────────────────────────────────────────────────

    /// <summary>Ham tek hedef DPS (armor hariç).</summary>
    public float RawSingleDPS => baseDamage * fireRate;

    /// <summary>
    /// Verilen armor degerine karsi efektif hasar carpani.
    /// Formul: 100 / (100 + max(armor - armorPen, 0))
    /// </summary>
    public float GetArmorDamageMultiplier(int targetArmor)
    {
        int effectiveArmor = Mathf.Max(0, targetArmor - armorPen);
        return 100f / (100f + effectiveArmor);
    }

    /// <summary>Verilen armor degerine karsi efektif DPS.</summary>
    public float GetEffectiveDPS(int targetArmor)
        => RawSingleDPS * GetArmorDamageMultiplier(targetArmor);

#if UNITY_EDITOR
    void OnValidate()
    {
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
    Shotgun,    // Slice disinda, ileride
    Launcher,   // Slice disinda, ileride
}