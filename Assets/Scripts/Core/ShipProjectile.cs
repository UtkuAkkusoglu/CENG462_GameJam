using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class ShipProjectile : NetworkBehaviour
{
    [SerializeField] private int damageAmount = 20;
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;

    private bool hasHit = false;

    public override void OnNetworkSpawn()
    {
        // Hız verme işlemini mermi doğunca yapıyoruz
        if (TryGetComponent<Rigidbody2D>(out var rb)) 
            rb.linearVelocity = transform.up * speed;

        if (IsServer) StartCoroutine(LifeTimeRoutine());
    }

    private IEnumerator LifeTimeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        if (!hasHit) DestroyProjectile();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || !IsServer) return;

        // KORUMA: Ateş eden gemiyi vurma
        if (other.gameObject.layer == LayerMask.NameToLayer("EnemyShip")) return;

        TankHealth tank = other.GetComponent<TankHealth>() ?? other.GetComponentInParent<TankHealth>();
        if (tank != null)
        {
            hasHit = true;
            // Gemi vurduğu için saldıran ID'sini 9999 (veya geçersiz bir ID) yapıyoruz
            tank.TakeDamage(damageAmount, 9999); 
            DestroyProjectile();
        }

        // 2. ENGEL (DUVAR VB.) KONTROLÜ
        // Eğer tank değilse ve bir şeye çarptıysa mermi yok olsun
        hasHit = true; 
        DestroyProjectile();
    }

    // ShipProjectile.cs içine ekle
    private void DestroyProjectile()
    {
        if (!IsServer) return;

        if (this.NetworkObject != null && this.NetworkObject.IsSpawned)
        {
            // Tüm clientlara "bu merminin görselini silin" diyoruz
            NotifyClientToDestroyDummyClientRpc(transform.position);
            this.NetworkObject.Despawn(true); 
        }
    }

    [ClientRpc]
    private void NotifyClientToDestroyDummyClientRpc(Vector3 impactPos)
    {
        // Burada mermi patlama efekti (VFX) yaratabilirsin
        // Ancak en önemlisi, merminin Client versiyonunu yok etmektir.
        // Client mermileri zaten kendi kendini 3 saniye sonra yok ediyor (Destroy(dummyProj, 3f)).
    }
}