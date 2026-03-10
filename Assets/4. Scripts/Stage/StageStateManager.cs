using UnityEngine;

public class StageStateManager : MonoBehaviour
{
    public bool IsEnemyExist = false;

    private InGameUIHandler _inGameUIHandler;
    private MainStageHandler _mainStageHandler;


    private void Awake()
    {
        _inGameUIHandler = FindFirstObjectByType<InGameUIHandler>();

        _mainStageHandler.OnScreenBlackout += () =>
        {
            EnvironmentSpawner _environmentSpawner = FindFirstObjectByType<EnvironmentSpawner>();
            _environmentSpawner.SpawnEnvironment();
        };
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
