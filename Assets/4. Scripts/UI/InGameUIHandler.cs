using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class InGameUIHandler : MonoBehaviour
{
    public Button ActivateElevator;
    public Button ActivateCombat;

    [Header("1. 퇴장 연출 (Hide Settings)")]
    [SerializeField, Tooltip("내려가기 전 위로 살짝 들리는(철컥) 높이")]
    private float _hideAnticipationAmount = 5f; // 높이를 줄여서 더 빠르고 간결하게
    [SerializeField, Tooltip("위로 들리는 데 걸리는 시간 (아주 짧아야 타격감이 삽니다)")]
    private float _hideAnticipationTime = 0.05f; // 0.15 -> 0.05로 대폭 감소 (즉발 느낌)
    [SerializeField, Tooltip("화면 밖으로 떨어지는 목표 위치 (Y)")]
    private float _hideDropY = -150f;
    [SerializeField, Tooltip("떨어지는 데 걸리는 시간")]
    private float _hideDropTime = 0.15f; // 0.3 -> 0.15로 대폭 감소 (훅 떨어짐)
    [SerializeField, Tooltip("떨어질 때의 가속도 느낌")]
    private Ease _hideEase = Ease.InExpo; // InCubic보다 훨씬 무겁고 빠르게 내리꽂히는 가속도

    [Header("2. 등장 연출 (Show Settings)")]
    [SerializeField, Tooltip("화면 안으로 올라올 목표 위치 (Y)")]
    private float _showTargetY = 80f;
    [SerializeField, Tooltip("올라오는 데 걸리는 시간")]
    private float _showTime = 0.5f;

    [Header("3. 등장 탄성 조절 (OutElastic Settings)")]
    [SerializeField, Tooltip("진폭(Amplitude): 목표치를 얼마나 크게 뚫고 나갔다가 돌아올지 제어")]
    private float _elasticAmplitude = 1.7f;
    [SerializeField, Tooltip("주기(Period): 얼마나 잘게 떨릴지 제어 (0.3 추천)")]
    private float _elasticPeriod = 0.3f;

    [Header("4. 회전 진동 연출 (Shake Settings - 선택)")]
    [SerializeField] private bool _useShake = false;
    [SerializeField] private float _shakeAngle = 1.5f;
    [SerializeField] private float _shakeDuration = 0.3f;
    [SerializeField] private int _shakeVibrato = 5;

    private RectTransform _elevatorBtnTransform;
    private RectTransform _combatBtnTransform;
    private GridSystem _gridSystem;

    private void Awake()
    {
        _gridSystem = FindFirstObjectByType<GridSystem>();
        ActivateCombat.onClick.AddListener(() =>
        {
            _gridSystem.StartStageAnim();
        });
    }

    private void Start()
    {
        _elevatorBtnTransform = ActivateElevator.GetComponent<RectTransform>();
        _combatBtnTransform = ActivateCombat.GetComponent<RectTransform>();

        _combatBtnTransform.anchoredPosition = new Vector2(_combatBtnTransform.anchoredPosition.x, _hideDropY);
        _combatBtnTransform.localScale = Vector3.one;
    }

    public void ElevatorArrived()
    {
        SwitchButtonAnim(_elevatorBtnTransform, _combatBtnTransform);
    }

    public void StageClear()
    {
        SwitchButtonAnim(_combatBtnTransform, _elevatorBtnTransform);
    }

    private void SwitchButtonAnim(RectTransform hideBtn, RectTransform showBtn)
    {
        hideBtn.DOKill();
        showBtn.DOKill();

        Sequence uiSeq = DOTween.Sequence();

        float currentY = hideBtn.anchoredPosition.y;

        // 1. 퇴장: 짧고 강하게 위로 덜컹! (0.05초)
        uiSeq.Append(hideBtn.DOAnchorPosY(currentY + _hideAnticipationAmount, _hideAnticipationTime).SetEase(Ease.OutQuad));

        // 2. 퇴장: 아래로 강하게 추락 (0.15초)
        uiSeq.Append(hideBtn.DOAnchorPosY(_hideDropY, _hideDropTime).SetEase(_hideEase));

        // 3. 등장 준비 및 상승 (★핵심 변경점: 순차 실행이 아니라 '교차 실행'으로 변경)
        // 기존 간판이 위로 들렸다가 '추락을 시작하는 정확한 시점(_hideAnticipationTime)'에 맞춰서 작동
        uiSeq.InsertCallback(_hideAnticipationTime, () =>
        {
            showBtn.anchoredPosition = new Vector2(showBtn.anchoredPosition.x, _hideDropY);
            showBtn.localScale = Vector3.one;
            showBtn.localRotation = Quaternion.identity;
        });

        // 기존 간판이 떨어짐과 동시에 새 간판이 위로 솟구침 (체인으로 연결된 듯한 연출)
        uiSeq.Insert(_hideAnticipationTime, showBtn.DOAnchorPosY(_showTargetY, _showTime).SetEase(Ease.OutElastic, _elasticAmplitude, _elasticPeriod));

        if (_useShake)
        {
            uiSeq.Insert(_hideAnticipationTime, showBtn.DOPunchRotation(new Vector3(0, 0, _shakeAngle), _shakeDuration, _shakeVibrato, 0.5f));
        }
    }
}