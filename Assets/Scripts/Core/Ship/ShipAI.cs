using Unity.Netcode;
using UnityEngine;

public class ShipAI : NetworkBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turningRate = 120f;
    [SerializeField] private float stopDistance = 8f;

    [Header("Saldırı Ayarları")]
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float fireRate = 2f;

    [Header("Referanslar")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject serverProjectilePrefab; // ShipProjectile scriptli prefab
    [SerializeField] private GameObject clientProjectilePrefab;

    private Transform currentTarget;
    private float lastFireTime;
    private Rigidbody2D rb;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    public override void OnNetworkSpawn()
    {
        // Gemi doğduğunda Rigidbody'yi kesin alalım
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!IsServer) return;
        FindNearestPlayer();
        if (currentTarget != null) TryShoot();
    }

    private void FixedUpdate()
    {
        if (rb == null || !IsServer || currentTarget == null) return;
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 dir = (currentTarget.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, currentTarget.position);

        // 1. DÖNME
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, targetAngle), turningRate * Time.fixedDeltaTime);

        // 2. ENGEL KONTROLÜ
        float obstacleCheckDistance = 3f;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, obstacleCheckDistance);

        bool obstacleAhead = false;
        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Island") || hit.collider.gameObject.layer == LayerMask.NameToLayer("WaterWall"))
            {
                obstacleAhead = true;
            }
        }

        // 3. HAREKET
        if (obstacleAhead) rb.linearVelocity = Vector2.zero;
        else if (distance > stopDistance) rb.linearVelocity = (Vector2)transform.up * moveSpeed;
        else rb.linearVelocity = Vector2.zero;

        rb.angularVelocity = 0f;
    }

    private void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDist = attackRange;
        currentTarget = null;
        foreach (var p in players)
        {
            float d = Vector2.Distance(transform.position, p.transform.position);
            if (d < closestDist) { closestDist = d; currentTarget = p.transform; }
        }
    }

    private void TryShoot()
    {
        Vector2 dir = currentTarget.position - transform.position;
        // Açıyı kontrol et: Tanka bakıyorsam ateş et
        if (Vector3.Angle(transform.up, dir) < 20f && Time.time >= lastFireTime + fireRate)
        {
            FireServerSide();
            lastFireTime = Time.time;
        }
    }

    private void FireServerSide()
    {
        if (serverProjectilePrefab == null || firePoint == null) return;

        // 1. Sunucu Mermisini Yarat
        GameObject serverProj = Instantiate(serverProjectilePrefab, firePoint.position, firePoint.rotation);

        // ÖNEMLİ: Merminin gemiye çarpmasını FİZİKSEL olarak engelle
        // (Layer hatası olsa bile bu kod kurtarır)
        IgnoreCollisionWithShip(serverProj.GetComponent<Collider2D>());

        // 2. Ağa Tanıt
        serverProj.GetComponent<NetworkObject>().Spawn();

        // 3. Clientlara Haber Ver
        SpawnDummyProjectileClientRpc(firePoint.position, firePoint.rotation);
    }

    [ClientRpc]
    private void SpawnDummyProjectileClientRpc(Vector3 spawnPos, Quaternion spawnRot)
    {
        // ARTIK SUNUCU DA GÖRSEL MERMİ ÜRETİYOR!
        // if (IsServer) return;  <-- BU SATIRI SİLDİK VEYA YORUMA ALDIK

        if (clientProjectilePrefab != null)
        {
            GameObject dummyProj = Instantiate(clientProjectilePrefab, spawnPos, spawnRot);

            if (dummyProj.TryGetComponent(out Rigidbody2D rb))
            {
                rb.linearVelocity = dummyProj.transform.up * 15f;
            }

            IgnoreCollisionWithShip(dummyProj.GetComponent<Collider2D>());

            Destroy(dummyProj, 3f);
        }
    }

    // BU FONKSİYON HAYAT KURTARIR: Gemi kendi mermisine çarpmaz
    private void IgnoreCollisionWithShip(Collider2D bulletCol)
    {
        if (bulletCol == null) return;
        // Geminin üzerindeki tüm colliderları bul (Gövde, triggerlar vs.)
        Collider2D[] myColliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in myColliders)
        {
            Physics2D.IgnoreCollision(col, bulletCol);
        }
    }
}