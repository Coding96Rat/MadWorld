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
    [SerializeField, Tooltip("화면이 까매지기 전까지 솟아오를 높이")]
    private float _departureRiseHeight = 5f;
    [SerializeField, Tooltip("가속하며 올라가는 시간")]
    private float _departureDuration = 1.0f;

    [Header("2. UI 페이드 설정 (Fade Settings)")]
    [SerializeField] private CanvasGroup _fadePanel;

    [SerializeField, Range(0f, 1f), Tooltip("화면이 어두워지는 최대 정도 (Amount)")]
    private float _fadeMaxAlpha = 1.0f;

    [SerializeField, Tooltip("출발(가속 상승) 시작 후 몇 초 뒤에 페이드 인을 시작할지")]
    private float _fadeInTime = 0.2f;

    [SerializeField, Tooltip("도착 지점(0)에 도달하기 몇 초 전에 페이드 아웃을 시작할지")]
    private float _fadeOutTime = 0.5f;

    private const float DefaultFadeInSpeed = 0.5f;
    private const float DefaultFadeOutSpeed = 1.0f;

    [Header("3. 도착 연출 (Arrival)")]
    [SerializeField, Tooltip("화면이 까말 때, 엘리베이터가 몰래 이동해 있을 지하 깊이")]
    private float _arrivalStartHeight = -5f;
    [SerializeField, Tooltip("감속하며 지상으로 올라오는 시간")]
    private float _arrivalDuration = 1.2f;

    [Header("4. 도착 덜컹 연출 (Arrival Bump)")]
    [SerializeField, Tooltip("도착 시 목표 지점(0)을 뚫고 살짝 위로 솟구치는 높이 (오버슈트)")]
    private float _arrivalOvershootAmount = 0.2f;
    [SerializeField, Tooltip("위로 솟구쳤다가 정위치(0)로 덜컹 하고 내려앉는 시간")]
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
        _elevatorSequence.Append(_stageContainer.DOLocalMoveY(originalStagePos.y - _dipAmount, _dipDuration).SetEase(Ease.OutQuad));
        _elevatorSequence.Join(_cameraTarget.DOMoveY(currentCamPos.y - _dipAmount, _dipDuration).SetEase(Ease.OutQuad));

        // --- Phase 1.5: 철-컥! (멈춤) ---
        _elevatorSequence.AppendInterval(_pauseDuration);

        // --- Phase 2: 출발 (가속 상승) ---
        _elevatorSequence.Append(_stageContainer.DOLocalMoveY(originalStagePos.y + _departureRiseHeight, _departureDuration).SetEase(Ease.InCubic));
        _elevatorSequence.Join(_cameraTarget.DOMoveY(currentCamPos.y + _departureRiseHeight, _departureDuration).SetEase(Ease.InCubic));

        if (_fadePanel != null)
        {
            float fadeInStartTime = _dipDuration + _pauseDuration + _fadeInTime;
            _elevatorSequence.InsertCallback(fadeInStartTime, () => _fadePanel.blocksRaycasts = true);
            _elevatorSequence.Insert(fadeInStartTime, _fadePanel.DOFade(_fadeMaxAlpha, DefaultFadeInSpeed));
        }

        // --- Phase 3: 완전 암전 상태 (The Void) --- 
        _elevatorSequence.AppendCallback(() =>
        {
            _stageContainer.localPosition = new Vector3(originalStagePos.x, originalStagePos.y + _arrivalStartHeight, originalStagePos.z);
            _cameraTarget.position = _defaultCamPos;

            if (_virtualCamera != null) _virtualCamera.PreviousStateIsValid = false;
            if (_fadePanel != null) _fadePanel.alpha = _fadeMaxAlpha;

            OnScreenBlackout?.Invoke();
        });

        // --- Phase 4: 도착 상승 (오버슈트 지점까지) ---
        float arrivalStartTime = _elevatorSequence.Duration();
        _elevatorSequence.Append(_stageContainer.DOLocalMoveY(originalStagePos.y + _arrivalOvershootAmount, _arrivalDuration).SetEase(Ease.OutCubic));

        if (_fadePanel != null)
        {
            float fadeOutDelay = Mathf.Max(0f, _arrivalDuration - _fadeOutTime);
            _elevatorSequence.Insert(arrivalStartTime + fadeOutDelay, _fadePanel.DOFade(0f, DefaultFadeOutSpeed));
        }

        // --- Phase 5: 도착 덜컹! (스무스 0.08 -> 바운스 0) ---
        // 5-1. 오버슈트 지점에서 0.08 높이까지 스으윽 부드럽게 내려감
        _elevatorSequence.Append(_stageContainer.DOLocalMoveY(originalStagePos.y + 0.08f, _arrivalSettleDuration * 0.5f).SetEase(Ease.InOutSine));

        // [수정된 부분] 0.08에서 0으로 바닥을 쾅! 찍고 바운스가 시작되는 정확히 그 찰나의 순간!
        _elevatorSequence.AppendCallback(() =>
        {
            _stageStateManager.ElevatorArrived();
        });

        // 5-2. 0.08에서 0으로 팍! 떨어지며 기계적인 바운스 터짐
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