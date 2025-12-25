using TMPro;
using UnityEngine;

public class LeaderboardEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text scoreText;

    public void Display(string playerName, int score, bool isMe)
    {
        playerNameText.text = isMe ? $"{playerName} (You)" : playerName;
        scoreText.text = score.ToString();

        playerNameText.color = isMe ? Color.red : Color.black;
        scoreText.color = isMe ? Color.red : Color.black;
    }
}