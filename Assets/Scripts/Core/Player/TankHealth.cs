using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI; // <--- Slider kullanmak için bu ŞART!

public class TankHealth : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private Slider healthSlider; // <--- Slider'ı buraya sürükleyeceksin

    [Header("Efektler")]
    [SerializeField] private GameObject explosionPrefab;

    private float damageCooldown = 0.1f;
    private float lastDamageTime;
    private bool isDead = false;

    public override void OnNetworkSpawn()
    {
        if (stats != null)
        {
            // Can değişince OnHealthChanged çalışsın
            stats.Health.OnValueChanged += OnHealthChanged;

            // Oyun başladığında barı mevcut cana eşitle
            UpdateHealthUI(stats.Health.Value);
        }

        if (IsServer) isDead = false;

        // DİKKAT: Artık "InitializeHealthUI" çağırmıyoruz çünkü
        // Slider zaten prefabın içinde, aramaya gerek yok.
    }

    public override void OnNetworkDespawn()
    {
        if (stats != null)
        {
            stats.Health.OnValueChanged -= OnHealthChanged;
        }
    }

    public void TakeDamage(int damage)
    {
        // Sadece sunucu yönetir
        if (!IsServer || isDead) return;

        // Bekleme süresi ve Kalkan kontrolü
        if (Time.time < lastDamageTime + damageCooldown) return;
        if (stats.IsShielded) return;

        lastDamageTime = Time.time;

        // Canı azalt
        stats.Health.Value = Mathf.Max(stats.Health.Value - damage, 0);

        if (stats.Health.Value <= 0)
        {
            isDead = true;

            // Patlama efektini tüm oyunculara gönder
            SpawnExplosionClientRpc(transform.position);

            if (RespawnManager.Instance != null)
            {
                RespawnManager.Instance.RespawnPlayer(OwnerClientId);
            }

            Invoke(nameof(DespawnTank), 0.1f);
        }
    }

    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 position)
    {
        // Patlamayı oluştur (Hata kontrolüyle birlikte)
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Patlama Prefabı atanmamış!");
        }
    }

    private void DespawnTank()
    {
        if (IsSpawned) GetComponent<NetworkObject>().Despawn();
    }

    // Can değiştiğinde çalışır
    private void OnHealthChanged(int oldV, int newV)
    {
        // IsOwner kontrolünü kaldırdık!
        // Böylece düşmanlar da senin canının azaldığını görebilir.
        UpdateHealthUI(newV);
    }

    private void UpdateHealthUI(int value)
    {
        if (healthSlider != null)
        {
            // Sadece değeri değiştiriyoruz, renk senin Inspector'da yaptığın kırmızı kalacak.
            healthSlider.value = value;
        }
    }
}