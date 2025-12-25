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

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Sadece sunucu yetkilidir
        if (hasHit || !IsServer) return;

        bool hitSomething = false;

        // 1. OYUNCU (TANK) KONTROLÜ
        if (canHitPlayers)
        {
            TankHealth tank = other.GetComponent<TankHealth>() ?? other.GetComponentInParent<TankHealth>();
            if (tank != null)
            {
                tank.TakeDamage(damageAmount);
                hitSomething = true;
            }
        }

        // 2. GEMİ (DÜŞMAN) KONTROLÜ
        // Eğer mermi gemiden çıkıyorsa 'canHitEnemies' müfettişte (Inspector) KAPALI olmalı!
        if (canHitEnemies && !hitSomething)
        {
            ShipHealth ship = other.GetComponent<ShipHealth>() ?? other.GetComponentInParent<ShipHealth>();
            if (ship != null)
            {
                ship.TakeDamage(damageAmount);
                hitSomething = true;
            }
        }

        if (hitSomething)
        {
            hasHit = true;
            StopAllCoroutines(); // Zamanlayıcıyı durdur
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