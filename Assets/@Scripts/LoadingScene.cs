using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScene : MonoBehaviour
{
    void Start()
    {
		StartCoroutine(LoadSceneAsync("DevScene"));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
	{
		yield return new WaitForSeconds(5f);

		AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

		while (asyncOperation.isDone == false)
		{
			Debug.Log(asyncOperation.progress);
			yield return null;
		}
	}
}
