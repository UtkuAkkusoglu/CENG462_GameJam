using TMPro;
using UnityEngine;

public class LeaderboardEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text scoreText;

    public void Display(string playerName, int score, bool isMe)
    {
        playerNameText.text = isMe ? $"{playerName}" : playerName;
        scoreText.text = score.ToString();

        playerNameText.color = isMe ? Color.yellow : Color.black;
        scoreText.color = isMe ? Color.yellow : Color.black;
    }
}