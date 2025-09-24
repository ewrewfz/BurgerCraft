using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreenManager : MonoBehaviour
{
    private Animator _animatorComponent;

    private void Start()
    {
        _animatorComponent = transform.GetComponent<Animator>();  

        // Remove it if you don't want to hide it in the Start function and call it elsewhere
        HideLoadingScreen();
    }

    public void RevealLoadingScreen()
    {
        _animatorComponent.SetTrigger("Reveal");
    }

    public void HideLoadingScreen()
    {
        // Call this function, if you want start hiding the loading screen
        _animatorComponent.SetTrigger("Hide");
    }

    public void OnFinishedReveal()
    {
        OnFinishedHide();

    }

    public void OnFinishedHide()
    {
        SceneManager.LoadScene("DevScene");
    }

}
