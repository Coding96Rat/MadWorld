
using UnityEngine;

public struct LeaderPosHistory
{
    public Vector3 Dir;
    public Vector3 Pos;
}

public class PartyMovement : MonoBehaviour
{

    private InputReader _inputReader;

    private ChampMoveController[] _partyMembers = new ChampMoveController[4];

    private void Awake()
    {
        _inputReader = GetComponent<InputReader>();
    }


    // Update is called once per frame
    void Update()
    {
        if (_inputReader.MoveInput != Vector2.zero)
        {
            // 1. 리더 이동
            _partyMembers[0].LeaderMove(new Vector3(_inputReader.MoveInput.x, 0, _inputReader.MoveInput.y));

        }
    }

    public void SetFormation(GameObject[] partyMembers, Vector3 partyOffset)
    {
        for (int i = 0; i < partyMembers.Length; i++)
        {
            if (partyMembers[i] != null)
            {
                if (partyMembers[i].TryGetComponent<ChampMoveController>(out ChampMoveController memberMoveCS))
                {
                    _partyMembers[i] = memberMoveCS;
                    Debug.Log("Saved");
                }

                if (i == 0)
                {
                    _partyMembers[0].LeaderChamp = true;
                }

                else
                {
                    _partyMembers[i].FollowerSet(_partyMembers[i - 1].transform, partyOffset.magnitude);
                }
            }
        }
    }

}
