using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Score Counter for the game.
public sealed class ScoreCounter : MonoBehaviour
{
    public static ScoreCounter Instance {get ; private set; }

    private int _score;

    public int Score {
        get => _score;

        set {
            if (_score == value) return;

            _score = value;

            scoreText.SetText($"Score: {_score}");
        }
    }

    [SerializeField] private TextMeshProUGUI scoreText;

    private void Awake() {
        Instance = this;
    }
}
