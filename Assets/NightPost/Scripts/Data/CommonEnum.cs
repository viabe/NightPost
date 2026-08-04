
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
    // 새로 도착
    New,
    // 대기중
    Waiting,
    // 배달중
    Delivering,
    // 완료
    Completed
}
public enum EFacilityEffectType
{
    None = 0,
    DeliveryTimeReduction = 2,
    LetterCapacityIncrease = 3
}
public enum EBGMType
{
    None,
    Morning,
    Day,
    Night
}
public enum ESFXType
{
    None,

    // 전령새와 편지 도착
    MessengerBirdArrive,

    // 편지와 종이
    LetterPickup,
    EnvelopeOpen,
    PaperUnfold,
    PaperPageTurn,
    PaperFold,
    LetterClose,
    StampInk,
    StampPress,
    LetterSortPlace,
    ReplyArrive,
    ReportOpen,

    // 배달
    CourierSelect,
    RouteMapOpen,
    DeliveryAssign,
    BicycleBell,
    DeliveryDepart,
    DeliveryComplete,
    DeliveryFail,
    RewardCollect,

    // 우체국 사물
    DrawerOpen,
    DrawerClose,
    MailboxOpen,
    MailboxClose,
    DeskObjectPlace,
    CurrencyGain,
    FacilityUpgrade,
    ContentUnlock,

    // 공통 UI
    UIClick,
    UIConfirm,
    UICancel,
    UIPopupOpen,
    UIPopupClose,
    UITabChange,
    UIError
}
