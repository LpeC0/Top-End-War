using System.Collections;
using UnityEngine;

/// <summary>
/// Top End War — AnchorBuffManager v1.0
///
/// Anchor pickup'larından gelen süreli buff'ları yönetir.
/// PlayerStats.ApplyGateConfig'e dokunmaz — ayrı runtime modifier tutar.
/// Buff süresi dolunca modifier sıfırlanır.
///
/// PlayerStats'a yeni alan eklemeden çalışır:
///   FireRate → PlayerStats._runFireRatePercent üzerinden
///   ArmorPen → PlayerStats._runArmorPenFlat üzerinden
///   (Bunlar zaten public property olarak var)
///
/// Kurulum: Sahneye boş obje ekle, bu scripti koy.
/// </summary>
public class AnchorBuffManager : MonoBehaviour
{
    public static AnchorBuffManager Instance { get; private set; }

    public static AnchorBuffManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        AnchorBuffManager found = FindFirstObjectByType<AnchorBuffManager>();
        if (found != null) return found;

        GameObject go = new GameObject("AnchorBuffManager");
        return go.AddComponent<AnchorBuffManager>();
    }

    // Aktif buff coroutine'lerini takip et — aynı tip buff üst üste gelirse öncekini iptal et
    Coroutine _fireRateCo;
    Coroutine _armorPenCo;

    // Şu an uygulanan buff değerleri (sıfırlamak için)
    float _activeFireRateBonus = 0f;
    int   _activeArmorPenBonus = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Buff Uygula ───────────────────────────────────────────────────────

    public void ApplyBuff(AnchorBuffType type, float value, float duration)
    {
        switch (type)
        {
            case AnchorBuffType.FireRate:
                if (_fireRateCo != null) StopCoroutine(_fireRateCo);
                _fireRateCo = StartCoroutine(FireRateBuffCo(value, duration));
                break;

            case AnchorBuffType.ArmorPen:
                if (_armorPenCo != null) StopCoroutine(_armorPenCo);
                _armorPenCo = StartCoroutine(ArmorPenBuffCo(Mathf.RoundToInt(value), duration));
                break;
        }
    }

    // ── Coroutine'ler ─────────────────────────────────────────────────────

    IEnumerator FireRateBuffCo(float percent, float duration)
    {
        // Önceki varsa sıfırla
        RemoveFireRateBuff();

        // Uygula — PlayerStats GateModifier2 ile ekleme yapıyor ama
        // biz direkt ApplyGateConfig'i çağırmak yerine event üzerinden ekliyoruz.
        // Basitlik için: PlayerStats'a bir public metod açmak yerine
        // GateRuntimeData ile minimal modifier paketi gönderiyoruz.
        _activeFireRateBonus = percent;
        ApplyFireRateToPlayerStats(percent);

        Debug.Log($"[AnchorBuffManager] FireRate +{percent}% başladı, süre={duration}s");
        GameEvents.OnAnchorBuffStarted?.Invoke(AnchorBuffType.FireRate, duration);

        yield return new UnityEngine.WaitForSeconds(duration);

        RemoveFireRateBuff();
        Debug.Log("[AnchorBuffManager] FireRate buff bitti.");
        GameEvents.OnAnchorBuffEnded?.Invoke(AnchorBuffType.FireRate);
    }

    IEnumerator ArmorPenBuffCo(int flat, float duration)
    {
        RemoveArmorPenBuff();

        _activeArmorPenBonus = flat;
        ApplyArmorPenToPlayerStats(flat);

        Debug.Log($"[AnchorBuffManager] ArmorPen +{flat} başladı, süre={duration}s");
        GameEvents.OnAnchorBuffStarted?.Invoke(AnchorBuffType.ArmorPen, duration);

        yield return new UnityEngine.WaitForSeconds(duration);

        RemoveArmorPenBuff();
        Debug.Log("[AnchorBuffManager] ArmorPen buff bitti.");
        GameEvents.OnAnchorBuffEnded?.Invoke(AnchorBuffType.ArmorPen);
    }

    // ── PlayerStats Bağlantısı ────────────────────────────────────────────
    // PlayerStats'ta GateModifier2 listesi yerine direkt metod çağrısı.
    // ApplyGateConfig zaten bunu yapıyor; biz sadece GateRuntimeData üretiyoruz.

    void ApplyFireRateToPlayerStats(float percent)
    {
        if (PlayerStats.Instance == null) return;
        var mod = new GateModifier2 { statType = GateStatType2.FireRatePercent, value = percent };
        PlayerStats.Instance.ApplyAnchorBuff(mod);
    }

    void RemoveFireRateBuff()
    {
        if (_activeFireRateBonus == 0f || PlayerStats.Instance == null) return;
        var mod = new GateModifier2 { statType = GateStatType2.FireRatePercent, value = -_activeFireRateBonus };
        PlayerStats.Instance.ApplyAnchorBuff(mod);
        _activeFireRateBonus = 0f;
    }

    void ApplyArmorPenToPlayerStats(int flat)
    {
        if (PlayerStats.Instance == null) return;
        var mod = new GateModifier2 { statType = GateStatType2.ArmorPenFlat, value = flat };
        PlayerStats.Instance.ApplyAnchorBuff(mod);
    }

    void RemoveArmorPenBuff()
    {
        if (_activeArmorPenBonus == 0 || PlayerStats.Instance == null) return;
        var mod = new GateModifier2 { statType = GateStatType2.ArmorPenFlat, value = -_activeArmorPenBonus };
        PlayerStats.Instance.ApplyAnchorBuff(mod);
        _activeArmorPenBonus = 0;
    }

    // ── Anchor bitti → buff'ları temizle ─────────────────────────────────

    void OnEnable()
    {
        GameEvents.OnAnchorModeChanged += HandleAnchorModeChanged;
    }

    void OnDisable()
    {
        GameEvents.OnAnchorModeChanged -= HandleAnchorModeChanged;
    }

    void HandleAnchorModeChanged(bool active)
    {
        if (active) return;
        // Anchor bitti, tüm buff'ları temizle
        if (_fireRateCo != null) StopCoroutine(_fireRateCo);
        if (_armorPenCo != null) StopCoroutine(_armorPenCo);
        RemoveFireRateBuff();
        RemoveArmorPenBuff();
    }
}
