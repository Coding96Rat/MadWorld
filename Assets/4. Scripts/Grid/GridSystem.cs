using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

// 이 스크립트를 붙이면 MainStageHandler도 자동으로 같이 붙습니다!
[RequireComponent(typeof(MainStageHandler))]
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

    [Header("Stage Animation Settings (초기 등장 연출)")]
    [SerializeField] private float _riseAmount = 0.45f;
    [SerializeField] private float _riseDuration = 1.5f;

    [Header("Dark Aura Settings")]
    [SerializeField] private ParticleSystem _borderAuraPrefab;

    private float _limitMinX, _limitMaxX, _limitMinZ, _limitMaxZ;
    private GameObject _stageContainer;
    private Vector2 _lastMoveInput;

    private void Awake()
    {
        if (GridPrefab != null)
        {
            GenerateGrid();
        }
        else
        {
            Debug.LogError("GridPrefab is not assigned.");
        }
    }

    private void Start()
    {
        CalculateCameraLimits();
        _gridCamPoint.position = new Vector3(_leftBottomLocation.x + (_columns * _gridSize) / 2 - _gridSize / 2, _leftBottomLocation.y, _limitMinZ);
        _followCamera.Follow = _gridCamPoint;
        _gridFirstCamPoint = _gridCamPoint;


        // 2. 새로 추가할 부분! (계산이 끝난 이 '정중앙 절대좌표'를 핸들러에 주입합니다)
        if (TryGetComponent(out MainStageHandler handler))
        {
            handler.SetDefaultCameraPosition(_gridCamPoint.position);
        }

        _maxZoom = (_columns * 2f) + 30f;
        _followCamera.Lens.FieldOfView = _maxZoom;
        _targetFOV = _followCamera.Lens.FieldOfView;
    }

    private void Update()
    {
        if (InputManager.Instance == null) return;

        // 이동 로직
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

        // 줌 로직
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

    private void GenerateGrid()
    {
        _stageContainer = new GameObject("StageContainer");
        _stageContainer.transform.SetParent(this.transform);
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

        // ★ 핵심: 그리드 생성이 끝나면, 같은 오브젝트에 붙어있는 MainStageHandler를 찾아 연결해 줍니다!
        if (TryGetComponent(out MainStageHandler handler))
        {
            handler.Initialize(_stageContainer.transform, _gridCamPoint, _followCamera);
        }
    }

    private IEnumerator AnimateGrid()
    {
        if (_stageContainer == null) yield break;

        Vector3 startPos = Vector3.zero;
        Vector3 endPos = new Vector3(0, _riseAmount, 0);

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

        _stageContainer.transform.localPosition = endPos;
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
            var emission = aura.emission;
            float baseRate = emission.rateOverTime.constant;
            emission.rateOverTime = baseRate * totalLength;
            var main = aura.main;
            main.maxParticles = Mathf.CeilToInt(baseRate * totalLength * 3f);
        }
        auraParent.Play(true);
    }

    public void Fight()
    {
        StartCoroutine(AnimateGrid());
    }
}