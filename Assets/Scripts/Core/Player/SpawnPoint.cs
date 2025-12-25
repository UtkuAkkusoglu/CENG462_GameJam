using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public enum SpawnType { Player, Collectible, Ship, Jeep }
    public SpawnType type;

    private static List<SpawnPoint> playerPoints = new List<SpawnPoint>();
    private static List<SpawnPoint> itemPoints = new List<SpawnPoint>();
    private static List<SpawnPoint> shipPoints = new List<SpawnPoint>();
    private static List<SpawnPoint> jeepPoints = new List<SpawnPoint>(); 

    private void Awake()
    {
        if (type == SpawnType.Player) { if (!playerPoints.Contains(this)) playerPoints.Add(this); }
        else if (type == SpawnType.Collectible) { if (!itemPoints.Contains(this)) itemPoints.Add(this); }
        else if (type == SpawnType.Ship) { if (!shipPoints.Contains(this)) shipPoints.Add(this); }
        else if (type == SpawnType.Jeep) { if (!jeepPoints.Contains(this)) jeepPoints.Add(this); }
    }

    private void OnDestroy()
    {
        if (type == SpawnType.Player) playerPoints.Remove(this);
        else if (type == SpawnType.Collectible) itemPoints.Remove(this);
        else if (type == SpawnType.Ship) shipPoints.Remove(this);
        else if (type == SpawnType.Jeep) jeepPoints.Remove(this);
    }

    public static Vector3 GetRandomPlayerPos()
    {
        // Önce listedeki "yok edilmiş" (null) objeleri temizle
        playerPoints.RemoveAll(p => p == null);

        if (playerPoints.Count == 0) return Vector3.zero;

        int randomIndex = Random.Range(0, playerPoints.Count);
        return playerPoints[randomIndex].transform.position;
    }

    public static Vector3 GetRandomItemPos()
    {
        // Önce listedeki "yok edilmiş" (null) objeleri temizle
        itemPoints.RemoveAll(p => p == null);

        if (itemPoints.Count == 0) return Vector3.zero;

        int randomIndex = Random.Range(0, itemPoints.Count);
        return itemPoints[randomIndex].transform.position;
    }

    public static Vector3 GetAvailableItemPos()
    {
        itemPoints.RemoveAll(p => p == null);
        
        // Dolu olmayan (boş) noktaları bulalım
        List<SpawnPoint> availablePoints = new List<SpawnPoint>();
        
        foreach (var point in itemPoints)
        {
            // Noktanın etrafında 1 birimlik alanda başka bir "Collectible" var mı bak
            // Not: Collectible objelerinin Layer'ını "Collectible" yaparsan çok daha performanslı olur
            Collider2D hit = Physics2D.OverlapCircle(point.transform.position, 1.0f);
            
            if (hit == null) // Eğer hiçbir şeye çarpmadıysa burası boştur
            {
                availablePoints.Add(point);
            }
        }

        if (availablePoints.Count == 0) return Vector3.zero;

        int randomIndex = Random.Range(0, availablePoints.Count);
        return availablePoints[randomIndex].transform.position;
    }

    // Gemi için boş nokta bulan metod
    public static Vector3 GetAvailableShipPos()
    {
        shipPoints.RemoveAll(p => p == null);
        List<SpawnPoint> availablePoints = new List<SpawnPoint>();
        
        foreach (var point in shipPoints)
        {
            // Gemiler büyük olduğu için 3 birimlik alana bakıyoruz
            Collider2D hit = Physics2D.OverlapCircle(point.transform.position, 3.0f);
            if (hit == null) availablePoints.Add(point);
        }

        if (availablePoints.Count == 0) return Vector3.zero;
        return availablePoints[Random.Range(0, availablePoints.Count)].transform.position;
    }

    // --- Jeep için boş nokta bulan metod ---
    public static Vector3 GetAvailableJeepPos()
    {
        jeepPoints.RemoveAll(p => p == null);
        List<SpawnPoint> availablePoints = new List<SpawnPoint>();
        
        foreach (var point in jeepPoints)
        {
            // Jeepler için 2-3 birimlik alan genellikle yeterlidir
            // LayerMask ekleyerek sadece engellere çarparsa 'dolu' say diyebilirsin
            Collider2D hit = Physics2D.OverlapCircle(point.transform.position, 2.5f);
            if (hit == null) availablePoints.Add(point);
        }

        if (availablePoints.Count == 0) return Vector3.zero;
        return availablePoints[Random.Range(0, availablePoints.Count)].transform.position;
    }

    private void OnDrawGizmos()
    {
        if (type == SpawnType.Player) Gizmos.color = Color.blue;
        else if (type == SpawnType.Collectible) Gizmos.color = Color.yellow;
        else if (type == SpawnType.Ship) Gizmos.color = Color.red;
        else if (type == SpawnType.Jeep) Gizmos.color = Color.green;

        Gizmos.DrawSphere(transform.position, 1.0f);
    }
}