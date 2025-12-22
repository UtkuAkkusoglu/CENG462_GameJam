using Unity.Netcode;
using Unity.Collections;
using TMPro; // TextMeshPro kullanacağız
using UnityEngine;

public class PlayerNameDisplay : NetworkBehaviour
{
    // 1. NetworkVariable oluşturma
    // WritePermission: Sadece Server yazabilir. ReadPermission: Herkes okuyabilir.
    private NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        new FixedString32Bytes(""),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private TMP_Text nameText;

    public override void OnNetworkSpawn()
    {
        // 3. UI güncelleme için OnValueChanged olayına abone ol
        playerName.OnValueChanged += HandleNameChanged;

        // Mevcut değeri hemen uygula (Yeni katılanlar için)
        HandleNameChanged("", playerName.Value);
    }

    public override void OnNetworkDespawn()
    {
        playerName.OnValueChanged -= HandleNameChanged;
    }

    private void HandleNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        nameText.text = newName.ToString();
    }

    // Bu metod sadece Server tarafından çağrılacak
    public void SetPlayerName(string name)
    {
        // Sadece sunucu NetworkVariable'ı değiştirebilir
        if (IsServer)
        {
            playerName.Value = name;
        }
    }
}