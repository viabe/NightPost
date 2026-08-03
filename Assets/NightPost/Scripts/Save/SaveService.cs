using System;
using System.Collections.Generic;
using System.IO;
using SQLite;
using UnityEngine;

// 현재 플레이어 상태 제공
public class SaveService : MonoBehaviour
{
    // 생성할 SQLite 데이터베이스 파일명임
    private const string DatabaseFileName = "NightPostSave.db";

    // 현재 데이터베이스 파일의 전체 경로임
    private string databasePath;

    // 현재 사용 중인 SQLite 연결 객체임
    private SQLiteConnection connection;

    public PlayerSaveData CurrentPlayerData { get; private set; }
    /// <summary>
    /// 저장 및 불러오기에 사용할 런타임 플레이어 데이터를 등록함
    /// </summary>
    public bool Initialize(PlayerSaveData playerData)
    {
        // 전달받은 런타임 플레이어 데이터가 없다면 초기화에 실패함
        if (playerData == null) return false;

        // 전달받은 런타임 플레이어 데이터를 현재 저장 대상으로 등록함
        CurrentPlayerData = playerData;

        // 저장 서비스 초기화가 완료되었음을 반환함
        return true;
    }
    /// <summary>
    /// 현재 열려 있는 SQLite 연결을 안전하게 종료함
    /// </summary>
    public void CloseDatabase()
    {
        // 데이터베이스 연결이 없다면 종료 처리를 진행하지 않음
        if(connection == null) return;
        // 데이터베이스 연결 종료 중 발생하는 예외를 처리함
        try
        {
            // 현재 SQLite 연결을 종료함
            connection.Close();
        }
        catch (Exception exception)
        {
            // 데이터베이스 연결 종료 실패 원인을 오류 로그로 출력함
            Debug.LogError($"[SaveService] 데이터베이스 종료 실패\n" + $"오류: {exception}");
        }
        finally
        {
            // 종료된 연결 참조를 초기화함
            connection = null;
        }
    }
    /// <summary>
    /// 앱의 영구 저장 경로에 SQLite 데이터베이스 연결을 생성함
    /// </summary>
    public bool InitializeDatabase()
    {
        // 이미 데이터베이스 연결이 생성되어 있다면 중복 생성하지 않고 성공 처리함
        if (connection != null) return true;
        // 앱의 영구 저장 폴더와 데이터베이스 파일명을 결합하여 전체 경로를 생성함
        databasePath = Path.Combine(Application.persistentDataPath, DatabaseFileName);

        // 데이터베이스 연결 생성 중 발생하는 예외를 처리함
        try
        {
            // 생성한 데이터베이스 경로를 사용하여 SQLite 연결 객체를 생성함
            connection = new SQLiteConnection(databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        }
        catch (Exception exception)
        {
            // 연결 생성에 실패했다면 연결 객체를 비움
            connection = null;

            // 연결 생성 실패 원인과 데이터베이스 경로를 오류 로그로 출력함
            Debug.LogError($"[SaveService] 데이터베이스 연결 생성 실패\n" + $"경로: {databasePath}\n" + $"오류: {exception}");

            // 데이터베이스 초기화 실패를 반환함
            return false;
        }

        Debug.Log( $"[SaveService] 데이터베이스 연결 생성 성공\n" + $"경로: {databasePath}");
        // 데이터베이스 연결 생성이 완료되었음을 반환함
        return true;
    }

    /// <summary>
    /// 현재 저장 구조에 필요한 SQLite 테이블을 생성함
    /// </summary>
    public bool CreateTables()
    {
        // 데이터베이스 연결이 생성되지 않았다면 테이블 생성에 실패함
        if( connection == null ) return false;
        // 테이블 생성 중 발생하는 예외를 처리함
        try
        {
            // 플레이어 기본 상태 테이블을 생성함
            connection.CreateTable<PlayerRecord>();
            // 보유 배달부 테이블을 생성함
            connection.CreateTable<OwnedCourierRecord>();
            // 해금 노선 테이블을 생성함
            connection.CreateTable<UnlockedRouteRecord>();
            // 편지 진행 상태 테이블을 생성함
            connection.CreateTable<LetterProgressRecord>();
            // 진행 중인 배달 테이블을 생성함
            connection.CreateTable<ActiveDeliveryRecord>();
            // 완료된 배달 결과 테이블을 생성함
            connection.CreateTable<DeliveryResultRecord>();
            // 시설 진행 상태 테이블을 생성함
            connection.CreateTable<FacilityProgressRecord>();
            // 수신한 답장 테이블을 생성함
            connection.CreateTable<ReceivedReplyRecord>();
            // 읽은 답장 테이블을 생성함
            connection.CreateTable<ReadReplyRecord>();
        }
        catch (Exception exception)
        {
            // 테이블 생성 실패 원인을 오류 로그로 출력함
            Debug.LogError($"[SaveService] 데이터베이스 테이블 생성 실패\n" + $"오류: {exception}");
            // 테이블 생성 실패를 반환함
            return false;
        }

        // 모든 테이블 생성이 완료되었음을 반환함
        return true;
    }

    /// <summary>
    /// 단일 저장 슬롯의 플레이어 저장 데이터 존재 여부를 확인함
    /// </summary>
    public bool TryHasSaveData(out bool hasSaveData)
    {
        // 기본적으로 저장 데이터가 없는 상태로 초기화함
        hasSaveData = false;

        // 데이터베이스 연결이 생성되지 않았다면 조회에 실패함
        if( connection == null ) return false;  

        // 플레이어 저장 행 조회 중 발생하는 예외를 처리함
        try
        {
            // 단일 저장 슬롯의 기본 키 1에 해당하는 플레이어 저장 행을 조회함
            PlayerRecord playerRecord = connection.Find<PlayerRecord>(1);

            // 조회된 플레이어 저장 행의 존재 여부를 저장함
            hasSaveData = playerRecord != null;
        }
        catch (Exception exception)
        {
            // 저장 데이터 조회 실패 원인을 오류 로그로 출력함
            Debug.LogError($"[SaveService] 저장 데이터 존재 여부 조회 실패\n" + $"오류: {exception}");
            // 저장 데이터 존재 여부 조회 실패를 반환함
            return false;
        }

        // 저장 데이터 존재 여부 조회가 완료되었음을 반환함
        return true;
    }
    #region Save
    /// <summary>
    /// 현재 플레이어의 전체 진행 상태를 하나의 트랜잭션으로 SQLite에 저장함
    /// </summary>
    public bool SaveAll()
    {
        // 데이터베이스 연결이 생성되지 않았다면 저장에 실패함
        if (connection == null) return false;

        // 현재 저장할 플레이어 데이터가 없다면 저장에 실패함
        if (CurrentPlayerData == null) return false;

        // 이번 저장에 사용할 현재 UTC Unix 시각을 생성함
        long saveUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 전체 데이터 저장 중 발생하는 예외를 처리함
        try
        {
            // 모든 저장 작업을 하나의 트랜잭션으로 실행함
            connection.RunInTransaction(() =>
            {
                // 플레이어의 기본 상태를 저장함
                SavePlayerRecord(saveUnixTime);

                // 플레이어가 보유한 배달부 목록을 저장함
                SaveOwnedCourierRecords();

                // 플레이어가 해금한 노선 목록을 저장함
                SaveUnlockedRouteRecords();

                // 편지별 진행 상태와 읽음 여부를 저장함
                SaveLetterProgressRecords();

                // 현재 진행 중인 배달 목록을 저장함
                SaveActiveDeliveryRecords();

                // 완료된 배달 결과 목록을 저장함
                SaveDeliveryResultRecords();

                // 시설별 현재 레벨을 저장함
                SaveFacilityProgressRecords();

                // 수신한 답장 목록을 저장함
                SaveReceivedReplyRecords();

                // 읽은 답장 목록을 저장함
                SaveReadReplyRecords();
            });
        }
        catch (Exception exception)
        {
            // 전체 저장 실패 원인을 오류 로그로 출력함
            Debug.LogError($"[SaveService] 전체 저장 실패\n" +$"오류: {exception}");

            // 전체 저장 실패를 반환함
            return false;
        }

        // 트랜잭션 저장에 성공한 시각을 런타임 데이터에도 반영함
        if (!CurrentPlayerData.SetLastSaveUnixTime(saveUnixTime))
        {
            // 저장 시각 반영 실패를 오류 로그로 출력함
            Debug.LogError("[SaveService] 데이터베이스 저장은 완료됐지만 " +"런타임 마지막 저장 시각을 변경하지 못했습니다.");

            // 런타임 저장 시각 반영 실패를 반환함
            return false;
        }

        // 전체 저장 완료 로그를 출력함
        Debug.Log($"[SaveService] 전체 저장 완료\n" +$"저장 시각: {saveUnixTime}");

        // 전체 데이터 저장이 완료되었음을 반환함
        return true;
    }
    /// <summary>
    /// 플레이어의 기본 상태를 단일 저장 행으로 저장함
    /// </summary>
    private void SavePlayerRecord(long saveUnixTime)
    {
        // 현재 런타임 플레이어 데이터로 DB 저장용 플레이어 레코드를 생성함
        PlayerRecord playerRecord = new();

        // 단일 저장 슬롯을 사용하도록 SaveID를 1로 설정함
        playerRecord.SaveID = 1;

        // 현재 보유 재화를 저장함
        playerRecord.Currency = CurrentPlayerData.Currency;

        // 현재 누적 배달 완료 횟수를 저장함
        playerRecord.CompletedDeliveryCount = CurrentPlayerData.CompleteDeliveryCount;

        // 이번 저장에 사용할 Unix 시각을 저장함
        playerRecord.LastSaveUnixTime = saveUnixTime; 

        // 동일한 기본 키의 행이 있다면 교체하고 없다면 새로 추가함
        connection.InsertOrReplace(playerRecord);
    }

    /// <summary>
    /// 현재 보유한 배달부 ID 목록을 SQLite에 저장함
    /// </summary>
    private void SaveOwnedCourierRecords()
    {
        // 이전에 저장된 보유 배달부 행을 모두 삭제함
        connection.DeleteAll<OwnedCourierRecord>();

        // 현재 보유한 전체 배달부 ID를 순회함
        foreach (int courierID in CurrentPlayerData.OwnedCourierIDs)
        {
            // 현재 배달부 ID를 저장할 레코드를 생성함
            OwnedCourierRecord ownedCourierRecord = new();

            // 레코드에 현재 배달부 ID를 저장함
            ownedCourierRecord.CourierID = courierID;

            // 생성한 보유 배달부 레코드를 테이블에 추가함
            connection.Insert(ownedCourierRecord);
        }
    }
    /// <summary>
    /// 현재 해금된 노선 ID 목록을 SQLite에 저장함
    /// </summary>
    private void SaveUnlockedRouteRecords()
    {
        // 이전에 저장된 해금 노선 행을 모두 삭제함
        connection.DeleteAll<UnlockedRouteRecord>();

        // 현재 해금된 전체 노선 ID를 순회함
        foreach (int routeID in CurrentPlayerData.UnlockedRouteIDs)
        {
            // 현재 노선 ID를 저장할 레코드를 생성함
            UnlockedRouteRecord unlockedRouteRecord = new();

            // 레코드에 현재 노선 ID를 저장함
            unlockedRouteRecord.RouteID = routeID;

            // 생성한 해금 노선 레코드를 테이블에 추가함
            connection.Insert(unlockedRouteRecord);
        }
    }

    /// <summary>
    /// 현재 보유한 편지별 진행 상태와 읽음 여부를 SQLite에 저장함
    /// </summary>
    private void SaveLetterProgressRecords()
    {
        // 이전에 저장된 편지 진행 상태 행을 모두 삭제함
        connection.DeleteAll<LetterProgressRecord>();

        // 현재 보유한 전체 편지 진행 데이터를 순회함
        foreach (LetterProgressData letterProgress in CurrentPlayerData.LetterProgressesList)
        {
            // 유효하지 않은 편지 진행 데이터는 저장하지 않고 건너뜀
            if (letterProgress == null) continue;

            // 현재 편지 진행 상태를 저장할 레코드를 생성함
            LetterProgressRecord letterProgressRecord = new();

            // 레코드에 편지 ID를 저장함
            letterProgressRecord.LetterID = letterProgress.LetterID;

            // 편지 진행 상태 열거형을 정수로 변환하여 저장함
            letterProgressRecord.State = (int)letterProgress.State;

            // 레코드에 편지 읽음 여부를 저장함
            letterProgressRecord.IsRead = letterProgress.IsRead;

            // 생성한 편지 진행 레코드를 테이블에 추가함
            connection.Insert(letterProgressRecord);
        }
    }
    /// <summary>
    /// 현재 진행 중인 배달 정보를 SQLite에 저장함
    /// </summary>
    private void SaveActiveDeliveryRecords()
    {
        // 이전에 저장된 진행 중 배달 행을 모두 삭제함
        connection.DeleteAll<ActiveDeliveryRecord>();

        // 현재 진행 중인 전체 배달 데이터를 순회함
        foreach (ActiveDeliveryData activeDelivery in CurrentPlayerData.ActiveDeliveryList)
        {
            // 유효하지 않은 진행 중 배달 데이터는 저장하지 않고 건너뜀
            if(activeDelivery == null) continue;

            // 현재 진행 중 배달 정보를 저장할 레코드를 생성함
            ActiveDeliveryRecord activeDeliveryRecord = new();

            // 레코드에 배달 중인 편지 ID를 저장함
            activeDeliveryRecord.LetterID = activeDelivery.LetterID;

            // 레코드에 배정된 배달부 ID를 저장함
            activeDeliveryRecord.CourierID = activeDelivery.CourierID;

            // 레코드에 선택된 노선 ID를 저장함
            activeDeliveryRecord.RouteID = activeDelivery.RouteID;

            // 레코드에 배달 시작 Unix 시각을 저장함
            activeDeliveryRecord.StartedAtUnixTime = activeDelivery.StartedAtUnixTime;

            // 레코드에 배달 완료 예정 Unix 시각을 저장함
            activeDeliveryRecord.CompleteAtUnixTime = activeDelivery.CompleteAtUnixTime;

            // 생성한 진행 중 배달 레코드를 테이블에 추가함
            connection.Insert(activeDeliveryRecord);
        }
    }

    /// <summary>
    /// 완료된 배달 결과와 확인 여부를 SQLite에 저장함
    /// </summary>
    private void SaveDeliveryResultRecords()
    {
        // 이전에 저장된 배달 결과 행을 모두 삭제함
        connection.DeleteAll<DeliveryResultRecord>();

        // 현재 저장된 전체 배달 결과 데이터를 순회함
        foreach (DeliveryResultData deliveryResult in CurrentPlayerData.DeliveryResultsList)
        {
            // 유효하지 않은 배달 결과 데이터는 저장하지 않고 건너뜀
            if(deliveryResult == null) continue;

            // 현재 배달 결과를 저장할 레코드를 생성함
            DeliveryResultRecord deliveryResultRecord = new();

            // 레코드에 완료된 편지 ID를 저장함
            deliveryResultRecord.LetterID =deliveryResult.LetterID;

            // 레코드에 실제 지급할 보상량을 저장함
            deliveryResultRecord.RewardAmount = deliveryResult.RewardAmount;

            // 레코드에 배달 완료 Unix 시각을 저장함
            deliveryResultRecord.CompletedAtUnixTime = deliveryResult.CompletedAtUnixTime;

            // 레코드에 결과 확인 여부를 저장함
            deliveryResultRecord.IsChecked = deliveryResult.IsChecked;

            // 생성한 배달 결과 레코드를 테이블에 추가함
            connection.Insert(deliveryResultRecord);
        }
    }
    /// <summary>
    /// 시설별 현재 레벨을 SQLite에 저장함
    /// </summary>
    private void SaveFacilityProgressRecords()
    {
        // 이전에 저장된 시설 진행 상태 행을 모두 삭제함
        connection.DeleteAll<FacilityProgressRecord>();

        // 현재 저장된 전체 시설 진행 데이터를 순회함
        foreach (FacilityProgressData facilityProgress in CurrentPlayerData.FacilityProgressesList)
        {
            // 유효하지 않은 시설 진행 데이터는 저장하지 않고 건너뜀
            if(facilityProgress == null) continue;

            // 현재 시설 진행 상태를 저장할 레코드를 생성함
            FacilityProgressRecord facilityProgressRecord = new();

            // 레코드에 시설 ID를 저장함
            facilityProgressRecord.FacilityID = facilityProgress.FacilityID;

            // 레코드에 현재 시설 레벨을 저장함
            facilityProgressRecord.CurrentLevel = facilityProgress.CurrentLevel;

            // 생성한 시설 진행 레코드를 테이블에 추가함
            connection.Insert(facilityProgressRecord);
        }
    }
    /// <summary>
    /// 플레이어가 수신한 답장 ID 목록을 SQLite에 저장함
    /// </summary>
    private void SaveReceivedReplyRecords()
    {
        // 이전에 저장된 수신 답장 행을 모두 삭제함
        connection.DeleteAll<ReceivedReplyRecord>();

        // 현재 플레이어가 수신한 전체 답장 ID를 순회함
        foreach (int replyID in CurrentPlayerData.ReceivedReplyIDs)
        {
            // 유효하지 않은 답장 ID는 저장하지 않고 건너뜀
            if (replyID <= 0) continue;

            // 현재 수신 답장 ID를 저장할 레코드를 생성함
            ReceivedReplyRecord receivedReplyRecord = new();

            // 레코드에 현재 답장 ID를 저장함
            receivedReplyRecord.ReplyID = replyID;

            // 생성한 수신 답장 레코드를 테이블에 추가함
            connection.Insert(receivedReplyRecord);
        }
    }

    /// <summary>
    /// 플레이어가 읽은 답장 ID 목록을 SQLite에 저장함
    /// </summary>
    private void SaveReadReplyRecords()
    {
        // 이전에 저장된 읽은 답장 행을 모두 삭제함
        connection.DeleteAll<ReadReplyRecord>();

        // 현재 플레이어가 읽은 전체 답장 ID를 순회함
        foreach (int replyID in CurrentPlayerData.ReadReplyIds)
        {
            // 유효하지 않은 답장 ID는 저장하지 않고 건너뜀
            if (replyID <= 0) continue;

            // 현재 읽은 답장 ID를 저장할 레코드를 생성함
            ReadReplyRecord readReplyRecord = new();

            // 레코드에 현재 답장 ID를 저장함
            readReplyRecord.ReplyID = replyID;

            // 생성한 읽은 답장 레코드를 테이블에 추가함
            connection.Insert(readReplyRecord);
        }
    }
    #endregion
    #region Load
    /// <summary>
    /// SQLite에 저장된 전체 플레이어 진행 데이터를 런타임 데이터에 복원함
    /// </summary>
    public bool LoadAll()
    {
        // 데이터베이스 연결이 생성되지 않았다면 불러오기에 실패함
        if(connection == null) return false;    

        // 현재 복원할 런타임 플레이어 데이터가 없다면 불러오기에 실패함
        if(CurrentPlayerData == null) return false;

        // 전체 저장 데이터 조회 및 복원 중 발생하는 예외를 처리함
        try
        {
            // 단일 저장 슬롯의 플레이어 기본 상태를 불러옴
            PlayerRecord player = LoadPlayerRecord();

            // 플레이어 기본 상태가 존재하지 않는다면 불러오기에 실패함
            if(player  == null) return false;

            // 보유 배달부 ID 목록을 불러옴
            List<int> ownedCourierIDs = LoadOwnedCourierIDs();

            // 해금 노선 ID 목록을 불러옴
            List<int> unlockedRouteIDs = LoadUnlockedRouteIDs();

            // 편지 진행 데이터 목록을 불러옴
            List<LetterProgressData> letterProgresses = LoadLetterProgresses();

            // 진행 중 배달 데이터 목록을 불러옴
            List<ActiveDeliveryData> activeDeliveries = LoadActiveDeliveries();

            // 완료 배달 결과 목록을 불러옴
            List<DeliveryResultData> deliveryResults = LoadDeliveryResults();

            // 시설 진행 데이터 목록을 불러옴
            List<FacilityProgressData> facilityProgresses = LoadFacilityProgresses();

            // 수신 답장 ID 목록을 불러옴
            List<int> receivedReplyIDs = LoadReceivedReplyIDs();

            // 읽은 답장 ID 목록을 불러옴
            List<int> readReplyIDs = LoadReadReplyIDs();

            // 불러온 전체 데이터를 현재 런타임 PlayerSaveData에 복원함
            bool isRestored = CurrentPlayerData.RestoreData
                (player.Currency, player.CompletedDeliveryCount, ownedCourierIDs,
                unlockedRouteIDs, letterProgresses, activeDeliveries, deliveryResults,
                facilityProgresses, receivedReplyIDs, readReplyIDs, player.LastSaveUnixTime);

            // 런타임 데이터 복원에 실패했다면 불러오기에 실패함
            if(!isRestored) return false;

        }
        catch (Exception exception)
        {
            // 전체 저장 데이터 불러오기 실패 원인을 오류 로그로 출력함
            Debug.LogError($"[SaveService] 전체 저장 데이터 불러오기 실패\n" + $"오류: {exception}");

            // 전체 저장 데이터 불러오기 실패를 반환함
            return false;
        }

        // 전체 저장 데이터 불러오기 완료 로그를 출력함
        Debug.Log($"[SaveService] 전체 저장 데이터 불러오기 완료\n" +$"마지막 저장 시각: {CurrentPlayerData.LastSaveUnixTime}");

        // 전체 저장 데이터 불러오기가 완료되었음을 반환함
        return true;
    }
    /// <summary>
    /// 단일 저장 슬롯에 저장된 플레이어 기본 상태를 불러옴
    /// </summary>
    private PlayerRecord LoadPlayerRecord()
    {
        // 데이터베이스 연결이 생성되지 않았다면 플레이어 상태를 불러오지 않음
        if(connection == null) return null;

        // 단일 저장 슬롯의 기본 키 1에 해당하는 플레이어 레코드를 조회함
        // 조회된 플레이어 레코드를 반환함
        return connection.Find<PlayerRecord>(1);
    }
    /// <summary>
    /// SQLite에 저장된 보유 배달부 ID 목록을 불러옴
    /// </summary>
    private List<int> LoadOwnedCourierIDs()
    {
        // 불러온 배달부 ID를 저장할 목록을 생성함
        List<int> ownedCourierIDs = new();

        // 보유 배달부 테이블의 전체 레코드를 조회함
        List<OwnedCourierRecord> ownedCourierRecords = connection.Table<OwnedCourierRecord>().ToList();

        // 조회한 전체 보유 배달부 레코드를 순회함
        foreach (OwnedCourierRecord ownedCourierRecord in ownedCourierRecords)
        {
            // 유효하지 않은 배달부 ID는 목록에 추가하지 않고 건너뜀
            if (ownedCourierRecord == null || ownedCourierRecord.CourierID <= 0) continue;

            // 조회한 배달부 ID를 반환 목록에 추가함
            ownedCourierIDs.Add(ownedCourierRecord.CourierID);
        }

        // 불러온 보유 배달부 ID 목록을 반환함
        return ownedCourierIDs;
    }
    /// <summary>
    /// SQLite에 저장된 해금 노선 ID 목록을 불러옴
    /// </summary>
    private List<int> LoadUnlockedRouteIDs()
    {
        // 불러온 노선 ID를 저장할 목록을 생성함
        List<int> unlockedRouteIDs = new();

        // 해금 노선 테이블의 전체 레코드를 조회함
        List<UnlockedRouteRecord> unlockedRouteRecords = connection.Table<UnlockedRouteRecord>().ToList();

        // 조회한 전체 해금 노선 레코드를 순회함
        foreach (UnlockedRouteRecord unlockedRouteRecord in unlockedRouteRecords)
        {
            // 유효하지 않은 레코드 또는 노선 ID는 목록에 추가하지 않고 건너뜀
            if(unlockedRouteRecord == null || unlockedRouteRecord.RouteID <= 0) continue;

            // 조회한 노선 ID를 반환 목록에 추가함
            unlockedRouteIDs.Add(unlockedRouteRecord.RouteID);
        }

        // 불러온 해금 노선 ID 목록을 반환함
        return unlockedRouteIDs;
    }

    /// <summary>
    /// SQLite에 저장된 편지별 진행 상태와 읽음 여부를 불러옴
    /// </summary>
    private List<LetterProgressData> LoadLetterProgresses()
    {
        // 불러온 편지 진행 데이터를 저장할 목록을 생성함
        List<LetterProgressData> letterProgressData = new();

        // 편지 진행 상태 테이블의 전체 레코드를 조회함
        List<LetterProgressRecord> letterProgressRecords = connection.Table<LetterProgressRecord>().ToList();

        // 조회한 전체 편지 진행 레코드를 순회함
        foreach (LetterProgressRecord letterProgressRecord in letterProgressRecords)
        {
            // 유효하지 않은 레코드 또는 편지 ID는 목록에 추가하지 않고 건너뜀
            if(letterProgressRecord == null || letterProgressRecord.LetterID <= 0) continue;

            // 저장된 진행 상태 값이 ELetterProgressState에 정의되지 않았다면 건너뜀
            if (!Enum.IsDefined(typeof(ELetterProgressState), letterProgressRecord.State)) continue;

            // 저장된 레코드 값을 사용하여 편지 진행 데이터를 생성함
            LetterProgressData letterProgress = new LetterProgressData(letterProgressRecord.LetterID, (ELetterProgressState)letterProgressRecord.State, letterProgressRecord.IsRead);

            // 생성한 편지 진행 데이터를 반환 목록에 추가함
            letterProgressData.Add(letterProgress);
        }

        // 불러온 편지 진행 데이터 목록을 반환함
        return letterProgressData;
    }

    /// <summary>
    /// SQLite에 저장된 진행 중 배달 목록을 불러옴
    /// </summary>
    private List<ActiveDeliveryData> LoadActiveDeliveries()
    {
        // 불러온 진행 중 배달 데이터를 저장할 목록을 생성함
        List<ActiveDeliveryData> activeDeliveryData = new();

        // 진행 중 배달 테이블의 전체 레코드를 조회함
        List<ActiveDeliveryRecord> activeDeliveryRecords = connection.Table<ActiveDeliveryRecord>().ToList();

        // 조회한 전체 진행 중 배달 레코드를 순회함
        foreach (ActiveDeliveryRecord activeDeliveryRecord in activeDeliveryRecords)
        {
            // 유효하지 않은 레코드는 목록에 추가하지 않고 건너뜀
            if(activeDeliveryRecord == null) continue;

            // 편지·배달부·노선 ID 중 하나라도 유효하지 않다면 건너뜀
            if (activeDeliveryRecord.LetterID <= 0) continue;
            if(activeDeliveryRecord.CourierID <= 0) continue;
            if(activeDeliveryRecord.RouteID <= 0) continue;

            // 배달 시작 시각 또는 완료 예정 시각이 유효하지 않다면 건너뜀
            if(activeDeliveryRecord.StartedAtUnixTime <= 0 || activeDeliveryRecord.CompleteAtUnixTime <= 0) continue;

            // 완료 예정 시각이 시작 시각보다 빠르다면 건너뜀
            if (activeDeliveryRecord.CompleteAtUnixTime < activeDeliveryRecord.StartedAtUnixTime) continue;

            // 저장된 레코드 값을 사용하여 진행 중 배달 데이터를 생성함
            ActiveDeliveryData activeDelivery = new ActiveDeliveryData(activeDeliveryRecord.LetterID, activeDeliveryRecord.CourierID, activeDeliveryRecord.RouteID, activeDeliveryRecord.StartedAtUnixTime, activeDeliveryRecord.CompleteAtUnixTime);

            // 생성한 진행 중 배달 데이터를 반환 목록에 추가함
            activeDeliveryData.Add(activeDelivery);
        }

        // 불러온 진행 중 배달 데이터 목록을 반환함
        return activeDeliveryData;
    }

    /// <summary>
    /// SQLite에 저장된 완료 배달 결과 목록을 불러옴
    /// </summary>
    private List<DeliveryResultData> LoadDeliveryResults()
    {
        // 불러온 배달 결과 데이터를 저장할 목록을 생성함
        List<DeliveryResultData> deliveryResults = new();

        // 완료 배달 결과 테이블의 전체 레코드를 조회함
        List<DeliveryResultRecord> deliveryResultRecords = connection.Table<DeliveryResultRecord>().ToList();

        // 조회한 전체 배달 결과 레코드를 순회함
        foreach (DeliveryResultRecord deliveryResultRecord in deliveryResultRecords)
        {
            // 유효하지 않은 레코드 또는 편지 ID는 목록에 추가하지 않고 건너뜀
            if(deliveryResultRecord == null || deliveryResultRecord.LetterID <= 0) continue;

            // 보상량 또는 완료 시각이 음수라면 유효하지 않은 데이터로 처리함
            if(deliveryResultRecord.RewardAmount < 0 || deliveryResultRecord.CompletedAtUnixTime < 0) continue;

            // 저장된 확인 여부까지 포함하여 배달 결과 데이터를 생성함
            DeliveryResultData delivery = new DeliveryResultData(deliveryResultRecord.LetterID, deliveryResultRecord.RewardAmount, deliveryResultRecord.CompletedAtUnixTime, deliveryResultRecord.IsChecked);

            // 생성한 배달 결과 데이터를 반환 목록에 추가함
            deliveryResults.Add(delivery);
        }

        // 불러온 배달 결과 데이터 목록을 반환함
        return deliveryResults;
    }

    /// <summary>
    /// SQLite에 저장된 시설별 진행 상태를 불러옴
    /// </summary>
    private List<FacilityProgressData> LoadFacilityProgresses()
    {
        // 불러온 시설 진행 데이터를 저장할 목록을 생성함
        List<FacilityProgressData> facilityProgressDatas = new();

        // 시설 진행 상태 테이블의 전체 레코드를 조회함
        List<FacilityProgressRecord> facilityProgressRecords = connection.Table<FacilityProgressRecord>().ToList();

        // 조회한 전체 시설 진행 레코드를 순회함
        foreach (FacilityProgressRecord facilityProgressRecord in facilityProgressRecords)
        {
            // 유효하지 않은 레코드 또는 시설 ID는 목록에 추가하지 않고 건너뜀
            if(facilityProgressRecord == null || facilityProgressRecord.FacilityID <= 0) continue;

            // 현재 시설 레벨이 음수라면 유효하지 않은 데이터로 처리함
            if (facilityProgressRecord.CurrentLevel < 0) continue;

            // 저장된 시설 ID와 현재 레벨로 시설 진행 데이터를 생성함
            FacilityProgressData facility = new FacilityProgressData(facilityProgressRecord.FacilityID, facilityProgressRecord.CurrentLevel);

            // 생성한 시설 진행 데이터를 반환 목록에 추가함
            facilityProgressDatas.Add(facility);
        }

        // 불러온 시설 진행 데이터 목록을 반환함
        return facilityProgressDatas;
    }

    /// <summary>
    /// SQLite에 저장된 수신 답장 ID 목록을 불러옴
    /// </summary>
    private List<int> LoadReceivedReplyIDs()
    {
        // 불러온 수신 답장 ID를 저장할 목록을 생성함
        List<int> receivedReplyIDs = new List<int>();

        // 수신 답장 테이블의 전체 레코드를 조회함
        List<ReceivedReplyRecord> receivedReplyRecords = connection.Table<ReceivedReplyRecord>().ToList();

        // 조회한 전체 수신 답장 레코드를 순회함
        foreach (ReceivedReplyRecord receivedReplyRecord in receivedReplyRecords)
        {
            // 유효하지 않은 레코드 또는 답장 ID는 목록에 추가하지 않고 건너뜀
            if (receivedReplyRecord == null || receivedReplyRecord.ReplyID <= 0) continue;

            // 조회한 답장 ID를 반환 목록에 추가함
            receivedReplyIDs.Add(receivedReplyRecord.ReplyID);
        }

        // 불러온 수신 답장 ID 목록을 반환함
        return receivedReplyIDs;
    }

    /// <summary>
    /// SQLite에 저장된 읽은 답장 ID 목록을 불러옴
    /// </summary>
    private List<int> LoadReadReplyIDs()
    {
        // 불러온 읽은 답장 ID를 저장할 목록을 생성함
        List<int> readReplyIDs = new();
        // 읽은 답장 테이블의 전체 레코드를 조회함
        List<ReadReplyRecord> readReplyRecords = connection.Table<ReadReplyRecord>().ToList();

        // 조회한 전체 읽은 답장 레코드를 순회함
        foreach (ReadReplyRecord readReplyRecord in readReplyRecords)
        {
            // 유효하지 않은 레코드 또는 답장 ID는 목록에 추가하지 않고 건너뜀
            if (readReplyRecord == null || readReplyRecord.ReplyID <= 0) continue;

            // 조회한 답장 ID를 반환 목록에 추가함
            readReplyIDs.Add(readReplyRecord.ReplyID);
        }

        // 불러온 읽은 답장 ID 목록을 반환함
        return readReplyIDs;
    }
    #endregion
}
