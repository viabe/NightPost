using UnityEngine;

[System.Serializable]
public class FacilityProgressData 
{
    // 설비 ID
    [SerializeField] private int facilityID = 0;

    // 현재 레벨
    [SerializeField] private int currentLevel = 0;
    public FacilityProgressData(int facilityID)
    {
        this.facilityID = facilityID;
        currentLevel = 0;
    }
    /// <summary>
    /// 저장 데이터에서 불러온 시설 진행 상태를 복원함
    /// </summary>
    public FacilityProgressData(int facilityID, int currentLevel)
    {
        // 전달받은 시설 ID를 저장함
        this.facilityID = facilityID;
        // 전달받은 현재 시설 레벨을 저장함
        this.currentLevel = currentLevel;
    }
    public int FacilityID => facilityID;
    public int CurrentLevel => currentLevel;
    public bool IncreaseLevel(int maxLevel)
    {
        // 최대 레벨이 1 미만이면 실패 처리
        if (maxLevel <= 0) return false;
        // 현재 레벨이 이미 최대 레벨이면 실패 처리
        if (currentLevel >= maxLevel) return false;
        // 현재 시설 레벨 1 증가
        currentLevel += 1;
        // 레벨 증가 성공 반환
        return true;
    }
}

