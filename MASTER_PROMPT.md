# Top End War — MASTER PROMPT v10
**Repo:** https://github.com/LpeC0/Top-End-War

Projem Unity 6.3 LTS URP 3D mobil runner/auto-shooter: Top End War

---

## OYUN TANIMI
Runner/auto-shooter. Player otomatik kosar, surukleme ile serbest hareket.
Yolda matematiksel kapılar (sol/sag — oyuncu birinden gecer).
Dusmanlar dalga halinde (3 tip), auto-shoot ile vurulur.
CP = savas gucu = can. Tier atlarken model morph.
1200m boss, sonra Turkiye haritasinda yeni sehir.

---

## DEGISTIRILEMEZ KURALLAR
```
xLimit = 8              PlayerController + Enemy + SpawnManager.ROAD_HALF_WIDTH AYNI
Player Rigidbody YOK    transform.position hareketi
Cinemachine YOK         SimpleCameraFollow (X sabit)
Input: Old/Legacy
Namespace YOK
GameEvents: Action<>    Raise...() metod YOK — abonelik += ile
PlayerStats.CP          Property (public field degil, partial class degil)
Gate shader: Sprites/Default
Gate Panel: QUAD (Cube degil)
SetActive(false)        pool icin (Destroy degil)
Unicode sembol KULLANMA
Player'a Enemy.cs/Bullet.cs EKLEME
DOTween kurulu
```

---

## HIERARCHY
```
SampleScene
  PoolManager         ObjectPooler  (Bullet:20, Enemy:20)
  DifficultyManager   DifficultyManager + ProgressionConfig (opsiyonel)
  GameOverManager     GameOverUI
  Player              PlayerController + PlayerStats + MorphController + GateFeedback [Tag:Player]
      FirePoint
  Main Camera         SimpleCameraFollow
  SpawnManager        SpawnManager (GatePrefab, EnemyPrefab, GateDataList — hepsi opsiyonel)
  ChunkManager        ChunkManager (RoadChunk X scale=1.6)
  Canvas
      CPText, TierText (TEXT BOSALT), PopupText, SynergyText
      DamageFlash (Image, full stretch, alpha=0, RaycastTarget=false)
      PiyadeBar, MekanizeBar, TeknolojiBar (Slider)
      HUDPanel        GameHUD
```

---

## TUM SCRIPTLER

### Bullet.cs
```csharp
using UnityEngine;
public class Bullet : MonoBehaviour
{
    public int   damage      = 60;
    public Color bulletColor = new Color(0.55f, 0f, 1f);
    Renderer _renderer;
    void Awake() { _renderer = GetComponentInChildren<Renderer>(); }
    void OnEnable()
    {
        if (_renderer != null) { if (_renderer.material.HasProperty("_BaseColor")) _renderer.material.SetColor("_BaseColor", bulletColor); else _renderer.material.color = bulletColor; }
        Invoke(nameof(ReturnToPool), 2.5f);
    }
    void OnDisable() { CancelInvoke(); }
    public void SetDamage(int d) { damage = d; }
    void OnTriggerEnter(Collider other) { if (!other.CompareTag("Enemy")) return; other.GetComponent<Enemy>()?.TakeDamage(damage); ReturnToPool(); }
    void ReturnToPool() { Rigidbody rb = GetComponent<Rigidbody>(); if (rb) rb.linearVelocity = Vector3.zero; gameObject.SetActive(false); }
}
```

### ChunkManager.cs
```csharp
using UnityEngine; using System.Collections.Generic;
public class ChunkManager : MonoBehaviour
{
    public GameObject chunkPrefab; public Transform playerTransform;
    public int initialChunks = 5; public float chunkLength = 50f;
    float spawnZ = 0f; Queue<GameObject> activeChunks = new Queue<GameObject>();
    void Start() { for (int i = 0; i < initialChunks; i++) SpawnChunk(); }
    void Update() { if (playerTransform == null) return; if (playerTransform.position.z - (chunkLength*1.5f) > (spawnZ-(initialChunks*chunkLength))) { SpawnChunk(); DeleteOldChunk(); } }
    void SpawnChunk() { GameObject c = Instantiate(chunkPrefab, new Vector3(0,0,spawnZ), Quaternion.identity); c.transform.SetParent(this.transform); activeChunks.Enqueue(c); spawnZ += chunkLength; }
    void DeleteOldChunk() { Destroy(activeChunks.Dequeue()); }
}
```

### DifficultyManager.cs
```csharp
using UnityEngine;
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }
    [Header("Config (Opsiyonel)")] public ProgressionConfig config;
    [Header("Guncelleme")] public float updateInterval = 50f;
    const float BASE_HP=100f,BASE_DMG=25f,BASE_SPEED=4.0f,MAX_SPEED=7.5f,BASE_REWARD=15f;
    public float CurrentDifficultyMultiplier { get; private set; } = 1f;
    public float PlayerPowerRatio            { get; private set; } = 1f;
    public readonly struct EnemyStats { public readonly int Health,Damage,CPReward; public readonly float Speed; public EnemyStats(int h,int d,float s,int r){Health=h;Damage=d;Speed=s;CPReward=r;} }
    Transform _player; float _lastZ=-9999f,_currentZ;
    void Awake() { if(Instance!=null){Destroy(gameObject);return;} Instance=this; }
    void Start() { if(PlayerStats.Instance!=null)_player=PlayerStats.Instance.transform; }
    void Update() { if(_player==null){if(PlayerStats.Instance!=null)_player=PlayerStats.Instance.transform;return;} _currentZ=_player.position.z; if(Mathf.Abs(_currentZ-_lastZ)>=updateInterval){Recalculate();_lastZ=_currentZ;} }
    void Recalculate()
    {
        CurrentDifficultyMultiplier=1f+Mathf.Pow(_currentZ/1000f,1.3f);
        int exp=config!=null?config.CalculateExpectedCP(_currentZ):Mathf.RoundToInt(200f*Mathf.Pow(1.15f,_currentZ/100f));
        float raw=(float)(PlayerStats.Instance?.CP??200)/Mathf.Max(1,exp);
        PlayerPowerRatio=Mathf.Lerp(PlayerPowerRatio,raw,0.08f);
        PlayerStats.Instance?.SetExpectedCP(exp);
        GameEvents.OnDifficultyChanged?.Invoke(CurrentDifficultyMultiplier,PlayerPowerRatio);
    }
    public EnemyStats GetScaledEnemyStats()
    {
        float diff=CurrentDifficultyMultiplier,pS=config!=null?Mathf.Lerp(1f,PlayerPowerRatio,config.playerCPScalingFactor):Mathf.Lerp(1f,PlayerPowerRatio,0.7f),f=diff*pS;
        float bH=config!=null?config.baseEnemyHealth:BASE_HP,bD=config!=null?config.baseEnemyDamage:BASE_DMG,bS=config!=null?config.baseEnemySpeed:BASE_SPEED,mS=config!=null?config.enemyMaxSpeed:MAX_SPEED;
        return new EnemyStats(Mathf.RoundToInt(bH*f),Mathf.RoundToInt(bD*f),Mathf.Min(bS*(1f+(diff-1f)*0.35f),mS),Mathf.RoundToInt(BASE_REWARD*diff));
    }
    public bool IsInPityZone(float boss) { float z=config!=null?config.noBadGateZoneBeforeBoss:200f; return _currentZ>=boss-z; }
    void OnDestroy() { if(Instance==this)Instance=null; }
}
```

### Enemy.cs
```csharp
using UnityEngine;
public class Enemy : MonoBehaviour
{
    public float xLimit=8f;
    int _maxHP,_hp,_dmg,_reward; float _spd; bool _init=false,_dead=false,_hit=false;
    Renderer _r; EnemyHealthBar _hpBar;
    float _lastSep=0f; Vector3 _sep=Vector3.zero; const float SEP_INT=0.15f;
    void Awake() { _r=GetComponentInChildren<Renderer>(); _hpBar=GetComponent<EnemyHealthBar>(); if(_hpBar==null)_hpBar=gameObject.AddComponent<EnemyHealthBar>(); UseDefaults(); }
    void OnEnable() { _dead=_hit=false; _sep=Vector3.zero; if(_r!=null)_r.material.color=Color.white; if(!_init)AutoInit(); _hpBar?.Init(_maxHP); }
    public void Initialize(DifficultyManager.EnemyStats s) { _maxHP=s.Health;_hp=_maxHP;_dmg=s.Damage;_spd=s.Speed;_reward=s.CPReward;_init=true;_dead=_hit=false; if(_r!=null)_r.material.color=Color.white; _hpBar?.Init(_maxHP); }
    void AutoInit() { float z=PlayerStats.Instance!=null?PlayerStats.Instance.transform.position.z:0f,m=1f+Mathf.Pow(z/1000f,1.3f); _maxHP=Mathf.RoundToInt(100f*m);_hp=_maxHP;_dmg=Mathf.RoundToInt(25f*m);_spd=Mathf.Min(4f+(m-1f)*1.4f,7.5f);_reward=Mathf.RoundToInt(15f*m); }
    void UseDefaults() { _maxHP=_hp=120;_dmg=50;_spd=4.5f;_reward=15; }
    void Update()
    {
        if(_dead||PlayerStats.Instance==null)return;
        float pZ=PlayerStats.Instance.transform.position.z; Vector3 pos=transform.position;
        if(pos.z>pZ+0.5f)pos.z-=_spd*Time.deltaTime;
        pos.x=Mathf.Clamp(Mathf.MoveTowards(pos.x,PlayerStats.Instance.transform.position.x,1.5f*Time.deltaTime),-xLimit,xLimit);
        if(Time.time-_lastSep>SEP_INT){_sep=CalcSep(pos);_lastSep=Time.time;}
        pos+=_sep*Time.deltaTime; pos.x=Mathf.Clamp(pos.x,-xLimit,xLimit); transform.position=pos;
        if(pos.z<pZ-15f)gameObject.SetActive(false);
    }
    Vector3 CalcSep(Vector3 pos) { Vector3 s=Vector3.zero;int c=0; foreach(Collider col in Physics.OverlapSphere(pos,1.8f)){if(col.gameObject==gameObject||!col.CompareTag("Enemy"))continue;Vector3 a=pos-col.transform.position;a.y=0f;if(a.magnitude<0.001f)a=new Vector3(Random.Range(-1f,1f),0,0).normalized*0.1f;s+=a.normalized/Mathf.Max(a.magnitude,0.1f);c++;} return c>0?(s/c)*3.5f:Vector3.zero; }
    public void TakeDamage(int d) { if(_dead)return;_hp-=d;_hpBar?.UpdateBar(_hp);if(_r!=null)_r.material.color=Color.red;Invoke(nameof(ResetColor),0.1f);if(_hp<=0)Die(); }
    void ResetColor(){if(!_dead&&_r!=null)_r.material.color=Color.white;}
    void Die(){if(_dead)return;_dead=_init=false;CancelInvoke();PlayerStats.Instance?.AddCPFromKill(_reward);gameObject.SetActive(false);}
    void OnTriggerEnter(Collider o){if(!o.CompareTag("Player")||_hit||_dead)return;_hit=true;o.GetComponent<PlayerStats>()?.TakeContactDamage(_dmg);Die();}
    void OnDisable(){CancelInvoke();_init=false;}
}
```

### EnemyHealthBar.cs
```csharp
using UnityEngine; using UnityEngine.UI;
public class EnemyHealthBar : MonoBehaviour
{
    public float barWidth=1.2f,barHeight=0.15f,barYOffset=1.8f;
    public Color fullColor=new Color(0.15f,0.85f,0.15f),halfColor=new Color(0.95f,0.75f,0.05f),lowColor=new Color(0.9f,0.15f,0.15f);
    Canvas _canvas; Image _fill; int _max; Transform _cam;
    void Awake(){BuildBar();_cam=Camera.main?.transform;}
    void LateUpdate(){if(_canvas==null||_cam==null)return;_canvas.transform.position=transform.position+Vector3.up*barYOffset;_canvas.transform.LookAt(_canvas.transform.position+_cam.forward);}
    public void Init(int m){_max=Mathf.Max(1,m);UpdateBar(m);}
    public void UpdateBar(int hp){if(_fill==null)return;float r=(float)Mathf.Max(0,hp)/_max;_fill.fillAmount=r;_fill.color=r>0.6f?fullColor:r>0.3f?Color.Lerp(halfColor,fullColor,(r-0.3f)/0.3f):Color.Lerp(lowColor,halfColor,r/0.3f);if(_canvas!=null)_canvas.gameObject.SetActive(hp>0);}
    void BuildBar(){var co=new GameObject("HPBarCanvas");co.transform.SetParent(transform);co.transform.localPosition=Vector3.up*barYOffset;_canvas=co.AddComponent<Canvas>();_canvas.renderMode=RenderMode.WorldSpace;_canvas.sortingOrder=10;co.GetComponent<RectTransform>().sizeDelta=new Vector2(barWidth,barHeight*2f);var bg=new GameObject("BG");bg.transform.SetParent(co.transform,false);var bi=bg.AddComponent<Image>();bi.color=new Color(0.1f,0.1f,0.1f,0.8f);var br=bg.GetComponent<RectTransform>();br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;var fo=new GameObject("Fill");fo.transform.SetParent(co.transform,false);_fill=fo.AddComponent<Image>();_fill.type=Image.Type.Filled;_fill.fillMethod=Image.FillMethod.Horizontal;_fill.color=fullColor;var fr=fo.GetComponent<RectTransform>();fr.anchorMin=Vector2.zero;fr.anchorMax=Vector2.one;fr.offsetMin=fr.offsetMax=Vector2.zero;}
}
```

### GameEvents.cs
```csharp
using System;
public static class GameEvents
{
    public static Action<int>    OnCPUpdated;
    public static Action<int>    OnTierChanged;
    public static Action<string> OnPathBoosted;
    public static Action         OnMergeTriggered;
    public static Action<string> OnSynergyFound;
    public static Action<int>    OnPlayerDamaged;
    public static Action         OnGameOver;
    public static Action<int>    OnRiskBonusActivated;
    public static Action<float,float> OnDifficultyChanged;
    public static Action         OnBossEncountered;
}
```

### GameHUD.cs
```csharp
// [Uploaded file — tam icerik uploads'tan geldi, aynen kullan]
// GameHUD v6 (Claude) — auto-build HUD, Observer pattern
// OnRiskBonusActivated listener var
// FindFirstObjectByType<Canvas>() kullanıyor
```

### GameOverUI.cs
```csharp
// [Uploaded file — tam icerik uploads'tan geldi, aynen kullan]
// GameOverUI (Claude) — programatik Canvas, Time.timeScale=0
// GameEvents.OnGameOver dinler, TEKRAR DENE butonu SceneManager.LoadScene
```

### Gate.cs
```csharp
using UnityEngine; using TMPro;
public class Gate : MonoBehaviour
{
    public GateData gateData; public Renderer panelRenderer; public TextMeshPro labelText;
    bool _triggered=false;
    void Start(){RemoveChildColliders();ApplyVisuals();FitBoxCollider();}
    void OnEnable(){_triggered=false;}
    public void Refresh(){ApplyVisuals();FitBoxCollider();}
    void RemoveChildColliders(){foreach(Collider c in GetComponentsInChildren<Collider>())if(c.gameObject!=gameObject)Destroy(c);}
    void ApplyVisuals(){if(gateData==null)return;if(labelText!=null){labelText.text=gateData.gateText;labelText.fontSize=5f;labelText.color=Color.white;labelText.alignment=TextAlignmentOptions.Center;labelText.fontStyle=FontStyles.Bold;labelText.overflowMode=TextOverflowModes.Truncate;labelText.enableWordWrapping=false;}if(panelRenderer!=null){Material m=new Material(Shader.Find("Sprites/Default"));Color c=gateData.gateColor;c.a=0.72f;m.color=c;panelRenderer.material=m;}}
    void FitBoxCollider(){BoxCollider b=GetComponent<BoxCollider>();if(b==null||panelRenderer==null)return;Vector3 s=panelRenderer.transform.localScale;b.size=new Vector3(s.x*0.95f,s.y,1.2f);b.center=Vector3.zero;}
    void OnTriggerEnter(Collider other){if(_triggered||!other.CompareTag("Player"))return;_triggered=true;other.GetComponent<PlayerStats>()?.ApplyGateEffect(gateData);Debug.Log("[Gate]"+gateData.gateText+"|CP:"+PlayerStats.Instance?.CP);Destroy(gameObject);}
}
```

### GateData.cs
```csharp
using UnityEngine;
public enum GateEffectType { AddCP,MultiplyCP,Merge,PathBoost_Piyade,PathBoost_Mekanize,PathBoost_Teknoloji,NegativeCP,RiskReward }
[CreateAssetMenu(fileName="NewGateData",menuName="TopEndWar/Gate Data")]
public class GateData : ScriptableObject
{
    [Header("Gorsel")] public string gateText="+60"; public Color gateColor=new Color(0.2f,0.85f,0.2f,0.7f);
    [Header("Etki")]   public GateEffectType effectType=GateEffectType.AddCP; public float effectValue=60f;
}
```

### GateFeedback.cs
```csharp
using UnityEngine; using DG.Tweening;
public class GateFeedback : MonoBehaviour
{
    [Header("Gate")] public float gatePopDuration=0.25f,gatePopScale=1.25f;
    [Header("Tier")] public float tierPopDuration=0.4f,tierPopScale=1.5f;
    [Header("Kamera")] public Camera mainCamera; public float shakeStrength=0.3f,shakeDuration=0.3f;
    Vector3 _orig; Tweener _t;
    void Start(){_orig=transform.localScale;GameEvents.OnCPUpdated+=OnCP;GameEvents.OnTierChanged+=OnTier;if(mainCamera==null)mainCamera=Camera.main;}
    void OnDestroy(){GameEvents.OnCPUpdated-=OnCP;GameEvents.OnTierChanged-=OnTier;}
    void OnCP(int _){ScalePop(gatePopScale,gatePopDuration);}
    void OnTier(int _){ScalePop(tierPopScale,tierPopDuration);if(mainCamera!=null)mainCamera.DOShakePosition(shakeDuration,shakeStrength,10,90,false);}
    void ScalePop(float peak,float dur){_t?.Kill();transform.localScale=_orig;_t=transform.DOScale(_orig*peak,dur*0.4f).SetEase(Ease.OutQuad).OnComplete(()=>transform.DOScale(_orig,dur*0.6f).SetEase(Ease.InOutQuad));}
}
```

### MorphController.cs
```csharp
using UnityEngine; using System.Collections; using DG.Tweening;
public class MorphController : MonoBehaviour
{
    [Header("Tier Prefablari")] public GameObject[] tierPrefabs;
    [Header("VFX")] public GameObject morphParticlePrefab;
    [Header("Anim")] public float shrinkDuration=0.15f,popDuration=0.35f,popPeak=1.35f;
    GameObject[] _models; int _idx=-1; bool _morphing=false;
    void Start(){PrewarmModels();GameEvents.OnTierChanged+=OnTier;ActivateTier(0);}
    void OnDestroy(){GameEvents.OnTierChanged-=OnTier;}
    void PrewarmModels(){int n=tierPrefabs!=null?tierPrefabs.Length:5;_models=new GameObject[n];for(int i=0;i<n;i++){GameObject m;if(tierPrefabs!=null&&i<tierPrefabs.Length&&tierPrefabs[i]!=null)m=Instantiate(tierPrefabs[i],transform);else{m=GameObject.CreatePrimitive(PrimitiveType.Capsule);m.transform.SetParent(transform);Destroy(m.GetComponent<Collider>());}m.transform.localPosition=Vector3.zero;m.transform.localScale=Vector3.one;foreach(Collider c in m.GetComponentsInChildren<Collider>())Destroy(c);m.SetActive(false);_models[i]=m;}}
    void OnTier(int t){int i=Mathf.Clamp(t-1,0,_models.Length-1);if(i==_idx||_morphing)return;StartCoroutine(MorphCo(i));}
    IEnumerator MorphCo(int ti){_morphing=true;if(_idx>=0&&_idx<_models.Length){var p=_models[_idx];if(p!=null){yield return p.transform.DOScale(Vector3.zero,shrinkDuration).SetEase(Ease.InBack).WaitForCompletion();p.SetActive(false);p.transform.localScale=Vector3.one;}}if(morphParticlePrefab!=null)Destroy(Instantiate(morphParticlePrefab,transform.position,Quaternion.identity),2f);ActivateTier(ti);_morphing=false;}
    void ActivateTier(int i){if(_models==null||i>=_models.Length)return;var m=_models[i];if(m==null)return;m.transform.localScale=Vector3.zero;m.SetActive(true);m.transform.DOScale(Vector3.one*popPeak,popDuration*0.5f).SetEase(Ease.OutBack).OnComplete(()=>{if(m!=null)m.transform.DOScale(Vector3.one,popDuration*0.5f).SetEase(Ease.InOutQuad);});_idx=i;}
}
```

### ObjectPooler.cs
```csharp
using System.Collections.Generic; using UnityEngine;
public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;
    [System.Serializable] public class Pool{public string tag;public GameObject prefab;public int size;}
    public List<Pool> pools; public Dictionary<string,Queue<GameObject>> poolDictionary;
    void Awake(){if(Instance==null)Instance=this;else{Destroy(gameObject);return;}poolDictionary=new Dictionary<string,Queue<GameObject>>();foreach(Pool p in pools){Queue<GameObject> q=new Queue<GameObject>();for(int i=0;i<p.size;i++){GameObject o=Instantiate(p.prefab);o.SetActive(false);o.transform.parent=this.transform;q.Enqueue(o);}poolDictionary.Add(p.tag,q);}}
    public GameObject SpawnFromPool(string tag,Vector3 pos,Quaternion rot){if(!poolDictionary.ContainsKey(tag))return null;GameObject o=poolDictionary[tag].Dequeue();o.SetActive(true);o.transform.position=pos;o.transform.rotation=rot;poolDictionary[tag].Enqueue(o);return o;}
}
```

### PlayerController.cs
```csharp
using UnityEngine;
public class PlayerController : MonoBehaviour
{
    [Header("Ileri")] public float forwardSpeed=10f;
    [Header("Yatay")] public float dragSensitivity=0.05f,smoothing=14f,xLimit=8f;
    [Header("Ates")] public Transform firePoint; public GameObject bulletPrefab; public float detectRange=35f;
    static readonly float[] fireRates={1.5f,2.5f,4.0f,6.0f,8.5f};
    static readonly int[]   damages  ={60,95,145,210,300};
    float targetX=0f,nextFire=0f,lastMouseX; bool dragging=false;
    void Start(){transform.position=new Vector3(0f,1.2f,0f);if(GetComponent<Collider>()==null){var c=gameObject.AddComponent<CapsuleCollider>();c.height=2f;c.radius=0.4f;c.isTrigger=false;}}
    void Update(){HandleDrag();MovePlayer();AutoShoot();}
    void HandleDrag(){if(Input.GetKey(KeyCode.LeftArrow))targetX=Mathf.Clamp(targetX-10f*Time.deltaTime,-xLimit,xLimit);if(Input.GetKey(KeyCode.RightArrow))targetX=Mathf.Clamp(targetX+10f*Time.deltaTime,-xLimit,xLimit);if(Input.GetMouseButtonDown(0)){dragging=true;lastMouseX=Input.mousePosition.x;}if(Input.GetMouseButtonUp(0))dragging=false;if(dragging){targetX=Mathf.Clamp(targetX+(Input.mousePosition.x-lastMouseX)*dragSensitivity,-xLimit,xLimit);lastMouseX=Input.mousePosition.x;}}
    void MovePlayer(){Vector3 p=transform.position;p.z+=forwardSpeed*Time.deltaTime;p.x=Mathf.Lerp(p.x,targetX,Time.deltaTime*smoothing);p.x=Mathf.Clamp(p.x,-xLimit,xLimit);p.y=1.2f;transform.position=p;}
    void AutoShoot(){if(!firePoint)return;int idx=Mathf.Clamp((PlayerStats.Instance!=null?PlayerStats.Instance.CurrentTier:1)-1,0,4);if(Time.time<nextFire)return;RaycastHit hit;if(!Physics.BoxCast(transform.position+Vector3.up,new Vector3(xLimit*0.55f,1.2f,0.5f),Vector3.forward,out hit,Quaternion.identity,detectRange)||!hit.collider.CompareTag("Enemy"))return;float d=Vector3.Distance(firePoint.position,hit.transform.position);Vector3 aim=hit.transform.position+Vector3.back*(d/30f*4f);Vector3 dir=(aim-firePoint.position).normalized;GameObject b=ObjectPooler.Instance?.SpawnFromPool("Bullet",firePoint.position,Quaternion.LookRotation(dir));if(b==null&&bulletPrefab!=null){b=Instantiate(bulletPrefab,firePoint.position,Quaternion.LookRotation(dir));Destroy(b,3f);}if(b==null)return;b.GetComponent<Bullet>()?.SetDamage(damages[idx]);Rigidbody rb=b.GetComponent<Rigidbody>();if(rb)rb.linearVelocity=dir*30f;nextFire=Time.time+1f/fireRates[idx];}
}
```

### PlayerStats.cs
```csharp
using UnityEngine;
[DefaultExecutionOrder(-10)]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }
    [Header("Baslangic")] public int startCP=200; public float invincibilityDuration=0.8f;
    public int   CP            { get; private set; }
    public int   CurrentTier   { get; private set; } = 1;
    public float PiyadePath    { get; private set; } = 33f;
    public float MekanizePath  { get; private set; } = 33f;
    public float TeknolojiPath { get; private set; } = 34f;
    public float SmoothedPowerRatio { get; private set; } = 1f;
    float lastDmgTime=-99f; int riskLeft=0; float expCP=200f;
    static readonly int[]    tierCP={0,300,800,2000,5000};
    static readonly string[] names={"Gonullu Er","Elit Komando","Gatling Timi","Hava Indirme","Suru Drone"};
    void Awake(){if(Instance!=null){Destroy(gameObject);return;}Instance=this;CP=startCP;}
    void Start()=>GameEvents.OnCPUpdated?.Invoke(CP);
    public void TakeContactDamage(int a){if(Time.time-lastDmgTime<invincibilityDuration)return;lastDmgTime=Time.time;int ot=CurrentTier;CP=Mathf.Max(30,CP-a);RefreshTier();GameEvents.OnPlayerDamaged?.Invoke(a);GameEvents.OnCPUpdated?.Invoke(CP);if(CurrentTier!=ot)GameEvents.OnTierChanged?.Invoke(CurrentTier);if(CP<=30)GameEvents.OnGameOver?.Invoke();}
    public void AddCPFromKill(int a){int ot=CurrentTier;CP+=a;RefreshTier();GameEvents.OnCPUpdated?.Invoke(CP);if(CurrentTier!=ot)GameEvents.OnTierChanged?.Invoke(CurrentTier);}
    public void ApplyGateEffect(GateData data)
    {
        if(data==null)return;int ot=CurrentTier;float b=riskLeft>0?1.5f:1f;
        switch(data.effectType){
            case GateEffectType.AddCP:CP+=Mathf.RoundToInt(data.effectValue*b);break;
            case GateEffectType.MultiplyCP:CP=Mathf.RoundToInt(CP*data.effectValue);break;
            case GateEffectType.Merge:HandleMerge(data);break;
            case GateEffectType.PathBoost_Piyade:CP+=Mathf.RoundToInt(data.effectValue*b);PiyadePath+=20f;GameEvents.OnPathBoosted?.Invoke("Piyade");break;
            case GateEffectType.PathBoost_Mekanize:CP+=Mathf.RoundToInt(data.effectValue*b);MekanizePath+=20f;GameEvents.OnPathBoosted?.Invoke("Mekanize");break;
            case GateEffectType.PathBoost_Teknoloji:CP+=Mathf.RoundToInt(data.effectValue*b);TeknolojiPath+=20f;GameEvents.OnPathBoosted?.Invoke("Teknoloji");break;
            case GateEffectType.NegativeCP:CP=Mathf.Max(30,CP-Mathf.RoundToInt(data.effectValue));break;
            case GateEffectType.RiskReward:int pen=Mathf.RoundToInt(CP*0.30f);CP=Mathf.Max(50,CP-pen);riskLeft=3;GameEvents.OnRiskBonusActivated?.Invoke(riskLeft);break;
        }
        if(riskLeft>0&&data.effectType!=GateEffectType.NegativeCP&&data.effectType!=GateEffectType.RiskReward){riskLeft--;if(riskLeft>0)GameEvents.OnRiskBonusActivated?.Invoke(riskLeft);}
        CP=Mathf.Max(30,CP);UpdateRatio();RefreshTier();CheckSynergy();GameEvents.OnCPUpdated?.Invoke(CP);if(CurrentTier!=ot)GameEvents.OnTierChanged?.Invoke(CurrentTier);
    }
    void HandleMerge(GateData data){float tot=PiyadePath+MekanizePath+TeknolojiPath;if(tot<1f){CP=Mathf.RoundToInt(CP*1.8f);GameEvents.OnMergeTriggered?.Invoke();return;}float p=PiyadePath/tot,m=MekanizePath/tot,t=TeknolojiPath/tot,th=0.5f;string role=t>=th?"Teknoloji":p>=th?"Piyade":m>=th?"Mekanize":"none";CP=Mathf.RoundToInt(CP*1.8f);if(role!="none"){PiyadePath=MekanizePath=TeknolojiPath=0f;Debug.Log("[Merge]"+role);}GameEvents.OnMergeTriggered?.Invoke();}
    public void SetExpectedCP(float e){expCP=Mathf.Max(1f,e);UpdateRatio();}
    void UpdateRatio(){SmoothedPowerRatio=Mathf.Lerp(SmoothedPowerRatio,(float)CP/expCP,0.08f);}
    void RefreshTier(){for(int i=tierCP.Length-1;i>=0;i--)if(CP>=tierCP[i]){CurrentTier=i+1;return;}CurrentTier=1;}
    void CheckSynergy(){float t=PiyadePath+MekanizePath+TeknolojiPath;if(t==0)return;float p=PiyadePath/t,m=MekanizePath/t,tk=TeknolojiPath/t;if(Mathf.Min(p,Mathf.Min(m,tk))>0.25f){GameEvents.OnSynergyFound?.Invoke("PERFECT GENETICS");return;}if(p>0.5f&&m>0.25f){GameEvents.OnSynergyFound?.Invoke("Exosuit Komutu");return;}if(p>0.5f&&tk>0.25f){GameEvents.OnSynergyFound?.Invoke("Drone Takimi");return;}if(m>0.4f&&tk>0.3f){GameEvents.OnSynergyFound?.Invoke("Fuzyon Robotu");return;}}
    public string GetTierName()=>names[Mathf.Clamp(CurrentTier-1,0,4)];
    public int GetRiskBonus()=>riskLeft;
}
```

### ProgressionConfig.cs
```csharp
using UnityEngine;
[CreateAssetMenu(fileName="ProgressionConfig",menuName="TopEndWar/Progression Config")]
public class ProgressionConfig : ScriptableObject
{
    [Header("Ilerleme")][Range(1.05f,1.5f)]public float growthRate=1.15f;[Range(1.0f,3.0f)]public float difficultyExponent=1.3f;public int baseStartCP=200;
    [Header("Dusman")]public int baseEnemyHealth=100,baseEnemyDamage=25;public float baseEnemySpeed=4.0f,enemyMaxSpeed=7.5f;[Range(0.5f,1.5f)]public float playerCPScalingFactor=0.9f;
    [Header("Kapi")]public float gateValueGrowthRate=1.12f,noBadGateZoneBeforeBoss=200f;public int minGateValue=20,maxGateValue=500;
    [Header("Tier")]public int[] tierThresholds={0,300,800,2000,5000};
    public int CalculateExpectedCP(float d)=>Mathf.RoundToInt(baseStartCP*Mathf.Pow(growthRate,d/100f));
    public float CalculateDifficultyMultiplier(float d)=>1f+Mathf.Pow(d/1000f,difficultyExponent);
    public int ScaleGateValue(int v,float d){int s=Mathf.RoundToInt(v*Mathf.Pow(gateValueGrowthRate,d/150f));if(s<minGateValue)return minGateValue;if(s>maxGateValue)return maxGateValue;return s;}
}
```

### SimpleCameraFollow.cs
```csharp
using UnityEngine;
public class SimpleCameraFollow : MonoBehaviour
{
    public Transform target; public float heightOffset=9f,backOffset=11f,followSpeed=12f;
    void LateUpdate(){if(target==null)return;Vector3 d=new Vector3(0f,target.position.y+heightOffset,target.position.z-backOffset);transform.position=Vector3.Lerp(transform.position,d,Time.deltaTime*followSpeed);transform.LookAt(target.position+Vector3.up*1.5f);}
}
```

### SpawnManager.cs
```csharp
// [Uploaded file — tam icerik uploads'tan geldi, aynen kullan]
// SpawnManager v6 (Claude) — standalone, runtime gate + enemy olusturma
// 3 dalga tipi (Normal/Agir/Kanat), pity timer, DifficultyManager entegrasyonu
```

---

## KULLANIM SABLONU
```
Projem Unity 6.3 LTS URP 3D mobil runner: Top End War
GitHub: https://github.com/LpeC0/Top-End-War
MASTER PROMPT: [bu dosyanin tamami]

Scriptler: PlayerController, PlayerStats, SimpleCameraFollow,
GameEvents, GateData, Gate, SpawnManager, GameHUD, ObjectPooler,
ChunkManager, MorphController, Enemy, Bullet, EnemyHealthBar,
ProgressionConfig, DifficultyManager, GameOverUI, GateFeedback

[X] yazmak istiyorum.
Unity 6.3 LTS URP, Rigidbody YOK, Input Legacy, DOTween kurulu.
xLimit=8, Sprites/Default shader, unicode yok, Namespace yok.
GameEvents: Action<> pattern, Raise...() metod yok.
PlayerStats.CP property (field degil).
```

---

## DEGISIKLIK GECMISI
```
v1-v3   Grok+Gemini+Claude: Temel sistem
v4-v6   Claude: Drag, pool, morph, spawn
v7      Claude: DDA, RiskReward, pity timer, 3 dalga
v8      Claude: HP bar, GameOver, GateFeedback
v9      Claude: MorphController crash fix (PrewarmModels)
v10     Claude: GPT tahribati temizlendi — GameEvents Action<> geri alindi,
               PlayerStats.CP property korundu, namespace kaldirildi,
               HandleMerge PlayerStats icine alindi, DESIGN_BIBLE v3
```
