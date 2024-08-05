using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuScreen : MonoBehaviour
{
    public Button levelOneButton;
    public Button levelTwoButton;

    // Start is called before the first frame update
    void Start()
    {
        GameObject.FindGameObjectWithTag("BGM").GetComponent<BGMusic>().PlayMusic();

        Button btn1 = levelOneButton.GetComponent<Button>();
        Button btn2 = levelTwoButton.GetComponent<Button>();

        btn1.onClick.AddListener(delegate{ChangeLevel(1);});
        btn2.onClick.AddListener(delegate{ChangeLevel(2);});
    }

    void ChangeLevel(int level)
    {
        SceneManager.LoadScene($"Scene_{level}");
    }
}
