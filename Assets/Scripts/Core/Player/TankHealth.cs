using Unity.Netcode;
using UnityEngine;
using TMPro;

public class TankHealth : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 100;

    // Hasar bekleme süresi (Invulnerability)
    private float damageCooldown = 0.1f;
    private float lastDamageTime;

    // Tankın birden fazla kez ölmesini engelleyen kilit
    private bool isDead = false;

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);
    private TMP_Text healthText;

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthChanged;

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDead = false; // Doğduğunda yaşıyor
        }

        if (IsOwner)
        {
            InitializeHealthUI();
        }
    }

    // --- GARANTİ ÇÖZÜM: TANK SİLİNİRKEN ÇALIŞIR ---
    public override void OnNetworkDespawn()
    {
        // Tank oyundan çıkarken (yok olurken) eğer bu benim tankımsa
        if (IsOwner && healthText != null)
        {
            // Son nefesinde ekrana 0 yazdır
            healthText.text = "0";
            healthText.color = Color.red;
        }

        // Event aboneliğini iptal et (Hafıza temizliği)
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void InitializeHealthUI()
    {
        // "HealthText" etiketini arıyoruz (Inspector ayarınla uyumlu)
        GameObject textObject = GameObject.FindGameObjectWithTag("HealthText");
        if (textObject != null)
        {
            healthText = textObject.GetComponent<TMP_Text>();
            UpdateHealthText(currentHealth.Value);
            healthText.gameObject.SetActive(true);
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer || isDead) return;
        if (Time.time < lastDamageTime + damageCooldown) return;

        lastDamageTime = Time.time;
        currentHealth.Value = Mathf.Max(currentHealth.Value - damage, 0);

        if (currentHealth.Value <= 0)
        {
            isDead = true;
            // KABUL KRİTERİ: Önce yeniden doğma sürecini başlatıyoruz
            RespawnManager.Instance.RespawnPlayer(OwnerClientId);
            
            // Sonra tankı yok ediyoruz
            Invoke(nameof(DespawnTank), 0.1f);
        }
    }

    private void DespawnTank()
    {
        // Eğer obje hala duruyorsa yok et
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
            Debug.Log("Tank tamamen yok edildi.");
        }
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (IsOwner)
        {
            UpdateHealthText(newValue);
        }
    }

    private void UpdateHealthText(int value)
    {
        if (healthText == null) return;

        healthText.text = value.ToString();

        if (value <= 40)
            healthText.color = Color.red;
        else
            healthText.color = Color.white;
    }
}
