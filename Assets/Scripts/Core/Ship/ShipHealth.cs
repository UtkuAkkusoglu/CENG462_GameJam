using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ShipHealth : NetworkBehaviour
{
    public NetworkVariable<int> Health = new NetworkVariable<int>(60);

    [Header("UI Referansları")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private GameObject healthCanvas; // Dönmemesi gereken Canvas

    [Header("Efektler")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int shipKillScore = 50;

    private int maxHealth = 60;
    private bool isDead = false;

    // Can barının geminin neresinde duracağını tutan ayar
    private Vector3 offset;

    public override void OnNetworkSpawn()
    {
        Health.OnValueChanged += OnHealthChanged;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            UpdateHealthUI(Health.Value);
        }

        // --- KESİN ÇÖZÜM BURADA ---
        if (healthCanvas != null)
        {
            // 1. Ofseti hesapla (Şu anki farkı kaydet)
            offset = healthCanvas.transform.position - transform.position;

            // 2. EVLATLIKTAN REDDET: Canvas'ı geminin içinden çıkar.
            // Artık Canvas özgür, gemi dönse de o dönmez.
            healthCanvas.transform.SetParent(null);
        }

        if (IsServer) isDead = false;
    }

    public override void OnNetworkDespawn()
    {
        Health.OnValueChanged -= OnHealthChanged;

        // Gemi yok olunca ortada sahipsiz bar kalmasın, onu da sil.
        if (healthCanvas != null)
        {
            Destroy(healthCanvas);
        }
    }

    // --- TAKİP SİSTEMİ ---
    private void LateUpdate()
    {
        // Eğer Canvas hala varsa (yok olmadıysa)
        if (healthCanvas != null)
        {
            // 1. Pozisyon: Gemiyi takip et + ofseti ekle
            healthCanvas.transform.position = transform.position + offset;

            // 2. Rotasyon: DÜNYAYA GÖRE DİMDİK DUR (Asla dönme)
            healthCanvas.transform.rotation = Quaternion.identity;
        }
    }

    private void OnHealthChanged(int oldV, int newV) => UpdateHealthUI(newV);

    private void UpdateHealthUI(int value)
    {
        if (healthSlider != null) healthSlider.value = value;
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
        AwardPointsToAttacker(attackerId, shipKillScore);
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