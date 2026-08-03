using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveServiceTester : MonoBehaviour
{
    private const string DatabaseFileName = "NightPostSave.db";
    private const string BackupSuffix = ".tester_backup";

    private static readonly string[] DatabaseRelatedSuffixes =
    {
        "",
        "-journal",
        "-wal",
        "-shm"
    };

    [Header("테스트 대상")]
    [SerializeField] private SaveService saveService;

    [Header("테스트 실행")]
    [SerializeField] private bool runOnStart = true;

    private int successCount;
    private int failureCount;
    private bool isTesting;

    /// <summary>
    /// 테스트 씬이 시작될 때 자동 테스트 설정에 따라 저장·로드 테스트를 실행함
    /// </summary>
    private void Start()
    {
        // 자동 실행이 비활성화되어 있다면 테스트하지 않음
        if (!runOnStart) return;

        // SQLite 저장·로드 통합 테스트를 실행함
        RunSaveLoadTest();
    }

    /// <summary>
    /// 샘플 플레이어 데이터를 SQLite에 저장한 뒤
    /// 새로운 런타임 데이터로 불러와 모든 값이 복원되는지 검사함
    /// </summary>
    [ContextMenu("SQLite 저장 로드 테스트")]
    public void RunSaveLoadTest()
    {
        // 플레이 모드가 아니라면 테스트하지 않음
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SaveServiceTester] 플레이 모드에서 테스트해야 합니다.");
            return;
        }

        // 이미 테스트가 실행 중이라면 중복 실행하지 않음
        if (isTesting) return;

        // SaveService가 연결되지 않았다면 테스트하지 않음
        if (saveService == null)
        {
            Debug.LogError("[SaveServiceTester] SaveService가 연결되지 않았습니다.");
            return;
        }

        isTesting = true;
        successCount = 0;
        failureCount = 0;

        PlayerSaveData sourceData = null;
        PlayerSaveData loadedData = null;

        string databasePath = Path.Combine(
            Application.persistentDataPath,
            DatabaseFileName);

        bool isBackupPrepared = false;

        try
        {
            // 기존 게임 데이터베이스를 백업하고 테스트용 빈 환경을 준비함
            isBackupPrepared = PrepareTestDatabase(databasePath);

            if (!isBackupPrepared)
            {
                throw new InvalidOperationException(
                    "기존 데이터베이스 백업에 실패했습니다.");
            }

            // SQLite에 저장할 테스트용 플레이어 데이터를 생성함
            sourceData = CreateSourceData();

            if (sourceData == null)
            {
                throw new InvalidOperationException(
                    "테스트용 플레이어 데이터 생성에 실패했습니다.");
            }

            // 테스트용 플레이어 데이터를 저장 대상으로 등록함
            Require(
                saveService.Initialize(sourceData),
                "SaveService 초기화에 실패했습니다.");

            // 테스트용 SQLite 데이터베이스를 생성함
            Require(
                saveService.InitializeDatabase(),
                "SQLite 데이터베이스 초기화에 실패했습니다.");

            // 저장에 필요한 테이블을 생성함
            Require(
                saveService.CreateTables(),
                "SQLite 테이블 생성에 실패했습니다.");

            // 테스트용 플레이어 데이터를 SQLite에 저장함
            Require(
                saveService.SaveAll(),
                "테스트 데이터 저장에 실패했습니다.");

            // 저장 데이터 존재 여부 조회가 정상 동작하는지 확인함
            Require(
                saveService.TryHasSaveData(out bool hasSaveData),
                "저장 데이터 존재 여부 조회에 실패했습니다.");

            // 플레이어 저장 행이 생성되었는지 검사함
            Check(
                hasSaveData,
                "PlayerRecord 저장 여부");

            // 저장된 데이터를 받을 새로운 빈 런타임 데이터를 생성함
            loadedData = ScriptableObject.CreateInstance<PlayerSaveData>();
            loadedData.name = "SaveServiceTester_LoadedData";

            // 불러오기 대상 플레이어 데이터로 교체함
            Require(
                saveService.Initialize(loadedData),
                "불러오기 대상 데이터 등록에 실패했습니다.");

            // SQLite 데이터를 새로운 런타임 데이터에 복원함
            Require(
                saveService.LoadAll(),
                "테스트 데이터 불러오기에 실패했습니다.");

            // 저장 전 데이터와 불러온 데이터를 비교함
            ValidateLoadedData(sourceData, loadedData);

            // 테스트 결과를 출력함
            PrintTestResult();
        }
        catch (Exception exception)
        {
            // 테스트 실행 중 발생한 예외를 출력함
            Debug.LogError(
                $"[SaveServiceTester] SQLite 저장·로드 테스트 중 예외 발생\n" +
                $"오류: {exception}");
        }
        finally
        {
            // 테스트에 사용한 SQLite 연결을 종료함
            saveService.CloseDatabase();

            // 테스트 데이터베이스를 제거하고 기존 데이터베이스를 복구함
            if (isBackupPrepared)
            {
                DeleteDatabaseFiles(databasePath);
                RestoreDatabaseBackup(databasePath);
            }

            // 테스트 과정에서 생성한 런타임 ScriptableObject를 제거함
            DestroyRuntimeData(sourceData);
            DestroyRuntimeData(loadedData);

            // 테스트 실행 상태를 초기화함
            isTesting = false;
        }
    }

    /// <summary>
    /// SQLite 저장 테스트에 사용할 샘플 PlayerSaveData를 생성함
    /// </summary>
    private PlayerSaveData CreateSourceData()
    {
        // 테스트 데이터에서 사용할 현재 Unix 시각을 생성함
        long currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 테스트용 편지 진행 데이터 목록을 생성함
        List<LetterProgressData> letterProgresses = new()
        {
            new LetterProgressData(
                101,
                ELetterProgressState.Waiting,
                true),

            new LetterProgressData(
                102,
                ELetterProgressState.Delivering,
                true),

            new LetterProgressData(
                103,
                ELetterProgressState.Completed,
                true)
        };

        // 테스트용 진행 중 배달 데이터 목록을 생성함
        List<ActiveDeliveryData> activeDeliveries = new()
        {
            new ActiveDeliveryData(
                102,
                1,
                11,
                currentUnixTime - 60,
                currentUnixTime + 600)
        };

        // 테스트용 완료 배달 결과 목록을 생성함
        List<DeliveryResultData> deliveryResults = new()
        {
            new DeliveryResultData(
                103,
                250,
                currentUnixTime - 120,
                true)
        };

        // 테스트용 시설 진행 데이터 목록을 생성함
        List<FacilityProgressData> facilityProgresses = new()
        {
            new FacilityProgressData(301, 2),
            new FacilityProgressData(302, 1)
        };

        // 테스트용 런타임 PlayerSaveData를 생성함
        PlayerSaveData sourceData =
            ScriptableObject.CreateInstance<PlayerSaveData>();

        sourceData.name = "SaveServiceTester_SourceData";

        // 전체 테스트 데이터를 PlayerSaveData에 복원 형태로 입력함
        bool isRestored = sourceData.RestoreData(
            1250,
            7,
            new List<int> { 1, 2 },
            new List<int> { 11, 12 },
            letterProgresses,
            activeDeliveries,
            deliveryResults,
            facilityProgresses,
            new List<int> { 401, 402 },
            new List<int> { 401 },
            0);

        // 테스트 데이터 구성에 실패했다면 생성한 데이터를 제거함
        if (!isRestored)
        {
            DestroyRuntimeData(sourceData);
            return null;
        }

        // 생성한 테스트용 플레이어 데이터를 반환함
        return sourceData;
    }

    /// <summary>
    /// 저장 전 데이터와 SQLite에서 불러온 데이터를 항목별로 비교함
    /// </summary>
    private void ValidateLoadedData(
        PlayerSaveData expected,
        PlayerSaveData actual)
    {
        // 기본 플레이어 상태를 비교함
        Check(
            expected.Currency == actual.Currency,
            "보유 재화 복원");

        Check(
            expected.CompleteDeliveryCount ==
            actual.CompleteDeliveryCount,
            "누적 배달 완료 수 복원");

        Check(
            expected.LastSaveUnixTime ==
            actual.LastSaveUnixTime &&
            actual.LastSaveUnixTime > 0,
            "마지막 저장 시각 복원");

        // ID 목록 데이터를 비교함
        CheckIntList(
            expected.OwnedCourierIDs,
            actual.OwnedCourierIDs,
            "보유 배달부 목록 복원");

        CheckIntList(
            expected.UnlockedRouteIDs,
            actual.UnlockedRouteIDs,
            "해금 노선 목록 복원");

        CheckIntList(
            expected.ReceivedReplyIDs,
            actual.ReceivedReplyIDs,
            "수신 답장 목록 복원");

        CheckIntList(
            expected.ReadReplyIds,
            actual.ReadReplyIds,
            "읽은 답장 목록 복원");

        // 객체 형태의 진행 데이터를 비교함
        CheckLetterProgresses(
            expected.LetterProgressesList,
            actual.LetterProgressesList);

        CheckActiveDeliveries(
            expected.ActiveDeliveryList,
            actual.ActiveDeliveryList);

        CheckDeliveryResults(
            expected.DeliveryResultsList,
            actual.DeliveryResultsList);

        CheckFacilityProgresses(
            expected.FacilityProgressesList,
            actual.FacilityProgressesList);
    }

    /// <summary>
    /// 두 정수 ID 목록이 동일한 항목을 가지고 있는지 검사함
    /// </summary>
    private void CheckIntList(
        IReadOnlyList<int> expected,
        IReadOnlyList<int> actual,
        string testName)
    {
        // 비교할 목록이 없다면 실패 처리함
        if (expected == null || actual == null)
        {
            Check(false, testName);
            return;
        }

        // 목록의 항목 수가 다르다면 실패 처리함
        if (expected.Count != actual.Count)
        {
            Check(false, testName);
            return;
        }

        // 기대 목록을 집합으로 변환함
        HashSet<int> expectedSet = new(expected);

        // 실제 목록과 기대 목록의 항목 구성이 같은지 검사함
        Check(
            expectedSet.SetEquals(actual),
            testName);
    }

    /// <summary>
    /// 편지 진행 상태 목록이 동일하게 복원되었는지 검사함
    /// </summary>
    private void CheckLetterProgresses(
        IReadOnlyList<LetterProgressData> expected,
        IReadOnlyList<LetterProgressData> actual)
    {
        bool isMatched =
            expected != null &&
            actual != null &&
            expected.Count == actual.Count;

        if (isMatched)
        {
            foreach (LetterProgressData expectedData in expected)
            {
                LetterProgressData actualData =
                    FindLetterProgress(actual, expectedData.LetterID);

                if (actualData == null ||
                    expectedData.State != actualData.State ||
                    expectedData.IsRead != actualData.IsRead)
                {
                    isMatched = false;
                    break;
                }
            }
        }

        Check(isMatched, "편지 진행 상태 복원");
    }

    /// <summary>
    /// 진행 중 배달 목록이 동일하게 복원되었는지 검사함
    /// </summary>
    private void CheckActiveDeliveries(
        IReadOnlyList<ActiveDeliveryData> expected,
        IReadOnlyList<ActiveDeliveryData> actual)
    {
        bool isMatched =
            expected != null &&
            actual != null &&
            expected.Count == actual.Count;

        if (isMatched)
        {
            foreach (ActiveDeliveryData expectedData in expected)
            {
                ActiveDeliveryData actualData =
                    FindActiveDelivery(actual, expectedData.LetterID);

                if (actualData == null ||
                    expectedData.CourierID != actualData.CourierID ||
                    expectedData.RouteID != actualData.RouteID ||
                    expectedData.StartedAtUnixTime !=
                    actualData.StartedAtUnixTime ||
                    expectedData.CompleteAtUnixTime !=
                    actualData.CompleteAtUnixTime)
                {
                    isMatched = false;
                    break;
                }
            }
        }

        Check(isMatched, "진행 중 배달 목록 복원");
    }

    /// <summary>
    /// 완료된 배달 결과 목록이 동일하게 복원되었는지 검사함
    /// </summary>
    private void CheckDeliveryResults(
        IReadOnlyList<DeliveryResultData> expected,
        IReadOnlyList<DeliveryResultData> actual)
    {
        bool isMatched =
            expected != null &&
            actual != null &&
            expected.Count == actual.Count;

        if (isMatched)
        {
            foreach (DeliveryResultData expectedData in expected)
            {
                DeliveryResultData actualData =
                    FindDeliveryResult(actual, expectedData.LetterID);

                if (actualData == null ||
                    expectedData.RewardAmount !=
                    actualData.RewardAmount ||
                    expectedData.CompletedAtUnixTime !=
                    actualData.CompletedAtUnixTime ||
                    expectedData.IsChecked != actualData.IsChecked)
                {
                    isMatched = false;
                    break;
                }
            }
        }

        Check(isMatched, "완료 배달 결과 복원");
    }

    /// <summary>
    /// 시설 진행 상태 목록이 동일하게 복원되었는지 검사함
    /// </summary>
    private void CheckFacilityProgresses(
        IReadOnlyList<FacilityProgressData> expected,
        IReadOnlyList<FacilityProgressData> actual)
    {
        bool isMatched =
            expected != null &&
            actual != null &&
            expected.Count == actual.Count;

        if (isMatched)
        {
            foreach (FacilityProgressData expectedData in expected)
            {
                FacilityProgressData actualData =
                    FindFacilityProgress(
                        actual,
                        expectedData.FacilityID);

                if (actualData == null ||
                    expectedData.CurrentLevel !=
                    actualData.CurrentLevel)
                {
                    isMatched = false;
                    break;
                }
            }
        }

        Check(isMatched, "시설 진행 상태 복원");
    }

    /// <summary>
    /// 편지 ID에 해당하는 편지 진행 데이터를 찾음
    /// </summary>
    private LetterProgressData FindLetterProgress(
        IReadOnlyList<LetterProgressData> list,
        int letterID)
    {
        foreach (LetterProgressData data in list)
        {
            if (data != null && data.LetterID == letterID)
            {
                return data;
            }
        }

        return null;
    }

    /// <summary>
    /// 편지 ID에 해당하는 진행 중 배달 데이터를 찾음
    /// </summary>
    private ActiveDeliveryData FindActiveDelivery(
        IReadOnlyList<ActiveDeliveryData> list,
        int letterID)
    {
        foreach (ActiveDeliveryData data in list)
        {
            if (data != null && data.LetterID == letterID)
            {
                return data;
            }
        }

        return null;
    }

    /// <summary>
    /// 편지 ID에 해당하는 완료 배달 결과를 찾음
    /// </summary>
    private DeliveryResultData FindDeliveryResult(
        IReadOnlyList<DeliveryResultData> list,
        int letterID)
    {
        foreach (DeliveryResultData data in list)
        {
            if (data != null && data.LetterID == letterID)
            {
                return data;
            }
        }

        return null;
    }

    /// <summary>
    /// 시설 ID에 해당하는 시설 진행 데이터를 찾음
    /// </summary>
    private FacilityProgressData FindFacilityProgress(
        IReadOnlyList<FacilityProgressData> list,
        int facilityID)
    {
        foreach (FacilityProgressData data in list)
        {
            if (data != null && data.FacilityID == facilityID)
            {
                return data;
            }
        }

        return null;
    }

    /// <summary>
    /// 테스트 조건을 검사하고 성공 또는 실패 횟수를 기록함
    /// </summary>
    private void Check(bool condition, string testName)
    {
        if (condition)
        {
            successCount++;
            Debug.Log($"[SaveServiceTester] 성공: {testName}");
            return;
        }

        failureCount++;
        Debug.LogError($"[SaveServiceTester] 실패: {testName}");
    }

    /// <summary>
    /// 테스트 진행에 반드시 필요한 조건을 검사함
    /// </summary>
    private void Require(bool condition, string message)
    {
        if (condition) return;

        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// 전체 SQLite 저장·로드 테스트 결과를 로그로 출력함
    /// </summary>
    private void PrintTestResult()
    {
        if (failureCount == 0)
        {
            Debug.Log(
                $"[SaveServiceTester] 전체 테스트 성공\n" +
                $"성공: {successCount}\n" +
                $"실패: {failureCount}");

            return;
        }

        Debug.LogError(
            $"[SaveServiceTester] 테스트 실패\n" +
            $"성공: {successCount}\n" +
            $"실패: {failureCount}");
    }

    /// <summary>
    /// 기존 SQLite 데이터베이스를 백업하고 테스트용 빈 저장 환경을 준비함
    /// </summary>
    private bool PrepareTestDatabase(string databasePath)
    {
        // 기존 연결을 종료하여 데이터베이스 파일 사용을 해제함
        saveService.CloseDatabase();

        try
        {
            // 이전 테스트에서 남은 백업 파일을 제거함
            DeleteBackupFiles(databasePath);

            // 기존 데이터베이스 관련 파일을 모두 백업함
            foreach (string suffix in DatabaseRelatedSuffixes)
            {
                string originalPath = databasePath + suffix;
                string backupPath = originalPath + BackupSuffix;

                if (!File.Exists(originalPath)) continue;

                File.Copy(originalPath, backupPath, true);
            }

            // 테스트가 빈 데이터베이스에서 시작되도록 기존 파일을 제거함
            DeleteDatabaseFiles(databasePath);

            return true;
        }
        catch (Exception exception)
        {
            // 백업 준비 실패 원인을 출력함
            Debug.LogError(
                $"[SaveServiceTester] 데이터베이스 백업 실패\n" +
                $"오류: {exception}");

            // 부분적으로 생성된 백업 파일을 사용해 원본 복원을 시도함
            RestoreDatabaseBackup(databasePath);

            return false;
        }
    }

    /// <summary>
    /// 테스트용 SQLite 데이터베이스 관련 파일을 제거함
    /// </summary>
    private void DeleteDatabaseFiles(string databasePath)
    {
        foreach (string suffix in DatabaseRelatedSuffixes)
        {
            string filePath = databasePath + suffix;

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    /// <summary>
    /// 이전 테스트에서 남은 데이터베이스 백업 파일을 제거함
    /// </summary>
    private void DeleteBackupFiles(string databasePath)
    {
        foreach (string suffix in DatabaseRelatedSuffixes)
        {
            string backupPath =
                databasePath + suffix + BackupSuffix;

            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
    }

    /// <summary>
    /// 테스트 전에 백업한 기존 데이터베이스 파일을 원래 위치로 복구함
    /// </summary>
    private void RestoreDatabaseBackup(string databasePath)
    {
        foreach (string suffix in DatabaseRelatedSuffixes)
        {
            string originalPath = databasePath + suffix;
            string backupPath = originalPath + BackupSuffix;

            if (!File.Exists(backupPath)) continue;

            if (File.Exists(originalPath))
            {
                File.Delete(originalPath);
            }

            File.Move(backupPath, originalPath);
        }
    }

    /// <summary>
    /// 테스트 과정에서 생성한 런타임 PlayerSaveData를 제거함
    /// </summary>
    private void DestroyRuntimeData(PlayerSaveData playerSaveData)
    {
        if (playerSaveData == null) return;

        Destroy(playerSaveData);
    }
}
