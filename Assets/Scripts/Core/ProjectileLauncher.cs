using UnityEngine;
using Unity.Netcode;

public class ProjectileLauncher : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] GameObject serverProjectilePrefab;
    [SerializeField] GameObject clientProjectilePrefab;
    [SerializeField] Transform projectileSpawnPoint;  // where the projectile appears (tip of barrel)
    [SerializeField] InputReader inputReader; // for reading the fire input event
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private Collider2D playerCollider; // to ignore self-collisions

    [Header("Settings")]
    [SerializeField] float projectileSpeed;
    [SerializeField] private float fireRate; // saniyedeki atış sayısı
    [SerializeField] private float muzzleFlashDuration; // flash'ın görünür kalma süresi (saniye)

    private bool shouldFire;
    private float previousFireTime;
    private float muzzleFlashTimer;

    private void Update()
    {
        if (muzzleFlashTimer > 0)     // herhalde dummy projectile aksine server onayı beklemez, herkes aynı anda görür
        {
            muzzleFlashTimer -= Time.deltaTime;
        }
        else
        {
            muzzleFlash.SetActive(false);
        }
        
        if (!IsOwner) return;
        if (!shouldFire) return;

        float timeBetweenShots = 1f / fireRate;
        if (Time.time < previousFireTime + timeBetweenShots)
            return; // yeterli süre geçmedi, henüz ateş edemez
        
        Vector3 spawnPos = projectileSpawnPoint.position;
        Vector3 direction = projectileSpawnPoint.up;

        SpawnDummyProjectile(spawnPos, direction);
        PrimaryFireServerRpc(spawnPos, direction);
        previousFireTime = Time.time;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        inputReader.PrimaryFireEvent += HandlePrimaryFire;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        inputReader.PrimaryFireEvent -= HandlePrimaryFire;
    }

    private void HandlePrimaryFire(bool isPressed)
    {
        shouldFire = isPressed;
    }

    private void SpawnDummyProjectile(Vector3 spawnPos, Vector3 direction)
    {
        GameObject projectileInstance = Instantiate(clientProjectilePrefab, spawnPos, Quaternion.identity);
        projectileInstance.transform.up = direction;
        Physics2D.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider2D>()); // Unity’de collision detection (çarpışma kontrolü) bir frame öncesinde veya FixedUpdate sırasında olur. Yani aynı frame içinde Instantiate → IgnoreCollision yazarsan çoğu durumda merminin sahneye gelmesi ve çarpışmayı hesaplamadan önce IgnoreCollision çalışır.
        if (projectileInstance.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = projectileInstance.transform.up * projectileSpeed;
        }
        muzzleFlash.SetActive(true);
        muzzleFlashTimer = muzzleFlashDuration;
    }
    
    // Owner client çağırıyor, ama metodun gövdesi sunucuda çalışıyor çünkü [ServerRpc] o yüzden içindeki metot tüm clientlar için çağırılır.
    [ServerRpc]
    private void PrimaryFireServerRpc(Vector3 spawnPos, Vector3 direction)
    {
        GameObject projectileInstance = Instantiate(serverProjectilePrefab, spawnPos, Quaternion.identity);
        projectileInstance.transform.up = direction;
        Physics2D.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider2D>());
        if (projectileInstance.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = projectileInstance.transform.up * projectileSpeed;
        }

        // Diğer client’lara dummy projectile spawnlamalarını bildir
        SpawnDummyProjectileClientRpc(spawnPos, direction);
        // Sunucu, client’in gönderdiği spawnPos ve direction’a güvenir.
        // Bu, mermiler için client-authoritative movement (client’in hareketi belirlediği) biçiminde bir yaklaşımdır.
    }
    
    // Bu attribute([ClientRpc]), methodun sunucudan tüm client’lara çağrılacağını belirtiyor.
    [ClientRpc]
    private void SpawnDummyProjectileClientRpc(Vector3 spawnPos, Vector3 direction)
    {
        if (IsOwner)
        {
            return;  // Ateşleyen client zaten kendi dummy’sini spawnladı.
        }
        
        SpawnDummyProjectile(spawnPos, direction);
    }
}
