using UnityEngine;
using UnityEngine.UI;

public class ExitButton : MonoBehaviour
{
    public Button exitButton;
    void Start()
    {
        Button btn = exitButton.GetComponent<Button>();
		btn.onClick.AddListener(ExitGame);
    }
    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Exiting...");
    }
}
