using UnityEngine;

[CreateAssetMenu(fileName = "LetterStaticData", menuName = "NightPost/Static Data/Letter")]
public class LetterStaticData : ScriptableObject
{
    // 편지 ID
    [SerializeField, Min(1)] private int letterID = 1;
    // 발신자
    [SerializeField] private string senderName = string.Empty;
    // 편지 제목
    [SerializeField] private string letterTitle = string.Empty;
    // 편지 본문
    [SerializeField] private string letterBody = string.Empty;
    // 편지 목적지
    [SerializeField] private ERegionType destinationRegion = ERegionType.None;
    // 편지 긴급도
    [SerializeField] ELetterUrgency urgency = ELetterUrgency.Normal;
    // 편지 무게
    [SerializeField] ELetterWeight weight = ELetterWeight.Normal;
    // 기본 보상
    [SerializeField] private int letterReward = 10;

    public int LetterID => letterID;
    public string SenderName => senderName;
    public string LetterTitle => letterTitle;
    public string LetterBody => letterBody;
    public ERegionType DestinationRegion => destinationRegion;
    public ELetterUrgency Urgency => urgency;
    public ELetterWeight Weight => weight;
    public int LetterReward => letterReward;
}
