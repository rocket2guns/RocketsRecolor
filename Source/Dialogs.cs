using UnityEngine;
using Verse;

namespace RecolorClothing
{
    /// <summary>
    /// Color chooser dialog with preset palette, HSV sliders, and hex input.
    /// Opened by clicking the color swatch in the bill config.
    /// </summary>
    public class Dialog_ChooseRecolorColor : Window
    {
        private RecolorBill bill;
        private Color currentColor;
        private float hue, sat, val;
        private string hexBuffer;

        private static readonly Color[] Presets = new Color[]
        {
            new Color(0.8f, 0.1f, 0.1f),   // Red
            new Color(0.1f, 0.4f, 0.8f),   // Blue
            new Color(0.1f, 0.7f, 0.2f),   // Green
            new Color(0.9f, 0.8f, 0.1f),   // Yellow
            new Color(0.6f, 0.1f, 0.7f),   // Purple
            new Color(0.9f, 0.5f, 0.1f),   // Orange
            new Color(0.1f, 0.1f, 0.1f),   // Black
            new Color(0.9f, 0.9f, 0.9f),   // White
            new Color(0.4f, 0.25f, 0.1f),  // Brown
            new Color(0.7f, 0.7f, 0.7f),   // Gray
            new Color(1f, 0.4f, 0.7f),     // Pink
            new Color(0f, 0.7f, 0.7f),     // Teal
            new Color(0.55f, 0f, 0f),      // Dark red
            new Color(0f, 0.25f, 0.5f),    // Navy
            new Color(0f, 0.4f, 0.2f),     // Forest green
            new Color(0.9f, 0.6f, 0.4f),   // Peach
            new Color(0.3f, 0.3f, 0.5f),   // Slate
            new Color(0.7f, 0.5f, 0.3f),   // Tan
        };

        public override Vector2 InitialSize => new Vector2(380f, 420f);

        public Dialog_ChooseRecolorColor(RecolorBill bill)
        {
            this.bill = bill;
            currentColor = bill.chosenColor;
            Color.RGBToHSV(currentColor, out hue, out sat, out val);
            hexBuffer = ColorUtility.ToHtmlStringRGB(currentColor);

            doCloseButton = false;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "Choose Color");
            Text.Font = GameFont.Small;

            // --- Preset grid ---
            float y = inRect.y + 38f;
            float presetSize = 34f;
            float padding = 4f;
            int cols = 6;

            for (int i = 0; i < Presets.Length; i++)
            {
                int row = i / cols;
                int col = i % cols;
                Rect presetRect = new Rect(
                    inRect.x + col * (presetSize + padding),
                    y + row * (presetSize + padding),
                    presetSize, presetSize);

                Widgets.DrawBoxSolid(presetRect, Presets[i]);
                Widgets.DrawBox(presetRect);

                if (Widgets.ButtonInvisible(presetRect))
                {
                    currentColor = Presets[i];
                    Color.RGBToHSV(currentColor, out hue, out sat, out val);
                    hexBuffer = ColorUtility.ToHtmlStringRGB(currentColor);
                }
            }

            // --- HSV sliders ---
            float sliderY = y + (Mathf.CeilToInt(Presets.Length / (float)cols)) * (presetSize + padding) + 12f;

            Rect hRect = new Rect(inRect.x, sliderY, inRect.width - 54f, 26f);
            hue = Widgets.HorizontalSlider(hRect, hue, 0f, 1f, false, $"Hue: {(int)(hue * 360)}");
            sliderY += 30f;

            Rect sRect = new Rect(inRect.x, sliderY, inRect.width - 54f, 26f);
            sat = Widgets.HorizontalSlider(sRect, sat, 0f, 1f, false, $"Saturation: {(int)(sat * 100)}%");
            sliderY += 30f;

            Rect vRect = new Rect(inRect.x, sliderY, inRect.width - 54f, 26f);
            val = Widgets.HorizontalSlider(vRect, val, 0f, 1f, false, $"Brightness: {(int)(val * 100)}%");

            currentColor = Color.HSVToRGB(hue, sat, val);

            // --- Preview swatch ---
            Rect previewRect = new Rect(inRect.xMax - 44f, sliderY - 60f, 40f, 86f);
            Widgets.DrawBoxSolid(previewRect, currentColor);
            Widgets.DrawBox(previewRect);

            // --- Hex input ---
            sliderY += 36f;
            Rect hexLabelRect = new Rect(inRect.x, sliderY, 30f, 26f);
            Widgets.Label(hexLabelRect, "#");

            Rect hexFieldRect = new Rect(inRect.x + 24f, sliderY, 80f, 26f);
            hexBuffer = Widgets.TextField(hexFieldRect, hexBuffer, 6);
            if (hexBuffer.Length == 6 && ColorUtility.TryParseHtmlString("#" + hexBuffer, out Color parsed))
            {
                currentColor = parsed;
                Color.RGBToHSV(currentColor, out hue, out sat, out val);
            }

            // Update hex buffer from sliders (if sliders changed)
            if (!hexFieldRect.Contains(Event.current.mousePosition) || !GUI.GetNameOfFocusedControl().Contains("TextField"))
            {
                hexBuffer = ColorUtility.ToHtmlStringRGB(currentColor);
            }

            // --- Buttons ---
            float btnY = inRect.yMax - 38f;

            if (Widgets.ButtonText(new Rect(inRect.x, btnY, 140f, 34f), "Confirm"))
            {
                bill.chosenColor = currentColor;
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.x + 150f, btnY, 140f, 34f), "Cancel"))
            {
                Close();
            }
        }
    }
}