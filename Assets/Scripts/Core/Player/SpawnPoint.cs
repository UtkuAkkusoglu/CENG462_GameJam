using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public enum SpawnType { Player, Collectible }
    public SpawnType type;

    private static List<SpawnPoint> playerPoints = new List<SpawnPoint>();
    private static List<SpawnPoint> itemPoints = new List<SpawnPoint>();

    private void Awake()
    {
        // Temiz bir başlangıç için listeye eklemeden önce varsa eskisini temizleyelim
        if (type == SpawnType.Player)
        {
            if (!playerPoints.Contains(this)) playerPoints.Add(this);
        }
        else
        {
            if (!itemPoints.Contains(this)) itemPoints.Add(this);
        }
    }

    private void OnDestroy()
    {
        // Obje yok edildiğinde listeden ÇIKARMAK ÇOK KRİTİK!
        if (type == SpawnType.Player) playerPoints.Remove(this);
        else itemPoints.Remove(this);
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

    private void OnDrawGizmos()
    {
        Gizmos.color = (type == SpawnType.Player) ? Color.blue : Color.yellow;
        Gizmos.DrawSphere(transform.position, 1.0f);
    }
}