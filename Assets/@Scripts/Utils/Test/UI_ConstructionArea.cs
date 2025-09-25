using UnityEngine;
using UnityEngine.UI;

public class UI_ConstructionArea : MonoBehaviour
{
    [SerializeField]
    Slider _slider;

    protected PlayerController _player {get;set;}
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Enter");

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            _player = pc;
        }
    }
    private void OnTriggerStay(Collider other)
    {
        _slider.value += 0.1f * Time.deltaTime;
        Debug.Log("Stay");
    }
    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Exit");
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            _player = null;
        }
    }
}
