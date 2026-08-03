using System.Collections.Generic;
using UnityEngine;

public class FacilityService : MonoBehaviour
{
    private StaticDataCatalog staticDataCatalog;
    private PlayerDataManager playerDataManager;

    /// <summary>
    /// 시설 시스템에 필요한 데이터와 관리자를 연결함
    /// </summary>
    public bool Initialize(StaticDataCatalog catalog, PlayerDataManager dataManager)
    {
        // 전달받은 StaticDataCatalog 유효성 확인
        if (catalog == null) return false;
        // 전달받은 PlayerDataManager 유효성 확인
        if(dataManager == null) return false;
        // 정적 데이터 카탈로그 저장
        staticDataCatalog = catalog;
        // 플레이어 데이터 관리자 저장
        playerDataManager = dataManager;
        // 초기화 성공 반환
        return true;
    }

    /// <summary>
    /// 지정한 시설의 다음 업그레이드 레벨 데이터를 조회함
    /// </summary>
    public FacilityLevelData GetNextLevelData(int facilityID)
    {
        // FacilityService 초기화 여부 확인
        if(staticDataCatalog == null || playerDataManager == null) return null;
        // facilityID 유효성 확인
        if (facilityID <= 0) return null;

        // 시설 정적 데이터 조회
        FacilityStaticData facilityStaticData = staticDataCatalog.GetFacility(facilityID);

        // 시설 정적 데이터 또는 레벨 목록이 없으면 null 반환
        if(facilityStaticData == null || facilityStaticData.LevelData == null) return null;

        // 플레이어의 해당 시설 진행 데이터 조회
        FacilityProgressData facilityProgressData = playerDataManager.GetFacilityProgress(facilityID);

        // 진행 데이터가 없으면 현재 레벨을 0으로 처리
        int currentLevel = facilityProgressData == null ? 0 : facilityProgressData.CurrentLevel;

        int nextLevel = currentLevel + 1;

        // 시설의 레벨 데이터 목록 순회 
        foreach (FacilityLevelData levelData in facilityStaticData.LevelData)
        {
            // null 레벨 데이터 건너뜀
            if (levelData == null) continue;

            // 다음 레벨과 일치하는 레벨 데이터 반환
            if (levelData.Level == nextLevel) return levelData;
        }

        // 다음 레벨 데이터가 없으면 최대 레벨이므로 null 반환
        return null;
    }

    /// <summary>
     /// 지정한 시설을 다음 레벨로 업그레이드할 수 있는지 확인함
     /// </summary>
    public bool CanUpgradeFacility(int facilityID)
    {
        // FacilityService 초기화 여부 확인
        if (staticDataCatalog == null || playerDataManager == null) return false;
        // facilityID 유효성 확인
        if (facilityID <= 0) return false;

        // 다음 업그레이드 레벨 데이터 조회
        FacilityLevelData nextLevelData = GetNextLevelData(facilityID);
        // 다음 레벨 데이터가 없으면 최대 레벨이므로 실패 처리
        if (nextLevelData == null) return false;

        // 업그레이드 비용이 음수이면 잘못된 데이터이므로 실패 처리
        if(nextLevelData.UpgradeCost < 0) return false;
        // 플레이어의 현재 재화 조회
        int currentCurrency = playerDataManager.GetCurrency();

        // 현재 재화가 업그레이드 비용보다 적으면 실패 처리
        if (currentCurrency < nextLevelData.UpgradeCost) return false;
        // 업그레이드 가능 반환
        return true;
    }
    /// <summary>
    /// 지정한 시설을 다음 레벨로 업그레이드함
    /// </summary>
    public bool UpgradeFacility(int facilityID)
    {
        // FacilityService 초기화 여부 확인
        if (staticDataCatalog == null || playerDataManager == null) return false;
        // facilityID 유효성 확인
        if (facilityID <= 0) return false;

        // 시설 업그레이드 가능 여부 확인
        if (!CanUpgradeFacility(facilityID)) return false;

        // 시설 정적 데이터 조회
        FacilityStaticData facilityStaticData = staticDataCatalog.GetFacility(facilityID);

        // 시설 정적 데이터가 없으면 실패 처리
        if(facilityStaticData == null) return false;

        // 다음 업그레이드 레벨 데이터 조회
        FacilityLevelData nextLevelData = GetNextLevelData(facilityID);

        // 다음 레벨 데이터가 없으면 실패 처리
        if(nextLevelData == null) return false;

        // 업그레이드 비용 저장
        int upgradeCost = nextLevelData.UpgradeCost;

        // 플레이어 재화 차감
        // 재화 차감에 실패하면 false 반환
        if (!playerDataManager.SpendCurrency(upgradeCost)) return false;

        // 시설 레벨 1 증가
        // 레벨 증가에 실패하면 false 반환
        if (!playerDataManager.IncreaseFacilityLevel(facilityID, facilityStaticData.MaxLevel))
        {
            if(upgradeCost > 0) playerDataManager.AddCurrency(upgradeCost);
            return false;
        }
        // 업그레이드된 시설 진행 데이터 조회
        FacilityProgressData facilityProgressData = playerDataManager.GetFacilityProgress(facilityID);
        if(facilityProgressData == null)return false;
        GameEvents.RaiseFacilityUpgraded(facilityID, facilityProgressData.CurrentLevel);
        // 업그레이드 성공 반환
        return true;
    }

    /// <summary>
    /// 지정한 시설의 현재 레벨 데이터를 조회함
    /// </summary>
    public FacilityLevelData GetCurrentLevelData(int facilityID)
    {
        // FacilityService 초기화 여부 확인
        if (staticDataCatalog == null || playerDataManager == null) return null;
        // facilityID 유효성 확인
        if (facilityID <= 0) return null;

        // 시설 정적 데이터 조회
        FacilityStaticData facilityStaticData = staticDataCatalog.GetFacility(facilityID);

        // 시설 정적 데이터 또는 레벨 목록이 없으면 null 반환
        if(facilityStaticData == null || facilityStaticData.LevelData == null) return null; 

        // 플레이어의 시설 진행 데이터 조회
        FacilityProgressData facilityProgressData = playerDataManager.GetFacilityProgress(facilityID);

        // 진행 데이터가 없으면 아직 업그레이드되지 않은 시설이므로 null 반환
        if(facilityProgressData == null) return null;

        // 현재 레벨이 1 미만이면 null 반환
        if(facilityProgressData.CurrentLevel < 1) return null;

        // 시설의 레벨 데이터 목록 순회
        foreach(FacilityLevelData facilityLevelData in facilityStaticData.LevelData)
        {
            // null 레벨 데이터 건너뜀
            if (facilityLevelData == null) continue;

            // 현재 레벨과 Level 값이 일치하면 해당 데이터 반환
            if (facilityLevelData.Level == facilityProgressData.CurrentLevel) return facilityLevelData;
        }

        // 일치하는 현재 레벨 데이터가 없으면 null 반환
        return null;
    }

    /// <summary>
    /// 지정한 효과 종류에 해당하는 현재 시설 효과값을 조회함
    /// </summary>
    public float GetFacilityEffectValue(int facilityID, EFacilityEffectType effectType)
    {
        // FacilityService 초기화 여부 확인
        if (staticDataCatalog == null || playerDataManager == null) return 0.0f;
        // facilityID 유효성 확인
        if (facilityID <= 0) return 0.0f;

        // 효과 종류가 None이면 0 반환
        if(effectType == EFacilityEffectType.None) return 0.0f;

        // 현재 시설 레벨 데이터 조회
        FacilityLevelData facilityLevel = GetCurrentLevelData(facilityID);

        // 현재 레벨 데이터가 없으면 0 반환
        if(facilityLevel == null) return 0.0f;  

        // 현재 레벨 데이터의 효과 종류가 요청한 효과와 다르면 0 반환
        if(facilityLevel.EffectType != effectType) return 0.0f;

        // 현재 레벨의 최종 누적 효과값 반환
        return facilityLevel.EffectValue;
    }

    /// <summary>
    /// 지정한 효과 종류에 해당하는 모든 시설의 효과값을 합산함
    /// </summary>
    public float GetTotalFacilityEffectValue(EFacilityEffectType effectType)
    {
        // FacilityService 초기화 여부 확인
        if (staticDataCatalog == null || playerDataManager == null) return 0.0f;

        // 효과 종류가 None이면 0 반환
        if (effectType == EFacilityEffectType.None) return 0.0f;

        // 정적 데이터 카탈로그에서 전체 시설 목록 조회
        IReadOnlyList<FacilityStaticData> facilities = staticDataCatalog.Facilities();

        // 전체 시설 목록이 없거나 비어 있다면 0 반환
        if (facilities == null || facilities.Count == 0) return 0.0f;

        // 전체 효과값을 저장할 변수 선언
        float totalFacilityEffectValue = 0.0f;

        // StaticDataCatalog의 전체 시설 목록 순회
        foreach (FacilityStaticData facilityStaticData in facilities)
        {
            // null 시설 데이터 건너뜀
            if(facilityStaticData == null || facilityStaticData.FacilityID <= 0) continue;

            // 현재 시설에서 요청한 효과값 조회
            // 조회한 효과값을 전체 효과값에 합산
            totalFacilityEffectValue += GetFacilityEffectValue(facilityStaticData.FacilityID, effectType);
        }

        // 합산된 전체 효과값 반환
        return totalFacilityEffectValue;
    }
    /// <summary>
    /// 시설 화면에 표시할 전체 시설 정적 데이터 목록을 반환함
    /// </summary>
    public IReadOnlyList<FacilityStaticData> GetFacilities()
    {
        // 화면에 제공할 시설 정적 데이터 목록을 생성함
        List<FacilityStaticData> facilityList = new();

        // 정적 데이터 카탈로그가 없다면 빈 목록을 반환함
        if(staticDataCatalog == null) return facilityList;

        // 정적 데이터 카탈로그에서 전체 시설 목록을 조회함
        IReadOnlyList<FacilityStaticData> facilities = staticDataCatalog.Facilities();

        // 전체 시설 목록이 없다면 빈 목록을 반환함
        if(facilities == null) return facilityList;

        // 전체 시설 정적 데이터를 순회함
        foreach (FacilityStaticData facility in facilities)
        {
            // 유효하지 않은 시설 데이터는 제외함
            if(facility == null || facility.FacilityID <= 0) continue;

            // 화면에 제공할 시설 목록에 추가함
            facilityList.Add(facility);
        }

        // 유효한 전체 시설 정적 데이터 목록을 반환함
        return facilityList; 
    }

    /// <summary>
    /// 지정한 시설의 현재 레벨을 반환함
    /// </summary>
    public int GetCurrentFacilityLevel(int facilityID)
    {
        // 플레이어 데이터 관리자가 없다면 0을 반환함
        if(playerDataManager == null) return 0;

        // 유효하지 않은 시설 ID라면 0을 반환함
        if(facilityID <= 0) return 0;

        // 지정한 시설의 진행 데이터를 조회함
        FacilityProgressData facilityProgressData = playerDataManager.GetFacilityProgress(facilityID);

        // 시설 진행 데이터가 없다면 아직 업그레이드되지 않은 시설이므로 0을 반환함
        if(facilityProgressData == null) return 0;

        // 현재 레벨이 0보다 작다면 잘못된 데이터이므로 0을 반환함
        if(facilityProgressData.CurrentLevel < 0)return 0;

        // 시설의 현재 레벨을 반환함
        return facilityProgressData.CurrentLevel;
    }
}
