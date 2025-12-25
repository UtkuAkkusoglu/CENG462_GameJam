using Unity.Netcode;
using UnityEngine;

public class DealDamageOnContact : NetworkBehaviour // NetworkBehaviour yaptık ki OwnerClientId alabilelim
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
            // Sahibi (ateş eden) bilgisini gönderiyoruz
            shipTarget.TakeDamage(damageAmount, OwnerClientId); 
            Destroy(gameObject);
            return;
        }

        // B. TANK KONTROLÜ
        TankHealth targetHealth = otherCollider.GetComponent<TankHealth>() ?? otherCollider.GetComponentInParent<TankHealth>();
        if (targetHealth != null)
        {
            // Kendi kendini vurma koruması
            var targetNetObj = targetHealth.GetComponent<NetworkObject>();
            if (targetNetObj != null && targetNetObj.OwnerClientId == OwnerClientId) return;

            hasHit = true;
            // TankHealth'e de attackerId gönderiyoruz (Sende TankHealth/Health hangisiyse)
            targetHealth.TakeDamage(damageAmount, OwnerClientId);
            Destroy(gameObject);
        }
    }
}