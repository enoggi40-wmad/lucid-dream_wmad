using LucidDream.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{

    [Header("Scene Names")]
    [SerializeField] private string Title = "Title";
    [SerializeField] private string success_Stage = "Success";
    [SerializeField] private string fail_Stage = "Fail";
    [SerializeField] private string Stage1 = "Stage1";

    [Header("Buttons")]
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private GameObject quitButton;


    [Header("Images")]
    [SerializeField] private GameObject titleImage;
    [SerializeField] private GameObject successImage;
    [SerializeField] private GameObject failImage;


    public void Start_Game()
    {
        ResetGameState();
        SceneManager.LoadScene(Stage1);
    }

    public void Quit_Game()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void Restart_Game()
    {
        ResetGameState();
        SceneManager.LoadScene(Stage1);
    }


    private void ApplyScreenByCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        bool isTitle = currentSceneName == Title;
        bool isSuccess = currentSceneName == success_Stage;
        bool isFail = currentSceneName == fail_Stage;

        SetActive(startButton, isTitle);
        SetActive(restartButton, isSuccess || isFail);
        SetActive(quitButton, isTitle || isSuccess || isFail);

        SetActive(titleImage, isTitle);
        SetActive(successImage, isSuccess);
        SetActive(failImage, isFail);
    }

    private void ResetGameState()
    {
        if (SanityManager.Instance != null)
            Destroy(SanityManager.Instance.gameObject);

        if (DissociationLevelManager.Instance != null)
            Destroy(DissociationLevelManager.Instance.gameObject);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
