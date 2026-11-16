using System;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomPartyNode
{
    /// <summary>Builds native-styled rectangles, boxes, labels, and buttons for Party-Up UI.</summary>
    internal sealed class PartyUi
    {
        internal static readonly Color Paper = new Color(1f, 0.94f, 0.79f);

        internal readonly TMP_FontAsset Font;

        internal PartyUi(TMP_FontAsset font)
        {
            Font = font;
        }

        internal static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.pivot = rect.anchorMax = rect.anchorMin = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        internal Image Box(Transform parent, string name, float x, float y, float width, float height, string spriteName)
        {
            Image image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = FoundationApi.Presentation.FindSprite(spriteName);
            image.type = Image.Type.Sliced;
            image.color = image.sprite ? Color.white : new Color(0.2f, 0.15f, 0.13f);
            return image;
        }

        internal TMP_Text Text(Transform parent, string text, float x, float y, float width, float height, float size)
        {
            TextMeshProUGUI label = Rect(parent, text, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font;
            label.fontSize = size;
            label.color = Paper;
            label.text = text;
            label.raycastTarget = false;
            label.richText = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            return label;
        }

        internal Button Button(Transform parent, string text, float x, float y, float width, float height, Action action)
        {
            Image background = Box(parent, text + "Button", x, y, width, height, "Button03");
            background.pixelsPerUnitMultiplier = 1.5f;

            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.94f, 0.76f);
            colors.pressedColor = new Color(0.7f, 0.65f, 0.5f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.65f);
            button.colors = colors;

            Text(background.transform, text, 0f, 0f, width, height, 18f).alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() => action());
            return button;
        }
    }
}
