using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseBoxButtonHoverController : MonoBehaviour
{
    [SerializeField] Color hoverBackgroundColor = new Color(0.18f, 0.62f, 0.86f, 0.55f);
    [SerializeField] float hoverFadeDuration = 0.1f;
    [SerializeField] bool hideButtonTargetGraphic = true;

    void OnEnable()
    {
        ConfigureButtons();
    }

    void ConfigureButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Image> hoverBackgrounds = FindHoverBackgroundImages(buttons);
        HashSet<Image> usedBackgrounds = new HashSet<Image>();

        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            button.transition = Selectable.Transition.None;
            button.interactable = true;

            if (hideButtonTargetGraphic && button.targetGraphic != null)
            {
                button.targetGraphic.color = Color.clear;
                button.targetGraphic.CrossFadeColor(Color.clear, 0f, true, true);
            }

            Image hoverBackground = FindClosestHoverBackground(button, hoverBackgrounds, usedBackgrounds);
            if (hoverBackground == null)
            {
                continue;
            }

            usedBackgrounds.Add(hoverBackground);

            MenuButtonHoverEffect hoverEffect = button.GetComponent<MenuButtonHoverEffect>();
            if (hoverEffect == null)
            {
                hoverEffect = button.gameObject.AddComponent<MenuButtonHoverEffect>();
            }

            hoverEffect.Initialize(hoverBackground, hoverBackgroundColor, hoverFadeDuration, null, null, 0f);
        }
    }

    List<Image> FindHoverBackgroundImages(Button[] buttons)
    {
        HashSet<Image> buttonImages = new HashSet<Image>();
        foreach (Button button in buttons)
        {
            if (button != null && button.targetGraphic is Image buttonImage)
            {
                buttonImages.Add(buttonImage);
            }
        }

        List<Image> hoverBackgrounds = new List<Image>();
        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image == null || buttonImages.Contains(image))
            {
                continue;
            }

            if (image.transform.parent != transform || image.gameObject.name == "Background" || image.gameObject.name == "Overlay")
            {
                continue;
            }

            if (!image.gameObject.activeSelf || image.gameObject.name.StartsWith("Image"))
            {
                hoverBackgrounds.Add(image);
            }
        }

        return hoverBackgrounds;
    }

    Image FindClosestHoverBackground(Button button, List<Image> hoverBackgrounds, HashSet<Image> usedBackgrounds)
    {
        RectTransform buttonRect = button.transform as RectTransform;
        Image closest = null;
        float closestDistance = float.PositiveInfinity;

        foreach (Image hoverBackground in hoverBackgrounds)
        {
            if (hoverBackground == null || usedBackgrounds.Contains(hoverBackground))
            {
                continue;
            }

            RectTransform backgroundRect = hoverBackground.transform as RectTransform;
            if (buttonRect == null || backgroundRect == null)
            {
                continue;
            }

            float distance = Mathf.Abs(buttonRect.anchoredPosition.y - backgroundRect.anchoredPosition.y);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = hoverBackground;
            }
        }

        return closest;
    }
}
