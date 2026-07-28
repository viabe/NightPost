using System;
using UnityEngine;

[Serializable]
public class CourierTraitData
{
    // 특성 종류
    [SerializeField] private ECourierTraitType traitType = ECourierTraitType.None;
    // 배달 시간 감소율
    [SerializeField, Range(0f, 1f)] private float timeReductionRate = 0.2f;

    public ECourierTraitType TraitType => traitType;
    public float TimeReductionRate => timeReductionRate;
}
[CreateAssetMenu(fileName = "Courier_", menuName = "NightPost/Static Data/Courier")]
public class CourierStaticData : ScriptableObject
{
    // 배달부 ID
    [SerializeField] private int courierID = 0;
    // 배달부 이름
    [SerializeField] private string courierName = string.Empty;
    // 이동 수단
    [SerializeField] private EVehicleType transportation = EVehicleType.None;
    // 기본 속도
    [SerializeField] private float speed = 1.0f;
    // 특성
    [SerializeField] private CourierTraitData trait = new();
   
    // 이미지
    [SerializeField] private Sprite courierImage = null;
    // 해금 조건
    [SerializeField] private UnlockConditionData unlockCondition = new();

    public int CourierID => courierID;
    public string CourierName => courierName;
    public EVehicleType Transportation => transportation;
    public float Speed => speed;
    public CourierTraitData Trait => trait;
    public Sprite CourierImage => courierImage;
    public UnlockConditionData UnlockCondition => unlockCondition;
}
