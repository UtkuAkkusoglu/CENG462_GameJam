using Unity.Netcode;
using UnityEngine;

public class DealDamageOnContact : MonoBehaviour
{
    [SerializeField] private int damageAmount = 40;

    // KORUMA: Merminin aynı anda birden fazla parçaya çarpıp
    // çift hasar vermesini engelleyen kilit
    private bool hasHit = false;

    private void OnTriggerEnter2D(Collider2D otherCollider)
    {
        if (hasHit || !NetworkManager.Singleton.IsServer) return;

        // A. ÖNCE GEMİ KONTROLÜ
        ShipHealth shipTarget = otherCollider.GetComponent<ShipHealth>() ?? otherCollider.GetComponentInParent<ShipHealth>();
        if (shipTarget != null)
        {
            hasHit = true;
            shipTarget.TakeDamage(damageAmount);
            Destroy(gameObject);
            return;
        }

        // B. TANK KONTROLÜ
        TankHealth targetHealth = otherCollider.GetComponent<TankHealth>() ?? otherCollider.GetComponentInParent<TankHealth>();
        if (targetHealth != null)
        {
            // ... (Kendi sahibini vurmama kontrolü aynı kalsın) ...
            hasHit = true;
            targetHealth.TakeDamage(damageAmount);
            Destroy(gameObject);
        }
    }
}
