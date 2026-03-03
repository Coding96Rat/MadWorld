using UnityEngine;

public class Grid : MonoBehaviour
{
    [SerializeField]
    private int visited = 1;
    [SerializeField]
    private int x = 0;
    [SerializeField]
    private int y = 0;


    public void SetGridCoordinate(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}
