using System.Collections.Generic;
using UnityEngine;

public class LetterServiceTest : MonoBehaviour
{
    [SerializeField] private LetterService letterService;
    [SerializeField] private PlayerDataManager playerDataManager;

    [Header("테스트 편지")]
    [SerializeField] private int letterID = 1001;
  
    [ContextMenu("01. Test Receive Letter")]
    private void TestReceiveLetter()
    {
        // LetterService 또는 PlayerDataManager가 연결되지 않았다면 종료한다.
        if (letterService == null || playerDataManager == null)
        {
            Debug.LogError(
                "[LetterTest] LetterService 또는 PlayerDataManager가 연결되지 않았습니다.");
            return;
        }

        Debug.Log(
            "========== Letter Receive Test 시작 ==========\n" +
            $"Test LetterID: {letterID}");
        // 테스트 전에 해당 편지의 진행 데이터가 이미 존재하는지 확인한다.
        LetterProgressData progressData = playerDataManager.GetLetterProgress(letterID);
        if(progressData != null)
        {
            Debug.LogError(
          $"[LetterTest][FAIL] LetterID {letterID}는 이미 받은 편지입니다.\n" +
          "PlayerSaveData에서 해당 진행 데이터를 제거하거나 " +
          "아직 받지 않은 다른 LetterID를 사용하세요.");
            // 이미 존재한다면 중복 수신 테스트가 되므로 경고를 출력하고 종료한다.
            return;
        }

        // LetterService.ReceiveLetter(letterID)를 호출한다.
        bool receiveResult = letterService.ReceiveLetter(letterID);
        LogResult( $"편지 수신 함수 반환값 LetterID={letterID}", receiveResult);

        // PlayerDataManager에서 같은 letterID의 진행 데이터를 다시 조회한다.
        LetterProgressData progressAfter = playerDataManager.GetLetterProgress(letterID);

        LogResult("PlayerSaveData에 편지 진행 데이터 추가",progressAfter != null);
        if (progressAfter == null)
        {
            Debug.LogError(
                "[LetterTest] ReceiveLetter()는 실행됐지만 " +
                "LetterProgressData를 조회할 수 없습니다.");

            return;
        }

        LogResult(
            $"초기 상태가 New인지 확인 " +
            $"Current={progressAfter.State}",
            progressAfter.State == ELetterProgressState.New);

        LogResult(
            $"초기 읽음 상태가 false인지 확인 " +
            $"IsRead={progressAfter.IsRead}",
            !progressAfter.IsRead);
        // 진행 데이터가 실제 PlayerSaveData에 추가됐는지 확인한다.
        bool duplicateReceiveResult = letterService.ReceiveLetter(letterID);
        LogResult(
        "같은 편지의 중복 수신 차단",
        !duplicateReceiveResult);

        Debug.Log(
            "[LetterTest] 최종 상태\n" +
            $"LetterID: {progressAfter.LetterID}\n" +
            $"State: {progressAfter.State}\n" +
            $"IsRead: {progressAfter.IsRead}");

        Debug.Log(
            "========== Letter Receive Test 종료 ==========");
    }

    private void LogResult(string testName, bool success)
    {
        if (success)
        {
            Debug.Log($"[LetterTest][PASS] {testName}");
        }
        else
        {
            Debug.LogError($"[LetterTest][FAIL] {testName}");
        }
    }
}
