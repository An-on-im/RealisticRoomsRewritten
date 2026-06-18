using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace RealisticRoomsRewritten
{
    [StaticConstructorOnStartup]
    public static class RealisticRoomsModOnStartup
    {
        static RealisticRoomsSettings settings;
        static RealisticRoomsModOnStartup()
        {
            settings = LoadedModManager.GetMod<RealisticRoomsMod>().GetSettings<RealisticRoomsSettings>();
            ApplySettings(settings);
        }
        public static void ApplySettings(RealisticRoomsSettings settings)
        {
            var scoreStages = DefDatabase<RoomStatDef>.GetNamed("Space").scoreStages;
            foreach (var stage in scoreStages)
            {
                if (stage.untranslatedLabel == "rather tight") { stage.minScore = settings.minSpaceRatherTight; }
                if (stage.untranslatedLabel == "average-sized") { stage.minScore = settings.minSpaceAverageSized; }
                if (stage.untranslatedLabel == "somewhat spacious") { stage.minScore = settings.minSpaceSomewhatSpacious; }
                if (stage.untranslatedLabel == "quite spacious") { stage.minScore = settings.minSpaceQuiteSpacious; }
                if (stage.untranslatedLabel == "very spacious") { stage.minScore = settings.minSpaceVerySpacious; }
                if (stage.untranslatedLabel == "extremely spacious") { stage.minScore = settings.minSpaceExtremelySpacious; }
            }

            int beauty = settings.filthTweakEnabled ? -4 : -12;
            ModifyBeautyStat("Filth_Dirt", beauty);
            ModifyBeautyStat("Filth_Sand", beauty);
        }

        private static void ModifyBeautyStat(string defName, int beauty)
        {
            var thingStats = DefDatabase<ThingDef>.GetNamed(defName).statBases;
            var beautyModifier = thingStats?.FirstOrDefault(s => s.stat == StatDefOf.Beauty);
            if (beautyModifier != null)
            {
                beautyModifier.value = beauty;
            }
            else
            {
                thingStats.Add(new StatModifier { stat = StatDefOf.Beauty, value = beauty });
            }
        }
    }

    public class RealisticRoomsSettings : ModSettings
    {
        public float minSpaceRatherTight = 6.5f;
        public float minSpaceAverageSized = 16.5f;
        public float minSpaceSomewhatSpacious = 28.5f;
        public float minSpaceQuiteSpacious = 49.5f;
        public float minSpaceVerySpacious = 84.5f;
        public float minSpaceExtremelySpacious = 174.5f;

        public bool filthTweakEnabled = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.minSpaceRatherTight, "minSpaceRatherTight", 6.5f);
            Scribe_Values.Look(ref this.minSpaceAverageSized, "minSpaceAverageSized", 16.5f);
            Scribe_Values.Look(ref this.minSpaceSomewhatSpacious, "minSpaceSomewhatSpacious", 28.5f);
            Scribe_Values.Look(ref this.minSpaceQuiteSpacious, "minSpaceQuiteSpacious", 49.5f);
            Scribe_Values.Look(ref this.minSpaceVerySpacious, "minSpaceVerySpacious", 84.5f);
            Scribe_Values.Look(ref this.minSpaceExtremelySpacious, "minSpaceExtremelySpacious", 174.5f);
            Scribe_Values.Look(ref this.filthTweakEnabled, "filthTweakEnabled", true);
            base.ExposeData();
        }
    }

    public class RealisticRoomsMod : Mod
    {
        RealisticRoomsSettings settings;

        public RealisticRoomsMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<RealisticRoomsSettings>();
        }

        public override string SettingsCategory() => "RealisticRoomsSettingsCategoryLabel".Translate();

        private static void DrawSliderRow(
            Listing_Standard listing,
            string label,
            ref float value,
            float min,
            float max,
            ref string buffer,
            float labelWidth,
            float sliderWidth,
            float fieldWidth,
            float rowHeight,
            GameFont labelFont,
            Action onChanged)
        {
            Rect rowRect = listing.GetRect(rowHeight);

            Rect labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight);
            Rect sliderRect = new Rect(rowRect.x + labelWidth + 8f,
                                       rowRect.y + (rowHeight - 22f) / 2,
                                       sliderWidth, 22f);
            Rect fieldRect = new Rect(rowRect.x + labelWidth + 8f + sliderWidth + 8f,
                                      rowRect.y + (rowHeight - 22f) / 2,
                                      fieldWidth, 22f);

            GameFont oldFont = Text.Font;
            Text.Font = labelFont;
            Widgets.Label(labelRect, label);
            Text.Font = oldFont;

            float curVal = value;
            float newSliderVal = Widgets.HorizontalSlider(sliderRect, curVal, min, max, true, null, null, null, -1f);
            string newText = Widgets.TextField(fieldRect, buffer);

            bool changed = false;
            float newVal = curVal;

            if (Math.Abs(newSliderVal - curVal) > 0.001f)
            {
                newVal = newSliderVal;
                changed = true;
            }
            else if (newText != buffer)
            {
                if (float.TryParse(newText, out float parsed))
                {
                    parsed = Mathf.Clamp(parsed, min, max);
                    if (Math.Abs(parsed - curVal) > 0.001f)
                    {
                        newVal = parsed;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                value = newVal;
                buffer = newVal.ToString("F1");
                onChanged?.Invoke();
            }
            else
            {
                buffer = curVal.ToString("F1");
            }
        }

        private static void DrawAutoSizeButton(
            Listing_Standard listing,
            string labelKey,
            Action onClick)
        {
            Rect buttonRect = listing.GetRect(30f);
            string fullText = labelKey.Translate();
            string displayText = fullText;
            GameFont usedFont = GameFont.Small;
            float buttonWidth = 0f;

            float maxWidth = buttonRect.width - 10f;

            Text.Font = GameFont.Small;
            Vector2 size = Text.CalcSize(fullText);
            if (size.x + 20f <= maxWidth)
            {
                buttonWidth = size.x + 20f;
                usedFont = GameFont.Small;
            }
            else
            {
                Text.Font = GameFont.Tiny;
                size = Text.CalcSize(fullText);
                if (size.x + 20f <= maxWidth)
                {
                    buttonWidth = size.x + 20f;
                    usedFont = GameFont.Tiny;
                }
                else
                {
                    string truncated = fullText;
                    Text.Font = GameFont.Tiny;
                    while (Text.CalcSize(truncated + "…").x + 20f > maxWidth && truncated.Length > 1)
                    {
                        truncated = truncated.Substring(0, truncated.Length - 1);
                    }
                    truncated += "…";
                    displayText = truncated;
                    buttonWidth = Text.CalcSize(displayText).x + 20f;
                    usedFont = GameFont.Tiny;
                    TooltipHandler.TipRegion(buttonRect, fullText);
                }
            }

            if (buttonWidth < 60f) buttonWidth = 60f;
            Rect buttonRightRect = new Rect(buttonRect.x + buttonRect.width - buttonWidth, buttonRect.y, buttonWidth, buttonRect.height);

            GameFont oldFont = Text.Font;
            Text.Font = usedFont;
            if (Widgets.ButtonText(buttonRightRect, displayText))
            {
                onClick?.Invoke();
            }
            Text.Font = oldFont;
        }

        public override void DoSettingsWindowContents(Rect canvas)
        {
            float margin = 20f;
            float gap = 8f;
            float innerWidth = canvas.width - margin * 2;
            float labelWidth = innerWidth * 0.35f;
            float sliderWidth = innerWidth * 0.45f;
            float fieldWidth = innerWidth * 0.20f;

            var spaceDef = DefDatabase<RoomStatDef>.GetNamed("Space");
            var stages = spaceDef.scoreStages;
            string[] labels = new string[6];
            for (int i = 0; i < 6; i++)
            {
                labels[i] = stages[i + 1].label;
            }

            float[] maxVals = new float[6];
            maxVals[0] = settings.minSpaceAverageSized;
            maxVals[1] = settings.minSpaceSomewhatSpacious;
            maxVals[2] = settings.minSpaceQuiteSpacious;
            maxVals[3] = settings.minSpaceVerySpacious;
            maxVals[4] = settings.minSpaceExtremelySpacious;
            maxVals[5] = 400f;

            string[] buffers = new string[6];
            float[] values = new float[6];
            values[0] = settings.minSpaceRatherTight;
            values[1] = settings.minSpaceAverageSized;
            values[2] = settings.minSpaceSomewhatSpacious;
            values[3] = settings.minSpaceQuiteSpacious;
            values[4] = settings.minSpaceVerySpacious;
            values[5] = settings.minSpaceExtremelySpacious;

            for (int i = 0; i < 6; i++)
                buffers[i] = values[i].ToString("F1");

            void ApplyCorrectionAndUpdate(int changedIndex)
            {
                for (int i = changedIndex - 1; i >= 0; i--)
                {
                    if (values[i] > values[i + 1])
                        values[i] = values[i + 1];
                }

                settings.minSpaceRatherTight = values[0];
                settings.minSpaceAverageSized = values[1];
                settings.minSpaceSomewhatSpacious = values[2];
                settings.minSpaceQuiteSpacious = values[3];
                settings.minSpaceVerySpacious = values[4];
                settings.minSpaceExtremelySpacious = values[5];

                for (int i = 0; i < 6; i++)
                    buffers[i] = values[i].ToString("F1");

                RealisticRoomsModOnStartup.ApplySettings(settings);
            }

            Text.Font = GameFont.Small;
            float maxLabelHeight = 0f;
            GameFont[] fontsToUse = new GameFont[6];

            for (int i = 0; i < 6; i++)
            {
                string label = labels[i];
                float neededHeight = Text.CalcHeight(label, labelWidth);
                if (neededHeight > Text.LineHeight + 2f)
                {
                    Text.Font = GameFont.Tiny;
                    float tinyHeight = Text.CalcHeight(label, labelWidth);
                    if (tinyHeight <= Text.LineHeight + 2f)
                    {
                        fontsToUse[i] = GameFont.Tiny;
                        neededHeight = Text.LineHeight;
                    }
                    else
                    {
                        fontsToUse[i] = GameFont.Small;
                        neededHeight = Text.CalcHeight(label, labelWidth);
                    }
                }
                else
                {
                    fontsToUse[i] = GameFont.Small;
                    neededHeight = Text.LineHeight;
                }
                if (neededHeight > maxLabelHeight)
                    maxLabelHeight = neededHeight;
            }

            float minRowHeight = 24f;
            float rowHeight = Math.Max(maxLabelHeight, minRowHeight) + 4f;

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(canvas);

            listing.Label("SettingsLabelTip".Translate());
            listing.GapLine();

            for (int i = 0; i < 6; i++)
            {
                int index = i;
                DrawSliderRow(
                    listing,
                    labels[index],
                    ref values[index],
                    0f,
                    maxVals[index],
                    ref buffers[index],
                    labelWidth,
                    sliderWidth,
                    fieldWidth,
                    rowHeight,
                    fontsToUse[index],
                    () => ApplyCorrectionAndUpdate(index)
                );
            }

            listing.Gap(10f);
            bool oldFilth = settings.filthTweakEnabled;
            listing.CheckboxLabeled("SettingsFilthLabel".Translate(), ref settings.filthTweakEnabled);
            if (settings.filthTweakEnabled != oldFilth)
            {
                RealisticRoomsModOnStartup.ApplySettings(settings);
            }

            listing.Gap(8f);
            DrawAutoSizeButton(
                listing,
                "SettingsResetButton",
                () =>
                {
                    settings.minSpaceRatherTight = 6.5f;
                    settings.minSpaceAverageSized = 16.5f;
                    settings.minSpaceSomewhatSpacious = 28.5f;
                    settings.minSpaceQuiteSpacious = 49.5f;
                    settings.minSpaceVerySpacious = 84.5f;
                    settings.minSpaceExtremelySpacious = 174.5f;

                    values[0] = settings.minSpaceRatherTight;
                    values[1] = settings.minSpaceAverageSized;
                    values[2] = settings.minSpaceSomewhatSpacious;
                    values[3] = settings.minSpaceQuiteSpacious;
                    values[4] = settings.minSpaceVerySpacious;
                    values[5] = settings.minSpaceExtremelySpacious;

                    for (int i = 0; i < 6; i++)
                        buffers[i] = values[i].ToString("F1");

                    RealisticRoomsModOnStartup.ApplySettings(settings);
                });

            listing.End();
        }
    }
}