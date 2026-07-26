using UnityEngine;

// 해금 조건 
[System.Serializable]
public class RouteUnlockConditionData
{
    // 게임 시작 시 기본 해금 여부
    [SerializeField] private bool unlockedByDefault = false;
    // 필요한 누적 배달 완료 수
    [SerializeField, Min(0)] private int requiredCompletedDeliveryCount = 0;

    public bool IsUnlockedByDefault => unlockedByDefault;
    public int RequiredCompletedDeliveryCount => requiredCompletedDeliveryCount;
}
[CreateAssetMenu(fileName = "Route_", menuName = "NightPost/Static Data/Route")]
public class RouteStaticData : ScriptableObject
{
    // 노선 ID
    [SerializeField] private int routeID = 0;
    // 노선 이름
    [SerializeField] private string routeName = string.Empty;
    // 지역
    [SerializeField] private ERegionType regionType = ERegionType.None;
    // 기본 소요 시간
    [SerializeField] private float baseDeliveryTimeSeconds = 1f;
    // 난이도
    [SerializeField] private ERouteDifficulty difficulty = ERouteDifficulty.None;
    // 해금 조건
    [SerializeField] private RouteUnlockConditionData unlockCondition = new RouteUnlockConditionData();

    public int RouteID => routeID;
    public string RouteName => routeName;
    public ERegionType RegionType => regionType;
    public float BaseDeliveryTimeSeconds => baseDeliveryTimeSeconds;
    public ERouteDifficulty Difficulty => difficulty;
    public RouteUnlockConditionData UnlockCondition => unlockCondition;
}
