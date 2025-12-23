using UnityEngine;

public class ClientProjectileVisuals : MonoBehaviour
{
    // Mermi bir şeye çarptığında (Trigger)
    private void OnTriggerEnter2D(Collider2D otherCollider)
    {
        // 1. Trigger olan görünmez alanlara (mesela suyun sınırları, spawn noktaları) çarpınca yok olmasın
        // Sadece fiziksel objelere (Tank, Duvar) çarpınca yok olsun
        if (otherCollider.isTrigger) return;

        // 2. Kendini yok et
        Destroy(gameObject);

        // İPUCU: İleride buraya patlama efekti (Particle System) eklenebilir
        // Instantiate(explosionEffect, transform.position, Quaternion.identity);
    }
}
