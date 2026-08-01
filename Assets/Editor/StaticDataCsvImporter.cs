using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// 사람이 CSV에서 편하게 작성한 정적 데이터를
// Unity가 사용할 수 있는 ScriptableObject 에셋으로 변환해주는 에디터 전용 도구
public static class StaticDataCSVImporter
{
    #region CSV 경로
    private const string LetterCsvPath = "Assets/NightPost/Data/CSV/Letters.csv";
    private const string CourierCsvPath = "Assets/NightPost/Data/CSV/Couriers.csv";
    private const string RouteCsvPath = "Assets/NightPost/Data/CSV/Routes.csv";
    private const string FacilityCsvPath = "Assets/NightPost/Data/CSV/Facilities.csv";
    private const string ReplyCsvPath = "Assets/NightPost/Data/CSV/Replies.csv";
    #endregion

    #region 출력 경로
    private const string LetterOutputFolder = "Assets/NightPost/Data/Static/Letters";
    private const string CourierOutputFolder = "Assets/NightPost/Data/Static/Couriers";
    private const string RouteOutputFolder = "Assets/NightPost/Data/Static/Routes";
    private const string FacilityOutputFolder = "Assets/NightPost/Data/Static/Facilities";
    private const string ReplyOutputFolder = "Assets/NightPost/Data/Static/Replies";
    #endregion
    #region Unity 메뉴
    // 상단 메뉴에 추가됨 
    [MenuItem("NightPost/Static Data/Import All CSV")]
    private static void ImportAllMenu()
    {
        int importedCount = 0;

        importedCount += ImportLetters();
        importedCount += ImportCouriers();
        importedCount += ImportRoutes();
        importedCount += ImportFacilities();
        importedCount += ImportReplies();

        FinishImport($"전체 정적 데이터 Import 완료: {importedCount}개");
    }
    [MenuItem("NightPost/Static Data/Import/Letters")]
    private static void ImportLettersMenu()
    {
        int importedCount = ImportLetters();
        FinishImport($"Letter Import 완료: {importedCount}개");
    }
    [MenuItem("NightPost/Static Data/Import/Couriers")]
    private static void ImportCouriersMenu()
    {
        int importedCount = ImportCouriers();

        FinishImport($"Courier Import 완료: {importedCount}개");
    }
    [MenuItem("NightPost/Static Data/Import/Routes")]
    private static void ImportRoutesMenu()
    {
        int importedCount = ImportRoutes();

        FinishImport($"Route Import 완료: {importedCount}개");
    }

    [MenuItem("NightPost/Static Data/Import/Facilities")]
    private static void ImportFacilitiesMenu()
    {
        int importedCount = ImportFacilities();

        FinishImport($"Facility Import 완료: {importedCount}개");
    }

    [MenuItem("NightPost/Static Data/Import/Replies")]
    private static void ImportRepliesMenu()
    {
        int importedCount = ImportReplies();

        FinishImport($"Reply Import 완료: {importedCount}개");
    }
    #endregion

    #region Letter Import
    // 흐름도
    // 1. CSV 읽기
    // 2. 출력 폴더 확인
    // 3. 행 반복
    // 4. 컬럼 수 검사
    // 5. 타입 변환
    // 6. 기존 SO 조회 또는 생성
    // 7. private 필드에 값 입력
    // 8. 변경 적용
    // 9. 생성 개수반환
    private static int ImportLetters()
    {
        List<List<string>> rows = ReadCSV(LetterCsvPath);
        if (rows == null) return 0;

        EnsureAssetFolder(LetterOutputFolder);

        int importedCount = 0;

        // 0번 행은 Header
        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> row = rows[rowIndex];
            int csvLine = rowIndex + 1;

            if (IsEmptyRow(row)) continue;
            if (row.Count < 10)
            {
                LogColumnError(LetterCsvPath, csvLine, 10, row.Count);
                continue;
            }
            if (!TryParseInt(row[0], LetterCsvPath, csvLine, "letterID", out int letterID)) continue;
            if (!TryParseEnum(row[4], LetterCsvPath, csvLine, "destinationRegion", out ERegionType destinationRegion)) continue;
            if (!TryParseEnum(row[5], LetterCsvPath, csvLine, "urgency", out ELetterUrgency urgency)) continue;
            if (!TryParseEnum(row[6], LetterCsvPath, csvLine, "weight", out ELetterWeight weight)) continue;
            if (!TryParseInt(row[7], LetterCsvPath, csvLine, "letterReward", out int letterReward)) continue;
            if (!TryParseBool(row[8], LetterCsvPath, csvLine, "unlockedByDefault", out bool unlockedByDefault)) continue;
            if (!TryParseInt(row[9], LetterCsvPath, csvLine, "requiredCompletedDeliveryCount", out int requiredCompletedDeliveryCount)) continue;

            LetterStaticData asset = GetOrCreateAsset<LetterStaticData>(LetterOutputFolder, $"Letter_{letterID}.asset");
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            serializedObject.FindProperty("letterID").intValue = letterID;
            serializedObject.FindProperty("senderName").stringValue = row[1];
            serializedObject.FindProperty("letterTitle").stringValue = row[2];
            serializedObject.FindProperty("letterBody").stringValue = DecodeNewLines(row[3]);
            serializedObject.FindProperty("destinationRegion").intValue = (int)destinationRegion;
            serializedObject.FindProperty("urgency").intValue = (int)urgency;
            serializedObject.FindProperty("weight").intValue = (int)weight;
            serializedObject.FindProperty("letterReward").intValue = letterReward;

            ApplyUnlockCondition(serializedObject.FindProperty("unlockCondition"), unlockedByDefault, requiredCompletedDeliveryCount);
            ApplySerializedObject(serializedObject, asset);

            importedCount++;
        }
        return importedCount;
    }
    #endregion

    #region Courier Import
    private static int ImportCouriers()
    {
        List<List<string>> rows = ReadCSV(CourierCsvPath);

        if (rows == null) return 0;

        EnsureAssetFolder(CourierOutputFolder);

        int importedCount = 0;

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> row = rows[rowIndex];
            int csvLine = rowIndex + 1;
            if (IsEmptyRow(row)) continue;
            if (row.Count < 9)
            {
                LogColumnError(CourierCsvPath, csvLine, 9, row.Count);
                continue;
            }
            if (!TryParseInt(row[0], CourierCsvPath, csvLine, "courierID", out int courierID)) continue;
            if (!TryParseEnum(row[2], CourierCsvPath, csvLine, "transportation", out EVehicleType transportation)) continue;
            if (!TryParseFloat(row[3], CourierCsvPath, csvLine, "speed", out float speed)) continue;
            if (!TryParseEnum(row[4], CourierCsvPath, csvLine, "traitType", out ECourierTraitType traitType)) continue;
            if (!TryParseFloat(row[5], CourierCsvPath, csvLine, "timeReductionRate", out float timeReductionRate)) continue;
            if (!TryParseBool(row[7], CourierCsvPath, csvLine, "unlockedByDefault", out bool unlockedByDefault)) continue;
            if (!TryParseInt(row[8], CourierCsvPath, csvLine, "requiredCompletedDeliveryCount", out int requiredCompletedDeliveryCount)) continue;

            CourierStaticData asset = GetOrCreateAsset<CourierStaticData>(CourierOutputFolder, $"Courier_{courierID}.asset");

            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();
            serializedObject.FindProperty("courierID").intValue = courierID;
            serializedObject.FindProperty("courierName").stringValue = row[1];
            serializedObject.FindProperty("transportation").intValue = (int)transportation;
            serializedObject.FindProperty("speed").floatValue = speed;

            SerializedProperty traitProperty = serializedObject.FindProperty("trait");
            if (traitProperty == null)
            {
                Debug.LogError($"[StaticDataCsvImporter] " + $"CourierStaticData의 trait 필드를 찾을 수 없습니다. " + $"CSV {csvLine}행");
                continue;
            }
            traitProperty.FindPropertyRelative("traitType").intValue = (int)traitType;

            traitProperty.FindPropertyRelative("timeReductionRate").floatValue = timeReductionRate;

            serializedObject.FindProperty("courierImage").objectReferenceValue = LoadSprite(row[6]);

            ApplyUnlockCondition(serializedObject.FindProperty("unlockCondition"), unlockedByDefault, requiredCompletedDeliveryCount);

            ApplySerializedObject(serializedObject, asset);

            importedCount++;
        }
        return importedCount;
    }
    #endregion
    #region Route Import
    private static int ImportRoutes()
    {
        List<List<string>> rows = ReadCSV(RouteCsvPath);
        if (rows == null) return 0;

        EnsureAssetFolder(RouteOutputFolder);

        int importedCount = 0;

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> row = rows[rowIndex];
            int csvLine = rowIndex + 1;
            if (IsEmptyRow(row)) continue;
            if (row.Count < 7)
            {
                LogColumnError(RouteCsvPath, csvLine, 7, row.Count);
                continue;
            }
            if (!TryParseInt(row[0], RouteCsvPath, csvLine, "routeID", out int routeID)) continue;
            if (!TryParseEnum(row[2], RouteCsvPath, csvLine, "regionType", out ERegionType regionType)) continue;
            if (!TryParseFloat(row[3], RouteCsvPath, csvLine, "baseDeliveryTimeSeconds", out float baseDeliveryTimeSeconds)) continue;
            if (!TryParseEnum(row[4], RouteCsvPath, csvLine, "difficulty", out ERouteDifficulty difficulty)) continue;
            if (!TryParseBool(row[5], RouteCsvPath, csvLine, "unlockedByDefault", out bool unlockedByDefault)) continue;
            if (!TryParseInt(row[6], RouteCsvPath, csvLine, "requiredCompletedDeliveryCount", out int requiredCompletedDeliveryCount)) continue;

            RouteStaticData asset = GetOrCreateAsset<RouteStaticData>(RouteOutputFolder, $"Route_{routeID}.asset");

            SerializedObject serializedObject = new SerializedObject(asset);

            serializedObject.Update();

            serializedObject.FindProperty("routeID").intValue = routeID;
            serializedObject.FindProperty("routeName").stringValue = row[1];
            serializedObject.FindProperty("regionType").intValue = (int)regionType;

            serializedObject.FindProperty("baseDeliveryTimeSeconds").floatValue = baseDeliveryTimeSeconds;

            serializedObject.FindProperty("difficulty").intValue = (int)difficulty;

            ApplyUnlockCondition(serializedObject.FindProperty("unlockCondition"), unlockedByDefault, requiredCompletedDeliveryCount);

            ApplySerializedObject(serializedObject, asset);

            importedCount++;
        }
        return importedCount;
    }
    #endregion
    #region Reply Import
    private static int ImportReplies()
    {
        List<List<string>> rows = ReadCSV(ReplyCsvPath);

        if (rows == null) return 0;

        EnsureAssetFolder(ReplyOutputFolder);

        int importedCount = 0;

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> row = rows[rowIndex];
            int csvLine = rowIndex + 1;

            if (IsEmptyRow(row)) continue;

            if (row.Count < 6)
            {
                LogColumnError(ReplyCsvPath, csvLine, 6, row.Count);
                continue;
            }
            if (!TryParseInt(row[0], ReplyCsvPath, csvLine, "replyID", out int replyID)) continue;
            if (!TryParseInt(row[1], ReplyCsvPath, csvLine, "linkedLetterID", out int linkedLetterID)) continue;

            ReplyStaticData asset = GetOrCreateAsset<ReplyStaticData>(ReplyOutputFolder, $"Reply_{replyID}.asset");

            SerializedObject serializedObject = new SerializedObject(asset);

            serializedObject.Update();

            serializedObject.FindProperty("replyID").intValue = replyID;
            serializedObject.FindProperty("linkedLetterID").intValue = linkedLetterID;
            serializedObject.FindProperty("senderName").stringValue = row[2];
            serializedObject.FindProperty("replyTitle").stringValue = row[3];
            serializedObject.FindProperty("replyBody").stringValue = DecodeNewLines(row[4]);
            serializedObject.FindProperty("replyImage").objectReferenceValue = LoadSprite(row[5]);

            ApplySerializedObject(serializedObject, asset);

            importedCount++;
        }

        return importedCount;
    }
    #endregion
    #region Facility Import
    private static int ImportFacilities()
    {
        List<List<string>> rows = ReadCSV(FacilityCsvPath);

        if (rows == null) return 0;

        EnsureAssetFolder(FacilityOutputFolder);

        Dictionary<int, FacilityImportData> facilityGroups = new Dictionary<int, FacilityImportData>();

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> row = rows[rowIndex];
            int csvLine = rowIndex + 1;

            if (IsEmptyRow(row)) continue;
            if (row.Count < 7)
            {
                LogColumnError(FacilityCsvPath, csvLine, 7, row.Count);
                continue;
            }

            if (!TryParseInt(row[0], FacilityCsvPath, csvLine, "facilityID", out int facilityID)) continue;
            if (!TryParseInt(row[3], FacilityCsvPath, csvLine, "level", out int level)) continue;
            if (!TryParseInt(row[4], FacilityCsvPath, csvLine, "upgradeCost", out int upgradeCost)) continue;
            if (!TryParseEnum(row[5], FacilityCsvPath, csvLine, "effectType", out EFacilityEffectType effectType)) continue;
            if (!TryParseFloat(row[6], FacilityCsvPath, csvLine, "effectValue", out float effectValue)) continue;

            if (!facilityGroups.TryGetValue(facilityID, out FacilityImportData facilityData))
            {
                facilityData = new FacilityImportData
                {
                    FacilityID = facilityID,
                    FacilityName = row[1],
                    Description = row[2],
                };
                facilityGroups.Add(facilityID, facilityData);
            }
            else
            {
                if (facilityData.FacilityName != row[1] || facilityData.Description != row[2])
                {
                    Debug.LogWarning($"[StaticDataCsvImporter] " + $"Facility ID {facilityID}의 이름 또는 설명이 " + $"행마다 다릅니다. 첫 번째 값을 사용합니다.");
                }
            }
            if (ContainsFacilityLevel(facilityData.Levels, level))
            {
                Debug.LogError($"[StaticDataCsvImporter] " + $"Facility ID {facilityID}에 " + $"중복 레벨 {level}이 있습니다. " + $"CSV {csvLine}행을 제외합니다.");

                continue;
            }

            facilityData.Levels.Add(
                new FacilityLevelImportData
                {
                    Level = level,
                    UpgradeCost = upgradeCost,
                    EffectType = effectType,
                    EffectValue = effectValue
                });
        }

        int importedCount = 0;
        foreach (FacilityImportData facilityImportData in facilityGroups.Values)
        {
            facilityImportData.Levels.Sort((left, right) => left.Level.CompareTo(right.Level));

            FacilityStaticData asset = GetOrCreateAsset<FacilityStaticData>(FacilityOutputFolder, $"Facility_{facilityImportData.FacilityID}.asset");

            SerializedObject serializedObject = new SerializedObject(asset);

            serializedObject.Update();

            serializedObject.FindProperty("facilityID").intValue = facilityImportData.FacilityID;
            serializedObject.FindProperty("facilityName").stringValue = facilityImportData.FacilityName;
            serializedObject.FindProperty("description").stringValue = facilityImportData.Description;

            SerializedProperty levelDataProperty = serializedObject.FindProperty("levelData");

            levelDataProperty.arraySize = facilityImportData.Levels.Count;

            for (int index = 0; index < facilityImportData.Levels.Count; index++)
            {
                FacilityLevelImportData level = facilityImportData.Levels[index];
                SerializedProperty levelProperty = levelDataProperty.GetArrayElementAtIndex(index);
                levelProperty.FindPropertyRelative("level").intValue = level.Level;
                levelProperty.FindPropertyRelative("upgradeCost").intValue = level.UpgradeCost;
                levelProperty.FindPropertyRelative("effectType").intValue = (int)level.EffectType;
                levelProperty.FindPropertyRelative("effectValue").floatValue = level.EffectValue;
            }
            ApplySerializedObject(serializedObject, asset);
            importedCount++;
        }

        return importedCount;
    }
    #endregion

    #region 제네릭 에셋 생성
    private static T GetOrCreateAsset<T>(string folderPath, string fileName) where T : ScriptableObject
    {
        EnsureAssetFolder(folderPath);
        string assetPath = $"{folderPath}/{fileName}";

        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset != null) return asset;

        asset = ScriptableObject.CreateInstance<T>();

        AssetDatabase.CreateAsset(asset, assetPath);

        return asset;
    }
    #endregion

    #region CSV 읽기 및 파싱
    private static List<List<string>> ReadCSV(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[StaticDataCsvImporter]" + $"CSV 파일을 찾을 수 없습니다: {csvPath}");

            return null;
        }
        try
        {
            string csvText = File.ReadAllText(csvPath, Encoding.UTF8);
            return ParseCsv(csvText, csvPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StaticDataCsvImporter] " + $"CSV 읽기 실패: {csvPath}\n" + e);

            return null;
        }
    }

    private static List<List<string>> ParseCsv(string csvText, string csvPath)
    {
        List<List<string>> rows = new List<List<string>>();

        List<string> currentRow = new List<string>();

        StringBuilder currentValue = new StringBuilder();

        bool insideQuotes = false;

        for (int index = 0; index < csvText.Length; index++)
        {
            char currentCharacter = csvText[index];
            if (currentCharacter == '"')
            {
                bool isEscapedQuote = insideQuotes && index + 1 < csvText.Length && csvText[index + 1] == '"';
                if (isEscapedQuote)
                {
                    currentValue.Append('"');
                    index++;
                }
                else insideQuotes = !insideQuotes;
                continue;
            }

            if (currentCharacter == ',' && !insideQuotes)
            {
                currentRow.Add(currentValue.ToString());
                currentValue.Clear();
                continue;
            }

            bool isLineBreak = currentCharacter == '\n' || currentCharacter == '\r';
            if (isLineBreak && !insideQuotes)
            {
                if (currentCharacter == '\r' && index + 1 < csvText.Length && csvText[index + 1] == '\n')
                {
                    index++;
                }

                currentRow.Add(currentValue.ToString());

                currentValue.Clear();

                rows.Add(currentRow);

                currentRow = new List<string>();

                continue;
            }
            currentValue.Append(currentCharacter);
        }

        if (insideQuotes)
        {
            Debug.LogError($"[StaticDataCsvImporter] " + $"닫히지 않은 큰따옴표가 있습니다: {csvPath}");
            return null;
        }

        if (currentValue.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentValue.ToString());
            rows.Add(currentRow);
        }
        if (rows.Count > 0 && rows[0].Count > 0)
        {
            rows[0][0] = rows[0][0].TrimStart('\uFEFF');
        }

        return rows;
    }
    #endregion
    #region 타입 변환
    private static bool TryParseInt(string value, string csvPath, int csvLine, string columnName, out int result)
    {
        if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result)) return true;
        Debug.LogError($"[StaticDataCsvImporter] " + $"{csvPath} {csvLine}행의 " + $"{columnName} 값을 int로 변환할 수 없습니다: " + $"'{value}'");

        return false;
    }
    private static bool TryParseFloat(string value, string csvPath, int csvLine, string columnName, out float result)
    {
        if (float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result)) return true;

        Debug.LogError($"[StaticDataCsvImporter] " + $"{csvPath} {csvLine}행의 " + $"{columnName} 값을 float로 변환할 수 없습니다: " + $"'{value}'");

        return false;
    }
    private static bool TryParseBool(string value, string csvPath, int csvLine, string columnName, out bool result)
    {
        if (bool.TryParse(value.Trim(), out result)) return true;

        Debug.LogError($"[StaticDataCsvImporter] " + $"{csvPath} {csvLine}행의 " + $"{columnName} 값을 bool로 변환할 수 없습니다: " + $"'{value}'");

        return false;
    }
    private static bool TryParseEnum<TEnum>(string value, string csvPath, int csvLine, string columnName, out TEnum result) where TEnum : struct, Enum
    {
        if (Enum.TryParse(value.Trim(), true, out result)) return true;

        Debug.LogError($"[StaticDataCsvImporter] " + $"{csvPath} {csvLine}행의 " + $"{columnName} 값을 " + $"{typeof(TEnum).Name}으로 변환할 수 없습니다: " + $"'{value}'");

        return false;
    }
    #endregion
    #region SerializedObject 공통 처리
    private static void ApplyUnlockCondition(SerializedProperty unlockProperty, bool unlockedByDefault, int requiredCompletedDeliveryCount)
    {
        if (unlockProperty == null)
        {
            Debug.LogError("[StaticDataCsvImporter] " + "unlockCondition 필드를 찾을 수 없습니다.");
            return;
        }

        unlockProperty.FindPropertyRelative("unlockedByDefault").boolValue = unlockedByDefault;
        unlockProperty.FindPropertyRelative("requiredCompletedDeliveryCount").intValue = requiredCompletedDeliveryCount;
    }

    private static void ApplySerializedObject(SerializedObject serializedObject, UnityEngine.Object asset)
    {
        // SerializedProperty의 변경사항을 실제 SO에 적용
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        // 변경사항 저장 
        EditorUtility.SetDirty(asset);
    }
    #endregion
    #region AssetDatabase  공통 처리
    private static void EnsureAssetFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] folderNames = folderPath.Split('/');

        if (folderNames.Length == 0 || folderNames[0] != "Assets")
        {
            throw new ArgumentException($"Unity Asset 경로가 아닙니다: {folderPath}");
        }

        string currentPath = "Assets";
        for (int index = 1; index < folderNames.Length; index++)
        {
            string nextPath = $"{currentPath}/{folderNames[index]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, folderNames[index]);
            }
            currentPath = nextPath;
        }
    }

    private static Sprite LoadSprite(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath)) return null;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath.Trim());
        if (sprite == null)
        {
            Debug.LogWarning($"[StaticDataCsvImporter] " + $"Sprite를 찾을 수 없습니다: {assetPath}");
        }
        return sprite;
    }
    private static void FinishImport(string message)
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StaticDataCsvImporter] {message}");
    }
    #endregion
    #region 기타 공통 함수
    private static bool IsEmptyRow(List<string> row)
    {
        if (row == null || row.Count == 0) return true;
        foreach (string value in row)
        {
            if (!string.IsNullOrWhiteSpace(value)) return false;
        }
        return true;
    }
    private static string DecodeNewLines(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("\\r\\n", "\n").Replace("\\n", "\n");
    }
    private static void LogColumnError(string csvPath, int csvLine, int requiredColumnCount, int currentColumnCount)
    {
        Debug.LogError($"[StaticDataCsvImporter] " + $"{csvPath} {csvLine}행의 컬럼이 부족합니다. " + $"필요: {requiredColumnCount}, " + $"현재: {currentColumnCount}");
    }
    private static bool ContainsFacilityLevel(List<FacilityLevelImportData> levels, int level)
    {
        foreach (FacilityLevelImportData data in levels)
        {
            if (data.Level == level) return true;
        }

        return false;
    }
    #endregion
    #region Facility Import DTO

    private class FacilityImportData
    {
        public int FacilityID;
        public string FacilityName;
        public string Description;

        public readonly List<FacilityLevelImportData> Levels = new List<FacilityLevelImportData>();
    }

    private class FacilityLevelImportData
    {
        public int Level;
        public int UpgradeCost;
        public EFacilityEffectType EffectType;
        public float EffectValue;
    }

    #endregion
}
