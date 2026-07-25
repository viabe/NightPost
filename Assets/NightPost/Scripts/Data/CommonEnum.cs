
public enum ELetterUrgency
{
    Normal,
    Urgent
}
public enum ELetterWeight
{
    Light,
    Normal,
    Heavy
}
public enum ERegionType
{
    None,
    // 기본 지역
    Town,
    // 산
    Mountain,
    // 외곽 지역
    Outskirts
}
public enum EVehicleType
{
    None,
    // 도보
    Walking,
    // 자전거
    Bicycle,
    // 긴 노선 / 긴급 편지
    Motorcycle,
    // 무게 조건
    Truck
}
public enum ECourierTraitType
{
    None,
    // 산길 노선 배달 시간 감소
    MountainExpert,
    // 야간 편지 또는 야간 노선 시간 감소
    NightExpert,
    // 긴급 편지 배달 시간 감소
    UrgentExpert,
    // 무거운 편지 시간 페널티 감소
    HeavyLoadExpert
}

public enum ERouteDifficulty
{
    None,
    Easy,
    Normal,
    Hard
}
public enum ELetterProgressState
{
    New,
    Waiting,
    Delivering,
    Completed
}