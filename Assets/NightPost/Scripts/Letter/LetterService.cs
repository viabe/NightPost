using System.Collections.Generic;
using UnityEngine;

// 현재 플레이어에게 어떤 편지를 보여줄 것인지
public class LetterService : MonoBehaviour
{
    [SerializeField] private StaticDataCatalog catalog;

    private Dictionary<int, LetterProgressData> letterProgressDatas = new();
    private void Awake()
    {
        if (catalog == null)
        {
            Debug.LogError( "[LetterService] StaticDataCatalog가 설정되지 않았습니다.");
        }
    }
    // DB에서 확인하고 초기화
    public void InitializeProgressData(List<LetterProgressData> progressDatas)
    {
        letterProgressDatas.Clear();
        if (progressDatas == null) return;
        foreach (LetterProgressData letterProgressData in progressDatas)
        {
            // 1. progress가 null이면 건너뛰기
            if(letterProgressData == null) continue;
            // 2. StaticDataCatalog에 해당 LetterID가 존재하는지 확인
            int id = letterProgressData.LetterID;
            LetterStaticData letterStaticData = catalog.GetLetter(id);
            if(letterStaticData == null) continue;
            // 3. 이미 같은 LetterID가 등록됐는지 확인
            if (letterProgressDatas.ContainsKey(id)) continue;
            // 4. Dictionary에 추가
            letterProgressDatas.Add(id, letterProgressData);
        }
    }
    // 받은 편지
    public bool ReceiveLetter(int letterID)
    {
        // 정적 편지가 존재하는지 확인
        LetterStaticData letterStaticData = catalog.GetLetter(letterID);
        if (letterStaticData == null) return false;
        // 이미 진행 데이터가 있는지 확인
        if (letterProgressDatas.ContainsKey(letterID)) return false;

        // 새 LetterProgressData 생성
        LetterProgressData data = new LetterProgressData(letterID);
        // Dictionary에 추가
        letterProgressDatas.Add(letterID, data);
        return true;
    }
    // 편지를 읽음 상태로 변경
    public bool MarkAsRead(int letterID)
    {
        // GetLetterProgress(letterID)
        LetterProgressData letter = GetLetterProgress(letterID);
        // 데이터가 없으면 false
        if(letter == null) return false;    
        // progress.MarkAsRead() 결과 반환
        return letter.MarkAsRead();
    }
    // 분류를 완료 했는가
    public bool CompleteSorting(int letterID)
    {
        // GetLetterProgress로 진행 데이터 조회
        LetterProgressData letter = GetLetterProgress(letterID);
        // 없으면 false
        if (letter == null) return false;
        // progress.CompleteSorting() 결과 반환
        return letter.CompleteSorting();
    }
    #region 조회함수
    public IReadOnlyList<LetterStaticData> GetAvailableLetters()
    {
        List<LetterStaticData> availableLetters = new();

        foreach (LetterProgressData progress in letterProgressDatas.Values)
        {
            // 1. progress 상태가 이용 가능한지 확인
            bool isAvailableLetter = (progress.State == ELetterProgressState.New || progress.State == ELetterProgressState.Waiting);
            // 2. progress의 LetterID로 catalog.GetLetter() 호출
            if(isAvailableLetter)
            {
                LetterStaticData letter = catalog.GetLetter(progress.LetterID);
                if (letter == null) continue;
                // 3. 조회된 LetterStaticData를 결과 목록에 추가
                availableLetters.Add(letter);
            }
        }
        return availableLetters;
    }

    public LetterProgressData GetLetterProgress(int letterID)
    {
        letterProgressDatas.TryGetValue(letterID, out LetterProgressData letterProgress);
        return letterProgress;
    }
    #endregion

}
