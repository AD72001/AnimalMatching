using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackToMenuButton : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip buttonSound;
    public Button backButton;
    // Start is called before the first frame update
    void Start()
    {
        Button btn = backButton.GetComponent<Button>();
		btn.onClick.AddListener(TaskOnClick);
    }

    // Update is called once per frame
    void TaskOnClick()
    {
        audioSource.PlayOneShot(buttonSound);
		SceneManager.LoadScene("MenuScene");
	}
}
