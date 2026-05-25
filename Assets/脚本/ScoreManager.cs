using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public static int score = 0;

    private int lastScore = -1;

    void Start()
    {
        ApplyScoreStyle();
        UpdateScoreText();
    }

    void Update()
    {
        UpdateScoreText();
    }

    void ApplyScoreStyle()
    {
        if (scoreText == null)
            return;

        scoreText.color = new Color(0.45f, 1f, 0.08f, 1f);
        scoreText.fontStyle = FontStyles.Bold;
    }

    void UpdateScoreText()
    {
        if (scoreText == null || score == lastScore)
            return;

        lastScore = score;
        scoreText.SetText("Score: {0}", score);
    }
}
