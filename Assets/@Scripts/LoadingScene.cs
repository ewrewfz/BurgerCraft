using System.Collections;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScene : MonoBehaviour
{
    void Start()
    {
		//StartCoroutine(LoadSceneAsync("DevScene"));
		SceneManager.LoadScene("DevScene");
    }

    private IEnumerator LoadSceneAsync(string sceneName)
	{
		AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

		while (asyncOperation.isDone == false)
		{
			Debug.Log(asyncOperation.progress);
			yield return null;
		}
	}
}
