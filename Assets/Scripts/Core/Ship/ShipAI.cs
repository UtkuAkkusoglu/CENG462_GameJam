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

        // 1. DÖNME (Her zaman hedefe dönmeye çalış)
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, targetAngle), turningRate * Time.fixedDeltaTime);

        // 2. ENGEL KONTROLÜ (Raycast)
        // Önümde ada (Island) veya su duvarı (WaterWall) var mı?
        float obstacleCheckDistance = 3f; 
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, obstacleCheckDistance);
        
        bool obstacleAhead = false;
        if (hit.collider != null)
        {
            // Tag veya Layer kontrolü yapıyoruz
            if (hit.collider.CompareTag("Island") || hit.collider.gameObject.layer == LayerMask.NameToLayer("WaterWall"))
            {
                obstacleAhead = true;
            }
        }

        // 3. HAREKET KARARI
        if (obstacleAhead)
        {
            rb.linearVelocity = Vector2.zero; // Karaya çıkma, dur!
        }
        else if (distance > stopDistance)
        {
            rb.linearVelocity = (Vector2)transform.up * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

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
        // Ateş etme açısını kontrol et (Tank önündeyse ateş et)
        Vector2 dir = currentTarget.position - transform.position;
        if (Vector3.Angle(transform.up, dir) < 20f && Time.time >= lastFireTime + fireRate)
        {
            FireServerSide();
            lastFireTime = Time.time;
        }
    }

    private void FireServerSide()
    {
        if (serverProjectilePrefab == null || firePoint == null) return;
        
        Vector3 pos = firePoint.position;
        Vector3 dir = firePoint.up;

        // 1. Sunucu mermisini yarat ve hız ver
        GameObject serverProj = Instantiate(serverProjectilePrefab, pos, firePoint.rotation);
        if (serverProj.TryGetComponent<Rigidbody2D>(out var rb))
        {
            // Tanktaki gibi hızı buradan veriyoruz
            rb.linearVelocity = dir * 15f; // Buradaki 15f, ShipProjectile içindeki speed ile aynı olmalı
        }
        
        // 2. Ağa tanıt (Spawn)
        serverProj.GetComponent<NetworkObject>().Spawn();
        
        // 3. TÜM clientlara dummy mermi oluşturmasını söyle
        // Tanktan farkı: Burada 'IsOwner' kontrolü olmadığı için herkes oluşturmalı.
        SpawnDummyProjectileClientRpc(pos, dir);
    }

    [ClientRpc]
    private void SpawnDummyProjectileClientRpc(Vector3 spawnPos, Vector3 direction)
    {
        // Sunucu zaten mermiyi teknik olarak görüyor, görseli sadece clientlara çizelim
        if (IsServer) return; 

        if (clientProjectilePrefab != null)
        {
            // 1. Oluştur
            GameObject dummyProj = Instantiate(clientProjectilePrefab, spawnPos, Quaternion.identity);
            
            // 2. Yönünü ayarla (Up vektörünü direction yap)
            dummyProj.transform.up = direction;

            // 3. Hız ver (Tank kodundaki gibi Rigidbody kontrolü ile)
            if (dummyProj.TryGetComponent<Rigidbody2D>(out var rb))
            {
                // Buradaki hız, sunucu mermisiyle senkronize olmalı (15f demiştik)
                rb.linearVelocity = direction * 15f; 
            }
            
            // 4. Mermiyi temizle (Ağ nesnesi olmadığı için Destroy yeterli)
            Destroy(dummyProj, 3f);
        }
    }
}