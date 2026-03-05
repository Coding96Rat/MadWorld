using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class EnvironmentGenerator : MonoBehaviour
{
    [Header("필수 참조 (에디터에서 직접 할당)")]
    public GameObject gridPrefab;
    public GameObject wallPrefab;

    [Header("메인 스테이지 기준 설정")]
    public int mainColumns = 10;
    public int mainRows = 10;
    public float gridSize = 1f;
    public Vector3 startLocation = Vector3.zero;

    [Header("환경 타일 설정")]
    public float envYOffset = -0.45f;
    public int expandRadius = 6;

    [Header("외곽선 벽 설정")]
    public float wallYPos = 7f;

    [Header("생성된 오브젝트 부모")]
    public Transform environmentParent;
    public Transform wallParent;

    public void BakeEnvironment()
    {
        if (gridPrefab == null || wallPrefab == null)
        {
            Debug.LogError("[EnvironmentGenerator] Grid 또는 Wall 프리팹이 할당되지 않았습니다!");
            return;
        }

        ClearEnvironment();
        GenerateContainers();

        // 1. 생성될 환경 타일의 좌표를 먼저 기록
        HashSet<Vector2Int> envTiles = new HashSet<Vector2Int>();
        int minX = -expandRadius;
        int maxX = mainColumns + expandRadius;
        int minZ = -expandRadius;
        int maxZ = mainRows + expandRadius;

        for (int x = minX; x < maxX; x++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                // 메인 스테이지 안쪽 패스
                if (x >= 0 && x < mainColumns && z >= 0 && z < mainRows) continue;

                float dx = Mathf.Max(0, 0 - x, x - (mainColumns - 1));
                float dz = Mathf.Max(0, 0 - z, z - (mainRows - 1));
                float distance = Mathf.Sqrt(dx * dx + dz * dz);

                if (distance <= expandRadius)
                {
                    envTiles.Add(new Vector2Int(x, z));
                }
            }
        }

        // 2. 타일 생성 및 진짜 외곽선 판별
        foreach (Vector2Int pos in envTiles)
        {
            // 환경 타일은 정상적으로 모두 생성
            SpawnEnvironmentTile(pos.x, pos.y, gridSize, startLocation);

            // [조건 1] Z좌표가 -1 이하(화면 앞쪽)인 곳은 벽 생성 안 함
            if (pos.y <= -1) continue;

            // [조건 2] 중앙 메인 스테이지와 닿는 부분(안쪽 경계선)은 무시하고, '진짜 바깥쪽 외곽선'인지 판별
            bool isOuterEdge = !IsTileValidOrMainStage(pos.x + 1, pos.y) ||
                               !IsTileValidOrMainStage(pos.x - 1, pos.y) ||
                               !IsTileValidOrMainStage(pos.x, pos.y + 1) ||
                               !IsTileValidOrMainStage(pos.x, pos.y - 1);

            // 바깥쪽 외곽선에만 벽 생성
            if (isOuterEdge)
            {
                SpawnWall(pos.x, pos.y, gridSize, startLocation);
            }
        }

        Debug.Log("[EnvironmentGenerator] 주변 타일 및 외곽선 벽 베이크 완료!");
    }

    // ★ 헬퍼 함수: 특정 좌표가 환경 타일이거나 '메인 스테이지'에 속하는지 확인
    private bool IsTileValidOrMainStage(int x, int z)
    {
        // 1. 메인 스테이지 영역이면 유효한(비어있지 않은) 공간으로 취급 (안쪽 벽 생성 방지)
        if (x >= 0 && x < mainColumns && z >= 0 && z < mainRows) return true;

        // 2. 환경 타일 영역에 속하는지 확인
        float dx = Mathf.Max(0, 0 - x, x - (mainColumns - 1));
        float dz = Mathf.Max(0, 0 - z, z - (mainRows - 1));
        float distance = Mathf.Sqrt(dx * dx + dz * dz);

        return distance <= expandRadius;
    }

    private void GenerateContainers()
    {
        if (environmentParent == null)
        {
            environmentParent = new GameObject("EnvironmentContainer").transform;
            environmentParent.SetParent(this.transform);
        }

        if (wallParent == null)
        {
            wallParent = new GameObject("WallContainer").transform;
            wallParent.SetParent(this.transform);
        }
    }

    private void SpawnEnvironmentTile(int gridX, int gridZ, float scale, Vector3 startLoc)
    {
        Vector3 spawnPos = new Vector3(
            startLoc.x + gridX * scale,
            startLoc.y + envYOffset,
            startLoc.z + gridZ * scale
        );

        float randomRotation = UnityEngine.Random.Range(0, 4) * 90f;
        InstantiatePrefab(gridPrefab, spawnPos, Quaternion.Euler(0, randomRotation, 0), environmentParent);
    }

    private void SpawnWall(int gridX, int gridZ, float scale, Vector3 startLoc)
    {
        Vector3 spawnPos = new Vector3(
            startLoc.x + gridX * scale,
            wallYPos,
            startLoc.z + gridZ * scale
        );

        GameObject wallObj = InstantiatePrefab(wallPrefab, spawnPos, Quaternion.identity, wallParent);

        if (wallObj != null && wallObj.TryGetComponent(out Renderer renderer))
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            mpb.SetVector("_Offset", new Vector2(gridX, gridZ));
            renderer.SetPropertyBlock(mpb);
        }
    }

    private GameObject InstantiatePrefab(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
    {
#if UNITY_EDITOR
        GameObject obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
        obj.transform.position = pos;
        obj.transform.rotation = rot;
        obj.transform.SetParent(parent);
        return obj;
#else
        return Instantiate(prefab, pos, rot, parent);
#endif
    }

    public void ClearEnvironment()
    {
        if (environmentParent != null)
        {
            for (int i = environmentParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(environmentParent.GetChild(i).gameObject);
            }
        }

        if (wallParent != null)
        {
            for (int i = wallParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(wallParent.GetChild(i).gameObject);
            }
        }
    }
}