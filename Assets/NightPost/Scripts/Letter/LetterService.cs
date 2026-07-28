using System.Collections.Generic;
using UnityEngine;

// 현재 플레이어에게 어떤 편지를 보여줄 것인지
public class LetterService : MonoBehaviour
{
    private PlayerDataManager playerDataManager;
    private StaticDataCatalog staticDataCatalog;
    public bool Initialize(PlayerDataManager dataManager, StaticDataCatalog catalog)
    {
        if (dataManager == null) return false;
        if (catalog == null) return false;

        playerDataManager = dataManager;
        staticDataCatalog = catalog;
        return true;
    }
    // 받은 편지
    public bool ReceiveLetter(int letterID)
    {
        if (staticDataCatalog == null || playerDataManager == null) return false;
        if(letterID <= 0) return false;
       // 정적 편지가 존재하는지 확인
       LetterStaticData letterStaticData = staticDataCatalog.GetLetter(letterID);

        if (letterStaticData == null) return false;

        LetterProgressData existingProgress = playerDataManager.GetLetterProgress(letterID);
        // 이미 진행 데이터가 있는지 확인
        if (existingProgress != null) return false;

        // 새 LetterProgressData 생성
        LetterProgressData data = new LetterProgressData(letterID);
        bool addResult = playerDataManager.AddLetterProgress(data);
        if (!addResult) return false;
        GameEvents.RaiseLetterReceived(letterID);
        return true;
    }
    // 편지를 읽음 상태로 변경
    public LetterStaticData OpenLetter(int letterID)
    {
        if (staticDataCatalog == null || playerDataManager == null) return null;
        LetterStaticData letterData = staticDataCatalog.GetLetter(letterID);
        if(letterData == null) return null;
        LetterProgressData progressData = playerDataManager.GetLetterProgress(letterID);
        if(progressData == null) return null;

        if(!progressData.IsRead)
        {
            if(!progressData.MarkAsRead()) return null;
            GameEvents.RaiseLetterRead(letterID);
        }
        return letterData;
    }
    // 분류를 완료 했는가
    public bool CompleteSorting(int letterID)
    {
        if(playerDataManager == null) return false;
        // GetLetterProgress로 진행 데이터 조회
        LetterProgressData letter = playerDataManager.GetLetterProgress(letterID);
        // 없으면 false
        if (letter == null) return false;
        // progress.CompleteSorting() 결과 반환
        bool isCompleted = letter.CompleteSorting();
        if(!isCompleted) return false;
        GameEvents.RaiseLetterStateChanged(letterID, ELetterProgressState.Waiting);
        return true;
    }
    #region 조회함수
    public IReadOnlyList<LetterStaticData> GetAvailableLetters()
    {
        List<LetterStaticData> availableLetters = new();
        if (staticDataCatalog == null || playerDataManager == null) return availableLetters;
        IReadOnlyList<LetterProgressData> progressDatas = playerDataManager.GetLetterProgresses();
        if(progressDatas == null) return availableLetters;

        foreach (LetterProgressData progress in progressDatas)
        {
            if(progress ==  null) continue;
            bool isAvailable = progress.State == ELetterProgressState.New || progress.State == ELetterProgressState.Waiting;
            if (!isAvailable) continue;
            LetterStaticData letterData = staticDataCatalog.GetLetter(progress.LetterID);
            if (letterData == null) continue;
            availableLetters.Add(letterData);
        }
        return availableLetters;
    }

    public LetterProgressData GetLetterProgress(int letterID)
    {
        if (playerDataManager == null) return null;
        return playerDataManager.GetLetterProgress(letterID);
    }
    #endregion

}
