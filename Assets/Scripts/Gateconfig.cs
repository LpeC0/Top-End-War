using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Kapı Konfigürasyonu v3 (Claude)
///
/// v2 → v3 Delta:
///   • GateFamily2 → GateFamily  (Solve + BossPrep eklendi, Tactical korundu)
///   • GateBalanceTier eklendi   (Minor | Standard | Solve | Army | Sustain | BossPrep)
///   • Localization key alanları eklendi: titleKey / tag1Key / tag2Key / descriptionKey
///   • title / tag1 / tag2 KORUNDU  → Gate.cs runtime'ı kırılmaz, fallback olarak çalışır
///   • bossPrepPriority → isBossPrepOnly (anlam aynı, isim netleşti)
///   • GateDeliveryType2 / GateModifier2 ve tüm alt enumlar DOKUNULMADI
///
/// GATE UI SÖZLEŞMESİ:
///   Üst satır : title  (veya titleKey → lokalize metin)
///   Alt satır : tag1 • tag2
///
/// ASSETS: Create > TopEndWar > GateConfig
/// </summary>
[CreateAssetMenu(fileName = "Gate_", menuName = "TopEndWar/GateConfig")]
public class GateConfig : ScriptableObject
{
    // ── Kimlik ────────────────────────────────────────────────────────────
    [Header("Kimlik")]
    public string gateId = "gate_hardline";

    // ── Localization Keys ─────────────────────────────────────────────────
    // Lokalizasyon sistemi hazır olduğunda bu alanlar kullanılır.
    // Şimdilik boş bırakılabilir; Gate.cs fallback olarak title/tag1/tag2'yi okur.
    [Header("Localization Keys  (Boş = fallback display string kullan)")]
    [Tooltip("Ana etki metni anahtarı  ör: gate_hardline_title")]
    public string titleKey       = "";
    [Tooltip("Alt satır sol tag anahtarı  ör: gate_hardline_tag1")]
    public string tag1Key        = "";
    [Tooltip("Alt satır sağ tag anahtarı  ör: gate_hardline_tag2")]
    public string tag2Key        = "";
    [Tooltip("Detay / tooltip açıklaması anahtarı  ör: gate_hardline_desc")]
    public string descriptionKey = "";

    // ── Runtime / Fallback Görüntü Metinleri ─────────────────────────────
    // Lokalizasyon sistemi aktif değilken Gate.cs bunları doğrudan kullanır.
    // Key alanları doldurulunca bu alanlar tasarım referansı olarak kalır.
    [Header("Görüntü  (Fallback — Lokalizasyon hazır olana kadar)")]
    [Tooltip("Kapi üst satır metni")]
    public string title = "+8% Silah Gücü";
    [Tooltip("Alt satır sol tag")]
    public string tag1  = "POWER";
    [Tooltip("Alt satır sağ tag")]
    public string tag2  = "EARLY";

    // ── Görsel ───────────────────────────────────────────────────────────
    [Header("Görsel")]
    public Color  gateColor = new Color(0.15f, 0.80f, 0.15f, 0.80f);
    public Sprite icon;

    // ── Sınıflandırma ─────────────────────────────────────────────────────
    [Header("Sınıflandırma")]
    [Tooltip("Ailenin içerik kimliği: Power / Tempo / Solve / Geometry / Army / Sustain / Tactical / BossPrep")]
    public GateFamily        family       = GateFamily.Power;
    [Tooltip("Güç bandı: Minor / Standard / Solve / Army / Sustain / BossPrep")]
    public GateBalanceTier   balanceTier  = GateBalanceTier.Standard;
    [Tooltip("Sunum türü: Single / Duel / Risk / Recovery / BossPrep")]
    public GateDeliveryType2 deliveryType = GateDeliveryType2.Single;

    // ── Spawn Kontrol ─────────────────────────────────────────────────────
    [Header("Spawn Kontrol")]
    [Tooltip("Bu kapıyı hangi stage'den itibaren havuza al")]
    public int   minStage        = 1;
    [Tooltip("Bu kapıyı hangi stage'den sonra havuzdan çıkar  (999 = her zaman)")]
    public int   maxStage        = 999;
    [Range(0f, 1f)]
    [Tooltip("Havuz içindeki göreli spawn ağırlığı")]
    public float spawnWeight     = 0.12f;
    [Tooltip("Tutorial stage'lerinde de çıkabilir mi?")]
    public bool  tutorialAllowed = true;
    [Tooltip("Yalnızca boss prep stage'lerinde kullanılabilir; normal havuza eklenmez")]
    public bool  isBossPrepOnly  = false;

    // ── Etkiler ───────────────────────────────────────────────────────────
    [Header("Modifiers  (Ana Etki)")]
    public List<GateModifier2> modifiers = new List<GateModifier2>();

    [Header("Ceza Modifiers  (Risk delivery için)")]
    public List<GateModifier2> penaltyModifiers = new List<GateModifier2>();

    // ── Dengeleme Notu ────────────────────────────────────────────────────
    [Header("Denge  (Tasarım referansı — oyuncuya gösterilmez)")]
    [Range(0.5f, 3f)]
    public float gateValueBudget = 1.0f;

    // ── Yardımcı Property'ler ─────────────────────────────────────────────
    public bool IsRisk     => deliveryType == GateDeliveryType2.Risk;
    public bool IsRecovery => deliveryType == GateDeliveryType2.Recovery;

    /// <summary>
    /// Boss prep alanı: hem family hem de isBossPrepOnly bayrağını kontrol eder.
    /// </summary>
    public bool IsBossPrep => family == GateFamily.BossPrep || isBossPrepOnly;

    /// <summary>
    /// Lokalizasyon sistemi varsa key döner, yoksa fallback display string.
    /// Gate.cs ve UI bu property'leri kullanabilir; doğrudan title/tag1/tag2 yerine.
    /// </summary>
    public string DisplayTitle => string.IsNullOrEmpty(titleKey) ? title : titleKey;
    public string DisplayTag1  => string.IsNullOrEmpty(tag1Key)  ? tag1  : tag1Key;
    public string DisplayTag2  => string.IsNullOrEmpty(tag2Key)  ? tag2  : tag2Key;

#if UNITY_EDITOR
    void OnValidate()
    {
    }
#endif
}

// ─────────────────────────────────────────────────────────────────────────
/// <summary>
/// Kapı Ailesi — yeni kanon (v3).
/// Solve: problem-çözücü, burst-power veya niche etki.
/// BossPrep: yalnızca boss öncesi stage'lerde çıkan güçlü hazırlık kapıları.
/// </summary>
public enum GateFamily
{
    Power,
    Tempo,
    Solve,
    Geometry,
    Army,
    Sustain,
    Tactical,
    BossPrep,
}

/// <summary>
/// Güç / etki bandı — spawn havuzlarında gruplama ve dengeleme için.
/// </summary>
public enum GateBalanceTier
{
    Minor,      // Küçük, güvenli etki
    Standard,   // Normal orta etki (en yaygın bant)
    Solve,      // Niche veya problem-çözücü, genelde geç stage
    Army,       // Ordu büyütme odaklı, orta-geç stage
    Sustain,    // Toparlanma odaklı, her aşamada olabilir
    BossPrep,   // Boss öncesi: büyük etki, seyrek çıkar
}

// ─────────────────────────────────────────────────────────────────────────
// Aşağıdaki tipler v2'den DOKUNULMADAN korundu.
// PlayerStats.ApplyGateConfig, SpawnManager ve diğer runtime sistemleri bunlara bağlı.
// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class GateModifier2
{
    [Tooltip("Bu modifier kime uygulanır?")]
    public GateTargetType2 targetType = GateTargetType2.CommanderWeapon;

    [Tooltip("Hangi stat?")]
    public GateStatType2   statType   = GateStatType2.WeaponPowerPercent;

    [Tooltip("İşlem: AddFlat=düz ekle, AddPercent=yüzde ekle, Promote=seviye atla, HealPercent=HP oranı")]
    public GateOperation2  operation  = GateOperation2.AddPercent;

    public float value = 8f;
}

public enum GateDeliveryType2
{
    Single,
    Duel,
    Risk,
    Recovery,
    BossPrep,
}

public enum GateTargetType2
{
    CommanderWeapon,    // Komutan silahı
    Commander,          // Komutanın kendisi
    AllSoldiers,        // Tüm askerler
    PiyadeSoldiers,
    MekanikSoldiers,
    TeknolojiSoldiers,
    WeakestSoldier,     // Field Promotion
}

public enum GateStatType2
{
    // Power
    WeaponPowerPercent,
    EliteDamagePercent,
    BossDamagePercent,
    ArmorPenFlat,
    ArmoredTargetDamagePercent,

    // Tempo
    FireRatePercent,

    // Geometry
    PierceCount,
    BounceCount,
    PelletCount,
    SplashRadiusPercent,

    // Army
    AddSoldierCount,
    SoldierDamagePercent,

    // Sustain
    HealCommanderPercent,
    HealSoldiersPercent,

    // Penalty (Risk için)
    CommanderMaxHpPercent,      // Negatif value ile ceza
    SoldierDamagePercentMalus,
}

public enum GateOperation2
{
    AddFlat,
    AddPercent,
    Promote,        // Field Promotion: en zayıf birlik +1 seviye
    HealPercent,    // Toparlanma: mevcut max HP'nin yüzde X'i
}
