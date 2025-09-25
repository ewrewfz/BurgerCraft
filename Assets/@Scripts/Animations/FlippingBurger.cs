using DG.Tweening;
using UnityEngine;

public class FlippingBurger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Flip();
    }


    void Flip()
    {
        float delay = Random.Range(1, 3);

        var sequence = DOTween.Sequence();
        sequence.SetDelay(delay);
        sequence.Append(transform.DOJump(transform.position, 1f, 1, 0.5f));
        sequence.Join(transform.DOLocalRotate(new Vector3(0, 0, 180), 0.5f, RotateMode.LocalAxisAdd));
        sequence.OnComplete(() => Flip());

    }
}
