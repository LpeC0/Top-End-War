using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// W1-01 fun loop ölçüm servisi. Runtime-only tutulur, save/asset yazmaz.
/// </summary>
public class RunDebugMetrics
{
    static RunDebugMetrics _instance;
    public static RunDebugMetrics Instance => _instance ??= new RunDebugMetrics();

    readonly List<string> _gateHistory = new List<string>();
    int _enemySpawned;
    int _enemyKilled;
    int _enemiesReachedAnchor;
    int _anchorDamageTaken;
    int _pickupCollectedCount;
    int _pickupMissedCount;
    int _coverageFullDamage;
    int _coverageWeakDamage;
    float _ttkTotal;
    int _ttkCount;
    float _lastThreatTime;
    string _lastThreatPreview = "-";

    public int EnemySpawned => _enemySpawned;
    public int EnemyKilled => _enemyKilled;
    public int EnemiesReachedAnchor => _enemiesReachedAnchor;
    public int AnchorDamageTaken => _anchorDamageTaken;
    public int PickupCollectedCount => _pickupCollectedCount;
    public int PickupMissedCount => _pickupMissedCount;
    public float AverageTTK => _ttkCount > 0 ? _ttkTotal / _ttkCount : 0f;
    public float TimeWithoutThreat => Mathf.Max(0f, Time.time - _lastThreatTime);
    public string LastThreatPreview => _lastThreatPreview;

    public void ResetForStage()
    {
        // DEĞİŞİKLİK: SGS debug metrikleri her stage başında temizlenir.
        _gateHistory.Clear();
        _enemySpawned = 0;
        _enemyKilled = 0;
        _enemiesReachedAnchor = 0;
        _anchorDamageTaken = 0;
        _pickupCollectedCount = 0;
        _pickupMissedCount = 0;
        _coverageFullDamage = 0;
        _coverageWeakDamage = 0;
        _ttkTotal = 0f;
        _ttkCount = 0;
        _lastThreatTime = Time.time;
        _lastThreatPreview = "-";
    }

    public void RecordGate(string title)
    {
        // DEĞİŞİKLİK: Seçilen gate geçmişi debug panelinde izlenir.
        if (string.IsNullOrWhiteSpace(title)) return;
        _gateHistory.Add(title);
        if (_gateHistory.Count > 6)
            _gateHistory.RemoveAt(0);
    }

    public void RecordEnemySpawn()
    {
        // DEĞİŞİKLİK: Enemy pressure ve boş bekleme teşhisi için spawn izlenir.
        _enemySpawned++;
        _lastThreatTime = Time.time;
    }

    public void RecordEnemyKilled(float ttk)
    {
        // DEĞİŞİKLİK: Ortalama TTK doğru/yanlış coverage etkisini okumaya yardım eder.
        _enemyKilled++;
        _ttkTotal += Mathf.Max(0f, ttk);
        _ttkCount++;
        _lastThreatTime = Time.time;
    }

    public void RecordEnemyReachedAnchor()
    {
        // DEĞİŞİKLİK: Yanlış pozisyonun consequence'a dönüşüp dönüşmediği ölçülür.
        _enemiesReachedAnchor++;
        _lastThreatTime = Time.time;
    }

    public void RecordAnchorDamage(int amount)
    {
        // DEĞİŞİKLİK: AnchorCore hasarı W1-01 başarı kriteri olarak izlenir.
        _anchorDamageTaken += Mathf.Max(0, amount);
    }

    public void RecordPickupCollected()
    {
        // DEĞİŞİKLİK: Pickup kararı bedava bonus mu riskli fırsat mı ölçülür.
        _pickupCollectedCount++;
    }

    public void RecordPickupMissed()
    {
        // DEĞİŞİKLİK: Kaçırılan pickup debug'a yansıtılır.
        _pickupMissedCount++;
    }

    public void RecordCoverageDamage(int damage, float multiplier)
    {
        // DEĞİŞİKLİK: Coverage hasarı full/weak ayrımıyla ölçülür.
        int safeDamage = Mathf.Max(0, damage);
        if (multiplier >= 0.90f)
            _coverageFullDamage += safeDamage;
        else if (multiplier < 0.45f)
            _coverageWeakDamage += safeDamage;
    }

    public void RecordThreatPreview(string preview)
    {
        // DEĞİŞİKLİK: Anchor giriş özeti oyuncunun en son gördüğü tehdidi gösterebilir.
        if (!string.IsNullOrWhiteSpace(preview))
            _lastThreatPreview = preview;
    }

    public string GetGateHistoryText()
    {
        return _gateHistory.Count == 0 ? "-" : string.Join(" > ", _gateHistory);
    }

    public string BuildDebugBlock()
    {
        // DEĞİŞİKLİK: HUD için tek noktadan kısa, okunabilir SGS debug özeti üretilir.
        var sb = new StringBuilder(256);
        int alive = Mathf.Max(0, _enemySpawned - _enemyKilled);
        float pressure = ThreatManager.Instance != null ? ThreatManager.Instance.ThreatScore : 0f;

        sb.AppendLine($"Enemies: {alive} alive | K:{_enemyKilled} | Core:{_enemiesReachedAnchor}");
        sb.AppendLine($"AnchorDmg: {_anchorDamageTaken} | AvgTTK: {AverageTTK:0.0}s");
        sb.AppendLine($"Pressure: {pressure:0.0} | Empty: {TimeWithoutThreat:0.0}s");
        sb.AppendLine($"Coverage dmg F/W: {_coverageFullDamage}/{_coverageWeakDamage}");
        sb.AppendLine($"Pickup C/M: {_pickupCollectedCount}/{_pickupMissedCount}");
        sb.Append($"Gates: {GetGateHistoryText()}");
        return sb.ToString();
    }
}
