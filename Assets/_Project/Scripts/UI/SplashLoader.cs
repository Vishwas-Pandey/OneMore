using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashLoader : MonoBehaviour
{
    [SerializeField] private float delaySeconds = 1f;
    [SerializeField] private string nextScene = "MainMenu";

    private void Start()
    {
        Invoke(nameof(LoadNext), delaySeconds);
    }

    private void LoadNext()
    {
        SceneManager.LoadScene(nextScene);
    }
}
