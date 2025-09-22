using UnityEngine;

public class Billboard : MonoBehaviour
{
    private void LateUpdate()
    {
        Vector3 dir = transform.position - Camera.main.transform.position;
        transform.LookAt(transform.position + dir); 
    }
}
