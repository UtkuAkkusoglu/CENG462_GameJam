using Unity.Netcode;
using UnityEngine;

public class ShipAI : NetworkBehaviour
{
    [Header("Hareket Ayarlar�")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turningRate = 120f;
    [SerializeField] private float stopDistance = 8f;

    [Header("Engel Alg�lama (Raycast)")]
    [SerializeField] private float obstacleCheckDistance = 2.5f; // �arpma riski mesafesi
    [SerializeField] private string obstacleTag = "Island";      // Durmas� gereken engel Tag'i

    [Header("Sald�r� Ayarlar�")]
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float projectileSpeed = 15f;

    [Header("Referanslar")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject serverProjectilePrefab;
    [SerializeField] private GameObject clientProjectilePrefab;

    private Transform currentTarget;
    private float lastFireTime;
    private Rigidbody2D rb;
    /*
    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    */
    private void Awake()
    {
        // Referansı burada almak, ağın hazır olmasını beklemez, hata riskini azaltır.
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
        // En başa rb kontrolü koyarak koruma kalkanı oluşturuyoruz
        if (rb == null) return; 

        if (!IsServer || currentTarget == null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        HandleTankMovement();
    }

    private void HandleTankMovement()
    {
        Vector2 directionToTarget = (currentTarget.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, currentTarget.position);

        // --- 1. �NCE D�NME ��LEM� (Engel olsa bile d�n!) ---
        // Bu kodu 'if'lerin d���na ald�k ki gemi her zaman d�nmeye devam etsin.
        float targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turningRate * Time.fixedDeltaTime);


        // --- 2. ENGEL KONTROL� (Fren Sistemi) ---
        // �n�me bak�yorum: Duvar var m�?
        // (Layer maskesi eklemedik, Matrix ayarlar�n do�ruysa Raycast zaten WaterWall'a �arpar)
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, obstacleCheckDistance);

        bool wallAhead = false;
        if (hit.collider != null)
        {
            // E�er �arpt���m �eyin Tag'i "Island" ise VEYA Layer'� "WaterWall" ise
            if (hit.collider.CompareTag(obstacleTag) || hit.collider.gameObject.layer == LayerMask.NameToLayer("WaterWall"))
            {
                wallAhead = true;
            }
        }

        // --- 3. �LERLEME ---
        if (wallAhead)
        {
            // �n�mde duvar var! Motorlar� durdur ama d�nmeye devam et.
            rb.linearVelocity = Vector2.zero;
        }
        else if (distance > stopDistance)
        {
            // �n�m bo� ve tank uzakta. �lerle.
            rb.linearVelocity = (Vector2)transform.up * moveSpeed;
        }
        else
        {
            // Tanka �ok yakla�t�m. Dur.
            rb.linearVelocity = Vector2.zero;
        }

        rb.angularVelocity = 0f; // Fiziksel d�nmeyi kapat (Biz kodla d�n�yoruz)
    }

    // Sahne ekran�nda k�rm�z� �izgiyi g�rmek i�in
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * obstacleCheckDistance);
    }

    // ... (Di�er fonksiyonlar ayn�: FindNearestPlayer, TryShoot, FireServerSide vs.) ...

    private void FindNearestPlayer()
    { /* Eski kodun ayn�s� */
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDist = attackRange;
        currentTarget = null;
        foreach (var player in players)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist < closestDist) { closestDist = dist; currentTarget = player.transform; }
        }
    }
    private void TryShoot()
    { /* Eski kodun ayn�s� */
        Vector2 dir = currentTarget.position - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        Quaternion idealRot = Quaternion.Euler(0, 0, angle);
        if (Quaternion.Angle(transform.rotation, idealRot) < 20f)
        {
            if (Time.time >= lastFireTime + fireRate) { FireServerSide(); lastFireTime = Time.time; }
        }
    }
    private void FireServerSide()
    { /* Eski kodun ayn�s� */
        if (serverProjectilePrefab == null || firePoint == null) return;
        GameObject serverProj = Instantiate(serverProjectilePrefab, firePoint.position, transform.rotation);
        var projectileScript = serverProj.GetComponent<ServerProjectile>();
        if (projectileScript != null) { projectileScript.canHitPlayers = true; projectileScript.canHitEnemies = false; }
        IgnoreCollisionWithShip(serverProj.GetComponent<Collider2D>());
        serverProj.GetComponent<NetworkObject>().Spawn();
        SpawnDummyProjectileClientRpc(firePoint.position, transform.rotation);
    }
    [ClientRpc]
    private void SpawnDummyProjectileClientRpc(Vector3 pos, Quaternion rot)
    {
        if (clientProjectilePrefab == null) return;
        GameObject dummyProj = Instantiate(clientProjectilePrefab, pos, rot);
        if (dummyProj.TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = dummyProj.transform.up * projectileSpeed;
        IgnoreCollisionWithShip(dummyProj.GetComponent<Collider2D>());
    }
    private void IgnoreCollisionWithShip(Collider2D bulletCol)
    {
        if (bulletCol == null) return;
        Collider2D[] myColliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in myColliders) Physics2D.IgnoreCollision(col, bulletCol);
    }
}