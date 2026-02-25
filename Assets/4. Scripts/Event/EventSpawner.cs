using UnityEngine;


public enum EventType 
{ 
    None,
    Enemy, // 1
    SignPost, // 2
    Trap, // 3
    Treasure, // 4
    Hero,
    Shelter,
    Boss,
}


public class EventSpawner : MonoBehaviour
{
    public GameObject[] EventSpawnPrefab;

    private EventNode[] _currentStageEvents;

    private void Awake()
    {
        _currentStageEvents = FindAnyObjectByType<StageInformation>().EventNodes;
    }
}
