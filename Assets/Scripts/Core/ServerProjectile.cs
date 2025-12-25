using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class ServerProjectile : NetworkBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private int damageAmount = 20;
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;

    [Header("Hedef Filtresi")]
    [Tooltip("Gemi mermisi ise bunu true yap")]
    public bool canHitPlayers = true;
    [Tooltip("Gemi mermisi ise bunu false yap ki gemi kendini vurmasın")]
    public bool canHitEnemies = true;

    private bool hasHit = false;
    private Rigidbody2D rb;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = transform.up * speed;
        }

        if (IsServer)
        {
            // Invoke bazen sapıtabilir, Coroutine her zaman daha sağlamdır
            StartCoroutine(LifeTimeRoutine());
        }
    }

    private IEnumerator LifeTimeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        if (!hasHit) DestroyProjectile();
    }

    private void OnTriggerEnter2D(Collider2D otherCollider)
    {
        if (hasHit || !IsServer) return;

        // 1. GEMİ VURMA KONTROLÜ (Hata CS7036'nın çözümü)
        ShipHealth shipTarget = otherCollider.GetComponent<ShipHealth>() ?? otherCollider.GetComponentInParent<ShipHealth>();
        if (shipTarget != null)
        {
            hasHit = true;
            // HATA BURADAYDI: Artık merminin sahibi olan 'OwnerClientId'yi gönderiyoruz
            shipTarget.TakeDamage(damageAmount, OwnerClientId); 
            DestroyProjectile();
            return;
        }

        // 2. TANK VURMA KONTROLÜ (Hata CS7036'nın çözümü)
        TankHealth targetHealth = otherCollider.GetComponent<TankHealth>() ?? otherCollider.GetComponentInParent<TankHealth>();
        if (targetHealth != null)
        {
            // Kendi kendini vurma koruması
            var targetNetObj = targetHealth.GetComponent<NetworkObject>();
            if (targetNetObj != null && targetNetObj.OwnerClientId == OwnerClientId) return;

            hasHit = true;
            // HATA BURADAYDI: Buraya da OwnerClientId ekledik
            targetHealth.TakeDamage(damageAmount, OwnerClientId);
            DestroyProjectile();
        }
    }

    private void DestroyProjectile()
    {
        if (!IsServer) return;

        if (IsSpawned)
        {
            // true: Objeyi ağdan siler ve tamamen yok eder
            GetComponent<NetworkObject>().Despawn(false);
        }
        else
        {
            // Ağda spawn edilmemiş mermiler için normal yok etme
            Destroy(gameObject);
        }
    }
}