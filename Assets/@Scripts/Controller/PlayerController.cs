using UnityEngine;
using static Define;


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

    private EState _state = EState.None;
    public EState State
    {
        get { return _state; }
        set
        {
            if (_state == value) return;
            _state = value;
            UpdateAnimation();
        }
    }

    public bool IsServing { get; set; } = false;

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
        moveDir = (Quaternion.Euler(0, 45, 0) * moveDir).normalized;


        if (moveDir != Vector3.zero)
        {
            // 이동 관련
            _controller.Move(moveDir * Time.deltaTime * _moveSpeed);

            // 고개 돌리기
            Quaternion lookRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * _rotateSpeed);



            State = EState.Move;
        }
        else
        {
            State = EState.Idle;
        }
    
        // 중력 작용
        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
    }

    public void UpdateAnimation()
    {
        switch (State)
        {
            case EState.Idle:
                _animator.CrossFade(IsServing ? Define.SERVING_IDLE : Define.IDLE, 0.1f);
                break;

            case EState.Move:
                _animator.CrossFade(IsServing ? Define.SERVING_MOVE : Define.MOVE, 0.05f);
                break;

        }

    }

}
