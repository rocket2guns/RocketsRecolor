using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RecolorClothing
{
    /// <summary>
    /// Color chooser dialog with:
    /// - Standard color palette (from ColorDefs, matching CraftWithColor's list)
    /// - Saved colors from CraftWithColor (if present) — shared live list
    /// - HSV sliders
    /// - Hex input
    /// - Large color preview swatch
    /// 
    /// Layout inspired by CraftWithColor's SelectColorDialog for visual consistency.
    /// </summary>
    public class Dialog_ChooseRecolorColor : Window
    {
        private readonly RecolorBill bill;
        private readonly Color originalColor;
        private Color currentColor;
        private float hue, sat, val;
        private string hexBuffer;

        // Color lists
        private List<Color> standardColors;
        private List<Color> savedColors; // null if CWC not present

        // Layout constants matching CWC's style
        private const float ColorSquareSize = 22f;
        private const float ColorSquarePadding = 2f;
        private const float ColorSquareSpace = ColorSquareSize + ColorSquarePadding;
        private const int ColorListColumns = 10;
        private const float ColorListWidth = ColorListColumns * ColorSquareSpace + 4f;
        private const float SectionGap = 12f;
        private const float SmallGap = 6f;
        private const float LabelHeight = 20f;
        private const float SliderHeight = 16f;
        private const float PreviewSize = 80f;
        private const float ButtonHeight = 30f;

        public override Vector2 InitialSize => new Vector2(
            Mathf.Max(ColorListWidth + 40f, 300f),
            500f);

        private static Rect lastWindowRect;

        public Dialog_ChooseRecolorColor(RecolorBill bill)
        {
            this.bill = bill;
            originalColor = bill.chosenColor;
            currentColor = bill.chosenColor;
            Color.RGBToHSV(currentColor, out hue, out sat, out val);
            hexBuffer = ColorUtility.ToHtmlStringRGB(currentColor);

            standardColors = CraftWithColorCompat.GetStandardColors();
            savedColors = CraftWithColorCompat.GetSavedColors();

            doCloseX = true;
            closeOnClickedOutside = true;
            draggable = true;
        }

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            if (lastWindowRect.width > 0)
            {
                windowRect = lastWindowRect;
            }
            else
            {
                windowRect.x += 200f;
            }
        }

        public override void PostClose()
        {
            lastWindowRect = windowRect;
            base.PostClose();
        }

        public override void OnCancelKeyPressed()
        {
            bill.chosenColor = originalColor;
            base.OnCancelKeyPressed();
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = inRect.y;
            float x = inRect.x;
            float width = inRect.width;

            // --- Title ---
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, width, 30f), "Select Color");
            Text.Font = GameFont.Small;
            y += 34f;

            // --- Preview swatch + sliders side by side ---
            float sliderAreaWidth = width - PreviewSize - SectionGap;

            // Preview
            Rect previewRect = new Rect(x, y, PreviewSize, PreviewSize);
            Widgets.DrawBoxSolid(previewRect, currentColor);
            Color oldGui = GUI.color;
            GUI.color = new Color(0.4f, 0.4f, 0.4f);
            Widgets.DrawBox(previewRect);
            GUI.color = oldGui;

            // Hex below preview
            Text.Font = GameFont.Tiny;
            Rect hexLabelRect = new Rect(x, previewRect.yMax + 2f, PreviewSize, 18f);
            string hexDisplay = "#" + ColorUtility.ToHtmlStringRGB(currentColor);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(hexLabelRect, hexDisplay);
            Text.Anchor = TextAnchor.UpperLeft;

            // HSV sliders to the right of preview
            float sx = x + PreviewSize + SectionGap;
            float sy = y;

            Text.Font = GameFont.Tiny;
            Rect hRect = new Rect(sx, sy, sliderAreaWidth, SliderHeight);
            hue = Widgets.HorizontalSlider(hRect, hue, 0f, 1f, false, $"Hue: {(int)(hue * 360)}");
            sy += SliderHeight + SmallGap;

            Rect sRect = new Rect(sx, sy, sliderAreaWidth, SliderHeight);
            sat = Widgets.HorizontalSlider(sRect, sat, 0f, 1f, false, $"Sat: {(int)(sat * 100)}%");
            sy += SliderHeight + SmallGap;

            Rect vRect = new Rect(sx, sy, sliderAreaWidth, SliderHeight);
            val = Widgets.HorizontalSlider(vRect, val, 0f, 1f, false, $"Bri: {(int)(val * 100)}%");
            sy += SliderHeight + SmallGap;

            currentColor = Color.HSVToRGB(hue, sat, val);

            // Hex input field
            Rect hexInputLabel = new Rect(sx, sy, 14f, 18f);
            Widgets.Label(hexInputLabel, "#");
            Rect hexInputRect = new Rect(sx + 16f, sy, 60f, 18f);
            hexBuffer = Widgets.TextField(hexInputRect, hexBuffer, 6);
            if (hexBuffer.Length == 6
                && ColorUtility.TryParseHtmlString("#" + hexBuffer, out Color parsed))
            {
                currentColor = parsed;
                Color.RGBToHSV(currentColor, out hue, out sat, out val);
            }

            Text.Font = GameFont.Small;
            y += PreviewSize + 22f + SectionGap;

            // --- Standard colors ---
            if (standardColors != null && standardColors.Count > 0)
            {
                y = DrawColorGrid(new Rect(x, y, width, 0f), "Standard", standardColors, ref currentColor);
                y += SectionGap;
            }

            // --- Saved colors (from CraftWithColor) ---
            if (savedColors != null && savedColors.Count > 0)
            {
                y = DrawColorGrid(new Rect(x, y, width, 0f), "Saved", savedColors, ref currentColor);
                y += SectionGap;
            }

            // Update HSV and hex from color (if changed by grid click)
            Color.RGBToHSV(currentColor, out hue, out sat, out val);
            hexBuffer = ColorUtility.ToHtmlStringRGB(currentColor);

            // --- Buttons ---
            float btnY = inRect.yMax - ButtonHeight;
            float btnWidth = (width - SectionGap) / 2f;

            if (Widgets.ButtonText(new Rect(x, btnY, btnWidth, ButtonHeight), "Confirm"))
            {
                bill.chosenColor = currentColor;
                Close();
            }
            if (Widgets.ButtonText(new Rect(x + btnWidth + SectionGap, btnY, btnWidth, ButtonHeight), "Cancel"))
            {
                bill.chosenColor = originalColor;
                Close();
            }
        }

        /// <summary>
        /// Draw a labeled grid of color swatches. Returns the Y position after the grid.
        /// Matches CraftWithColor's ColorSelector layout.
        /// </summary>
        private float DrawColorGrid(Rect startRect, string label, List<Color> colors, ref Color selected)
        {
            float x = startRect.x;
            float y = startRect.y;
            float width = startRect.width;

            // Label
            Color oldGui = GUI.color;
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(x, y, width, LabelHeight), label);
            Text.Font = GameFont.Small;
            GUI.color = oldGui;
            y += LabelHeight;

            // Grid
            int cols = Mathf.FloorToInt(width / ColorSquareSpace);
            if (cols < 1) cols = 1;

            for (int i = 0; i < colors.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                Rect swatch = new Rect(
                    x + col * ColorSquareSpace,
                    y + row * ColorSquareSpace,
                    ColorSquareSize,
                    ColorSquareSize);

                Widgets.DrawBoxSolid(swatch, colors[i]);

                // Highlight if this is the selected color
                if (colors[i].IndistinguishableFrom(selected))
                {
                    oldGui = GUI.color;
                    GUI.color = Color.white;
                    Widgets.DrawBox(swatch, 2);
                    GUI.color = oldGui;
                }

                if (Widgets.ButtonInvisible(swatch))
                {
                    selected = colors[i];
                }
            }

            int totalRows = Mathf.CeilToInt(colors.Count / (float)cols);
            return y + totalRows * ColorSquareSpace;
        }
    }
}