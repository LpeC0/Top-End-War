using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Top End War — Boss Yoneticisi v3 (Claude)
///
/// SAVAŞ AKIŞI:
///   1. SpawnManager OnBossEncountered ateşler
///   2. BossManager OnAnchorModeChanged(true) ile oyuncuyu durdurur
///   3. Boss karşıdan (önden) oyuncuya doğru gelir
///   4. Oyuncu X ekseninde hareket ederek dodge yapar, auto-shoot ile vurur
///   5. Boss yenilince OnAnchorModeChanged(false) → oyuncu tekrar koşar
///
/// Faz geçişleri (HP yüzdeye göre):
///   Faz1 %100-60: Normal saldırı
///   Faz2 %60-30:  Minyon spawn (her 8sn)
///   Faz3 %30-0:   Hız x2, minyon yok
///
/// Boss prefab yoksa → fallback kırmızı küp (tamamen çalışır).
/// </summary>
public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Boss İstatistikleri")]
    public int   bossMaxHP      = 60000;
    public float bossApproachSpeed = 3.0f;  // Yaklaşma hızı
    public int   bossContactDmg = 100;
    public float bossStopDist   = 12f;      // Bu mesafeye gelince durur (chase)

    [Header("Faz Eşikleri")]
    [Range(0,1)] public float phase2At = 0.60f;
    [Range(0,1)] public float phase3At = 0.30f;

    [Header("Minyon (Faz2)")]
    public GameObject minionPrefab;
    public int        minionCount    = 3;
    public float      minionInterval = 8f;

    [Header("Boss Prefab (boş bırak → fallback küp)")]
    public GameObject bossPrefab;

    // ── Internal ──────────────────────────────────────────────────────────
    int    _hp;
    int    _phase  = 0;
    bool   _active = false;
    bool   _dead   = false;

    GameObject     _bossObj;
    Renderer       _bossRend;
    Canvas         _bossCanvas;
    Image          _hpFill;
    TextMeshProUGUI _phaseLabel;

    PlayerController _playerCtrl;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        GameEvents.OnBossEncountered += StartBossFight;
        BuildHPBar();
        _bossCanvas?.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        GameEvents.OnBossEncountered -= StartBossFight;
    }

    // ── Boss savaşı başlat ─────────────────────────────────────────────────
    void StartBossFight()
    {
        if (_active) return;
        Debug.Log("[Boss] Encounter tetiklendi!");
        _playerCtrl = FindFirstObjectByType<PlayerController>();
        StartCoroutine(BossEntranceCo());
    }

    IEnumerator BossEntranceCo()
    {
        // 1. Oyuncuyu durdur (Anchor modu)
        GameEvents.OnAnchorModeChanged?.Invoke(true);
        yield return new WaitForSeconds(0.3f);

        // 2. Boss'u OYUNCUNUN ÖNÜNDEKİ mesafeye spawn et (karşıdan gelecek)
        Vector3 playerPos = PlayerStats.Instance != null
            ? PlayerStats.Instance.transform.position
            : Vector3.zero;

        Vector3 spawnPos = new Vector3(0f, 1.2f, playerPos.z + 40f); // 40m önde

        SpawnBossAt(spawnPos);
        _hp     = bossMaxHP;
        _phase  = 1;
        _active = true;

        _bossCanvas?.gameObject.SetActive(true);
        UpdateHPBar();
        SetPhaseLabel("GOKMEDRESE MUHAFIZI — FAZ 1: TAS ZIRH");

        Debug.Log("[Boss] Gokmedrese Muhafizi sahneye girdi! HP=" + _hp);
    }

    // ── Her Frame ─────────────────────────────────────────────────────────
    void Update()
    {
        if (!_active || _dead || _bossObj == null) return;
        if (PlayerStats.Instance == null) return;

        Vector3 playerPos = PlayerStats.Instance.transform.position;
        Vector3 bossPos   = _bossObj.transform.position;

        // Boss oyuncuya DOĞRU hareket eder (Z azalır — karşıdan geliyor)
        float targetZ = playerPos.z + bossStopDist;

        if (bossPos.z > targetZ)
        {
            // Yaklaşıyor
            Vector3 newPos = bossPos;
            newPos.z = Mathf.MoveTowards(bossPos.z, targetZ, bossApproachSpeed * Time.deltaTime);
            _bossObj.transform.position = newPos;
        }
        else
        {
            // Mesafe kapandı — hasar ver
            PlayerStats.Instance.TakeContactDamage(bossContactDmg);
            // Boss biraz geri iter
            Vector3 p = _bossObj.transform.position;
            p.z += 8f;
            _bossObj.transform.position = p;
        }
    }

    // ── Boss Objesi Oluştur ────────────────────────────────────────────────
    void SpawnBossAt(Vector3 pos)
    {
        if (bossPrefab != null)
        {
            _bossObj = Instantiate(bossPrefab, pos, Quaternion.identity);
        }
        else
        {
            // Fallback — büyük kırmızı küp
            _bossObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _bossObj.transform.position   = pos;
            _bossObj.transform.localScale = new Vector3(5f, 7f, 5f);
            if (_bossObj.TryGetComponent<Renderer>(out var rend))
                rend.material.color = new Color(0.55f, 0.0f, 0.0f);
        }

        _bossRend = _bossObj.GetComponent<Renderer>();
        _bossObj.tag = "Enemy";

        // Bullet OverlapSphere için trigger OLMAMALI
        // BoxCollider isTrigger=false → Bullet.Update OverlapSphere zaten bulur
        Collider existing = _bossObj.GetComponent<Collider>();
        if (existing == null) existing = _bossObj.AddComponent<BoxCollider>();
        // Collider isTrigger=FALSE bırak — Bullet.OverlapSphere hem trigger hem non-trigger bulur

        // BossHitReceiver ekle (Bullet.Update BossHitReceiver'a direkt erişir)
        if (_bossObj.GetComponent<BossHitReceiver>() == null)
        {
            var recv = _bossObj.AddComponent<BossHitReceiver>();
            recv.bossManager = this;
        }
    }

    // ── Hasar Al ──────────────────────────────────────────────────────────
    public void TakeDamage(int dmg)
    {
        if (!_active || _dead) return;
        _hp = Mathf.Max(0, _hp - dmg);
        UpdateHPBar();
        StartCoroutine(HitFlashCo());
        CheckPhaseTransition();
        if (_hp <= 0) Defeated();
    }

    void CheckPhaseTransition()
    {
        float r = (float)_hp / bossMaxHP;
        if      (_phase == 1 && r <= phase2At) { _phase = 2; EnterPhase2(); }
        else if (_phase == 2 && r <= phase3At) { _phase = 3; EnterPhase3(); }
    }

    void EnterPhase2()
    {
        Debug.Log("[Boss] FAZ 2: MINYON!");
        SetPhaseLabel("FAZ 2: MINYON");
        if (_bossRend) _bossRend.material.color = new Color(0.7f, 0.3f, 0.1f);
        InvokeRepeating(nameof(SpawnMinions), 1f, minionInterval);
    }

    void EnterPhase3()
    {
        Debug.Log("[Boss] FAZ 3: CEKIRDEK!");
        SetPhaseLabel("FAZ 3: CEKIRDEK");
        if (_bossRend) _bossRend.material.color = new Color(0.9f, 0.05f, 0.05f);
        bossApproachSpeed *= 2f;
        CancelInvoke(nameof(SpawnMinions));
    }

    void SpawnMinions()
    {
        if (!_active || _dead || PlayerStats.Instance == null) return;
        Vector3 center = PlayerStats.Instance.transform.position + Vector3.forward * 8f;

        for (int i = 0; i < minionCount; i++)
        {
            Vector3 p = center + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(2f, 10f));
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
                new DifficultyManager.EnemyStats(200, 50, 5f, 10));
        }
    }

    // ── Zafer ─────────────────────────────────────────────────────────────
    void Defeated()
    {
        if (_dead) return;
        _dead = true; _active = false;
        CancelInvoke();

        PlayerStats.Instance?.AddCPFromKill(1000);
        Debug.Log("[Boss] BOSS YENILDI! +1000 CP");

        if (_bossObj) Destroy(_bossObj);
        _bossCanvas?.gameObject.SetActive(false);

        // Anchor modunu kapat — oyuncu tekrar koşabilir
        GameEvents.OnAnchorModeChanged?.Invoke(false);

        StartCoroutine(VictoryCo());
    }

    IEnumerator VictoryCo()
    {
        yield return new WaitForSeconds(1.5f);
        ShowVictoryScreen();
    }

    void ShowVictoryScreen()
    {
        Canvas c = FindFirstObjectByType<Canvas>();
        if (c == null) return;

        var panel = new GameObject("VictoryPanel");
        panel.transform.SetParent(c.transform, false);
        panel.AddComponent<Image>().color = new Color(0, 0, 0, 0.87f);
        var r = panel.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;

        MakeTxt(panel, "SIVAS ELE GECIRILDI!", new Vector2(0.5f,0.5f), new Vector2(0,100), 46, new Color(1f,0.85f,0f), FontStyles.Bold);
        MakeTxt(panel, "Son CP: " + (PlayerStats.Instance?.CP.ToString("N0") ?? "0"), new Vector2(0.5f,0.5f), new Vector2(0,30), 28, Color.white, FontStyles.Normal);
        MakeTxt(panel, "Mermi Sayisi: " + (PlayerStats.Instance?.BulletCount ?? 1), new Vector2(0.5f,0.5f), new Vector2(0,-15), 22, new Color(0.7f,0.7f,1f), FontStyles.Normal);

        MakeBtn(panel, "TEKRAR DENE",
            new Vector2(0.5f,0.5f), new Vector2(0,-80), new Vector2(240,55),
            new Color(0.2f,0.7f,0.2f),
            () =>
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            });
    }

    // ── HP Bar ─────────────────────────────────────────────────────────────
    void BuildHPBar()
    {
        var co = new GameObject("BossHPCanvas");
        _bossCanvas = co.AddComponent<Canvas>();
        _bossCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _bossCanvas.sortingOrder = 50;
        co.AddComponent<CanvasScaler>();
        co.AddComponent<GraphicRaycaster>();

        var strip = new GameObject("Strip");
        strip.transform.SetParent(_bossCanvas.transform, false);
        strip.AddComponent<Image>().color = new Color(0,0,0,0.78f);
        var sr = strip.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.04f, 0.90f);
        sr.anchorMax = new Vector2(0.96f, 0.97f);
        sr.offsetMin = sr.offsetMax = Vector2.zero;

        var fo = new GameObject("Fill");
        fo.transform.SetParent(strip.transform, false);
        _hpFill = fo.AddComponent<Image>();
        _hpFill.type = Image.Type.Filled;
        _hpFill.fillMethod = Image.FillMethod.Horizontal;
        _hpFill.color = new Color(0.2f, 0.85f, 0.2f);
        var fr = fo.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0.005f, 0.08f);
        fr.anchorMax = new Vector2(0.995f, 0.92f);
        fr.offsetMin = fr.offsetMax = Vector2.zero;

        _phaseLabel = MakeTxt(strip, "GOKMEDRESE MUHAFIZI",
            new Vector2(0.5f,0.5f), Vector2.zero, 15, Color.white, FontStyles.Bold);
        var lr = _phaseLabel.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
    }

    void UpdateHPBar()
    {
        if (!_hpFill) return;
        float r = (float)_hp / bossMaxHP;
        _hpFill.fillAmount = r;
        _hpFill.color = r > 0.6f ? new Color(0.2f,0.85f,0.2f)
                      : r > 0.3f ? new Color(1f,0.7f,0f)
                                 : new Color(0.9f,0.1f,0.1f);
    }

    void SetPhaseLabel(string txt) { if (_phaseLabel) _phaseLabel.text = txt; }

    IEnumerator HitFlashCo()
    {
        if (!_bossRend) yield break;
        Color orig = _bossRend.material.color;
        _bossRend.material.color = Color.white;
        yield return new WaitForSeconds(0.07f);
        if (_bossRend) _bossRend.material.color = orig;
    }

    // ── UI Yardımcılar ─────────────────────────────────────────────────────
    TextMeshProUGUI MakeTxt(GameObject parent, string txt, Vector2 anch, Vector2 pos,
        float sz, Color col, FontStyles fs)
    {
        var o = new GameObject("Txt"); o.transform.SetParent(parent.transform, false);
        var t = o.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = sz; t.color = col;
        t.fontStyle = fs; t.alignment = TextAlignmentOptions.Center;
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = anch; r.anchorMax = anch;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = new Vector2(600, 60);
        return t;
    }

    void MakeBtn(GameObject parent, string lbl, Vector2 anch, Vector2 pos, Vector2 sz,
        Color bg, UnityEngine.Events.UnityAction onClick)
    {
        var bo = new GameObject("Btn"); bo.transform.SetParent(parent.transform, false);
        var im = bo.AddComponent<Image>(); im.color = bg;
        var bt = bo.AddComponent<Button>(); bt.targetGraphic = im;
        bt.onClick.AddListener(onClick);
        var r = bo.GetComponent<RectTransform>();
        r.anchorMin = anch; r.anchorMax = anch;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = sz;

        var lo = new GameObject("Lbl"); lo.transform.SetParent(bo.transform, false);
        var lt = lo.AddComponent<TextMeshProUGUI>();
        lt.text = lbl; lt.fontSize = 20; lt.color = Color.white;
        lt.fontStyle = FontStyles.Bold; lt.alignment = TextAlignmentOptions.Center;
        var lr = lo.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
    }
}

/// <summary>
/// Boss objesine eklenir.
/// Bullet.cs Update() OverlapSphere bu component'i arar ve TakeDamage çağırır.
/// </summary>
public class BossHitReceiver : MonoBehaviour
{
    public BossManager bossManager;
}