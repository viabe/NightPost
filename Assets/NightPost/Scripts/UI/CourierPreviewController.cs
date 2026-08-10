using UnityEngine;
using UnityEngine.UI;

public class CourierPreviewController : MonoBehaviour
{
    [SerializeField] private Image _selectedCourierImage;

    /// <summary>
    /// 선택한 배달부 이미지를 변경함
    /// </summary>
    public void SetCourierImage(Sprite sprite)
    {
        if (_selectedCourierImage == null) return;

        _selectedCourierImage.sprite = sprite;
    }
}
