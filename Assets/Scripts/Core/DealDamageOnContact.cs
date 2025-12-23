using Unity.Netcode;
using UnityEngine;

public class DealDamageOnContact : MonoBehaviour
{
    [SerializeField] private int damageAmount = 40;

    // KORUMA: Merminin ayný anda birden fazla parçaya çarpýp 
    // çift hasar vermesini engelleyen kilit.
    private bool hasHit = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. KÝLÝT KONTROLÜ
        // Eðer bu mermi zaten bir þeye hasar verdiyse, iþlemi durdur.
        if (hasHit) return;

        // Hasar iþlemini SADECE Sunucu yapar
        if (!NetworkManager.Singleton.IsServer) return;

        // 2. TankHealth Scriptini Bulma
        TankHealth targetHealth = other.GetComponent<TankHealth>();
        if (targetHealth == null)
        {
            targetHealth = other.GetComponentInParent<TankHealth>();
        }

        if (targetHealth != null)
        {
            // Kendi kendimizi vurmayalým
            ulong myOwnerId = GetComponent<NetworkObject>().OwnerClientId;
            NetworkObject targetNetObj = other.GetComponent<NetworkObject>();
            if (targetNetObj == null) targetNetObj = other.GetComponentInParent<NetworkObject>();

            if (targetNetObj != null && myOwnerId != targetNetObj.OwnerClientId)
            {
                // --- ÝÞTE BURASI KRÝTÝK NOKTA ---
                // Hasar vermeden hemen önce kilidi kapatýyoruz.
                hasHit = true;

                targetHealth.TakeDamage(damageAmount);
                Debug.Log($"[Mermi] Düþman vuruldu! Hasar: {damageAmount}");

                Destroy(gameObject);
            }
        }
        else
        {
            // Duvar vb. çarpýnca da yok olsun ama hasHit'i açmaya gerek yok
            // çünkü duvara çift çarpmasý sorun yaratmaz.
            Debug.Log($"[Mermi] Engele çarptý: {other.name}");
            Destroy(gameObject);
        }
    }
}