using Unity.Netcode;
using UnityEngine;
using TMPro;

public class TankHealth : NetworkBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private int maxHealth = 100;

    // Hasar bekleme süresi (Invulnerability)
    private float damageCooldown = 0.1f;
    private float lastDamageTime;

    // Tankýn birden fazla kez ölmesini engelleyen kilit
    private bool isDead = false;

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);
    private TMP_Text healthText;

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += CanDegisti;

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDead = false; // Doðduðunda yaþýyor
        }

        if (IsOwner)
        {
            InitializeHealthUI();
        }
    }

    // --- GARANTÝ ÇÖZÜM: TANK SÝLÝNÝRKEN ÇALIÞIR ---
    public override void OnNetworkDespawn()
    {
        // Tank oyundan çýkarken (yok olurken) eðer bu benim tankýmsa
        if (IsOwner && healthText != null)
        {
            // Son nefesinde ekrana 0 yazdýr
            healthText.text = "0";
            healthText.color = Color.red;
        }

        // Event aboneliðini iptal et (Hafýza temizliði)
        currentHealth.OnValueChanged -= CanDegisti;
    }

    private void InitializeHealthUI()
    {
        // "HealthTag" etiketini arýyoruz (Senin Inspector ayarýnla uyumlu)
        GameObject textObj = GameObject.FindGameObjectWithTag("HealthText");
        if (textObj != null)
        {
            healthText = textObj.GetComponent<TMP_Text>();
            UpdateText(currentHealth.Value);
            healthText.gameObject.SetActive(true);
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsServer)
        {
            // Ölü tanka vurulmaz
            if (isDead) return;

            // Bekleme süresi kontrolü
            if (Time.time < lastDamageTime + damageCooldown) return;

            lastDamageTime = Time.time;

            int yeniCan = currentHealth.Value - damage;
            if (yeniCan < 0) yeniCan = 0;

            currentHealth.Value = yeniCan;

            if (currentHealth.Value <= 0)
            {
                isDead = true; // Tanký ölü olarak iþaretle

                // --- PÜF NOKTASI BURADA ---
                // Hemen yok etme! 0.1 saniye bekle ki "0" mesajý client'a gitsin.
                Invoke(nameof(TankiYokEt), 0.1f);
            }
        }
    }

    private void TankiYokEt()
    {
        // Eðer obje hala duruyorsa yok et
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
            Debug.Log("Tank tamamen yok edildi.");
        }
    }

    private void CanDegisti(int eskiDeger, int yeniDeger)
    {
        if (IsOwner)
        {
            UpdateText(yeniDeger);
        }
    }

    private void UpdateText(int value)
    {
        if (healthText != null)
        {
            healthText.text = value.ToString();

            if (value <= 40) healthText.color = Color.red;
            else healthText.color = Color.white;
        }
    }
}