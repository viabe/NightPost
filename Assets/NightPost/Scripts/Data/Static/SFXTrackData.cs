using UnityEngine;

[System.Serializable]
public class SFXTrackData
{
    [SerializeField] private ESFXType soundType;
    [SerializeField] private AudioClip audioClip;

    public ESFXType SoundType => soundType;
    public AudioClip AudioClip => audioClip;
}
