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
    public GameObject GridSidePrefab;
    [Space(10)]
    [Header("Grid Camera Setting")]
    [SerializeField]
    private CinemachineCamera _followCamera;
    [SerializeField]
    private Transform _gridCamPoint;
    private Transform _gridFirstCamPoint;
    [SerializeField]
    private float _camSpeed = 10f;
    [Space(5)]
    [SerializeField] private float _zoomSpeed = 10f;
    [SerializeField] private float _zoomSmoothSpeed = 10f; // 줌 부드러움 정도 (값이 작을수록 미끄러지듯 멈춤)
    private float _targetFOV; // 마우스 휠로 결정될 '목표' 시야각
    [SerializeField] private float _minZoom = 5f;
    [SerializeField] private float _maxZoom = 20f;
    [SerializeField] private float _zoomStepAmount = 5f; // 한 번 스크롤 시 변하는 줌 크기 (딱딱 끊기게)

    // 더 이상 인스펙터에서 설정하지 않고 코드에서 계산됩니다.
    private float _limitMinX;
    private float _limitMaxX;
    private float _limitMinZ;
    private float _limitMaxZ;

    private void Awake()
    {
        if (GridPrefab != null)
        {
            GenerateGrid();
           
        }
        else
        {
            Debug.LogError("GridPrefab is not assigned in the inspector.");
        }

        
    }

    private void Start()
    {
        // 시작할 때 제한 구역을 계산합니다.
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
        // 1. 입력 확인 (GetKey -> GetKeyDown으로 변경하여 한 번 누를 때 딱 한 칸만 이동)
        Vector3 moveInput = Vector3.zero;

        // 대각선 이동을 막고 상하좌우 중 한 방향으로만 딱딱 움직이도록 else if 사용
        if (Input.GetKeyDown(KeyCode.D)) moveInput += Vector3.right;
        else if (Input.GetKeyDown(KeyCode.A)) moveInput += Vector3.left;
        else if (Input.GetKeyDown(KeyCode.W)) moveInput += Vector3.forward;
        else if (Input.GetKeyDown(KeyCode.S)) moveInput += Vector3.back;

        // 2. 이동 적용 및 구역 제한 (입력이 있을 때만 실행)
        if (moveInput != Vector3.zero)
        {
            // 현재 위치에서 그리드 사이즈만큼 이동했을 때의 '목표 위치'를 먼저 계산
            Vector3 targetPosition = _gridCamPoint.position + (moveInput * _gridSize);

            // 목표 위치 자체가 인스펙터에서 설정한 제한 구역을 넘지 않도록 Clamp 처리
            targetPosition.x = Mathf.Clamp(targetPosition.x, _limitMinX, _limitMaxX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, _limitMinZ, _limitMaxZ);

            // 딱딱 끊어지게끔 목표 위치로 즉시 이동
            _gridCamPoint.position = new Vector3(targetPosition.x, _gridCamPoint.position.y, targetPosition.z);
        }

        // 4. 스무스를 뺀 딱딱 끊어지는 스텝 방식의 줌 인/아웃
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            LensSettings lens = _followCamera.Lens;

            // 스크롤 방향에 따라 _zoomStepAmount 만큼 즉시 가감
            if (scroll > 0) // 마우스 휠 위로 (줌 인)
            {
                lens.FieldOfView -= _zoomStepAmount;
            }
            else if (scroll < 0) // 마우스 휠 아래로 (줌 아웃)
            {
                lens.FieldOfView += _zoomStepAmount;
            }

            // Min과 Max를 넘어가지 않도록 가두기
            lens.FieldOfView = Mathf.Clamp(lens.FieldOfView, _minZoom, _maxZoom);

            // 변경된 값 적용
            _followCamera.Lens = lens;
        }
    }


    private void GenerateGrid()
    {
        for (int i = 0; i < _columns; i++)
        {
            for (int j = 0; j < _rows; j++)
            {
                float randomRotation = Random.Range(0, 4) * 90; // Random rotation in multiples of 90 degrees
                GameObject TileObj = Instantiate(GridPrefab,
                    new Vector3(_leftBottomLocation.x + i * _gridSize, _leftBottomLocation.y,
                                                            _leftBottomLocation.z + j * _gridSize), Quaternion.Euler(90, randomRotation, 0));
                TileObj.transform.SetParent(gameObject.transform);
                if (TileObj.TryGetComponent(out Grid grid))
                {
                    grid.SetGridCoordinate(i, j);
                }
            }
        }

        // [수정된 부분] (_columns - 1) / 2f * _gridSize 을 사용하여 짝수/홀수 모두 정확한 중앙을 잡습니다.
        float centerX = _leftBottomLocation.x + ((_columns - 1) / 2f) * _gridSize;
        GameObject TileSideObj = Instantiate(GridSidePrefab, new Vector3(centerX, _leftBottomLocation.y - 0.5f, _leftBottomLocation.z - 0.5f), Quaternion.identity);

        if (TileSideObj.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.material.SetVector("_Tiling", new Vector2(_columns, 1));
        }

        TileSideObj.transform.localScale = new Vector3(_columns * _gridSize, 1, 1);
        TileSideObj.transform.SetParent(gameObject.transform);


    }

    // 맵 크기에 맞춰 카메라 이동 제한 구역을 수학적으로 계산하는 함수
    private void CalculateCameraLimits()
    {
        // X축: 0부터 (Columns - 1)까지
        _limitMinX = 0f;
        _limitMaxX = (_columns - 1) * _gridSize;

        // Z축: 시작점(오프셋)은 Rows의 절반, 최대점은 시작점 + (Rows - 1)
        _limitMinZ = (_rows / 2f) * _gridSize; // 6 / 2 = 3
        _limitMaxZ = _limitMinZ + (_rows - 1) * _gridSize; // 3 + 5 = 8
    }
}
