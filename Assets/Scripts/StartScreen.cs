using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    private bool keyPressed = false;
    void Start()
    {
        GameObject.FindGameObjectWithTag("BGM").GetComponent<BGMusic>().PlayMusic();
    }
    void Update()
    {
        if (Input.anyKey && !keyPressed)
        {
            keyPressed = true;

            SceneManager.LoadScene("MenuScene");
        }

        keyPressed = false;
    }
}
