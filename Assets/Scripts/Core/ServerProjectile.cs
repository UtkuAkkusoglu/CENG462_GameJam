using Unity.Netcode;
using UnityEngine;

public class ServerProjectile : NetworkBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private int damageAmount = 20;
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;

    [Header("Hedef Filtresi")]
    public bool canHitPlayers = true;
    public bool canHitEnemies = true;

    // Çift hasarý önlemek için kilit (Eski koddan aldýk)
    private bool hasHit = false;
    private Rigidbody2D rb;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = transform.up * speed;
        }

        if (IsServer)
        {
            Invoke(nameof(DestroyProjectile), lifeTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Zaten vurduysa veya Sunucu deðilse çýk
        if (hasHit || !IsServer) return;

        // --- TANK KONTROLÜ (Geliþtirilmiþ) ---
        if (canHitPlayers)
        {
            // Önce çarptýðým parçada ara, yoksa babasýnda (Parent) ara
            TankHealth tank = other.GetComponent<TankHealth>();
            if (tank == null) tank = other.GetComponentInParent<TankHealth>();

            if (tank != null)
            {
                hasHit = true; // Kilidi kapat
                tank.TakeDamage(damageAmount);
                DestroyProjectile();
                return;
            }
        }

        // --- GEMÝ KONTROLÜ (Geliþtirilmiþ) ---
        if (canHitEnemies)
        {
            ShipHealth ship = other.GetComponent<ShipHealth>();
            if (ship == null) ship = other.GetComponentInParent<ShipHealth>();

            if (ship != null)
            {
                hasHit = true; // Kilidi kapat
                ship.TakeDamage(damageAmount);
                DestroyProjectile();
                return;
            }
        }

        // --- DUVAR KONTROLÜ ---
        // Burada bir þey yapmýyoruz. 
        // Mermi tanka veya gemiye çarpmadýysa yoluna devam eder.
        // (Su duvarýnýn veya adanýn içinden geçer)
    }

    private void DestroyProjectile()
    {
        if (IsSpawned) GetComponent<NetworkObject>().Despawn();
    }
}