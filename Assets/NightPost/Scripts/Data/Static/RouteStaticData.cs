using UnityEngine;

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
    [SerializeField] private UnlockConditionData unlockCondition = new();

    public int RouteID => routeID;
    public string RouteName => routeName;
    public ERegionType RegionType => regionType;
    public float BaseDeliveryTimeSeconds => baseDeliveryTimeSeconds;
    public ERouteDifficulty Difficulty => difficulty;
    public UnlockConditionData UnlockCondition => unlockCondition;
}
