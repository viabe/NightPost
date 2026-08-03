using UnityEngine;

[System.Serializable]
public class BGMTrackData
{
    [SerializeField] private EBGMType bgmType;
    [SerializeField] private AudioClip audioClip;

    public EBGMType BGMType => bgmType;
    public AudioClip AudioClip => audioClip;
}
