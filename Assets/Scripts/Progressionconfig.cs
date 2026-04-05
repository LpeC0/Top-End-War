using UnityEngine;

/// <summary>
/// Top End War — Ilerleme Konfigurasyonu v2 (Claude)
///
/// v2 degisiklikleri:
///   difficultyExponent varsayilan:    1.3 → 1.1
///   playerCPScalingFactor varsayilan: 0.9 → 0.5
///   minPowerAdjust / maxPowerAdjust eklendi (carpan siniri)
///
/// Assets > Create > TopEndWar > ProgressionConfig ile olustur.
/// DifficultyManager bu SO'yu okur.
/// </summary>
[CreateAssetMenu(fileName = "ProgressionConfig", menuName = "TopEndWar/ProgressionConfig")]
public class ProgressionConfig : ScriptableObject
{
    [Header("Zorluk Egrisi")]
    [Tooltip("Mesafeye gore zorluk artis ussu. 1.1 = yavash artar, 1.5 = agresif")]
    [Range(0.8f, 2.0f)]
    public float difficultyExponent = 1.1f;

    [Tooltip("Mesafe olcegi. Kucuk = daha erken zorlasmaya baslar")]
    public float distanceScale = 1000f;

    [Header("Oyuncu Gucu Uyumu")]
    [Tooltip(
        "Oyuncunun gucune gore zorluk ne kadar uyum saglar?\n" +
        "0.0 = hic uyum yok (saf Fixed Difficulty)\n" +
        "0.5 = orta uyum (oyuncu cok gucluyse hafif zorlar)\n" +
        "1.0 = tam uyum (kostu bandi — onerilmez)")]
    [Range(0f, 1f)]
    public float playerCPScalingFactor = 0.5f;

    [Tooltip("Guc ayari alt siniri (oyuncuyu asiri kolaylastirmaz)")]
    [Range(0.5f, 1f)]
    public float minPowerAdjust = 0.7f;

    [Tooltip("Guc ayari ust siniri (oyuncuyu asiri cezalandirmaz)")]
    [Range(1f, 2f)]
    public float maxPowerAdjust = 1.4f;

    [Header("Beklenen CP (SpawnManager kullanir)")]
    [Tooltip("Her 1000 unitede beklenen CP artisi")]
    public float expectedCPGrowthPerKm = 150f;
}