using System;

namespace Beasts.Runtime;

internal sealed record MainSettingsBindingTargets(
    Action DrawSettingsOverviewPanel,
    Action DrawConfigurationHeader,
    Action QueuePriceFetch,
    Action DrawBeastPricesActionsRow,
    Action DrawBeastPickerPanel);

internal static class MainSettingsBindings
{
    public static void Bind(Settings settings, MainSettingsBindingTargets targets)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(targets);

        settings.OverviewPanel.DrawDelegate = targets.DrawSettingsOverviewPanel;
        settings.ConfigurationHeader.DrawDelegate = targets.DrawConfigurationHeader;
        settings.RefreshPrices.OnPressed = targets.QueuePriceFetch;

        var beastPrices = settings.BeastPrices;
        beastPrices.ActionsRow.DrawDelegate = targets.DrawBeastPricesActionsRow;
        beastPrices.BeastPickerPanel.DrawDelegate = targets.DrawBeastPickerPanel;
    }
}
