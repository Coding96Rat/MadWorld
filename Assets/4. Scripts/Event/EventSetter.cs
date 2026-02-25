using UnityEngine;

[System.Serializable]
public struct EventSpawnRate
{
    public EventType eventType; // 누락되었던 타입 변수 추가

    [Range(0f, 100f)]
    [Header("확률")]
    public float spawnRateAmount;
}

public class EventSetter : MonoBehaviour
{
    [SerializeField]
    [Tooltip("빈 공간(None)에 채워 넣을 이벤트들의 확률을 설정하세요.")]
    private EventSpawnRate[] _spawnRates;

    private void Start()
    {
        // 1. Transform을 거치지 않고 EventNode를 바로 배열로 싹 긁어옵니다. (성능상 더 유리함)
        EventNode[] allNodes = GetComponentsInChildren<EventNode>();

        // 2. 전체 확률 총합 계산 (for문 밖에서 단 1번만 계산하여 성능 최적화)
        float totalWeight = 0f;
        foreach (EventSpawnRate rate in _spawnRates)
        {
            totalWeight += rate.spawnRateAmount;
        }

        // 확률 세팅이 안 되어있으면 에러 방지 후 종료
        if (totalWeight <= 0f) return;

        // 3. 긁어온 모든 노드를 순회하며 빈칸 채우기
        foreach (EventNode node in allNodes)
        {
            // 수동으로 할당해둔 노드(None이 아닌 것)는 건너뜁니다.
            if (node.designatedType != EventType.None) continue;

            // --- 여기부터는 빈칸(None)인 노드만 실행됨 ---

            float randomValue = Random.Range(0f, totalWeight);
            float cumulativeChance = 0f;

            foreach (EventSpawnRate rate in _spawnRates)
            {
                cumulativeChance += rate.spawnRateAmount;

                if (randomValue <= cumulativeChance)
                {
                    // [핵심] 프리팹 생성이 아니라, 노드의 타입을 결정해줍니다.
                    node.designatedType = rate.eventType;
                    break;
                }
            }
        }
    }
}