using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D crosshairTexture; 

    void Start()
    {
        if (crosshairTexture != null)
        {
            // Tam ortadan nişan alması için hotspot ayarı
            Vector2 hotSpot = new Vector2(crosshairTexture.width / 2, crosshairTexture.height / 2);
            Cursor.SetCursor(crosshairTexture, hotSpot, CursorMode.Auto);
        }
    }

    // Sahne değiştiğinde veya bu obje yok olduğunda imleci sıfırla
    private void OnDestroy()
    {
        // null geçmek imleci işletim sisteminin orijinal ok simgesine döndürür
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}