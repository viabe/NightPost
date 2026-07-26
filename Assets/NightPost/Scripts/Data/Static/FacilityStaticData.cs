using UnityEngine;
[System.Serializable]
public class FacilityLevelData
{
    // 레벨 
    [SerializeField, Min(1)] private int level = 1;
    // 업그레이드 비용
    [SerializeField] private int upgradeCost = 0;
    // 업그레이드 되는 효과
    [SerializeField]private EFacilityEffectType effectType = EFacilityEffectType.None;
    // 업그레이드 값
    [SerializeField, Min(0f)] private float effectValue = 0f;
    [SerializeField] private EVehicleType unlockedVehicle = EVehicleType.None;

    public int Level => level;
    public int UpgradeCost => upgradeCost;
    public EFacilityEffectType EffectType => effectType;
    public float EffectValue => effectValue;
    public EVehicleType UnlockedVehicle => unlockedVehicle;
}
[CreateAssetMenu(fileName = "Facility_", menuName = "NightPost/Static Data/Facility")]
public class FacilityStaticData : ScriptableObject
{
    // 설비 ID
    [SerializeField] private int facilityID = 0;
    // 이름
    [SerializeField] private string facilityName = string.Empty;
    // 설명
    [SerializeField] private string description = string.Empty;
    // 레벨 데이터
    [SerializeField] private FacilityLevelData[] levelData = new FacilityLevelData[0];

    public int FacilityID => facilityID;
    public string FacilityName => facilityName;
    public string Description => description;   
    public FacilityLevelData[] LevelData => levelData;
    public int MaxLevel => levelData == null ? 0 : levelData.Length;
}
