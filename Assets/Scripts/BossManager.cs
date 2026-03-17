using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Top End War — Boss v4 (Claude)
///
/// Boss HP 60000 → 150000
/// (T4+4mermi=5040DPS → 30s; fazlarla ~60s; T3+2m=1160DPS → 129s ✓)
/// </summary>
public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Boss")]
    public int   bossMaxHP         = 150000;
    public float bossApproachSpeed = 2.5f;
    public int   bossContactDmg    = 100;
    public float bossStopDist      = 12f;

    [Header("Fazlar")]
    [Range(0,1)] public float phase2At = 0.60f;
    [Range(0,1)] public float phase3At = 0.30f;

    [Header("Minyon (Faz2)")]
    public GameObject minionPrefab;
    public int   minionCount    = 3;
    public float minionInterval = 8f;

    [Header("Prefab (boş = fallback küp)")]
    public GameObject bossPrefab;

    int    _hp;
    int    _phase  = 0;
    bool   _active = false;
    bool   _dead   = false;

    GameObject      _bossObj;
    Renderer        _bossRend;
    Canvas          _bossCanvas;
    Image           _hpFill;
    TextMeshProUGUI _phaseLabel;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        GameEvents.OnBossEncountered += StartFight;
        BuildHPBar();
        _bossCanvas?.gameObject.SetActive(false);
    }

    void OnDestroy() => GameEvents.OnBossEncountered -= StartFight;

    void StartFight()
    {
        if (_active) return;
        Debug.Log("[Boss] Başlıyor!");
        StartCoroutine(EntranceCo());
    }

    IEnumerator EntranceCo()
    {
        GameEvents.OnAnchorModeChanged?.Invoke(true);
        yield return new WaitForSeconds(0.5f);

        Vector3 spawnPos = new Vector3(0f, 1.2f,
            (PlayerStats.Instance?.transform.position.z ?? 0f) + 45f);
        SpawnBoss(spawnPos);

        _hp     = bossMaxHP;
        _phase  = 1;
        _active = true;

        _bossCanvas?.gameObject.SetActive(true);
        UpdateBar();
        SetLabel("GOKMEDRESE MUHAFIZI  |  FAZ 1: TAS ZIRH");
        Debug.Log("[Boss] Aktif! HP=" + _hp);
    }

    void SpawnBoss(Vector3 pos)
    {
        _bossObj = bossPrefab != null
            ? Instantiate(bossPrefab, pos, Quaternion.identity)
            : MakeFallbackCube(pos);

        _bossRend = _bossObj.GetComponent<Renderer>();
        _bossObj.tag = "Enemy";

        Collider col = _bossObj.GetComponent<Collider>() ?? _bossObj.AddComponent<BoxCollider>();
        // isTrigger=false → Bullet.OverlapSphere tag="Enemy" ile bulur

        var recv = _bossObj.AddComponent<BossHitReceiver>();
        recv.bossManager = this;
    }

    GameObject MakeFallbackCube(Vector3 pos)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.transform.position   = pos;
        obj.transform.localScale = new Vector3(5f, 7f, 5f);
        obj.GetComponent<Renderer>().material.color = new Color(0.55f, 0f, 0f);
        return obj;
    }

    void Update()
    {
        if (!_active || _dead || _bossObj == null || PlayerStats.Instance == null) return;

        float tZ  = PlayerStats.Instance.transform.position.z + bossStopDist;
        Vector3 p = _bossObj.transform.position;

        if (p.z > tZ)
            p.z = Mathf.MoveTowards(p.z, tZ, bossApproachSpeed * Time.deltaTime);
        else
        {
            PlayerStats.Instance.TakeContactDamage(bossContactDmg);
            p.z += 8f;
        }
        _bossObj.transform.position = p;
    }

    public void TakeDamage(int dmg)
    {
        if (!_active || _dead) return;
        _hp = Mathf.Max(0, _hp - dmg);
        UpdateBar();
        StartCoroutine(Flash());
        CheckPhase();
        if (_hp <= 0) Defeat();
    }

    void CheckPhase()
    {
        float r = (float)_hp / bossMaxHP;
        if      (_phase == 1 && r <= phase2At) { _phase = 2; Phase2(); }
        else if (_phase == 2 && r <= phase3At) { _phase = 3; Phase3(); }
    }

    void Phase2()
    {
        SetLabel("FAZ 2: MINYON");
        if (_bossRend) _bossRend.material.color = new Color(0.7f, 0.3f, 0.1f);
        InvokeRepeating(nameof(SpawnMinions), 1f, minionInterval);
    }

    void Phase3()
    {
        SetLabel("FAZ 3: CEKIRDEK");
        if (_bossRend) _bossRend.material.color = new Color(0.9f, 0.05f, 0.05f);
        bossApproachSpeed *= 2.2f;
        CancelInvoke(nameof(SpawnMinions));
    }

    void SpawnMinions()
    {
        if (!_active || _dead || PlayerStats.Instance == null) return;
        Vector3 center = PlayerStats.Instance.transform.position + Vector3.forward * 10f;
        for (int i = 0; i < minionCount; i++)
        {
            Vector3 p = center + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(2f, 12f));
            GameObject min;
            if (minionPrefab != null) min = Instantiate(minionPrefab, p, Quaternion.identity);
            else
            {
                min = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                min.transform.position   = p;
                min.transform.localScale = Vector3.one * 0.7f;
                Destroy(min.GetComponent<Collider>());
                var cc = min.AddComponent<CapsuleCollider>(); cc.isTrigger = true;
                var rb = min.AddComponent<Rigidbody>(); rb.isKinematic = true;
                min.tag = "Enemy";
                min.AddComponent<Enemy>();
            }
            min.GetComponent<Enemy>()?.Initialize(
                new DifficultyManager.EnemyStats(300, 60, 5f, 10));
        }
    }

    void Defeat()
    {
        _dead = true; _active = false;
        CancelInvoke();
        PlayerStats.Instance?.AddCPFromKill(2000);
        if (_bossObj) Destroy(_bossObj);
        _bossCanvas?.gameObject.SetActive(false);
        GameEvents.OnAnchorModeChanged?.Invoke(false);
        StartCoroutine(VictoryCo());
    }

    IEnumerator VictoryCo() { yield return new WaitForSeconds(1.5f); Victory(); }

    void Victory()
    {
        Canvas c = FindFirstObjectByType<Canvas>(); if (!c) return;
        var panel = new GameObject("Victory"); panel.transform.SetParent(c.transform, false);
        panel.AddComponent<Image>().color = new Color(0,0,0,0.88f);
        var r = panel.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;

        Txt(panel, "SIVAS ELE GECIRILDI!", new Vector2(0,100), 46, new Color(1f,0.85f,0f), FontStyles.Bold);
        Txt(panel, "CP: " + (PlayerStats.Instance?.CP.ToString("N0") ?? "0"), new Vector2(0,25), 28, Color.white, FontStyles.Normal);
        Txt(panel, "Mermi: " + (PlayerStats.Instance?.BulletCount ?? 1), new Vector2(0,-20), 22, new Color(0.7f,0.7f,1f), FontStyles.Normal);
        Btn(panel, "TEKRAR DENE", new Vector2(0,-85), new Vector2(240,55), new Color(0.2f,0.7f,0.2f), () =>
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });
    }

    // ── HP Bar ────────────────────────────────────────────────────────────
    void BuildHPBar()
    {
        var co = new GameObject("BossHPCanvas");
        _bossCanvas = co.AddComponent<Canvas>();
        _bossCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _bossCanvas.sortingOrder = 50;
        co.AddComponent<CanvasScaler>(); co.AddComponent<GraphicRaycaster>();

        var strip = new GameObject("S"); strip.transform.SetParent(co.transform, false);
        strip.AddComponent<Image>().color = new Color(0,0,0,0.78f);
        var sr = strip.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.04f, 0.90f); sr.anchorMax = new Vector2(0.96f, 0.97f);
        sr.offsetMin = sr.offsetMax = Vector2.zero;

        var fo = new GameObject("F"); fo.transform.SetParent(strip.transform, false);
        _hpFill = fo.AddComponent<Image>();
        _hpFill.type = Image.Type.Filled; _hpFill.fillMethod = Image.FillMethod.Horizontal;
        _hpFill.color = new Color(0.2f,0.85f,0.2f);
        var fr = fo.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0.005f, 0.08f); fr.anchorMax = new Vector2(0.995f, 0.92f);
        fr.offsetMin = fr.offsetMax = Vector2.zero;

        _phaseLabel = Txt(strip, "GOKMEDRESE MUHAFIZI", Vector2.zero, 14, Color.white, FontStyles.Bold);
        var lr = _phaseLabel.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
    }

    void UpdateBar()
    {
        if (!_hpFill) return;
        float r = (float)_hp / bossMaxHP;
        _hpFill.fillAmount = r;
        _hpFill.color = r > 0.6f ? new Color(0.2f,0.85f,0.2f) : r > 0.3f ? new Color(1f,0.7f,0f) : new Color(0.9f,0.1f,0.1f);
    }

    void SetLabel(string s) { if (_phaseLabel) _phaseLabel.text = s; }

    IEnumerator Flash()
    {
        if (!_bossRend) yield break;
        Color o = _bossRend.material.color;
        _bossRend.material.color = Color.white;
        yield return new WaitForSeconds(0.07f);
        if (_bossRend) _bossRend.material.color = o;
    }

    // ── UI Helpers ─────────────────────────────────────────────────────────
    TextMeshProUGUI Txt(GameObject p, string txt, Vector2 pos, float sz, Color col, FontStyles fs)
    {
        var o = new GameObject("T"); o.transform.SetParent(p.transform, false);
        var t = o.AddComponent<TextMeshProUGUI>();
        t.text=txt; t.fontSize=sz; t.color=col; t.fontStyle=fs; t.alignment=TextAlignmentOptions.Center;
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f,0.5f); r.anchorMax = new Vector2(0.5f,0.5f);
        r.pivot = new Vector2(0.5f,0.5f); r.anchoredPosition=pos; r.sizeDelta=new Vector2(600,60);
        return t;
    }

    void Btn(GameObject p, string lbl, Vector2 pos, Vector2 sz, Color bg, UnityEngine.Events.UnityAction onClick)
    {
        var bo = new GameObject("B"); bo.transform.SetParent(p.transform, false);
        var im = bo.AddComponent<Image>(); im.color=bg;
        var bt = bo.AddComponent<Button>(); bt.targetGraphic=im; bt.onClick.AddListener(onClick);
        var r = bo.GetComponent<RectTransform>();
        r.anchorMin=new Vector2(0.5f,0.5f); r.anchorMax=new Vector2(0.5f,0.5f);
        r.pivot=new Vector2(0.5f,0.5f); r.anchoredPosition=pos; r.sizeDelta=sz;
        var lo = new GameObject("L"); lo.transform.SetParent(bo.transform, false);
        var lt = lo.AddComponent<TextMeshProUGUI>();
        lt.text=lbl; lt.fontSize=20; lt.color=Color.white; lt.fontStyle=FontStyles.Bold; lt.alignment=TextAlignmentOptions.Center;
        var lr = lo.GetComponent<RectTransform>();
        lr.anchorMin=Vector2.zero; lr.anchorMax=Vector2.one; lr.offsetMin=lr.offsetMax=Vector2.zero;
    }
}

public class BossHitReceiver : MonoBehaviour
{
    public BossManager bossManager;
}