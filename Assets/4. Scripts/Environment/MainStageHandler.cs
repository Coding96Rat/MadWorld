using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class MainStageHandler : MonoBehaviour
{
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

    // 인스펙터에 노출하지 않는 기본 알파 변화 시간 (원래 기본값)
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
    private float _arrivalSettleDuration = 0.15f;

    public System.Action OnScreenBlackout;

    private Transform _stageContainer;
    private Transform _cameraTarget;
    private CinemachineCamera _virtualCamera;
    private bool _isMoving = false;

    private Vector3 _defaultCamPos;

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
        StartCoroutine(ElevatorRoutine());
    }

    private IEnumerator ElevatorRoutine()
    {
        _isMoving = true;

        Vector3 originalStagePos = _stageContainer.localPosition;
        Vector3 currentCamPos = _cameraTarget.position;

        // --- Phase 1: 덜컹! ---
        Vector3 dipPos = originalStagePos + new Vector3(0, -_dipAmount, 0);
        Vector3 camDipPos = currentCamPos + new Vector3(0, -_dipAmount, 0);

        float elapsed = 0f;
        while (elapsed < _dipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _dipDuration);
            float easeT = Mathf.Sin(t * Mathf.PI * 0.5f);

            _stageContainer.localPosition = Vector3.Lerp(originalStagePos, dipPos, easeT);
            _cameraTarget.position = Vector3.Lerp(currentCamPos, camDipPos, easeT);
            yield return null;
        }

        // --- Phase 1.5: 철-컥! (멈춤) ---
        if (_pauseDuration > 0f) yield return new WaitForSeconds(_pauseDuration);

        // --- Phase 2: 출발 (가속 상승) ---
        Vector3 departureEndPos = originalStagePos + new Vector3(0, _departureRiseHeight, 0);
        Vector3 camDepartureEndPos = currentCamPos + new Vector3(0, _departureRiseHeight, 0);

        if (_fadePanel != null) _fadePanel.blocksRaycasts = true;

        // 페이드 인 코루틴 실행 (설정한 _fadeInTime 지연 후 시작)
        Coroutine fadeCoroutine = StartCoroutine(FadeInRoutine(_fadeInTime));

        elapsed = 0f;
        while (elapsed < _departureDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _departureDuration);
            float easeT = t * t * t; // Ease In

            _stageContainer.localPosition = Vector3.Lerp(dipPos, departureEndPos, easeT);
            _cameraTarget.position = Vector3.Lerp(camDipPos, camDepartureEndPos, easeT);
            yield return null;
        }

        if (fadeCoroutine != null) yield return fadeCoroutine;

        // --- Phase 3: 완전 암전 상태 (The Void) --- 
        Vector3 arrivalStartPos = originalStagePos + new Vector3(0, _arrivalStartHeight, 0);

        _stageContainer.localPosition = arrivalStartPos;
        _cameraTarget.position = _defaultCamPos;

        if (_virtualCamera != null) _virtualCamera.PreviousStateIsValid = false;
        if (_fadePanel != null) _fadePanel.alpha = _fadeMaxAlpha;

        OnScreenBlackout?.Invoke();

        // --- Phase 4: 도착 상승 (오버슈트 지점까지) ---
        Vector3 overshootPos = originalStagePos + new Vector3(0, _arrivalOvershootAmount, 0);

        // 도착 전 특정 남은 시간에 페이드 아웃이 시작되도록 계산하여 실행
        float fadeOutDelay = Mathf.Max(0f, _arrivalDuration - _fadeOutTime);
        StartCoroutine(FadeOutRoutine(fadeOutDelay));

        elapsed = 0f;
        while (elapsed < _arrivalDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _arrivalDuration);
            float easeT = 1f - Mathf.Pow(1f - t, 3f); // Ease Out (감속)

            _stageContainer.localPosition = Vector3.Lerp(arrivalStartPos, overshootPos, easeT);
            _cameraTarget.position = _defaultCamPos;

            yield return null;
        }

        // --- Phase 5: 도착 덜컹! ---
        elapsed = 0f;
        while (elapsed < _arrivalSettleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _arrivalSettleDuration);
            float easeT = Mathf.Sin(t * Mathf.PI * 0.5f);

            _stageContainer.localPosition = Vector3.Lerp(overshootPos, originalStagePos, easeT);
            _cameraTarget.position = _defaultCamPos;

            yield return null;
        }

        // --- 오차 보정 ---
        _stageContainer.localPosition = originalStagePos;
        _cameraTarget.position = _defaultCamPos;

        if (_fadePanel != null) _fadePanel.blocksRaycasts = false;
        _isMoving = false;
    }

    private IEnumerator FadeInRoutine(float delay)
    {
        if (_fadePanel == null) yield break;

        if (delay > 0f) yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        while (elapsed < DefaultFadeInSpeed)
        {
            elapsed += Time.deltaTime;
            _fadePanel.alpha = Mathf.Lerp(0f, _fadeMaxAlpha, elapsed / DefaultFadeInSpeed);
            yield return null;
        }
        _fadePanel.alpha = _fadeMaxAlpha;
    }

    private IEnumerator FadeOutRoutine(float delay)
    {
        if (_fadePanel == null) yield break;

        if (delay > 0f) yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        while (elapsed < DefaultFadeOutSpeed)
        {
            elapsed += Time.deltaTime;
            _fadePanel.alpha = Mathf.Lerp(_fadeMaxAlpha, 0f, elapsed / DefaultFadeOutSpeed);
            yield return null;
        }
        _fadePanel.alpha = 0f;
    }
}