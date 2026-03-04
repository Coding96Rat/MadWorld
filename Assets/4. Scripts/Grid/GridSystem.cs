using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class GridSystem : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector3 _leftBottomLocation = new Vector3(0, 0, 0);
    [Space(10)]
    public int _rows = 10;
    public int _columns = 10;
    public float _gridSize = 1;
    [Space(10)]
    public GameObject GridPrefab;
    [Space(10)]

    [Header("Grid Camera Setting")]
    [SerializeField] private CinemachineCamera _followCamera;
    [SerializeField] private Transform _gridCamPoint;
    private Transform _gridFirstCamPoint;
    [SerializeField] private float _camSpeed = 10f;
    [Space(5)]
    [SerializeField] private float _zoomSpeed = 10f;
    [SerializeField] private float _zoomSmoothSpeed = 10f;
    private float _targetFOV;
    [SerializeField] private float _minZoom = 5f;
    [SerializeField] private float _maxZoom = 20f;
    [SerializeField] private float _zoomStepAmount = 5f;

    [Header("Stage Animation Settings (등장 연출)")]
    [SerializeField, Tooltip("스테이지가 위로 솟아오를 높이")]
    private float _riseAmount = 0.45f;

    [SerializeField, Tooltip("목표 위치까지 도달하는데 걸리는 시간 (초)")]
    private float _riseDuration = 1.5f;

    [Header("Dark Aura Settings (경계선 파티클)")]
    [SerializeField] private ParticleSystem _borderAuraPrefab;

    private float _limitMinX;
    private float _limitMaxX;
    private float _limitMinZ;
    private float _limitMaxZ;

    // 분리된 시스템을 위한 전역 변수
    private GameObject _stageContainer;
    private Vector2 _lastMoveInput;

    private void Awake()
    {
        if (GridPrefab != null)
        {
            // 1. 0의 위치에서 생성
            GenerateGrid();

            // 2. 0에서 _riseAmount 만큼 위로 올라가는 연출 시작
            StartCoroutine(AnimateGrid());
        }
        else
        {
            Debug.LogError("GridPrefab is not assigned in the inspector.");
        }
    }

    private void Start()
    {
        CalculateCameraLimits();
        _gridCamPoint.position = new Vector3(_leftBottomLocation.x + (_columns * _gridSize) / 2 - _gridSize / 2, _leftBottomLocation.y, _limitMinZ);
        _followCamera.Follow = _gridCamPoint;
        _gridFirstCamPoint = _gridCamPoint;

        _maxZoom = (_columns * 2f) + 30f;
        _followCamera.Lens.FieldOfView = _maxZoom;
        _targetFOV = _followCamera.Lens.FieldOfView;
    }

    private void Update()
    {
        if (InputManager.Instance == null) return;

        // 1. 이동 로직
        Vector2 currentMove = InputManager.Instance.Move;
        Vector3 moveInput = Vector3.zero;

        if (currentMove.x > 0 && _lastMoveInput.x <= 0) moveInput += Vector3.right;
        else if (currentMove.x < 0 && _lastMoveInput.x >= 0) moveInput += Vector3.left;

        if (currentMove.y > 0 && _lastMoveInput.y <= 0) moveInput += Vector3.forward;
        else if (currentMove.y < 0 && _lastMoveInput.y >= 0) moveInput += Vector3.back;

        _lastMoveInput = currentMove;

        if (moveInput != Vector3.zero)
        {
            Vector3 targetPosition = _gridCamPoint.position + (moveInput * _gridSize);
            targetPosition.x = Mathf.Clamp(targetPosition.x, _limitMinX, _limitMaxX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, _limitMinZ, _limitMaxZ);
            _gridCamPoint.position = new Vector3(targetPosition.x, _gridCamPoint.position.y, targetPosition.z);
        }

        // 2. 줌(Scroll) 로직
        float scroll = InputManager.Instance.Scroll;
        if (scroll != 0f)
        {
            float scrollDir = Mathf.Sign(scroll);
            LensSettings lens = _followCamera.Lens;

            if (scrollDir > 0) lens.FieldOfView -= _zoomStepAmount;
            else if (scrollDir < 0) lens.FieldOfView += _zoomStepAmount;

            lens.FieldOfView = Mathf.Clamp(lens.FieldOfView, _minZoom, _maxZoom);
            _followCamera.Lens = lens;
        }
    }

    // ★ 기능 분리: 타일 인스턴스화 담당
    private void GenerateGrid()
    {
        _stageContainer = new GameObject("StageContainer");
        _stageContainer.transform.SetParent(this.transform);

        // 컨테이너의 시작 위치는 정확히 0
        _stageContainer.transform.localPosition = Vector3.zero;

        for (int i = 0; i < _columns; i++)
        {
            for (int j = 0; j < _rows; j++)
            {
                float randomRotation = UnityEngine.Random.Range(0, 4) * 90;
                GameObject TileObj = Instantiate(GridPrefab,
                    new Vector3(_leftBottomLocation.x + i * _gridSize, _leftBottomLocation.y, _leftBottomLocation.z + j * _gridSize),
                    Quaternion.Euler(0, randomRotation, 0));

                TileObj.transform.SetParent(_stageContainer.transform);

                if (TileObj.TryGetComponent(out Grid grid))
                {
                    grid.SetGridCoordinate(i, j);
                }
            }
        }

        // (이전에 있던 밑으로 끌어내리는 코드는 완전히 삭제했습니다.)
    }

    // ★ 기능 분리: 0에서 시작하여 위로(_riseAmount) 솟아오르는 연출
    private IEnumerator AnimateGrid()
    {
        if (_stageContainer == null) yield break;

        // 시작 지점: 본래 위치인 0 (_leftBottomLocation.y 기준)
        Vector3 startPos = Vector3.zero;

        // 목표 지점: 0에서 _riseAmount 만큼 위로 올라간 위치
        Vector3 endPos = new Vector3(0, _riseAmount, 0);

        // 첫 프레임 강제 고정
        _stageContainer.transform.localPosition = startPos;

        float elapsed = 0f;
        while (elapsed < _riseDuration)
        {
            float t = Mathf.Clamp01(elapsed / _riseDuration);
            float easeT = t * t * (3f - 2f * t);

            _stageContainer.transform.localPosition = Vector3.Lerp(startPos, endPos, easeT);

            yield return null;
            elapsed += Time.deltaTime;
        }

        // 루프가 끝나면 목표 높이에 정확히 안착
        _stageContainer.transform.localPosition = endPos;

        // ★ 애니메이션 종료 후 아우라 생성
        GenerateBorderAuras();
    }

    private void CalculateCameraLimits()
    {
        _limitMinX = 0f;
        _limitMaxX = (_columns - 1) * _gridSize;
        _limitMinZ = (_rows / 2f) * _gridSize;
        _limitMaxZ = _limitMinZ + (_rows - 1) * _gridSize;
    }

    private void GenerateBorderAuras()
    {
        if (_borderAuraPrefab == null) return;

        float halfScale = _gridSize / 2f;

        // ★ 핵심: 상승 연출이 끝나면 스테이지의 최종 Y축 위치는 _leftBottomLocation.y + _riseAmount 가 됩니다.
        // 아우라도 그 위에 딱 맞게 생성되도록 수정했습니다.
        float finalYPos = _leftBottomLocation.y + _riseAmount + 0.5f;

        Vector3 leftPos = new Vector3(_leftBottomLocation.x - halfScale, finalYPos, _leftBottomLocation.z + (_rows - 1) * _gridSize / 2f);
        CreateAuraLine(leftPos, Quaternion.Euler(0, -90, 0), _rows, _gridSize);

        Vector3 rightPos = new Vector3(_leftBottomLocation.x + (_columns - 1) * _gridSize + halfScale, finalYPos, _leftBottomLocation.z + (_rows - 1) * _gridSize / 2f);
        CreateAuraLine(rightPos, Quaternion.Euler(0, 90, 0), _rows, _gridSize);

        Vector3 topPos = new Vector3(_leftBottomLocation.x + (_columns - 1) * _gridSize / 2f, finalYPos, _leftBottomLocation.z + (_rows - 1) * _gridSize + halfScale);
        CreateAuraLine(topPos, Quaternion.Euler(0, 0, 0), _columns, _gridSize);

        Vector3 bottomPos = new Vector3(_leftBottomLocation.x + (_columns - 1) * _gridSize / 2f, finalYPos, _leftBottomLocation.z - halfScale);
        CreateAuraLine(bottomPos, Quaternion.Euler(0, 180, 0), _columns, _gridSize);
    }

    private void CreateAuraLine(Vector3 pos, Quaternion rot, int lengthInTiles, float scale)
    {
        ParticleSystem auraParent = Instantiate(_borderAuraPrefab, pos, rot, this.transform);
        float totalLength = lengthInTiles * scale;
        ParticleSystem[] allAuras = auraParent.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem aura in allAuras)
        {
            var shape = aura.shape;
            shape.radius = totalLength / 2f;

            float baseRate = aura.emission.rateOverTime.constant;
            var emission = aura.emission;
            emission.rateOverTime = baseRate * totalLength;

            var main = aura.main;
            main.maxParticles = Mathf.CeilToInt(baseRate * totalLength * 3f);
        }
        auraParent.Play(true);
    }
}