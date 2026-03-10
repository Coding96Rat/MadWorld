using UnityEngine;

public class EnvironmentSpawner : MonoBehaviour
{
    public GameObject TopFloor;
    public GameObject CurrentEnvironment;
    [SerializeField]
    private GameObject Floor1_10;
    public void SpawnEnvironment()
    {
        if (Floor1_10 != null)
        {
            if(CurrentEnvironment != null)
            {
                CurrentEnvironment.SetActive(false);
            }
            CurrentEnvironment = Instantiate(Floor1_10, this.transform);
        }
        else
        {
            Debug.LogWarning("Environment prefab is null. Please assign a valid prefab.");
        }
    }
}
