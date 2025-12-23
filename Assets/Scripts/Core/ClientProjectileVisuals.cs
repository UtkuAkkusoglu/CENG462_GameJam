using UnityEngine;

public class ClientProjectileVisuals : MonoBehaviour
{
    // Mermi bir þeye çarptýðýnda (Trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Trigger olan görünmez alanlara (mesela suyun sýnýrlarý, spawn noktalarý) çarpýnca yok olmasýn.
        // Sadece fiziksel objelere (Tank, Duvar) çarpýnca yok olsun.
        if (other.isTrigger) return;

        // 2. Kendini Yok Et
        Destroy(gameObject);

        // ÝPUCU: Ýleride buraya patlama efekti (Particle System) de ekleyebiliriz.
        // Instantiate(patlamaEfekti, transform.position, ...);
    }
}