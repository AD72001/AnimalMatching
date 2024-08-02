using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Goal of a stage.
public sealed class Goal : MonoBehaviour
{
    public static Goal Instance {get ; private set; }

    public int goal;

    public int GoalScore {
        get => goal;

        set {
            if (goal == value) return;

            goal = value;

            goalText.SetText($"Goal: {goal}");
        }
    }

    [SerializeField] private TextMeshProUGUI goalText;

    private void Awake() {
        Instance = this;
    }
}
