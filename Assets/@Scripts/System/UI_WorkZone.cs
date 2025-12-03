using System.Net.NetworkInformation;
using UnityEngine;

public class UI_WorkZone : MonoBehaviour
{
    [SerializeField]
    GameObject activeImage;
    [SerializeField]
    GameObject deActiveImage;


    private void Start()
    {
        GetComponent<WorkerInteraction>().OnTriggerStart = OnEnterPlace;
        GetComponent<WorkerInteraction>().OnTriggerEnd = OnLeavePlace;
    }


    public void OnEnterPlace(WorkerController wc)
    {
        activeImage.SetActive(true);
        deActiveImage.SetActive(false);
    }
    public void OnLeavePlace(WorkerController wc)
    {
        activeImage.SetActive(false);
        deActiveImage.SetActive(true);
    }



}
