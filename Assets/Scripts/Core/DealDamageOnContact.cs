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
        // 1. KİLİT KONTROLÜ
        // Eğer bu mermi zaten bir şeye hasar verdiyse, işlemi durdur
        if (hasHit) return;

        // Hasar işlemini SADECE Sunucu yapar
        if (!NetworkManager.Singleton.IsServer) return;

        // 2. TankHealth scriptini bulma
        TankHealth targetHealth = otherCollider.GetComponent<TankHealth>();
        if (targetHealth == null)
        {
            targetHealth = otherCollider.GetComponentInParent<TankHealth>();
        }

        if (targetHealth != null)
        {
            // Kendi kendimizi vurmayalım
            ulong myOwnerClientId = GetComponent<NetworkObject>().OwnerClientId;

            NetworkObject targetNetworkObject =
                otherCollider.GetComponent<NetworkObject>() ??
                otherCollider.GetComponentInParent<NetworkObject>();

            if (targetNetworkObject != null &&
                myOwnerClientId != targetNetworkObject.OwnerClientId)
            {
                // --- İŞTE BURASI KRİTİK NOKTA ---
                // Hasar vermeden hemen önce kilidi kapatıyoruz
                hasHit = true;

                targetHealth.TakeDamage(damageAmount);
                Debug.Log($"[Projectile] Enemy hit! Damage: {damageAmount}");

                Destroy(gameObject);
            }
        }
        else
        {
            // Duvar vb. çarpınca da yok olsun ama hasHit'i açmaya gerek yok
            // Çünkü duvara çift çarpması sorun yaratmaz
            Debug.Log($"[Projectile] Hit obstacle: {otherCollider.name}");
            Destroy(gameObject);
        }
    }
}
