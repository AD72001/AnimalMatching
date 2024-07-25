using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class MoveCounter : MonoBehaviour
{
    public static MoveCounter Instance { get; private set; }

    private int _move;

    public int Move {
        get => _move;

        set {
            if (_move == value) return;
            _move = value;

            moveText.SetText($"Move: {_move}");
        }
    }

    [SerializeField] private TextMeshProUGUI moveText;

    private void Awake() {
        Instance = this;
    }
}
