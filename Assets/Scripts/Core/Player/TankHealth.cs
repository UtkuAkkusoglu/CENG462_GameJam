using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TankHealth : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private Slider healthSlider;

    [Header("Efektler")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int playerKillScore = 200; // Öldürenin alacağı puan

    private float damageCooldown = 0.1f;
    private float lastDamageTime;
    private bool isDead = false;

    public override void OnNetworkSpawn()
    {
        if (stats != null)
        {
            stats.Health.OnValueChanged += OnHealthChanged;
            UpdateHealthUI(stats.Health.Value);
        }
        if (IsServer) isDead = false;
    }

    public override void OnNetworkDespawn()
    {
        if (stats != null) stats.Health.OnValueChanged -= OnHealthChanged;
    }

    // GÜNCELLEDİK: Artık attackerId alıyor
    public void TakeDamage(int damage, ulong attackerId) 
    {
        if (!IsServer || isDead) return;
        if (Time.time < lastDamageTime + damageCooldown) return;
        if (stats.IsShielded.Value) return;

        lastDamageTime = Time.time;
        stats.Health.Value = Mathf.Max(stats.Health.Value - damage, 0);

        if (stats.Health.Value <= 0)
        {
            isDead = true;
            
            // PATLATAN KİŞİYE PUAN VER
            AwardPointsToAttacker(attackerId, playerKillScore);

            SpawnExplosionClientRpc(transform.position);

            if (RespawnManager.Instance != null)
                RespawnManager.Instance.RespawnPlayer(OwnerClientId);

            Invoke(nameof(DespawnTank), 0.1f);
        }
    }

    private void AwardPointsToAttacker(ulong attackerId, int amount)
    {
        // Kendi kendini vurduysa puan verme
        if (attackerId == OwnerClientId) return;

        foreach (var player in FindObjectsByType<PlayerStats>(FindObjectsSortMode.None))
        {
            if (player.OwnerClientId == attackerId)
            {
                player.AddScore(amount);
                break;
            }
        }
    }

    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 position)
    {
        if (explosionPrefab != null) Instantiate(explosionPrefab, position, Quaternion.identity);
    }

    private void DespawnTank()
    {
        if (IsSpawned) GetComponent<NetworkObject>().Despawn();
    }

    private void OnHealthChanged(int oldV, int newV) => UpdateHealthUI(newV);

    private void UpdateHealthUI(int value)
    {
        if (healthSlider != null) healthSlider.value = value;
    }
}