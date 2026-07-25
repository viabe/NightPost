using UnityEngine;

public class FacilityProgressData : ScriptableObject
{
    // 설비 ID
    [SerializeField] private int facilityID = 0;

    // 현재 레벨
    [SerializeField] private int currentLevel = 0;

    public int FacilityID => facilityID;
    public int CurrentLevel => currentLevel;
}

