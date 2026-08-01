using UnityEngine;

public class SortingService
{
    private StaticDataCatalog staticDataCatalog;
    private LetterService letterService;

    /// <summary>
    /// 분류 판정에 필요한 정적 데이터와 편지 서비스를 등록함
    /// </summary>
    public bool Initialize(StaticDataCatalog catalog,LetterService service)
    {
        // 정적 데이터 카탈로그 또는 편지 서비스가 없으면 초기화하지 않음
        if(catalog == null || service == null) return false;
        // 전달받은 정적 데이터 카탈로그를 내부 필드에 저장함
        staticDataCatalog = catalog;
        // 전달받은 편지 서비스를 내부 필드에 저장함
        letterService = service;
        // 모든 의존성 등록이 끝났으므로 초기화 성공을 반환함
        return true;
    }

    /// <summary>
    /// 플레이어가 선택한 지역·긴급도·무게를 편지의 정답 데이터와 비교함
    /// </summary>
    public SortingResultData ValidateSorting(int letterID,ERegionType selectedRegion,ELetterUrgency selectedUrgency,ELetterWeight selectedWeight)
    {
        // 정적 데이터 카탈로그가 등록되지 않았다면 판정하지 않음
        if(staticDataCatalog == null || letterService == null) return null;
        // 유효하지 않은 편지 ID라면 판정하지 않음
        if(letterID <= 0) return null;

        // 편지 ID에 해당하는 정적 데이터를 조회함
        LetterStaticData letterStaticData = staticDataCatalog.GetLetter(letterID);

        // 해당하는 편지 정적 데이터가 없다면 판정하지 않음
        if(letterStaticData == null ) return null;

        // 플레이어가 선택한 지역과 편지의 목적 지역이 같은지 판정함
        bool isRegionCorrect = letterStaticData.DestinationRegion == selectedRegion;

        // 플레이어가 선택한 긴급도와 편지의 긴급도가 같은지 판정함
        bool isUrgencyCorrect = letterStaticData.Urgency == selectedUrgency;

        // 플레이어가 선택한 무게와 편지의 무게가 같은지 판정함
        bool isWeightCorrect = letterStaticData.Weight == selectedWeight;

        // 각 항목의 정답 여부를 담은 분류 결과를 생성해 반환함
        SortingResultData resultData = new SortingResultData(isRegionCorrect, isUrgencyCorrect, isWeightCorrect);
        return resultData;
    }
    /// <summary>
    /// 플레이어가 제출한 분류 결과를 판정하고 정답이면 편지 분류를 완료함
    /// </summary>
    public SortingResultData SubmitSorting(int letterID,  ERegionType selectedRegion, ELetterUrgency selectedUrgency, ELetterWeight selectedWeight)
    {
        // 정적 데이터 카탈로그 또는 편지 서비스가 등록되지 않았다면 처리하지 않음
        if (staticDataCatalog == null || letterService == null) return null;
        // 지역·긴급도·무게 선택값을 실제 편지 데이터와 비교함
        SortingResultData resultData = ValidateSorting(letterID, selectedRegion, selectedUrgency, selectedWeight);
        // 판정 결과를 생성하지 못했다면 처리하지 않음
        if (resultData == null ) return null;
        // 하나 이상의 항목이 틀렸다면 편지 상태를 변경하지 않고 판정 결과만 반환함
        if (!resultData.IsSuccess) return resultData;

        // 모든 항목이 정답이면 편지를 New 상태에서 Waiting 상태로 변경함
        bool isSorting = letterService.CompleteSorting(letterID);
        // 편지 상태 변경에 실패했다면 정상적인 분류 완료로 처리하지 않음
        if (!isSorting) return null;
        // 분류 판정 결과를 반환함
        return resultData;
    }
}
