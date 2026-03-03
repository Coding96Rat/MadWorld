using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentGenerator : MonoBehaviour
{
    private GridSystem _gridSystem;
    private GameObject _gridPrefab;
    private GameObject _gridSidePrefab;
    private Vector3 _gridLeftBottomLocation;

    [Header("환경 확장 설정")]
    [SerializeField, Tooltip("좌우로 확장할 단계(칸) 수")]
    private int _expandColumn = 5;
    [SerializeField, Tooltip("위(상단)로 확장할 단계(칸) 수")]
    private int _expandRow = 8;

    [Header("시야 차광(어둡기) 설정")]
    [SerializeField, Range(0f, 1f), Tooltip("메인 그리드와 맞닿은 첫 레이어의 밝기 (1=원래색, 0=검정)")]
    private float _startBrightness = 0.5f;
    [SerializeField, Range(0f, 1f), Tooltip("가장 바깥쪽 레이어의 밝기")]
    private float _endBrightness = 0.1f;
    // ★ 새로 추가된 세기(가속도) 조절 변수
    [SerializeField, Range(0.1f, 5f), Tooltip("어두워지는 세기. 1=일정함. 1보다 작으면 초반에 급격히 어두워지고, 1보다 크면 끝부분에서 확 어두워집니다.")]
    private float _darknessFalloff = 1f;

    [Header("다크 아우라(경계선 파티클) 설정")]
    [SerializeField, Tooltip("완성한 파티클 시스템 프리팹을 여기에 넣으세요.")]
    private ParticleSystem _borderAuraPrefab;

    [SerializeField, Tooltip("타일 1칸당 파티클 방출량 (현재 100으로 세팅하셨으므로 100)")]
    private float _particleRatePerTile = 100f;

    private void Awake()
    {
        _gridSystem = FindFirstObjectByType<GridSystem>();
        _gridPrefab = _gridSystem.GridPrefab;
        _gridSidePrefab = _gridSystem.GridSidePrefab;
        _gridLeftBottomLocation = _gridSystem._leftBottomLocation;
    }

    private void Start()
    {
        // GridSystem의 메인 그리드 생성이 끝난 직후 호출되도록 설정하세요.
        GenerateEnvironment();
        GenerateBorderAuras();
    }

    public void GenerateEnvironment()
    {
        // GridSystem의 변수들 읽어오기
        int mainCols = _gridSystem._columns;
        int mainRows = _gridSystem._rows;
        float scale = _gridSystem._gridSize;

        int maxSteps = Mathf.Max(_expandColumn, _expandRow);

        for (int step = 1; step <= maxSteps; step++)
        {
            // 1. 기본 진행도 (0.0 ~ 1.0)
            float t = (float)step / maxSteps;

            // 2. ★ 세기(Falloff)를 적용하여 진행도를 곡선으로 변형
            // Mathf.Pow를 사용해 t값을 _darknessFalloff만큼 제곱합니다.
            float curvedT = Mathf.Pow(t, _darknessFalloff);

            // 3. 변형된 곡선 비율을 기반으로 최종 밝기 계산
            float currentBrightness = Mathf.Lerp(_startBrightness, _endBrightness, curvedT);
            Color stepColor = new Color(currentBrightness, currentBrightness, currentBrightness, 1f);

            int currentLeft = -Mathf.Min(step, _expandColumn);
            int currentRight = mainCols - 1 + Mathf.Min(step, _expandColumn);
            int currentTop = mainRows - 1 + Mathf.Min(step, _expandRow);

            // 좌/우측 기둥 생성
            if (step <= _expandColumn)
            {
                for (int z = 0; z <= currentTop; z++)
                {
                    SpawnEnvironmentTile(-step, z, scale, stepColor);
                    SpawnEnvironmentTile(mainCols - 1 + step, z, scale, stepColor);
                }
            }

            // 상단 지붕 생성
            if (step <= _expandRow)
            {
                int startX = (step <= _expandColumn) ? currentLeft + 1 : currentLeft;
                int endX = (step <= _expandColumn) ? currentRight - 1 : currentRight;

                for (int x = startX; x <= endX; x++)
                {
                    SpawnEnvironmentTile(x, currentTop, scale, stepColor);
                }
            }
        }
    }

    private void SpawnEnvironmentTile(int gridX, int gridZ, float scale, Color color)
    {
        Vector3 spawnPos = new Vector3(
            _gridLeftBottomLocation.x + gridX * scale,
            _gridLeftBottomLocation.y,
            _gridLeftBottomLocation.z + gridZ * scale
        );

        float randomRotation = Random.Range(0, 4) * 90f;
        GameObject tileObj = Instantiate(_gridPrefab, spawnPos, Quaternion.Euler(90, randomRotation, 0), this.transform);

        ApplyColor(tileObj, color);

        if (tileObj.TryGetComponent(out Grid grid))
        {
            grid.SetGridCoordinate(gridX, gridZ);
        }

        if (gridZ == 0)
        {
            Vector3 sidePos = new Vector3(spawnPos.x, spawnPos.y - 0.5f, spawnPos.z - 0.5f);
            GameObject sideObj = Instantiate(_gridSidePrefab, sidePos, Quaternion.identity, this.transform);

            sideObj.transform.localScale = new Vector3(scale, 1, 1);

            if (sideObj.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                spriteRenderer.color = color;
                spriteRenderer.material.SetVector("_Tiling", new Vector2(1, 1));
            }
        }
    }

    private static MaterialPropertyBlock _propBlock;

    private void ApplyColor(GameObject obj, Color color)
    {
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

        if (obj.TryGetComponent(out Renderer renderer))
        {
            // 기존 머티리얼을 복제하지 않고 프로퍼티만 덮어씌움
            renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", color); // 셰이더의 메인 컬러 속성명 확인 필요 (보통 _Color 또는 _BaseColor)
            renderer.SetPropertyBlock(_propBlock);
        }
    }
    public void GenerateBorderAuras()
    {
        if (_borderAuraPrefab == null) return;

        int cols = _gridSystem._columns;
        int rows = _gridSystem._rows;
        float scale = _gridSystem._gridSize;

        // 타일의 절반 크기 (중앙에서 경계선으로 밀어내기 위한 오프셋)
        float halfScale = scale / 2f;

        // 1. 왼쪽 경계선 (Z축 방향으로 길게 배치)
        // X좌표: 0에서 왼쪽으로 절반만큼 이동 (-halfScale)
        // Z좌표: 맨 아래 타일과 맨 위 타일의 정중앙 위치
        Vector3 leftPos = new Vector3(
            _gridLeftBottomLocation.x - halfScale,
            _gridLeftBottomLocation.y,
            _gridLeftBottomLocation.z + (rows - 1) * scale / 2f
        );
        CreateAuraLine(leftPos, Quaternion.Euler(0, -90, 0), rows, scale);

        // 2. 오른쪽 경계선 (Z축 방향으로 길게 배치)
        // X좌표: 맨 오른쪽 타일에서 오른쪽으로 절반만큼 이동
        Vector3 rightPos = new Vector3(
            _gridLeftBottomLocation.x + (cols - 1) * scale + halfScale,
            _gridLeftBottomLocation.y,
            _gridLeftBottomLocation.z + (rows - 1) * scale / 2f
        );
        CreateAuraLine(rightPos, Quaternion.Euler(0, 90, 0), rows, scale);

        // 3. 위쪽 경계선 (X축 방향으로 길게 배치)
        // Z좌표: 맨 위 타일에서 위쪽으로 절반만큼 이동
        Vector3 topPos = new Vector3(
            _gridLeftBottomLocation.x + (cols - 1) * scale / 2f,
            _gridLeftBottomLocation.y,
            _gridLeftBottomLocation.z + (rows - 1) * scale + halfScale
        );
        // 위쪽은 X축을 따라 누워야 하므로 Rotation을 (0, 0, 0)으로 줍니다.
        CreateAuraLine(topPos, Quaternion.Euler(0, 0, 0), cols, scale);
    }

    private void CreateAuraLine(Vector3 pos, Quaternion rot, int lengthInTiles, float scale)
    {
        // 1. 지정된 위치에 파티클 프리팹 생성 (부모 객체)
        ParticleSystem auraParent = Instantiate(_borderAuraPrefab, pos, rot, this.transform);

        // 실제 물리적 길이
        float totalLength = lengthInTiles * scale;

        // 2. 부모와 자식에 있는 "모든" 파티클 시스템을 배열로 가져옵니다. ★ 핵심 포인트
        ParticleSystem[] allAuras = auraParent.GetComponentsInChildren<ParticleSystem>();

        // 3. 찾은 모든 파티클(선 파티클 + 번짐 파티클)에 동일한 규칙을 적용합니다.
        foreach (ParticleSystem aura in allAuras)
        {
            // 길이(Radius) 늘리기
            var shape = aura.shape;
            shape.radius = totalLength / 2f;

            // ★ 각 파티클이 원래 가지고 있던 방출량(1칸 기준)을 가져와서 타일 수만큼 곱해줍니다.
            // 이렇게 하면 부모와 자식의 기본 방출량이 달라도 비율이 예쁘게 유지됩니다.
            float baseRate = aura.emission.rateOverTime.constant;
            var emission = aura.emission;
            emission.rateOverTime = baseRate * totalLength;

            // Max Particles(최대 제한) 넉넉하게 늘리기
            var main = aura.main;
            main.maxParticles = Mathf.CeilToInt(baseRate * totalLength * 3f);
        }

        // 4. 세팅이 끝난 후 모든 파티클을 한 번에 재생
        auraParent.Play(true);
    }
}
