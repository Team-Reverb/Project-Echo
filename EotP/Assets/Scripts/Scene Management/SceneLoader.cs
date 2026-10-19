using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (messageText != null)
        {
            messageText.text = "";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            if (messageText != null)
            {
                messageText.text = "That level isn't available yet.";
            }
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
