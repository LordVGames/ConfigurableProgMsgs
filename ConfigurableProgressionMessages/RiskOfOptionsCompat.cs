using RiskOfOptions;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;

namespace ConfigurableProgressionMessages
{
    public static class RiskOfOptionsCompat
    {
        public const string PluginName = "com.rune580.riskofoptions";
        private static bool? _modexists;
        public static bool ModIsRunning
        {
            get
            {
                if (_modexists == null)
                {
                    _modexists = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(PluginName);
                }
                return (bool)_modexists;
            }
        }

        public static void AddProgMsgsToRiskOfOptions()
        {
            ModSettingsManager.SetModDescription("Adds some chat messages that you can configure the contents of, along with when they appear in your runs.");
            for (int i = 0; i < ConfigurableProgressionMessages.ProgMsgsCount; i++)
            {
                string CategoryName = $"Message #{i + 1}";

                ModSettingsManager.AddOption(
                    new StringInputFieldOption(
                        ConfigurableProgressionMessages.ModConfigEntries.Messages[i],
                        new InputFieldConfig()
                        {
                            category = CategoryName,
                            name = ConfigurableProgressionMessages.ConfigEntryNames.Message,
                            description = $"The message(s) for Progression Message #{i + 1}.\n{ConfigurableProgressionMessages.DetailedMessageConfigDesc}",
                            richText = false,
                            lineType = TMPro.TMP_InputField.LineType.MultiLineNewline,
                            submitOn = InputFieldConfig.SubmitEnum.OnExit,
                        }
                    )
                );
                ModSettingsManager.AddOption(
                    new IntFieldOption(
                        ConfigurableProgressionMessages.ModConfigEntries.SendOnStageX[i],
                        new IntFieldConfig()
                        {
                            category = CategoryName,
                            name = ConfigurableProgressionMessages.ConfigEntryNames.SendOnStageX,
                            description = $"The stage number that Progression Message #{i + 1} will be sent at the start of. Set to -1 for no stage.",
                            Min = -1,
                        }
                    )
                );
                ModSettingsManager.AddOption(
                    new IntFieldOption(
                        ConfigurableProgressionMessages.ModConfigEntries.SendAgainAfterXStages[i],
                        new IntFieldConfig()
                        {
                            category = CategoryName,
                            name = ConfigurableProgressionMessages.ConfigEntryNames.SendAgainAfterXStages,
                            description = $"The amount of stages to wait before Progression Message #{i + 1} will be sent again. Set to -1 for no stage.",
                            Min = -1,
                        }
                    )
                );
                ModSettingsManager.AddOption(
                    new ChoiceOption(
                        ConfigurableProgressionMessages.ModConfigEntries.SendOnLoopStart[i],
                        new ChoiceConfig()
                        {
                            category = CategoryName,
                            name = ConfigurableProgressionMessages.ConfigEntryNames.SendOnLoopStart,
                            description = $"Whether to send the message at the start of your first loop, at the start of every loop, or never.",
                        }
                    )
                );
                ModSettingsManager.AddOption(
                    new ChoiceOption(
                        ConfigurableProgressionMessages.ModConfigEntries.SendOnBazaarVisit[i],
                        new ChoiceConfig()
                        {
                            category = CategoryName,
                            name = ConfigurableProgressionMessages.ConfigEntryNames.SendOnBazaarVisit,
                            description = $"Whether to send the message on your first visit to the Bazaar, on every visit, or never.",
                        }
                    )
                );
                ModSettingsManager.AddOption(
                    new ChoiceOption(
                        ConfigurableProgressionMessages.ModConfigEntries.SendOnVoidFieldsVisit[i],
                        new ChoiceConfig()
                        {
                            category = CategoryName,
                            name = ConfigurableProgressionMessages.ConfigEntryNames.SendOnVoidFieldsVisit,
                            description = $"Whether to send the message on your first visit to the Void Fields, on every visit, or never.",
                        }
                    )
                );
            }
        }
    }
}
