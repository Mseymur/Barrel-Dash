using UnityEngine;
using UnityEngine.UI;

public class LockInFeedback : MonoBehaviour
{
    public Image fillImage;
    public CanvasGroup canvasGroup;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    public void UpdateProgress(float progress)
    {
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (fillImage != null) fillImage.fillAmount = progress;
    }

    public void Hide()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (fillImage != null) fillImage.fillAmount = 0f;
    }
}
