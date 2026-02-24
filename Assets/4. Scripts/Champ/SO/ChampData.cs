using UnityEngine;



[CreateAssetMenu(fileName = "Champ Data", menuName = "Create Champ Data/New Champ Data", order = int.MaxValue)]
public class ChampData : ScriptableObject
{
    public int ChampID;
    public string ChampName;
    [Space(10)]
    public GameObject ChampPrefab;
    public Sprite ChampThumbnailSprite;


}
