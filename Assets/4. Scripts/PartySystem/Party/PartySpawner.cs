using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class PartySpawner : MonoBehaviour
{
    // 1. 추 후에, GameObject => PartyMemeber로 변경할 예정
    // 2. PartySpwaner에게 PartyMember의 정보를 넘겨주는 스크립트 작성 필요 (ex. PartyMemberHandler)
    // 3. SerializeField를 삭제 할 예정.

    [SerializeField]
    private CinemachineCamera _followCam;
    public Vector3 _partyOffset;

    [Space(10)]

    [SerializeField]
    private ChampData[] _champDatas;
    private Dictionary<int, ChampData> _champDataDic = new Dictionary<int, ChampData>();

    private GameObject[] _partyMembers = new GameObject[4];
    [SerializeField]
    private PartyMovement _champGroup;
    [SerializeField]
    private Transform _spawnTransform;

    [SerializeField]
    private int[] TestPartyIndex;

    private void Awake()
    {

        // 1. 멤버 도서관 준비 (멤버ID, 멤버프리팹)
        foreach (var champ in _champDatas)
        {
            _champDataDic.Add(champ.ChampID, champ);
        }

        // 임시 테스트
        SpawnMembers(TestPartyIndex);
    }


    // 1-1. SetMember 이전에, 스폰 할 챔프 데이터를 받고 스폰
    public void SpawnMembers(int[] memberID)
    {
        float offSetX = 0;
        float offSetZ = 0;
        for (int i = 0; i < memberID.Length; i++)
        {
            // 1-2. 해당 칸에 멤버가 없음.
            if (memberID[i] == -1)
            {
                // 리더인 경우
                if (i == 0)
                {
                    Debug.LogError("리더가 존재하지 않음.챔프 배치 시스템에서 오른쪽 칸 부터 설정하게끔 해야 함.");
                }
                else
                {
                    _partyMembers[i] = null;
                }
            }
            // 1-3. 해당 칸에 멤버가 있음.
            else
            {

                // ** memberID로 플레이어 멤버 찾기 **
                if (_champDataDic.TryGetValue(memberID[i], out ChampData data))
                {
                    // ** 멤버 생성 후 초기화 **
                    _partyMembers[i] = Instantiate(data.ChampPrefab, _champGroup.transform);


                    // ** 추 후, 필요한 턴 베이스 캐릭터 정보 가져 올 예정 TryGetComponent로 **

                    // ** ~~ **


                    // ** 위치 설정 **
                    _partyMembers[i].transform.position = new Vector3(_spawnTransform.position.x + offSetX, 
                        _spawnTransform.position.y + _partyOffset.y, _spawnTransform.position.z + offSetZ);
                    offSetX += _partyOffset.x;
                    offSetZ += _partyOffset.z;
                    _partyMembers[i].transform.rotation = Quaternion.Euler(_followCam.transform.rotation.eulerAngles.x, 0, 0);
                    // **************



                    // ** 리더 세팅 **

                    if (i == 0)
                    {
                        _followCam.Follow = _partyMembers[i].transform;
                    }

                }
            }

        }

        // 2. Party Move 활성화
        _champGroup.enabled = true;
        
        _champGroup.SetFormation(_partyMembers, _partyOffset);
    }


    // 1. 스테이지 클리어 시, 부르는 함수
    public void StageClear()
    {
        for (int i = 0; i < _partyMembers.Length; i++)
        {
            if (_partyMembers[i] != null)
            {
                // 1. 경험치 획득
                // 2. 레벨업 체크

                // 3. 파티 초기화
                if (_partyMembers[i] != null)
                {
                    Destroy(_partyMembers[i]);
                }
            }
        }

        // 2. Party Move 비활성화
        _champGroup.enabled = false;
    }
}
