using Unity.Netcode;
using UnityEngine;
using TMPro;

public class TankHealth : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats stats; // Merkezi depo

    [Header("Efektler")]
    [SerializeField] private GameObject explosionPrefab; // <--- YENİ: Patlama Prefabını buraya sürükleyeceksin

    private float damageCooldown = 0.1f;
    private float lastDamageTime;
    private bool isDead = false;
    private TMP_Text healthText;

    public override void OnNetworkSpawn()
    {
        // Can değiştiğinde UI'ı güncellemek için PlayerStats'taki değişkene abone ol
        stats.Health.OnValueChanged += OnHealthChanged;

        if (IsServer) isDead = false;

        if (IsOwner) InitializeHealthUI();
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && healthText != null) healthText.text = "0";
        stats.Health.OnValueChanged -= OnHealthChanged;
    }

    private void InitializeHealthUI()
    {
        GameObject textObject = GameObject.FindGameObjectWithTag("HealthText");
        if (textObject != null)
        {
            healthText = textObject.GetComponent<TMP_Text>();
            UpdateHealthUI(stats.Health.Value); // İlk değeri merkezden al
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer || isDead) return;
        if (Time.time < lastDamageTime + damageCooldown) return;

        // --- KRİTİK DÜZELTME: KALKAN KONTROLÜ ---
        if (stats.IsShielded) 
        {
            Debug.Log("Kalkan hasarı engelledi!");
            return;
        }

        lastDamageTime = Time.time;
        
        // Hasarı PlayerStats üzerinden düşür
        stats.Health.Value = Mathf.Max(stats.Health.Value - damage, 0);

        if (stats.Health.Value <= 0)
        {
            isDead = true;

            SpawnExplosionClientRpc(transform.position);

            RespawnManager.Instance.RespawnPlayer(OwnerClientId);
            Invoke(nameof(DespawnTank), 0.1f);
        }
    }

    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 position)
    {
        // 1. Konsola mesaj yazdır (Çalışıp çalışmadığını görelim)
        Debug.Log($"[PATLAMA] Efekt oluşturuluyor: {position}");

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, position, Quaternion.identity);
        }
        else
        {
            // 2. Eğer prefab yoksa hata versin
            Debug.LogError("[PATLAMA HATASI] Explosion Prefab atanmamış! Inspector'ı kontrol et.");
        }
    }

    private void DespawnTank()
    {
        if (IsSpawned) GetComponent<NetworkObject>().Despawn();
    }

    private void OnHealthChanged(int oldV, int newV) { if (IsOwner) UpdateHealthUI(newV); }

    private void UpdateHealthUI(int value)
    {
        if (healthText == null) return;
        healthText.text = value.ToString();
        healthText.color = value <= 40 ? Color.red : Color.white;
    }
}