using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class ServerProjectile : NetworkBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private int damageAmount = 20;
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;

    private bool hasHit = false;
    private Rigidbody2D rb;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = transform.up * speed;
        if (IsServer) StartCoroutine(LifeTimeRoutine());
    }

    private IEnumerator LifeTimeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        if (!hasHit) DestroyProjectile();
    }

    // --- CASUS KODLAR BURADA ---

    // 1. İçinden Geçme Durumu (Trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;
        Debug.Log($"[CASUS MERMİ] Trigger Çarpışması Algılandı! Çarpılan: {other.gameObject.name}");
        HandleCollision(other);
    }

    // 2. Küt Diye Çarpma Durumu (Collision)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;
        Debug.Log($"[CASUS MERMİ] Fiziksel Çarpışma (Collision) Algılandı! Çarpılan: {collision.gameObject.name}");
        HandleCollision(collision.collider);
    }

    private void HandleCollision(Collider2D otherCollider)
    {
        if (hasHit) return;

        // JEEP KONTROLÜ
        JeepHealth jeep = otherCollider.GetComponent<JeepHealth>() ?? otherCollider.GetComponentInParent<JeepHealth>();
        if (jeep != null)
        {
            Debug.Log("[CASUS MERMİ] JEEP BULUNDU! Hasar Veriliyor...");
            hasHit = true;
            jeep.TakeDamage(damageAmount, OwnerClientId);
            DestroyProjectile();
            return;
        }
        else
        {
            // JeepHealth bulunamazsa bunu yazar
            if (otherCollider.name.Contains("Jeep"))
                Debug.Log("[CASUS MERMİ] DİKKAT: Jeep'e çarptım ama 'JeepHealth' scriptini bulamadım!");
        }

        // GEMİ KONTROLÜ
        ShipHealth ship = otherCollider.GetComponent<ShipHealth>() ?? otherCollider.GetComponentInParent<ShipHealth>();
        if (ship != null)
        {
            Debug.Log("[CASUS MERMİ] Gemi Vuruldu.");
            hasHit = true;
            ship.TakeDamage(damageAmount, OwnerClientId);
            DestroyProjectile();
            return;
        }

        // TANK KONTROLÜ
        TankHealth tank = otherCollider.GetComponent<TankHealth>() ?? otherCollider.GetComponentInParent<TankHealth>();
        if (tank != null)
        {
            var netObj = tank.GetComponent<NetworkObject>();
            if (netObj != null && netObj.OwnerClientId == OwnerClientId) return;

            Debug.Log("[CASUS MERMİ] Tank Vuruldu.");
            hasHit = true;
            tank.TakeDamage(damageAmount, OwnerClientId);
            DestroyProjectile();
        }
    }

    private void DestroyProjectile()
    {
        if (!IsServer) return;

        if (IsSpawned) GetComponent<NetworkObject>().Despawn(false);
        else Destroy(gameObject);
    }
}


