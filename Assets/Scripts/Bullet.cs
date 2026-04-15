using UnityEngine;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    public int    damage      = 60;
    public Color  bulletColor = new Color(0.6f, 0.1f, 1.0f);

    [HideInInspector] public string hitterPath = "Commander"; 
    [HideInInspector] public int   armorPen = 0;
    [HideInInspector] public int   pierceCount = 0;
    [HideInInspector] public float eliteDamageMult = 1f;
    [HideInInspector] public float bossDamageMult = 1f;

    const float HIT_RADIUS = 0.4f;
    const float LIFETIME   = 1.8f;

    Renderer _rend;
    bool _hit = false;

    int _remainingPierce = 0;
    readonly HashSet<int> _hitTargets = new HashSet<int>();

    // DEĞİŞİKLİK
    Vector3 _lastPos;

    void Awake() => _rend = GetComponentInChildren<Renderer>();

    void OnEnable()
    {
        _hit = false;
        _remainingPierce = Mathf.Max(0, pierceCount);
        _hitTargets.Clear();
        ApplyColor();

        // DEĞİŞİKLİK
        _lastPos = transform.position;

        Invoke(nameof(ReturnToPool), LIFETIME);
    }

    void OnDisable()
    {
        CancelInvoke();
        _hit = false;
        _remainingPierce = 0;
        _hitTargets.Clear();
    }

    public void SetDamage(int d) => damage = d;

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

        // DEĞİŞİKLİK
        Collider[] cols = Physics.OverlapCapsule(_lastPos, transform.position, HIT_RADIUS);

        foreach (Collider col in cols)
        {
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
            }
            else if (enemy != null)
            {
                enemy.TakeDamage(
                    rawDamage: damage,
                    armorPenValue: armorPen,
                    eliteMultiplier: eliteDamageMult,
                    hitColor: DamagePopup.GetColor(hitterPath));
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

        // DEĞİŞİKLİK
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
        if (_rend == null) return;
        if (_rend.material.HasProperty("_BaseColor"))
            _rend.material.SetColor("_BaseColor", bulletColor);
        else
            _rend.material.color = bulletColor;
    }
}