using Unity.Netcode;
using UnityEngine;
using System.Collections; // Coroutine için gerekli
using System;

public class PlayerStats : NetworkBehaviour
{
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);
    public NetworkVariable<int> Health = new NetworkVariable<int>(100);
    
    public float SpeedBoostMultiplier { get; private set; } = 1f;  // PlayerMovement'ta hızı ayarlarken kullanılacak çarpan
    public float FireRateBoostMultiplier { get; private set; } = 1f; // ProjectileLauncher'ta ateş hızını ayarlarken kullanılacak çarpan
    public bool IsShielded { get; private set; } = false; // Kalkan durumu

    // Çalışan mevcut hız arttırıcıyı takip etmek için referans
    private Coroutine speedBoostCoroutine;
    private Coroutine fireRateCoroutine;
    private Coroutine shieldCoroutine;

    [SerializeField] private GameObject shieldVisual; // Tankın etrafındaki kalkan görseli

    public override void OnNetworkSpawn()
    {
        // ... eski UI kodların ...
        if (IsServer)
        {
            // Panel sahnede sabit olduğu için direkt bulup ekleyebiliriz
            FindFirstObjectByType<Leaderboard>()?.AddPlayerToLeaderboard(this);
        }
    }

    public void AddScore(int amount)
    {
        if (!IsServer) return; 
        Score.Value += amount;

        // HAKEME SOR: Birisi kazandı mı?
        MatchManager.Instance?.CheckWinCondition(OwnerClientId, Score.Value);
        
        Debug.Log($"[Stats] Yeni Skor: {Score.Value}");
    }

    public void DeductScoreOnDeath(int penalty)
    {
        if (!IsServer) return;
        Score.Value = Mathf.Max(0, Score.Value - penalty); // Puanın eksiye düşmesini engeller
        Debug.Log($"[Stats] Ceza uygulandı! Yeni Skor: {Score.Value}");
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (!IsServer) return; // Etki sunucu tarafında hesaplanır

        // Eğer halihazırda bir hız bonusu varsa, eskisini durdurup yenisini başlat
        if (speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);
        }

        speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    public void ApplyFireRateBoost(float multiplier, float duration)
    {
        if (!IsServer) return;
        if (fireRateCoroutine != null) StopCoroutine(fireRateCoroutine);
        fireRateCoroutine = StartCoroutine(FireRateRoutine(multiplier, duration));
    }

    public void ApplyShield(float duration)
    {
        if (!IsServer) return;
        if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);
        shieldCoroutine = StartCoroutine(ShieldRoutine(duration));
    }

    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        SpeedBoostMultiplier = multiplier;
        Debug.Log($"[Stats] Hız Arttı! Çarpan: {multiplier}");

        // Belirlenen süre kadar bekle
        yield return new WaitForSeconds(duration);

        // Süre dolunca hızı eski haline döndür
        SpeedBoostMultiplier = 1f;
        speedBoostCoroutine = null;
        Debug.Log("[Stats] Hız Etkisi Geçti.");
    }

    private IEnumerator FireRateRoutine(float multiplier, float duration)
    {
        FireRateBoostMultiplier = multiplier;
        Debug.Log($"[Stats] Ateş Hızı Arttı! Çarpan: {multiplier}");

        yield return new WaitForSeconds(duration);

        FireRateBoostMultiplier = 1f;
        fireRateCoroutine = null;
        Debug.Log("[Stats] Ateş Hızı Etkisi Geçti.");
    }

    private IEnumerator ShieldRoutine(float duration)
    {
        IsShielded = true;
        SetShieldVisualClientRpc(true); // Görseli ağda aç
        yield return new WaitForSeconds(duration);
        IsShielded = false;
        SetShieldVisualClientRpc(false); // Görseli ağda kapat
    }

    [ClientRpc]
    private void SetShieldVisualClientRpc(bool isActive) { if(shieldVisual) shieldVisual.SetActive(isActive); }
}