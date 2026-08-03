using UnityEngine;

[System.Serializable]
public class DeliveryResultData 
{
    // 완료된 편지 ID
    [SerializeField] private int letterID = 0;
    // 실제 지급할 보상
    [SerializeField] private int rewardAmount = 0;
    // 배달 완료 시각
    [SerializeField] private long completedAtUnixTime = 0;
    // 플레이어가 결과를 확인했는지
    [SerializeField] private bool isChecked = false;

    public DeliveryResultData(int letterID, int rewardAmount, long completedAtUnixTime)
    {
        this.letterID = letterID;
        this.rewardAmount = rewardAmount;
        this.completedAtUnixTime = completedAtUnixTime;
        this.isChecked = false;
    }
    /// <summary>
    /// 저장 데이터에서 불러온 배달 결과 상태를 복원함
    /// </summary>
    public DeliveryResultData(int letterID, int rewardAmount, long completedAtUnixTime, bool isChecked)
    {
        // 전달받은 완료 편지 ID를 저장함
        this.letterID = letterID;

        // 전달받은 실제 지급 보상을 저장함
        this.rewardAmount = rewardAmount;

        // 전달받은 배달 완료 시각을 저장함
        this.completedAtUnixTime = completedAtUnixTime;

        // 전달받은 결과 확인 여부를 저장함
        this.isChecked = isChecked;

    }
    public bool MarkAsChecked()
    {
        // 이미 확인한 배달 결과라면
        // 중복 확인 처리를 막기 위해 false를 반환
        if (isChecked) return false;

        // 아직 확인하지 않은 결과라면
        // isChecked를 true로 변경
        isChecked = true;

        // 정상적으로 확인 상태가 변경됐으므로 true를 반환
        return true;
    }

    public int LetterID => letterID;
    public int RewardAmount => rewardAmount;
    public long CompletedAtUnixTime => completedAtUnixTime;
    public bool IsChecked => isChecked;
}

