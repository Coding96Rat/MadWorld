using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class InGameUIHandler : MonoBehaviour
{
    public Button ActivateElevator;
    public Button ActivateCombat;
    public Transform ChampSkillSlotTransform;

    [Header("1. 퇴장 연출 (Hide Settings)")]
    [SerializeField] private float _hideAnticipationAmount = 5f;
    [SerializeField] private float _hideAnticipationTime = 0.05f;
    [SerializeField] private float _hideDropY = -150f;
    [SerializeField] private float _hideDropTime = 0.15f;
    [SerializeField] private Ease _hideEase = Ease.InExpo;

    [Header("2. 등장 연출 (Show Settings)")]
    [SerializeField] private float _showTargetY = 80f;
    [SerializeField] private float _showTime = 0.5f;

    [Header("3. 등장 탄성 조절 (OutElastic Settings)")]
    [SerializeField] private float _elasticAmplitude = 1.7f;
    [SerializeField] private float _elasticPeriod = 0.3f;

    [Header("4. 회전 진동 연출 (Shake Settings)")]
    [SerializeField] private bool _useShake = false;
    [SerializeField] private float _shakeAngle = 1.5f;
    [SerializeField] private float _shakeDuration = 0.3f;
    [SerializeField] private int _shakeVibrato = 5;

    [Header("5. 전투 진입 연출 동기화 (Combat Sync)")]
    [SerializeField] private float _cameraTransitionTime = 1.5f;

    [Header("6. 스킬 슬롯 전용 등장 연출 (Skill Slot Settings)")]
    [SerializeField, Tooltip("스킬 슬롯이 올라오는 데 걸리는 시간 (거리가 멀기 때문에 0.8~1.0초 추천)")]
    private float _skillSlotShowTime = 0.8f;
    [SerializeField, Tooltip("스킬 슬롯의 탄성 진폭 (기본 1.0)")]
    private float _skillSlotElasticAmplitude = 1.0f;
    [SerializeField, Tooltip("스킬 슬롯의 탄성 주기 (0.4 정도로 살짝 여유를 주면 묵직합니다)")]
    private float _skillSlotElasticPeriod = 0.4f;

    private RectTransform _elevatorBtnTransform;
    private RectTransform _combatBtnTransform;
    private RectTransform _skillSlotBtnTransform;
    private GridSystem _gridSystem;

    private void Awake()
    {
        _gridSystem = FindFirstObjectByType<GridSystem>();

        ActivateCombat.onClick.AddListener(() =>
        {
            _gridSystem.StartStageAnim();
            HideSingleButtonAnim(_combatBtnTransform);

            // 카메라 이동이 끝나는 시간에 맞춰 스킬 슬롯 등장
            DOVirtual.DelayedCall(_cameraTransitionTime, ShowSkillSlotAnim);
        });
    }

    private void Start()
    {
        _elevatorBtnTransform = ActivateElevator.GetComponent<RectTransform>();
        _combatBtnTransform = ActivateCombat.GetComponent<RectTransform>();

        if (ChampSkillSlotTransform != null)
        {
            _skillSlotBtnTransform = ChampSkillSlotTransform.GetComponent<RectTransform>();
        }

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

        uiSeq.Append(hideBtn.DOAnchorPosY(currentY + _hideAnticipationAmount, _hideAnticipationTime).SetEase(Ease.OutQuad));
        uiSeq.Append(hideBtn.DOAnchorPosY(_hideDropY, _hideDropTime).SetEase(_hideEase));

        uiSeq.InsertCallback(_hideAnticipationTime, () =>
        {
            showBtn.anchoredPosition = new Vector2(showBtn.anchoredPosition.x, _hideDropY);
            showBtn.localScale = Vector3.one;
            showBtn.localRotation = Quaternion.identity;
        });

        uiSeq.Insert(_hideAnticipationTime, showBtn.DOAnchorPosY(_showTargetY, _showTime).SetEase(Ease.OutElastic, _elasticAmplitude, _elasticPeriod));

        if (_useShake)
        {
            uiSeq.Insert(_hideAnticipationTime, showBtn.DOPunchRotation(new Vector3(0, 0, _shakeAngle), _shakeDuration, _shakeVibrato, 0.5f));
        }
    }

    private void ShowSkillSlotAnim()
    {
        if (_skillSlotBtnTransform == null) return;

        _skillSlotBtnTransform.DOKill();

        float targetY = _skillSlotBtnTransform.anchoredPosition.y * -1f;

        // ★ 분리된 전용 세팅값(_skillSlotShowTime 등)을 사용합니다!
        _skillSlotBtnTransform.DOAnchorPosY(targetY, _skillSlotShowTime)
            .SetEase(Ease.OutElastic, _skillSlotElasticAmplitude, _skillSlotElasticPeriod);

        if (_useShake)
        {
            _skillSlotBtnTransform.DOPunchRotation(new Vector3(0, 0, _shakeAngle), _shakeDuration, _shakeVibrato, 0.5f);
        }
    }

    private void HideSingleButtonAnim(RectTransform btn)
    {
        btn.DOKill();

        Sequence hideSeq = DOTween.Sequence();
        float currentY = btn.anchoredPosition.y;

        hideSeq.Append(btn.DOAnchorPosY(currentY + _hideAnticipationAmount, _hideAnticipationTime).SetEase(Ease.OutQuad));
        hideSeq.Append(btn.DOAnchorPosY(_hideDropY, _hideDropTime).SetEase(_hideEase));
    }
}