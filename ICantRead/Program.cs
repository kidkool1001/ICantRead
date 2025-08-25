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
        private void Awake()
        {
            EnableMod = Config.Bind("General", "Enabled",  true, "Enable or disable 12-hour time format.");
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
                    new[] { "HH:mm:ss", "HH:mm" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime dt))
                {
                    label.text = dt.ToString("hh:mm:ss") + $"<line-height=0.7><size=40%>{dt.ToString("tt")}</size></line-height>";
                }
            }
        }
    }
}
