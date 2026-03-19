using UnityEngine;

/// <summary>
/// Asker path tipleri — GateData ve ArmyManager ile eslenik olmali.
/// </summary>
public enum SoldierPath { Piyade, Mekanik, Teknoloji }

/// <summary>
/// Top End War — Bireysel Asker (Claude)
///
/// UNITY KURULUM:
///   ArmyManager bunu runtime olusturur — elle prefab GEREKMEZ.
///   Ama isterseniz Capsule prefab yapip ArmyManager.soldierPrefab slotuna surukleyin.
///
/// Her asker:
///   - Player'i takip eder (formationOffset ile)
///   - En yakin dusmana otomatik ates eder
///   - Hasar alinca HP duser, 0'da ArmyManager'a bildirir
///   - Biyom carpani BiomeManager'dan alinir
/// </summary>
public class SoldierUnit : MonoBehaviour
{
    // ── Kimlik ────────────────────────────────────────────────────────────
    [HideInInspector] public SoldierPath path;
    [HideInInspector] public string      biome       = "Tas";
    [HideInInspector] public int         mergeLevel  = 1;

    // ── Statlar (ArmyManager.Initialize ile set edilir) ──────────────────
    [HideInInspector] public int   maxHP;
    [HideInInspector] public int   currentHP;
    [HideInInspector] public float baseAtk;
    [HideInInspector] public float atkSpeed;   // atis/saniye

    // ── Formasyon ────────────────────────────────────────────────────────
    [HideInInspector] public Vector3 formationOffset;

    const float FOLLOW_SPEED  = 14f;
    const float DETECT_RADIUS = 28f;

    // ── Dahili ────────────────────────────────────────────────────────────
    Renderer _rend;
    float    _nextFire;
    bool     _dead;

    // ── Referanslar ───────────────────────────────────────────────────────
    static readonly int[] _mergeDamageNumerator = { 10, 18, 35, 70 }; // × base

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
        gameObject.tag = "Soldier"; // dusman mermilerinden muaf tut
    }

    void OnEnable()
    {
        _dead    = false;
        _nextFire= Time.time + Random.value / Mathf.Max(atkSpeed, 0.1f); // stagger
    }

    void Update()
    {
        if (_dead || PlayerStats.Instance == null) return;
        FollowPlayer();
        if (Time.time >= _nextFire) TryShoot();
    }

    // ── Hareket ──────────────────────────────────────────────────────────
    void FollowPlayer()
    {
        Vector3 target = PlayerStats.Instance.transform.position + formationOffset;
        target.y = 1.2f;
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * FOLLOW_SPEED);
    }

    // ── Ates ─────────────────────────────────────────────────────────────
    void TryShoot()
    {
        _nextFire = Time.time + 1f / Mathf.Max(atkSpeed, 0.01f);

        // En yakin dusmani bul
        Collider best    = null;
        float    bestDist= DETECT_RADIUS * DETECT_RADIUS;

        foreach (Collider col in Physics.OverlapSphere(transform.position, DETECT_RADIUS))
        {
            if (!col.CompareTag("Enemy")) continue;
            float d = (col.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = col; }
        }

        if (best == null) return;

        // Hasar hesapla — biyom carpani BiomeManager'dan
        float biomeMultiplier = BiomeManager.Instance != null
            ? BiomeManager.Instance.GetMultiplier(path)
            : 1f;

        float cmdAura = (PlayerStats.Instance?.CurrentTier ?? 1) switch
        {
            1 => 0f, 2 => 0.10f, 3 => 0.20f, 4 => 0.30f, _ => 0.40f
        };

        float mergeMult = mergeLevel switch { 2 => 1.8f, 3 => 3.5f, 4 => 7.0f, _ => 1.0f };

        int finalDamage = Mathf.RoundToInt(
            baseAtk * mergeMult * (1f + cmdAura) * biomeMultiplier);

        // Ates — bullet pool veya dogrudan hasar (collision olmadigi icin dogrudan)
        // Asker mermisi: OverlapSphere metodu yerine dogrudan hasar, cunku
        // askerler surekli hedefin yanindadir.
        FireBullet(best, finalDamage);
    }

    void FireBullet(Collider target, int dmg)
    {
        // Bullet pool kullan
        Vector3 dir = (target.transform.position - transform.position).normalized;
        GameObject b = ObjectPooler.Instance?.SpawnFromPool(
            "Bullet", transform.position + Vector3.up * 0.5f, Quaternion.LookRotation(dir));

        if (b == null) return; // pool doluysa atlayabiliriz

        Bullet bComp = b.GetComponent<Bullet>();
        if (bComp != null)
        {
            bComp.SetDamage(dmg);
            // Soldiere ozel renk
            bComp.bulletColor = GetPathColor() * 0.8f;
        }

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * 32f;
    }

    // ── Hasar Al ─────────────────────────────────────────────────────────
    public void TakeDamage(int dmg)
    {
        if (_dead) return;
        currentHP -= dmg;
        if (_rend) StartCoroutine(FlashRed());
        if (currentHP <= 0) Die();
    }

    System.Collections.IEnumerator FlashRed()
    {
        if (!_rend) yield break;
        Color orig = _rend.material.color;
        _rend.material.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        if (_rend && !_dead) _rend.material.color = orig;
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;
        ArmyManager.Instance?.RemoveSoldier(this);
        gameObject.SetActive(false);
    }

    // ── HP Heal ──────────────────────────────────────────────────────────
    public void HealPercent(float pct)
    {
        currentHP = Mathf.Min(maxHP, currentHP + Mathf.RoundToInt(maxHP * pct));
    }

    // ── Renk ─────────────────────────────────────────────────────────────
    public Color GetPathColor()
    {
        Color baseColor = path switch
        {
            SoldierPath.Piyade    => new Color(0.2f, 0.8f, 0.2f),  // yesil
            SoldierPath.Mekanik   => new Color(0.6f, 0.6f, 0.6f),  // gri
            SoldierPath.Teknoloji => new Color(0.2f, 0.5f, 1.0f),  // mavi
            _                     => Color.white
        };

        // Merge level → parlaklik
        float brightness = mergeLevel switch { 2 => 1.2f, 3 => 1.6f, 4 => 2.2f, _ => 1.0f };
        return baseColor * brightness;
    }
}