using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

// [추가] 스크립트를 넣으면 자동으로 CharacterController를 붙여줍니다.
[RequireComponent(typeof(CharacterController))]
public class ChampMoveController : MonoBehaviour
{
    [SerializeField]
    private float _moveSpeedHorizontal;
    [SerializeField]
    private float _moveSpeedVertical;

    public bool LeaderChamp = false;

    private Transform _follow;
    private float _maxDistance = 2;
    private float _activateDistance = 2;
    private float _followSpeedHorizontal;
    private bool _isTracking = false;

    private ChampAnimator _champAnimator;
    private Vector3 _lastPosition;

    // [추가] CharacterController 컴포넌트를 담을 변수
    private CharacterController _characterController;

    private void Awake()
    {
        _champAnimator = GetComponentInChildren<ChampAnimator>();
        _characterController = GetComponent<CharacterController>(); // 컴포넌트 가져오기
    }

    private void Start()
    {
        _lastPosition = transform.position;
        _followSpeedHorizontal = _moveSpeedHorizontal;
    }

    private void LateUpdate()
    {
        if (!LeaderChamp)
        {
            if (_follow == null) return; // 안전장치

            Vector3 offset = transform.position - _follow.position;
            float actualDistance = offset.magnitude;

            // [상태 스위치] 유지
            if (actualDistance > _activateDistance)
            {
                _isTracking = true;
            }
            else if (actualDistance <= _maxDistance)
            {
                _isTracking = false;
            }

            // [이동 로직] CharacterController.Move() 방식에 맞게 수정
            if (_isTracking)
            {
                Vector3 direction = offset.normalized;
                Vector3 targetPos = _follow.position + (direction * _maxDistance);

                // 1. 이번 프레임에 이동해야 할 목표 X 좌표를 구합니다.
                float newX = Mathf.MoveTowards(transform.position.x, targetPos.x, Time.deltaTime * _followSpeedHorizontal);

                // 2. 목표 X 좌표까지의 '차이(거리)'를 계산합니다. (Y, Z는 0으로 두어 이동하지 않음)
                Vector3 moveDelta = new Vector3(newX - transform.position.x, 0, 0);

                // 3. 계산된 거리만큼 CharacterController로 밉니다.
                _characterController.Move(moveDelta);
            }
        }

        // [애니메이션 로직] CharacterController 적용 시에도 완벽하게 작동하도록 유지
        Vector3 deltaMove = transform.position - _lastPosition;
        deltaMove.y = 0;

        bool isMoving = deltaMove.sqrMagnitude > 0.000001f;
        _champAnimator.MoveAnimation(isMoving);

        if (isMoving && Mathf.Abs(deltaMove.x) > 0.001f)
        {
            _champAnimator.FlipCharacter(deltaMove.x);
        }

        _lastPosition = transform.position;
    }

    public void LeaderMove(Vector3 moveInput)
    {
        Vector3 dir = moveInput.normalized;
        dir = new Vector3(dir.x * _moveSpeedHorizontal, 0, 0);

        // [변경] Translate 대신 CharacterController.Move 사용
        _characterController.Move(dir * Time.deltaTime);
    }

    public void FollowerSet(Transform follow, float maxDistance)
    {
        _follow = follow;
        _maxDistance = maxDistance;
        _activateDistance = maxDistance * 1.4f;
    }
}