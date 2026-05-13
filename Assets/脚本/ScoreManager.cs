using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public static int score = 0;

    void Start()
    {
        ApplyScoreStyle();
    }

    void Update()
    {
        if (scoreText != null)
        {
            ApplyScoreStyle();
            scoreText.SetText("Score: {0}", score);
        }
    }

    void ApplyScoreStyle()
    {
        if (scoreText == null)
            return;

        scoreText.color = new Color(0.45f, 1f, 0.08f, 1f);
        scoreText.fontStyle = FontStyles.Bold;
    }
}
