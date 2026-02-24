using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ChampMoveController : MonoBehaviour
{

    [SerializeField]
    private float _moveSpeedHorizontal;
    [SerializeField]
    private float _moveSpeedVertical;

    public bool LeaderChamp = false;


    private Transform _follow;
    private float _maxDistance = 2;
    private float _followSpeedHorizontal;
    private float _followSpeedVertical;

    private ChampAnimator _champAnimator;
    // [ 애니메이션 ] 이전 프레임의 위치를 저장 할 변수
    private Vector3 _lastPosition;
    private void Awake()
    {
        _champAnimator = GetComponentInChildren<ChampAnimator>();
    }

    private void Start()
    {
        _lastPosition = transform.position;

        _followSpeedHorizontal = _moveSpeedHorizontal - 0.5f;
        _followSpeedVertical = _moveSpeedVertical - 0.5f;
    }

    private void Update()
    {
        if (!LeaderChamp)
        {
            // 1. 리더와 나 사이의 방향과 거리를 구합니다.
            Vector3 offset = transform.position - _follow.position;
            float actualDistance = offset.magnitude;

            // 2. 거리가 설정값보다 멀 때만 이동
            if (actualDistance > _maxDistance)
            {
                Vector3 direction = offset.normalized;
                // 내가 가야 할 최종 목적지 (리더 주변 Edge)
                Vector3 targetPos = _follow.position + (direction * _maxDistance);

                // [핵심 추가] 내 현재 위치와 '최종 목적지' 사이의 거리를 잽니다.
                // (Y축 제외하고 수평 거리만 계산)
                float distToTarget = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                                      new Vector3(targetPos.x, 0, targetPos.z));

                // [설정] 이 거리 안으로 들어오면 강제로 딱! 붙입니다. (0.05f ~ 0.1f 추천)
                float snapThreshold = 0.1f;

                if (distToTarget < snapThreshold)
                {
                    // 3-A. 거의 다 왔으면 그냥 바로 목표 위치로 이동 (미끄러짐 방지)
                    transform.position = new Vector3(targetPos.x, transform.position.y, targetPos.z);
                }
                else
                {
                    // 3-B. 아직 멀었으면 부드럽게 Lerp 이동
                    Vector3 currentPos = transform.position;

                    float newX = Mathf.Lerp(currentPos.x, targetPos.x, Time.deltaTime * _followSpeedHorizontal);
                    float newZ = Mathf.Lerp(currentPos.z, targetPos.z, Time.deltaTime * _followSpeedVertical);

                    transform.position = new Vector3(newX, currentPos.y, newZ);
                }
            }
        }
    }

    private void LateUpdate()
    {
        Vector3 deltaMove = transform.position - _lastPosition;
        deltaMove.y = 0;

        bool isMoving = deltaMove.sqrMagnitude > 0.000001f;
        Debug.Log(isMoving);
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
        transform.Translate(dir * Time.deltaTime, Space.World);
    }

    public void FollowerSet(Transform follow, float maxDistance)
    {
        _follow = follow;
        _maxDistance = maxDistance;
    }
}
