using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UISlideAnimation : MonoBehaviour
{
    public float slideDuration = 0.3f;

    private RectTransform rectTransform;
    private Coroutine slideCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Call this from GameManager to slide to the new position in the hierarchy
    public void SlideToPosition(int targetIndex)
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(SlideToSiblingIndex(targetIndex));
    }

    IEnumerator SlideToSiblingIndex(int targetIndex)
    {
        Transform parent = rectTransform.parent;
        int originalIndex = rectTransform.GetSiblingIndex();

        // Get current world position
        Vector3 startWorldPos = rectTransform.position;

        // Move to target index and force layout update
        rectTransform.SetSiblingIndex(targetIndex);
        LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);
        Vector3 targetWorldPos = rectTransform.position;

        // Move back to original index and force layout update
        rectTransform.SetSiblingIndex(originalIndex);
        LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);

        // Animate by moving in world space, while layout group controls anchored position
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            Vector3 lerpedPos = Vector3.Lerp(startWorldPos, targetWorldPos, elapsed / slideDuration);
            rectTransform.position = lerpedPos;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Snap to target position and update sibling index
        rectTransform.SetSiblingIndex(targetIndex);
        LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);
        rectTransform.position = targetWorldPos;
    }
}
