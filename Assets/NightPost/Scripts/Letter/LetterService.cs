using System.Collections.Generic;
using UnityEngine;

// 현재 플레이어에게 표시할 편지와 편지 상태 변경을 관리함
public class LetterService : MonoBehaviour
{
    // 플레이어의 편지 진행 데이터를 관리하는 매니저임
    private PlayerDataManager playerDataManager;
    // 편지 정적 데이터를 조회하는 카탈로그임
    private StaticDataCatalog staticDataCatalog;
    /// <summary>
    /// LetterService에서 사용할 데이터 매니저와 정적 데이터 카탈로그를 등록함
    /// </summary>
    public bool Initialize(PlayerDataManager dataManager, StaticDataCatalog catalog)
    {
        // 전달받은 데이터 매니저가 없다면 초기화에 실패함
        if (dataManager == null) return false;
        // 전달받은 정적 데이터 카탈로그가 없다면 초기화에 실패함
        if (catalog == null) return false;

        // 플레이어 데이터 매니저를 저장함
        playerDataManager = dataManager;
        // 정적 데이터 카탈로그를 저장함
        staticDataCatalog = catalog;
        // 필요한 참조 등록이 완료되었음을 반환함
        return true;
    }
    /// <summary>
    /// 지정한 편지를 플레이어의 수신 편지 목록에 등록함
    /// </summary>
    public bool ReceiveLetter(int letterID)
    {
        // 서비스 초기화가 완료되지 않았다면 편지를 수신하지 않음
        if (staticDataCatalog == null || playerDataManager == null) return false;
        // 유효하지 않은 편지 ID라면 수신하지 않음
        if (letterID <= 0) return false;
        // 지정한 ID에 해당하는 편지 정적 데이터가 존재하는지 확인함
        LetterStaticData letterStaticData = staticDataCatalog.GetLetter(letterID);

        // 편지 정적 데이터가 없다면 수신하지 않음
        if (letterStaticData == null) return false;

        // 동일한 편지의 기존 진행 데이터를 조회함
        LetterProgressData existingProgress = playerDataManager.GetLetterProgress(letterID);
        // 이미 진행 데이터가 있다면 중복 수신하지 않음
        if (existingProgress != null) return false;

        // 신규 편지 진행 데이터를 생성함
        LetterProgressData data = new LetterProgressData(letterID);
        // 생성한 진행 데이터를 플레이어 데이터에 추가함
        bool addResult = playerDataManager.AddLetterProgress(data);
        // 진행 데이터 추가에 실패했다면 수신 처리에 실패함
        if (!addResult) return false;
        // 편지 수신 이벤트를 발생시킴
        GameEvents.RaiseLetterReceived(letterID);
        // 편지 수신이 완료되었음을 반환함
        return true;
    }
    /// <summary>
    /// 지정한 편지를 열고 읽음 상태로 변경한 뒤 정적 데이터를 반환함
    /// </summary>
    public LetterStaticData OpenLetter(int letterID)
    {
        // 서비스 초기화가 완료되지 않았다면 편지를 열지 않음
        if (staticDataCatalog == null || playerDataManager == null) return null;
        // 지정한 ID에 해당하는 편지 정적 데이터를 조회함
        LetterStaticData letterData = staticDataCatalog.GetLetter(letterID);
        // 편지 정적 데이터가 없다면 편지를 열지 않음
        if (letterData == null) return null;
        // 지정한 편지의 진행 데이터를 조회함
        LetterProgressData progressData = playerDataManager.GetLetterProgress(letterID);
        // 편지 진행 데이터가 없다면 편지를 열지 않음
        if (progressData == null) return null;

        // 아직 읽지 않은 편지인 경우에만 읽음 상태 변경을 처리함
        if (!progressData.IsRead)
        {
            // 읽음 상태 변경에 실패했다면 편지 데이터를 반환하지 않음
            if (!progressData.MarkAsRead()) return null;
            // 편지 읽음 이벤트를 발생시킴
            GameEvents.RaiseLetterRead(letterID);
        }
        // 화면에 표시할 편지 정적 데이터를 반환함
        return letterData;
    }
    /// <summary>
    /// 지정한 편지의 분류를 완료하고 대기 상태로 변경함
    /// </summary>
    public bool CompleteSorting(int letterID)
    {
        // 플레이어 데이터 매니저가 없다면 분류를 처리하지 않음
        if (playerDataManager == null) return false;
        // 지정한 편지의 진행 데이터를 조회함
        LetterProgressData letter = playerDataManager.GetLetterProgress(letterID);
        // 편지 진행 데이터가 없다면 분류를 처리하지 않음
        if (letter == null) return false;
        // 편지 진행 데이터에 분류 완료를 요청함
        bool isCompleted = letter.CompleteSorting();
        // 상태 변경에 실패했다면 분류 완료로 처리하지 않음
        if (!isCompleted) return false;
        // 편지가 배달 대기 상태로 변경되었음을 알리는 이벤트를 발생시킴
        GameEvents.RaiseLetterStateChanged(letterID, ELetterProgressState.Waiting);
        // 편지 분류가 완료되었음을 반환함
        return true;
    }
    #region 조회함수
    /// <summary>
    /// 현재 플레이어가 확인할 수 있는 신규 및 배달 대기 편지 목록을 반환함
    /// </summary>
    public IReadOnlyList<LetterStaticData> GetAvailableLetters()
    {
        // 조회 결과를 저장할 편지 목록을 생성함
        List<LetterStaticData> availableLetters = new();
        // 서비스 초기화가 완료되지 않았다면 빈 목록을 반환함
        if (staticDataCatalog == null || playerDataManager == null) return availableLetters;
        // 플레이어가 보유한 전체 편지 진행 데이터를 조회함
        IReadOnlyList<LetterProgressData> progressDatas = playerDataManager.GetLetterProgresses();
        // 편지 진행 데이터 목록이 없다면 빈 목록을 반환함
        if (progressDatas == null) return availableLetters;

        // 전체 편지 진행 데이터를 순회함
        foreach (LetterProgressData progress in progressDatas)
        {
            // 유효하지 않은 진행 데이터는 제외함
            if (progress == null) continue;
            // 신규 또는 배달 대기 상태인지 확인함
            bool isAvailable = progress.State == ELetterProgressState.New || progress.State == ELetterProgressState.Waiting;
            // 현재 표시할 수 없는 상태라면 제외함
            if (!isAvailable) continue;
            // 진행 데이터와 연결된 편지 정적 데이터를 조회함
            LetterStaticData letterData = staticDataCatalog.GetLetter(progress.LetterID);
            // 연결된 편지 정적 데이터가 없다면 제외함
            if (letterData == null) continue;
            // 확인 가능한 편지 목록에 추가함
            availableLetters.Add(letterData);
        }
        // 확인 가능한 편지 목록을 반환함
        return availableLetters;
    }

    /// <summary>
    /// 지정한 편지의 진행 데이터를 반환함
    /// </summary>
    public LetterProgressData GetLetterProgress(int letterID)
    {
        // 플레이어 데이터 매니저가 없다면 진행 데이터를 반환하지 않음
        if (playerDataManager == null) return null;
        // 지정한 편지 ID에 해당하는 진행 데이터를 반환함
        return playerDataManager.GetLetterProgress(letterID);
    }
    #endregion

}
