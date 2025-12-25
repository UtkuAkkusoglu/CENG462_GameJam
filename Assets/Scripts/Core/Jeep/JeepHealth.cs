using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class JeepHealth : NetworkBehaviour
{
    // Caný buradan deðiþtirebilirsin (80, 100, 40 fark etmez)
    public NetworkVariable<int> Health = new NetworkVariable<int>(40);

    [Header("UI Referanslarý")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private GameObject healthCanvas;

    [Header("Efektler ve Puan")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int jeepKillScore = 30;

    private bool isDead = false;
    private Vector3 offset;

    public override void OnNetworkSpawn()
    {
        Health.OnValueChanged += OnHealthChanged;

        if (healthSlider != null)
        {
            // --- DÜZELTME BURADA ---
            // Max deðeri elle 40 yazmak yerine, gerçek can neyse onu alýyoruz.
            healthSlider.maxValue = Health.Value;
            healthSlider.value = Health.Value;
        }

        // Canvas'ý Jeep'ten ayýr (Dönmemesi için)
        if (healthCanvas != null)
        {
            offset = healthCanvas.transform.position - transform.position;
            healthCanvas.transform.SetParent(null);
        }

        if (IsServer) isDead = false;
    }

    public override void OnNetworkDespawn()
    {
        Health.OnValueChanged -= OnHealthChanged;
        if (healthCanvas != null) Destroy(healthCanvas);
    }

    private void LateUpdate()
    {
        if (healthCanvas != null)
        {
            healthCanvas.transform.position = transform.position + offset;
            healthCanvas.transform.rotation = Quaternion.identity;
        }
    }

    private void OnHealthChanged(int oldV, int newV)
    {
        if (healthSlider != null) healthSlider.value = newV;
    }

    public void TakeDamage(int damage, ulong attackerId)
    {
        if (!IsServer || isDead) return;

        Health.Value = Mathf.Max(Health.Value - damage, 0);

        if (Health.Value <= 0)
        {
            isDead = true;
            Die(attackerId);
        }
    }

    private void Die(ulong attackerId)
    {
        AwardPointsToAttacker(attackerId, jeepKillScore);
        SpawnExplosionClientRpc(transform.position);

        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    private void AwardPointsToAttacker(ulong attackerId, int amount)
    {
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
    private void SpawnExplosionClientRpc(Vector3 pos)
    {
        if (explosionPrefab != null) Instantiate(explosionPrefab, pos, Quaternion.identity);
    }
}