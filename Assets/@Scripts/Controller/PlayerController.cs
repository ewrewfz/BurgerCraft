using UnityEngine;


// 컴포넌트 할당 안될 시 자동으로 할당 시켜주는 기능
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CharacterController))]

public class PlayerController : MonoBehaviour
{
    [SerializeField, Range(1, 5)]
    private float _moveSpeed = 3;

    [SerializeField]
    private float _rotateSpeed = 360;

    private Animator _animator;
    private CharacterController _controller;
    private AudioSource _audioSource;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Vector3 dir = GameManager.Instance.JoystickDir;
        Vector3 moveDir = new Vector3(dir.x, 0, dir.y);
        // 3D 맵상 기준 45도 방향으로 회전
        moveDir = (Quaternion.Euler(0, 45 ,0) * moveDir).normalized;


        if(moveDir != Vector3.zero)
        {
            // 이동 관련
            _controller.Move(moveDir * Time.deltaTime*_moveSpeed);

            // 고개 돌리기
            Quaternion lookRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation,lookRotation, Time.deltaTime * _rotateSpeed);
        }
        else
        {

        }
        // 중력 작용
        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
    }
}
