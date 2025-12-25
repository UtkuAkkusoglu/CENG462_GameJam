using Unity.Netcode;
using UnityEngine;

public class DealDamageOnContact : NetworkBehaviour
{
    [SerializeField] private int damageAmount = 40;
    private bool hasHit = false;

    private void OnTriggerEnter2D(Collider2D otherCollider)
    {
        if (hasHit || !IsServer) return;

        // A. GEMİ KONTROLÜ
        ShipHealth shipTarget = otherCollider.GetComponent<ShipHealth>() ?? otherCollider.GetComponentInParent<ShipHealth>();
        if (shipTarget != null)
        {
            hasHit = true;
            shipTarget.TakeDamage(damageAmount, OwnerClientId);
            Destroy(gameObject);
            return;
        }

        // --- B. JEEP KONTROLÜ (YENİ EKLENDİ) --- 🚙
        // Artık bu nesneye çarpan Jeep de hasar alacak
        JeepHealth jeepTarget = otherCollider.GetComponent<JeepHealth>() ?? otherCollider.GetComponentInParent<JeepHealth>();
        if (jeepTarget != null)
        {
            hasHit = true;
            jeepTarget.TakeDamage(damageAmount, OwnerClientId);
            Destroy(gameObject);
            return;
        }
        // ---------------------------------------

        // C. TANK KONTROLÜ
        TankHealth targetHealth = otherCollider.GetComponent<TankHealth>() ?? otherCollider.GetComponentInParent<TankHealth>();
        if (targetHealth != null)
        {
            // Kendi kendini vurma koruması (Eğer mayını tank koyduysa)
            var targetNetObj = targetHealth.GetComponent<NetworkObject>();
            if (targetNetObj != null && targetNetObj.OwnerClientId == OwnerClientId) return;

            hasHit = true;
            targetHealth.TakeDamage(damageAmount, OwnerClientId);
            Destroy(gameObject);
        }
    }
}