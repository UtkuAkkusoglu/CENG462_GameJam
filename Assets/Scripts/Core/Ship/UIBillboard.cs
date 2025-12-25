using UnityEngine;

public class UIBillboard : MonoBehaviour
{
    // LateUpdate: Tüm fizik ve hareket hesaplamalarý bittikten SONRA çalýþýr.
    // Böylece gemi ne kadar dönerse dönsün, en son sözü bu script söyler.
    private void LateUpdate()
    {
        // Rotasyonu (Dönüþü) sýfýrla = Dünya'ya göre dimdik dur.
        transform.rotation = Quaternion.identity;
    }
}