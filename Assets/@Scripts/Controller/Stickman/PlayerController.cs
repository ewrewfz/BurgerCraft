using System;
using UnityEngine;
using static Define;

public class PlayerController : WorkerController
{
    protected override void Awake()
    {
        base.Awake();

        _navMeshAgent.enabled = false;
        Tray.IsPlayer = true;
    }

    protected override void Update()
    {
        base.Update();

        Vector3 dir = GameManager.Instance.JoystickDir;
        Vector3 moveDir = new Vector3(dir.x, 0, dir.y);
        moveDir = (Quaternion.Euler(0, 45, 0) * moveDir).normalized;

        if (moveDir != Vector3.zero)
        {
            // 이동.
            _controller.Move(moveDir * Time.deltaTime * _moveSpeed);
            
            // CharacterController.Move()가 충돌이나 Step Offset으로 Y축을 올린 경우 즉시 보정.
            Vector3 pos = transform.position;
            if (Mathf.Abs(pos.y) > 0.01f)
            {
                transform.position = new Vector3(pos.x, 0, pos.z);
            }

            // 고개 돌리기.
            Quaternion lookRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * _rotateSpeed);

            State = EAnimState.Move;
        }
        else
        {
            State = EAnimState.Idle;
        }
    }

}
