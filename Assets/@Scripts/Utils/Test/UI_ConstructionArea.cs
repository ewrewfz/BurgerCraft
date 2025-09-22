using UnityEngine;
using UnityEngine.UI;

public class UI_ConstructionArea : MonoBehaviour
{
    [SerializeField]
    Slider _slider;

    private void Start()
    {
       
    }

    private void Update()
    {
        _slider.value += 0.1f * Time.deltaTime;
    }

}
