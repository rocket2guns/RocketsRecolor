using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RecolorClothing
{
    /// <summary>
    /// Color chooser dialog matching CraftWithColor's SelectColorDialog layout:
    /// - Left side: large color preview, RGB sliders, hex/decimal input
    /// - Right side: Standard color grid, Saved color grid (from CWC if available)
    /// - Bottom: Delete, Save, Cancel, Accept buttons
    /// </summary>
    public class Dialog_ChooseRecolorColor : Window
    {
        private readonly RecolorBill bill;
        private readonly Color originalColor;
        private Color color;

        // Color lists
        private readonly List<Color> standardColors;
        private readonly List<Color> savedColors; // null if CWC not present

        private static Rect lastWindowRect;

        // Layout constants — matching CWC's SelectColorDialog
        private const float DIALOG_MARGIN = 24f;
        private const float DIALOG_LABEL_HEIGHT = 32f;
        private const float DIALOG_LABEL_MARGIN_TOP = 16f;
        private const float COLOR_WIDTH = 164f;
        private const float GAP = 16f;
        private const float SMALL_GAP = 8f;
        private const float SLIDERS_HEIGHT = 50f;
        private const float SLIDER_LABEL = 15f;
        private const float TEXT_BOX_HEIGHT = 18f;
        private const float TEXT_BOX_MARGIN = 6f;
        private const float COLOR_LIST_LABEL_HEIGHT = 20f;
        private const float COLOR_LIST_COLORS_ADJUST = -2f;
        private const float COLOR_LIST_SQUARE_SIZE = 28f;
        private const float COLOR_LIST_MARGIN = 2f;
        private const int COLOR_LIST_COLUMNS = 10;
        private const int COLOR_LIST_STANDARD_ROWS = 4;
        private const int COLOR_LIST_SAVED_ROWS = 2;
        private const float BUTTONS_HEIGHT = 30f;
        private const float BUTTONS_GAP = 24f;

        private const int COLOR_LIST_ROWS = COLOR_LIST_STANDARD_ROWS + COLOR_LIST_SAVED_ROWS;
        private const float COLOR_LIST_HEIGHT = COLOR_LIST_COLORS_ADJUST + 2 * COLOR_LIST_MARGIN + COLOR_LIST_LABEL_HEIGHT;
        private const float COLOR_LIST_WIDTH = COLOR_LIST_COLUMNS * COLOR_LIST_SQUARE_SIZE + 2 * COLOR_LIST_MARGIN;
        private const float DIALOG_MARGIN_ADJUST = DIALOG_MARGIN - DIALOG_LABEL_MARGIN_TOP;
        private const float LIST_SIDE_HEIGHT = 2 * COLOR_LIST_HEIGHT + COLOR_LIST_ROWS * COLOR_LIST_SQUARE_SIZE + GAP;
        private const float COLOR_HEIGHT = LIST_SIDE_HEIGHT - 2 * SMALL_GAP - SLIDERS_HEIGHT - TEXT_BOX_HEIGHT;
        private const float CONTENT_WIDTH = COLOR_WIDTH + GAP + COLOR_LIST_WIDTH;
        private const float CONTENT_HEIGHT = LIST_SIDE_HEIGHT + BUTTONS_GAP + BUTTONS_HEIGHT;
        private const float WIDTH = 2 * DIALOG_MARGIN + CONTENT_WIDTH;
        private const float HEIGHT = 2 * DIALOG_MARGIN + DIALOG_LABEL_HEIGHT + CONTENT_HEIGHT;

        private static readonly Color dimmedMult = new Color(0.4f, 0.4f, 0.4f);
        private static Color normalColor;
        private static Color dimmedColor;
        private static GUIStyle textBoxRightAlign;
        private static bool setupDone;

        public override Vector2 InitialSize => new Vector2(WIDTH, HEIGHT);
        protected override float Margin => DIALOG_LABEL_MARGIN_TOP;

        public Dialog_ChooseRecolorColor(RecolorBill bill)
        {
            this.bill = bill;
            originalColor = bill.chosenColor;
            color = bill.chosenColor;

            standardColors = GetStandardColors();
            savedColors = CraftWithColorCompat.GetSavedColors();

            doCloseX = true;
            closeOnClickedOutside = true;
            draggable = true;
        }

        private static List<Color> GetStandardColors()
        {
            var colors = (
                from x in DefDatabase<ColorDef>.AllDefsListForReading
                where x.colorType == ColorType.Ideo || x.colorType == ColorType.Misc
                select x.color
            ).ToList();
            colors.SortByColor(c => c);
            return colors;
        }

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            if (lastWindowRect.width > 0)
                windowRect = lastWindowRect;
            else
                windowRect.x += 200f;
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

        public override void OnAcceptKeyPressed()
        {
            bill.chosenColor = color;
            bill.isRandomColor = false;
            base.OnAcceptKeyPressed();
        }

        private static void Setup()
        {
            if (!setupDone)
            {
                normalColor = GUI.color;
                dimmedColor = Dimmed(normalColor);
                textBoxRightAlign = new GUIStyle(Text.CurTextFieldStyle)
                {
                    alignment = TextAnchor.MiddleRight
                };
                setupDone = true;
            }
        }

        private static Color Dimmed(Color c) => c * dimmedMult;

        public override void DoWindowContents(Rect inRect)
        {
            Setup();

            bool hasSaved = savedColors != null && savedColors.Count > 0;

            Rect labelRect = inRect.TopPartPixels(DIALOG_LABEL_HEIGHT).ContractedBy(DIALOG_MARGIN_ADJUST, 0f);
            inRect = inRect.BottomPartPixels(inRect.height - DIALOG_LABEL_HEIGHT).ContractedBy(DIALOG_MARGIN_ADJUST);

            TextAnchor anchor = Text.Anchor;

            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(labelRect, "Select Color");
            Text.Font = GameFont.Small;

            // --- Left side: preview, sliders, hex ---
            Rect colorRect = new Rect(inRect.x, inRect.y, COLOR_WIDTH, COLOR_HEIGHT);
            Rect[] sliderRect = SliceHorizontal(
                new Rect(inRect.x, colorRect.yMax + SMALL_GAP, COLOR_WIDTH, SLIDERS_HEIGHT), 3);
            Rect hexRect = new Rect(inRect.x, sliderRect[2].yMax + SMALL_GAP, COLOR_WIDTH, TEXT_BOX_HEIGHT);

            // Color preview swatch
            Widgets.DrawBoxSolid(colorRect, color);

            // RGB sliders
            Text.Anchor = TextAnchor.MiddleLeft;
            ColorSlider(sliderRect[0], "R", ref color.r);
            ColorSlider(sliderRect[1], "G", ref color.g);
            ColorSlider(sliderRect[2], "B", ref color.b);

            // Hex + decimal input
            ColorBoxes(hexRect, ref color);

            // --- Right side: color grids ---
            Text.Anchor = TextAnchor.UpperLeft;
            float colorListsLeft = colorRect.xMax + GAP;
            Rect colorList = new Rect(colorListsLeft, inRect.y, COLOR_LIST_WIDTH, 0f);

            DrawColorGrid(ref colorList, ref color, "Standard", standardColors, COLOR_LIST_STANDARD_ROWS);
            if (hasSaved)
            {
                DrawColorGrid(ref colorList, ref color, "Saved", savedColors, COLOR_LIST_SAVED_ROWS);
            }

            // --- Buttons ---
            Rect[] buttonRect = SliceVertical(
                new Rect(inRect.x, inRect.yMax - BUTTONS_HEIGHT, inRect.width, BUTTONS_HEIGHT),
                hasSaved ? 4 : 2, BUTTONS_GAP);

            Text.Anchor = anchor;

            int btnIdx = 0;
            if (hasSaved)
            {
                bool isSaved = savedColors.FindIndex(c => c.IndistinguishableFrom(color)) > -1;
                DrawButton(buttonRect[btnIdx++], "Delete", () => savedColors.Remove(color), isSaved);
                DrawButton(buttonRect[btnIdx++], "Save", () => savedColors.Add(color),
                    !isSaved && savedColors.Count < COLOR_LIST_SAVED_ROWS * COLOR_LIST_COLUMNS);
            }
            DrawButton(buttonRect[btnIdx++], "Cancel", () =>
            {
                bill.chosenColor = originalColor;
                Close();
            });
            DrawButton(buttonRect[btnIdx], "Accept", () =>
            {
                bill.chosenColor = color;
                bill.isRandomColor = false;
                Close();
            });
        }

        private static void ColorSlider(Rect rect, string label, ref float value)
        {
            rect.y -= 1.6f;
            Widgets.Label(rect, label);
            rect.y += 1.6f;
            rect.xMin += SLIDER_LABEL;
            value = Widgets.HorizontalSlider(rect, value, 0f, 1f);
        }

        private static void DrawColorGrid(ref Rect rect, ref Color color, string label,
            List<Color> colors, int rows)
        {
            rect.height = COLOR_LIST_HEIGHT + rows * COLOR_LIST_SQUARE_SIZE;
            float colorsHeight = rows * COLOR_LIST_SQUARE_SIZE - COLOR_LIST_COLORS_ADJUST;
            Rect innerRect = rect.ContractedBy(COLOR_LIST_MARGIN);

            GUI.color = dimmedColor;
            Widgets.DrawBox(rect);
            GUI.color = normalColor;

            Widgets.Label(innerRect.TopPartPixels(COLOR_LIST_LABEL_HEIGHT), label);

            // Draw color swatches
            Rect colorsRect = innerRect.BottomPartPixels(colorsHeight);
            int cols = COLOR_LIST_COLUMNS;
            float squareSize = COLOR_LIST_SQUARE_SIZE;

            for (int i = 0; i < colors.Count && i < rows * cols; i++)
            {
                int col = i % cols;
                int row = i / cols;
                Rect swatch = new Rect(
                    colorsRect.x + col * squareSize,
                    colorsRect.y + row * squareSize,
                    squareSize - 2f,
                    squareSize - 2f);

                Widgets.DrawBoxSolid(swatch, colors[i]);

                if (colors[i].IndistinguishableFrom(color))
                {
                    Color old = GUI.color;
                    GUI.color = Color.white;
                    Widgets.DrawBox(swatch, 2);
                    GUI.color = old;
                }

                if (Widgets.ButtonInvisible(swatch))
                {
                    color = colors[i];
                }
            }

            rect.y += rect.height + GAP;
        }

        private static void ColorBoxes(Rect rect, ref Color color)
        {
            string wideHex = "FFFFFF";
            string wideDec = "255";
            float hexWidth = Text.CalcSize(wideHex).x + TEXT_BOX_MARGIN;
            float decWidth = Text.CalcSize(wideDec).x + TEXT_BOX_MARGIN;
            Rect hexRect = rect.LeftPartPixels(hexWidth);
            Rect[] decRect = SliceVertical(rect.RightPartPixels(3f * decWidth), 3, 1f);
            Rect slashRect = new Rect(hexRect.xMax + 0.8f, rect.y + 1.6f,
                decRect[0].x - hexRect.xMax, rect.height);

            Color32 col32 = color;
            uint colInt = (((uint)col32.r) << 16) | (((uint)col32.g) << 8) | ((uint)col32.b);
            string oldHex = Convert.ToString(colInt, 16).ToUpper().PadLeft(6, '0');
            string newHex = Widgets.TextField(hexRect, oldHex).ToUpper();
            if (oldHex != newHex)
            {
                if (oldHex.Length != newHex.Length)
                    newHex = newHex.PadRight(6, '0').Substring(0, 6);
                try { colInt = Convert.ToUInt32(newHex, 16); }
                catch { return; }
                col32.r = (byte)(colInt >> 16);
                col32.g = (byte)((colInt >> 8) & 0xff);
                col32.b = (byte)(colInt & 0xff);
                color = col32;
            }

            if (slashRect.width >= Text.CalcSize("/").x)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(slashRect, "/");
            }

            ColorDecBox(decRect[0], ref color.r);
            ColorDecBox(decRect[1], ref color.g);
            ColorDecBox(decRect[2], ref color.b);
        }

        private static void ColorDecBox(Rect rect, ref float value)
        {
            int intValue = Mathf.RoundToInt(255f * value);
            string oldText = intValue.ToString();
            string newText = GUI.TextField(rect, oldText, textBoxRightAlign);
            if (oldText != newText)
            {
                try
                {
                    uint newIntValue = Convert.ToUInt32(newText);
                    value = Mathf.Clamp(newIntValue / 255f, 0f, 1f);
                }
                catch { }
            }
        }

        private void DrawButton(Rect rect, string label, Action action, bool active = true)
        {
            GUI.color = active ? normalColor : dimmedColor;
            bool pressed = Widgets.ButtonText(rect, label, active: active);
            GUI.color = normalColor;
            if (pressed) action();
        }

        // Utility: split a rect into N horizontal rows
        private static Rect[] SliceHorizontal(Rect rect, int n, float gap = 0f)
        {
            float height = (rect.height - gap * (n - 1)) / n;
            Rect[] result = new Rect[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = rect;
                result[i].height = height;
                result[i].y = rect.y + i * (height + gap);
            }
            return result;
        }

        // Utility: split a rect into N vertical columns
        private static Rect[] SliceVertical(Rect rect, int n, float gap = 0f)
        {
            float width = (rect.width - gap * (n - 1)) / n;
            Rect[] result = new Rect[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = rect;
                result[i].width = width;
                result[i].x = rect.x + i * (width + gap);
            }
            return result;
        }
    }
}