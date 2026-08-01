using UnityEngine;

public class SortingResultData
{
    // 지역 선택의 정답 여부임
    public bool IsRegionCorrect { get; private set; }

    // 긴급도 선택의 정답 여부임
    public bool IsUrgencyCorrect { get; private set; }

    // 무게 선택의 정답 여부임
    public bool IsWeightCorrect { get; private set; }

    // 세 항목이 모두 정답인지 반환함
    public bool IsSuccess =>IsRegionCorrect &&IsUrgencyCorrect &&IsWeightCorrect;

    /// <summary>
    /// 지역·긴급도·무게의 분류 판정 결과를 생성함
    /// </summary>
    public SortingResultData(bool isRegionCorrect, bool isUrgencyCorrect,bool isWeightCorrect)
    {
        // 전달받은 지역 정답 여부 저장
        IsRegionCorrect = isRegionCorrect;
        // 전달받은 긴급도 정답 여부 저장
        IsUrgencyCorrect = isUrgencyCorrect;
        // 전달받은 무게 정답 여부 저장
        IsWeightCorrect = isWeightCorrect;
    }
}
