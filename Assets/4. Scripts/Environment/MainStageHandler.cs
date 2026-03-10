using Unity.Cinemachine;
using UnityEngine;
using DG.Tweening;

public class MainStageHandler : MonoBehaviour
{
    private StageStateManager _stageStateManager;

    [Header("1. 출발 연출 (Departure)")]
    [SerializeField] private float _dipAmount = 0.15f;
    [SerializeField] private float _dipDuration = 0.1f;
    [SerializeField] private float _pauseDuration = 0.08f;
    [SerializeField, Tooltip("화면이 까매지기 전까지 밑으로 꺼질 깊이 (하강용)")]
    private float _departureDropDistance = 5f;
    [SerializeField, Tooltip("가속하며 내려가는 시간")]
    private float _departureDuration = 1.0f;

    [Header("2. UI 페이드 설정 (Fade Settings)")]
    [SerializeField] private CanvasGroup _fadePanel;

    [SerializeField, Range(0f, 1f), Tooltip("화면이 어두워지는 최대 정도 (Amount)")]
    private float _fadeMaxAlpha = 1.0f;

    [SerializeField, Tooltip("출발(가속 하강) 시작 후 몇 초 뒤에 페이드 인을 시작할지")]
    private float _fadeInTime = 0.2f;

    [SerializeField, Tooltip("도착 지점(0)에 도달하기 몇 초 전에 페이드 아웃을 시작할지")]
    private float _fadeOutTime = 0.5f;

    private const float DefaultFadeInSpeed = 0.5f;
    private const float DefaultFadeOutSpeed = 1.0f;

    [Header("3. 도착 연출 (Arrival)")]
    [SerializeField, Tooltip("화면이 까말 때, 엘리베이터가 대기할 위쪽 공중 높이")]
    private float _arrivalStartHeight = 5f; // 하강이므로 +5(위)에서 시작
    [SerializeField, Tooltip("감속하며 지상(0)으로 떨어지는 시간")]
    private float _arrivalDuration = 1.2f;

    [Header("4. 도착 덜컹 연출 (Arrival Bump)")]
    [SerializeField, Tooltip("도착 시 바닥(0)을 뚫고 무게 때문에 살짝 아래로 압축되는 깊이 (언더슈트)")]
    private float _arrivalOvershootAmount = 0.2f;
    [SerializeField, Tooltip("바닥으로 압축됐다가 정위치(0)로 덜컹 하고 튕겨 올라오는 시간")]
    private float _arrivalSettleDuration = 0.3f;


    public System.Action OnScreenBlackout;

    private Transform _stageContainer;
    private Transform _cameraTarget;
    private CinemachineCamera _virtualCamera;
    private bool _isMoving = false;

    private Vector3 _defaultCamPos;

    private Sequence _elevatorSequence;

    private void Awake()
    {
        _stageStateManager = GetComponent<StageStateManager>();
    }

    public void Initialize(Transform stageContainer, Transform cameraTarget, CinemachineCamera vCam)
    {
        _stageContainer = stageContainer;
        _cameraTarget = cameraTarget;
        _virtualCamera = vCam;

        if (_fadePanel != null)
        {
            _fadePanel.alpha = 0f;
            _fadePanel.blocksRaycasts = false;
        }
    }

    public void SetDefaultCameraPosition(Vector3 defaultPos)
    {
        _defaultCamPos = defaultPos;
    }

    public void MoveUp()
    {
        if (_isMoving || _stageContainer == null || _cameraTarget == null) return;
        if (_elevatorSequence != null && _elevatorSequence.IsActive()) return;

        _isMoving = true;
        PlayElevatorSequence();
    }

    private void PlayElevatorSequence()
    {
        Vector3 originalStagePos = _stageContainer.localPosition;
        Vector3 currentCamPos = _cameraTarget.position;

        _elevatorSequence = DOTween.Sequence();

        // --- Phase 1: 덜컹! (Dip) ---
        // 내려가기 전 무게가 실리며 밑으로 덜컹! (기존 유지)
        _elevatorSequence.Append(_stageContainer.DOLocalMoveY(originalStagePos.y + _dipAmount, _dipDuration).SetEase(Ease.OutQuad));

        // --- Phase 1.5: 철-컥! (멈춤) ---
        _elevatorSequence.AppendInterval(_pauseDuration);

        // --- Phase 2: 출발 (가속 하강) ---
        // 원래 위치에서 _departureDropDistance 만큼 훅 꺼지며 추락
        _elevatorSequence.Append(_stageContainer.DOLocalMoveY(originalStagePos.y - _departureDropDistance, _departureDuration).SetEase(Ease.InCubic));
        _elevatorSequence.Join(_cameraTarget.DOMoveY(currentCamPos.y - _departureDropDistance, _departureDuration).SetEase(Ease.InCubic));

        if (_fadePanel != null)
        {
            float fadeInStartTime = _dipDuration + _pauseDuration + _fadeInTime;
            _elevatorSequence.InsertCallback(fadeInStartTime, () => _fadePanel.blocksRaycasts = true);
            _elevatorSequence.Insert(fadeInStartTime, _fadePanel.DOFade(_fadeMaxAlpha, DefaultFadeInSpeed));
        }

        // --- Phase 3: 완전 암전 상태 (The Void) --- 
        _elevatorSequence.AppendCallback(() =>
        {
            // 이제 지하가 아니라, 하늘(+5) 위로 위치를 세팅합니다.
            _stageContainer.localPosition = new Vector3(originalStagePos.x, originalStagePos.y + _arrivalStartHeight, originalStagePos.z);
            _cameraTarget.position = _defaultCamPos;

            if (_virtualCamera != null) _virtualCamera.PreviousStateIsValid = false;
            if (_fadePanel != null) _fadePanel.alpha = _fadeMaxAlpha;

            OnScreenBlackout?.Invoke();
        });

        // --- Phase 4: 도착 하강 (바닥 압축 지점까지) ---
        float arrivalStartTime = _elevatorSequence.Duration();
        // 하늘에서 떨어져 바닥(0)을 찍고 무게 때문에 -0.2f까지 살짝 더 밑으로 압축됨
        _elevatorSequence.Append(_stageContainer.DOLocalMoveY(originalStagePos.y - _arrivalOvershootAmount, _arrivalDuration).SetEase(Ease.OutCubic));

        if (_fadePanel != null)
        {
            float fadeOutDelay = Mathf.Max(0f, _arrivalDuration - _fadeOutTime);
            _elevatorSequence.Insert(arrivalStartTime + fadeOutDelay, _fadePanel.DOFade(0f, DefaultFadeOutSpeed));
        }

        // --- Phase 5: 도착 덜컹! (스무스 -0.08 -> 바운스 0) ---
        // 5-1. 강하게 압축된 상태(-0.2)에서 -0.08 높이까지 스으윽 부드럽게 반동으로 올라옴
        _elevatorSequence.Append(_stageContainer.DOLocalMoveY(originalStagePos.y - 0.08f, _arrivalSettleDuration * 0.5f).SetEase(Ease.InOutSine));

        //  버튼 스위칭 타이밍: 정확히 반동으로 튕겨오르며 0으로 꽂히는 찰나에 실행!
        _elevatorSequence.AppendCallback(() =>
        {
            _stageStateManager.ElevatorArrived();
        });

        // 5-2. -0.08에서 0으로 팍! 튕겨 올라가며 기계적인 바운스 터짐 (OutBounce)
        _elevatorSequence.Append(_stageContainer.DOLocalMoveY(originalStagePos.y, _arrivalSettleDuration * 0.5f).SetEase(Ease.OutBounce));

        // --- 오차 보정 및 종료 처리 ---
        _elevatorSequence.OnComplete(() =>
        {
            _stageContainer.localPosition = originalStagePos;
            _cameraTarget.position = _defaultCamPos;

            if (_fadePanel != null) _fadePanel.blocksRaycasts = false;
            _isMoving = false;
        });
    }

    private void OnDestroy()
    {
        if (_elevatorSequence != null)
        {
            _elevatorSequence.Kill();
        }
    }
}