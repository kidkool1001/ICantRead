using BepInEx;
using TMPro;
using UnityEngine;
using System;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;
using BepInEx.Configuration;

namespace ICantRead
{
    [BepInPlugin("com.kidkool1001.icantread", "I Can't Read", "1.0.0")]
    public class ICantReadPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> EnableMod;
        public static ConfigEntry<int> IlliteracySlider;
        private void Awake()
        {
            EnableMod = Config.Bind("General", "Enabled", true, "Enable or disable 12-hour time format.");
            IlliteracySlider = Config.Bind("General", "Illiteracy Levels", 0,
                new ConfigDescription("0 = H:MM:SS, 1 = H:MM, 2 = Day/Night Only",
                new AcceptableValueRange<int>(0, 2)));
            new TimePatch().Enable();
            Logger.LogInfo("I can't read... but now I can tell the time!");
        }
    }

    public class TimePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocationConditionsPanel).GetMethod("Set", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(LocationConditionsPanel __instance)
        {
            if (__instance == null) return;

            if (__instance.gameObject.GetComponent<TimeLabelUpdater>() == null)
            {
                __instance.gameObject.AddComponent<TimeLabelUpdater>();
            }
        }
    }

    public class TimeLabelUpdater : MonoBehaviour
    {
        private TMP_Text[] _labels;
        private static readonly Regex MSpaceRegex = new Regex("<.*?>", RegexOptions.Compiled);

        private void Awake()
        {
            _labels = GetComponentsInChildren<TMP_Text>(true);
        }

        private void LateUpdate()
        {
            if (!ICantReadPlugin.EnableMod.Value) return;

            foreach (var label in _labels)
            {
                if (string.IsNullOrEmpty(label.text)) continue;

                string clean = MSpaceRegex.Replace(label.text, "");

                if (DateTime.TryParseExact(clean,
                    new[] { "HH:mm:ss" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime dt))
                {
                    string finalText = "";

                    switch (ICantReadPlugin.IlliteracySlider.Value)
                    {
                        case 0:
                            // Full H:MM:SS + AM/PM
                            finalText = $"{dt:h:mm:ss} <line-height=0.7><size=40%>{dt:tt}</size></line-height>";
                            break;

                        case 1:
                            // Drop seconds → H:MM + AM/PM
                            finalText = $"{dt:h:mm} <line-height=0.7><size=40%>{dt:tt}</size></line-height>";
                            break;

                        case 2:
                            // Replace with DAY or NIGHT
                            finalText = IsDaytime(dt)
                                ? "DAY"
                                : "NIGHT";
                            break;
                    }

                    // lil easter egg
                    if ((dt.Hour == 4 || dt.Hour == 16) && dt.Minute == 20 && ICantReadPlugin.IlliteracySlider.Value != 2)
                    {
                        finalText = $"<color=#00FF00>{finalText}</color>";
                    }

                    label.text = finalText;
                }
            }
        }
        private bool IsDaytime(DateTime dt)
        {
            return dt.Hour >= 6 && dt.Hour < 22;
        }
    }
}
