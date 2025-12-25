using Unity.Netcode;
using UnityEngine;

public class ShipAI : NetworkBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turningRate = 120f;
    [SerializeField] private float stopDistance = 8f;

    [Header("Akıllı Radar")]
    [SerializeField] private float obstacleCheckDistance = 4f;
    [SerializeField] private int rayCount = 7; // Sadece ön tarafı tarayacağız (7 ışın yeterli)
    [SerializeField] private float fovAngle = 180f; // Sadece önündeki 180 dereceye baksın
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Saldırı Ayarları")]
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float fireRate = 2f;

    [Header("Referanslar")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject serverProjectilePrefab;
    [SerializeField] private GameObject clientProjectilePrefab;

    private Transform currentTarget;
    private float lastFireTime;
    private Rigidbody2D rb;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();
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

        // 1. DÖNME (HER ZAMAN AKTİF!)
        // Gemi duvara çarpıp dursa bile dönmeye devam etmeli ki kurtulabilsin.
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, targetAngle), turningRate * Time.fixedDeltaTime);

        // 2. RADAR KONTROLÜ
        bool wallAhead = CheckFrontalObstacles();

        // 3. HAREKET KARARI
        if (wallAhead)
        {
            // Duvar var! Sadece gazı kes, ama dönmeyi engelleme.
            rb.linearVelocity = Vector2.zero;
        }
        else if (distance > stopDistance)
        {
            // Yol temiz, ilerle
            rb.linearVelocity = (Vector2)transform.up * moveSpeed;
        }
        else
        {
            // Tanka yaklaştım, dur
            rb.linearVelocity = Vector2.zero;
        }

        // Dönme fiziğini kapat (Kodla dönüyoruz)
        rb.angularVelocity = 0f;
    }

    // --- ÖN TARAFI TARAYAN FONKSİYON ---
    private bool CheckFrontalObstacles()
    {
        float angleStep = fovAngle / (rayCount - 1);
        float startAngle = -fovAngle / 2f; // En sol açıdan başla

        for (int i = 0; i < rayCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);

            // Geminin dönüşüne (rotation) göre açıyı hesapla
            Vector3 direction = Quaternion.Euler(0, 0, currentAngle) * transform.up;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, obstacleCheckDistance, obstacleLayers);

            if (hit.collider != null)
            {
                // Tank veya Mermi değilse duvardır
                if (hit.collider.GetComponent<TankHealth>() == null && hit.collider.GetComponent<ServerProjectile>() == null)
                {
                    return true; // Önümde engel var!
                }
            }
        }
        return false;
    }

    // --- FAIL-SAFE: EĞER FİZİKSEL OLARAK ÇARPARSA (Raycast kaçırırsa) ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        // Eğer çarptığım şey bir "Duvar" katmanıysa
        if (((1 << collision.gameObject.layer) & obstacleLayers) != 0)
        {
            // Gemiyi çarptığı yönün tersine sertçe it (Geri tepme)
            Vector2 pushDir = (transform.position - collision.transform.position).normalized;
            rb.AddForce(pushDir * 5f, ForceMode2D.Impulse);
        }
    }

    // GÖRSEL (Yelpaze şeklinde radar)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        float angleStep = fovAngle / (rayCount - 1);
        float startAngle = -fovAngle / 2f;

        for (int i = 0; i < rayCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Vector3 direction = Quaternion.Euler(0, 0, currentAngle) * transform.up;
            Gizmos.DrawRay(transform.position, direction * obstacleCheckDistance);
        }
    }

    // ... (FindNearestPlayer, TryShoot, FireServerSide vb. AYNI - Kopyalamana gerek yok, eskisi kalsın) ...
    private void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDist = attackRange; currentTarget = null;
        foreach (var p in players)
        {
            float d = Vector2.Distance(transform.position, p.transform.position);
            if (d < closestDist) { closestDist = d; currentTarget = p.transform; }
        }
    }
    private void TryShoot()
    {
        Vector2 dir = currentTarget.position - transform.position;
        if (Vector3.Angle(transform.up, dir) < 20f && Time.time >= lastFireTime + fireRate)
        {
            FireServerSide(); lastFireTime = Time.time;
        }
    }
    private void FireServerSide()
    {
        if (serverProjectilePrefab == null || firePoint == null) return;
        GameObject serverProj = Instantiate(serverProjectilePrefab, firePoint.position, firePoint.rotation);
        IgnoreCollisionWithShip(serverProj.GetComponent<Collider2D>());
        serverProj.GetComponent<NetworkObject>().Spawn();
        SpawnDummyProjectileClientRpc(firePoint.position, firePoint.rotation);
    }
    [ClientRpc]
    private void SpawnDummyProjectileClientRpc(Vector3 pos, Quaternion rot)
    {
        if (clientProjectilePrefab != null)
        {
            GameObject dummyProj = Instantiate(clientProjectilePrefab, pos, rot);
            if (dummyProj.TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = dummyProj.transform.up * 15f;
            IgnoreCollisionWithShip(dummyProj.GetComponent<Collider2D>());
            Destroy(dummyProj, 3f);
        }
    }
    private void IgnoreCollisionWithShip(Collider2D bulletCol)
    {
        if (bulletCol == null) return;
        Collider2D[] myColliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in myColliders) Physics2D.IgnoreCollision(col, bulletCol);
    }

    // --- ÇARPIŞAN ARABA MODU (EDGE COLLIDER İÇİN) ---
    // Gemi duvara (Edge Collider) değdiği sürece çalışır
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsServer) return;

        // Çarptığım şey benim "Engel" listemde var mı? (Layer kontrolü)
        // (1 << layer) işlemi bit-mask kontrolüdür.
        if (((1 << collision.gameObject.layer) & obstacleLayers) != 0)
        {
            // 1. Temas noktasını bul
            Vector2 contactPoint = collision.contacts[0].point;

            // 2. Gemi merkezinden temas noktasına ters yöne bir vektör çiz (Geri İtme Yönü)
            Vector2 pushDirection = ((Vector2)transform.position - contactPoint).normalized;

            // 3. Gemiyi o yöne doğru sertçe it (Fiziksel Darbe)
            // ForceMode2D.Impulse = Ani darbe
            // 5f gücünde itiyoruz ki duvara yapışmasın
            rb.AddForce(pushDirection * 5f, ForceMode2D.Force);

            // 4. Gazı Kes (Patinaj çekmesin)
            // Eğer duvara sürtünüyorsa motoru yavaşlat
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f);
        }
    }
}