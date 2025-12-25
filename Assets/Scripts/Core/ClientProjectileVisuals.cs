using UnityEngine;

public class ClientProjectileVisuals : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Çarptığım şeyin kendisinde veya babasında TANK veya GEMİ canı var mı?
        bool hitTank = other.GetComponent<TankHealth>() != null || other.GetComponentInParent<TankHealth>() != null;
        bool hitShip = other.GetComponent<ShipHealth>() != null || other.GetComponentInParent<ShipHealth>() != null;

        // 2. Eğer geçerli bir hedefe çarptıysam
        if (hitTank || hitShip)
        {
            // Debug.Log($"Hedef vuruldu: {other.name}"); // İstersen konsola yazdır

            // Görsel mermiyi yok et
            Destroy(gameObject);
        }
    }
}