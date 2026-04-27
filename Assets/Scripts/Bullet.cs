using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary>
/// Top End War — Mermi v1.2 (Gameplay Fix Patch)
///
/// v1.1 → v1.2 Fix Delta:
///   • Physics.OverlapCapsule(): QueryTriggerInteraction.Collide eklendi.
///     Proje Physics Settings'de "Queries Hit Triggers" kapalıysa,
///     enemy'nin isTrigger collider'ı hiç algılanmıyordu → 0 hasar.
///     Bu parametre explicit olarak trigger'ları da yakalar.
///   • HIT_RADIUS: 0.5f (v1.1'den korundu)
/// </summary>
public class Bullet : MonoBehaviour
{
    public int    damage      = 60;
    public Color  bulletColor = new Color(0.6f, 0.1f, 1.0f);

    [HideInInspector] public string hitterPath = "Commander";
    [HideInInspector] public int   armorPen = 0;
    [HideInInspector] public int   pierceCount = 0;
    [HideInInspector] public float eliteDamageMult = 1f;
    [HideInInspector] public float bossDamageMult = 1f;

    const float HIT_RADIUS = 0.5f;
    const float LIFETIME   = 1.8f;
    const float DEFAULT_MAX_RANGE = 24f;

    Renderer _rend;
    TrailRenderer _trail;
    static Material _trailMaterial;
    bool _hit = false;
    float _maxRange = DEFAULT_MAX_RANGE;

    int _remainingPierce = 0;
    readonly HashSet<int> _hitTargets = new HashSet<int>();

    Vector3 _spawnPos;
    Vector3 _lastPos;
    float _trailTime = 0.10f;
    float _trailStartWidth = 0.08f;
    float _trailEndWidth = 0.008f;

    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
        EnsureTrail();
    }

    void OnEnable()
    {
        _hit = false;
        _remainingPierce = Mathf.Max(0, pierceCount);
        _hitTargets.Clear();
        _maxRange = DEFAULT_MAX_RANGE;
        ConfigureTrail(0.08f, 0.060f, 0.006f);
        EnsureTrail();
        ApplyColor();

        // ObjectPooler artık pozisyonu SetActive'den önce atıyor,
        // bu yüzden _lastPos burada doğru pozisyonu yakalar.
        _spawnPos = transform.position;
        _lastPos = transform.position;

        Invoke(nameof(ReturnToPool), LIFETIME);
    }

    void OnDisable()
    {
        CancelInvoke();
        _hit = false;
        _remainingPierce = 0;
        _hitTargets.Clear();
        _maxRange = DEFAULT_MAX_RANGE;
        if (_trail != null) _trail.Clear();
    }

    public void SetDamage(int d) => damage = d;

    public void SetMaxRange(float range)
    {
        _maxRange = Mathf.Max(4f, range);
    }

    public void SetTrailProfile(WeaponFamily family)
    {
        switch (family)
        {
            case WeaponFamily.SMG:
                ConfigureTrail(0.06f, 0.045f, 0.004f);
                break;
            case WeaponFamily.Sniper:
                ConfigureTrail(0.10f, 0.075f, 0.008f);
                break;
            default:
                ConfigureTrail(0.08f, 0.060f, 0.006f);
                break;
        }
    }

    public void SetTracerColor(Color color)
    {
        bulletColor = color;
        ApplyColor();
    }

    public void SetCombatStats(int newDamage, int newArmorPen = 0, int newPierceCount = 0, float newEliteDamageMult = 1f, float newBossDamageMult = 1f)
    {
        damage = newDamage;
        armorPen = Mathf.Max(0, newArmorPen);
        pierceCount = Mathf.Max(0, newPierceCount);
        eliteDamageMult = Mathf.Max(1f, newEliteDamageMult);
        bossDamageMult = Mathf.Max(1f, newBossDamageMult);
        _remainingPierce = pierceCount;
    }

    void Update()
    {
        if (_hit) return;

        if (Vector3.Distance(_spawnPos, transform.position) >= _maxRange)
        {
            ReturnToPool();
            return;
        }

        // FIX: QueryTriggerInteraction.Collide — isTrigger collider'ları da algıla.
        // "Queries Hit Triggers" project ayarından bağımsız olarak çalışır.
        Collider[] cols = Physics.OverlapCapsule(
            _lastPos,
            transform.position,
            HIT_RADIUS,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        foreach (Collider col in cols)
        {
            // FIX: Deaktif objeleri atla.
            if (!col.gameObject.activeInHierarchy) continue;

            BossHitReceiver bossRecv = col.GetComponent<BossHitReceiver>() ?? col.GetComponentInParent<BossHitReceiver>();
            Enemy enemy = col.GetComponent<Enemy>() ?? col.GetComponentInParent<Enemy>();

            if (bossRecv == null && enemy == null)
                continue;

            int targetId = bossRecv != null ? bossRecv.gameObject.GetInstanceID()
                                            : enemy.gameObject.GetInstanceID();

            if (_hitTargets.Contains(targetId))
                continue;

            if (PlayerStats.Instance != null)
            {
                float playerZ = PlayerStats.Instance.transform.position.z;
                Transform t = bossRecv != null ? bossRecv.transform : enemy.transform;
                if (t.position.z < playerZ - 2f) continue;
            }

            if (bossRecv != null)
            {
                bossRecv.TakeDamage(damage, armorPen, bossDamageMult);
                DamagePopup.Show(col.transform.position, damage,
                    DamagePopup.GetColor(hitterPath), damage > 500);
                SpawnImpactVfx(col.transform.position, DamagePopup.GetColor(hitterPath));
            }
            else if (enemy != null)
            {
                enemy.TakeDamage(
                    rawDamage: damage,
                    armorPenValue: armorPen,
                    eliteMultiplier: eliteDamageMult,
                    hitColor: DamagePopup.GetColor(hitterPath));
                SpawnImpactVfx(col.transform.position, DamagePopup.GetColor(hitterPath));
            }

            _hitTargets.Add(targetId);

            if (_remainingPierce > 0)
            {
                _remainingPierce--;
                continue;
            }

            Hit();
            return;
        }

        _lastPos = transform.position;
    }

    void Hit()
    {
        if (_hit) return;
        _hit = true;
        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (!gameObject.activeSelf) return;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = Vector3.zero;
        gameObject.SetActive(false);
    }

    void ApplyColor()
    {
        if (_rend != null)
        {
            _rend.enabled = true;

            if (_rend.material.HasProperty("_BaseColor"))
                _rend.material.SetColor("_BaseColor", bulletColor);
            else
                _rend.material.color = bulletColor;
        }

        if (_trail != null)
        {
            _trail.startColor = new Color(bulletColor.r, bulletColor.g, bulletColor.b, 0.95f);
            _trail.endColor   = new Color(bulletColor.r, bulletColor.g, bulletColor.b, 0f);
            _trail.time       = _trailTime;
            _trail.startWidth = _trailStartWidth;
            _trail.endWidth   = _trailEndWidth;
        }
    }

    void EnsureTrail()
    {
        if (_trail == null)
            _trail = GetComponent<TrailRenderer>() ?? gameObject.AddComponent<TrailRenderer>();

        _trail.minVertexDistance = 0.05f;
        _trail.alignment = LineAlignment.View;
        _trail.shadowCastingMode = ShadowCastingMode.Off;
        _trail.receiveShadows = false;
        _trail.generateLightingData = false;
        _trail.material = GetTrailMaterial();
        _trail.emitting = true;
        ApplyColor();
    }

    void ConfigureTrail(float time, float startWidth, float endWidth)
    {
        _trailTime = time;
        _trailStartWidth = startWidth;
        _trailEndWidth = endWidth;
        ApplyColor();
    }

    void SpawnImpactVfx(Vector3 pos, Color color)
    {
        var go = new GameObject("BulletImpactVfx");
        go.transform.position = pos;

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 1.5f;
        light.intensity = 1.2f;
        light.color = color;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.12f;
        main.startSpeed = 1.8f;
        main.startSize = 0.12f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        ps.Play();
        Destroy(go, 0.35f);
    }

    static Material GetTrailMaterial()
    {
        if (_trailMaterial == null)
            _trailMaterial = new Material(Shader.Find("Sprites/Default"));
        return _trailMaterial;
    }
}
