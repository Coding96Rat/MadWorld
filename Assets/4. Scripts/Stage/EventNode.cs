using UnityEngine;

public class EventNode : MonoBehaviour
{
    [Tooltip("이 위치에 고정으로 등장할 이벤트 타입을 설정하세요.")]
    public EventType designatedType = EventType.None;

    private void OnDrawGizmos()
    {
        // 설정된 타입에 따라 씬 뷰에 보여질 색상을 다르게 지정합니다.
        switch (designatedType)
        {
            case EventType.Enemy:
            case EventType.Boss:
                Gizmos.color = Color.red;
                break;
            case EventType.SignPost:
                Gizmos.color = Color.blue;
                break;
            case EventType.Trap:
                Gizmos.color = Color.yellow;
                break;
            case EventType.Treasure: // 이름 변경 반영됨
                Gizmos.color = Color.green;
                break;

            // 새로 추가된 이벤트들 
            case EventType.Hero:
                Gizmos.color = Color.cyan; // 영웅 이벤트는 눈에 띄는 밝은 청록색
                break;
            case EventType.Shelter:
                // Color.orange를 써도 되지만, RGB 값을 직접 넣어 더 선명한 주황색으로 만들 수도 있습니다.
                Gizmos.color = new Color(1f, 0.5f, 0f);
                break;

            case EventType.None:
            default:
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // 빈 공간은 반투명 회색
                break;
        }

        // 해당 오브젝트 위치에 반지름 0.5짜리 구슬을 그립니다.
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}