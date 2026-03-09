using UnityEngine;

public class StageStateManager : MonoBehaviour
{
    public bool IsEnemyExist = false;

    private InGameUIHandler _inGameUIHandler;

    private void Awake()
    {
        _inGameUIHandler = FindFirstObjectByType<InGameUIHandler>();
    }

    // 맵 이동 시 도착 후 발동 함수
    public void ElevatorArrived()
    {
        IsEnemyExist = true;
        _inGameUIHandler.ElevatorArrived();
    }

    public void StageClear()
    {

    }
}
