using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Collections;

public class PlayerStats : NetworkBehaviour
{
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);
    public NetworkVariable<int> Health = new NetworkVariable<int>(100);

    // --- ARTIK BUNLAR NETWORKVARIABLE ---
    public NetworkVariable<float> SpeedBoostMultiplier = new NetworkVariable<float>(1f);
    public NetworkVariable<float> FireRateBoostMultiplier = new NetworkVariable<float>(1f);
    public NetworkVariable<bool> IsShielded = new NetworkVariable<bool>(false);

    private Coroutine speedBoostCoroutine;
    private Coroutine fireRateCoroutine;
    private Coroutine shieldCoroutine;

    [SerializeField] private GameObject shieldVisual; 

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            FindFirstObjectByType<Leaderboard>()?.AddPlayerToLeaderboard(this);
        }

        // --- CLIENT TARAFI İÇİN KALKAN GÖRSELİNİ GÜNCELLE ---
        // Kalkan değişkeni ağda değiştiğinde görseli aç/kapat
        IsShielded.OnValueChanged += (oldVal, newVal) => {
            if (shieldVisual != null) shieldVisual.SetActive(newVal);
        };
    }

    public void AddScore(int amount)
    {
        if (!IsServer) return; 
        Score.Value += amount;
        MatchManager.Instance?.CheckWinCondition(OwnerClientId, Score.Value);
    }

    public void DeductScoreOnDeath(int penalty)
    {
        if (!IsServer) return;
        Score.Value = Mathf.Max(0, Score.Value - penalty);
    }

    // --- SPEED BOOST ---
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (!IsServer) return;
        if (speedBoostCoroutine != null) StopCoroutine(speedBoostCoroutine);
        speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        SpeedBoostMultiplier.Value = multiplier; // .Value eklendi
        yield return new WaitForSeconds(duration);
        SpeedBoostMultiplier.Value = 1f;
        speedBoostCoroutine = null;
    }

    // --- FIRE RATE BOOST ---
    public void ApplyFireRateBoost(float multiplier, float duration)
    {
        if (!IsServer) return;
        if (fireRateCoroutine != null) StopCoroutine(fireRateCoroutine);
        fireRateCoroutine = StartCoroutine(FireRateRoutine(multiplier, duration));
    }

    private IEnumerator FireRateRoutine(float multiplier, float duration)
    {
        FireRateBoostMultiplier.Value = multiplier; // .Value eklendi
        yield return new WaitForSeconds(duration);
        FireRateBoostMultiplier.Value = 1f;
        fireRateCoroutine = null;
    }

    // --- SHIELD ---
    public void ApplyShield(float duration)
    {
        if (!IsServer) return;
        if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);
        shieldCoroutine = StartCoroutine(ShieldRoutine(duration));
    }

    private IEnumerator ShieldRoutine(float duration)
    {
        IsShielded.Value = true; // .Value eklendi
        yield return new WaitForSeconds(duration);
        IsShielded.Value = false;
    }
}