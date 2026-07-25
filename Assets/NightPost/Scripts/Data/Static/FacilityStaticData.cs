using UnityEngine;
[System.Serializable]
public class FacilityLevelData
{
    [SerializeField] private int upgradeCost = 0;
    [SerializeField, Range(0f, 1f)] private float deliveryTimeReductionRate = 0.0f;

    public int UpgradeCost => upgradeCost;
    public float DeliveryTimeReductionRate => deliveryTimeReductionRate;
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
    [SerializeField] private FacilityLevelData[] levelData;
        
    public int FacilityID => facilityID;
    public string FacilityName => facilityName;
    public string Description => description;   
    public FacilityLevelData[] LevelData => levelData;
    public int MaxLevel => levelData.Length > 0 ? levelData.Length - 1 : 0;
}
