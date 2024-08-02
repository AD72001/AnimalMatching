using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Move Counter for the game.
public sealed class MoveCounter : MonoBehaviour
{
    public static MoveCounter Instance { get; private set; }

    public int move;

    public int Move {
        get => move;

        set {
            if (move == value) return;
            move = value;

            moveText.SetText($"Move: {move}");
        }
    }

    [SerializeField] private TextMeshProUGUI moveText;

    private void Awake() {
        Instance = this;
    }
}
