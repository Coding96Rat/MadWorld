using UnityEngine;
using UnityEngine.Rendering;

public class SpriteSortSetting : MonoBehaviour
{
    private void Awake()
    {
        // 투명 정렬 모드를 '사용자 정의 축(Custom Axis)'으로 변경
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;

        // 기준 축을 Y축(0, 1, 0)으로 설정
        // Y값이 높을수록(위) 뒤에 그려지고, 낮을수록(아래) 앞에 그려짐
        GraphicsSettings.transparencySortAxis = new Vector3(0.0f, 0.0f, 1.0f);
    }
}
