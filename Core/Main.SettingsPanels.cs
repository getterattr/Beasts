using System.Globalization;
using ImGuiNET;
using Vector4 = System.Numerics.Vector4;

namespace Beasts;

public partial class Main
{
    private static readonly Vector4 SummaryAccentColor = new(0.95f, 0.74f, 0.26f, 1f);
    private static readonly Vector4 SummaryOkColor = new(0.47f, 0.90f, 0.56f, 1f);
    private static readonly Vector4 SummaryMutedColor = new(0.63f, 0.66f, 0.72f, 1f);
    private const ImGuiTableFlags SummaryTableFlags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg;

    private void DrawSettingsOverviewPanel()
    {
        var settings = Settings;
        if (settings == null)
        {
            ImGui.TextDisabled("Settings unavailable.");
            return;
        }

        ImGui.Dummy(new System.Numerics.Vector2(0, 12));
        ImGui.TextColored(SummaryAccentColor, "Overview");
        ImGui.Separator();

        if (ImGui.BeginTable("##BeastsDashboardSummary", 2, SummaryTableFlags))
        {
            DrawSummaryRow("Plugin state", settings.Enable.Value ? "Enabled" : "Disabled", GetStateColor(settings.Enable.Value));
            DrawSummaryRow("Tracked beasts", settings.BeastPrices.EnabledBeasts.Count.ToString(CultureInfo.InvariantCulture));
            DrawSummaryRow("Configured league", FormatText(settings.BeastPrices.League?.Value, "Not set"));
            DrawSummaryRow("Price refresh cadence", FormatRefreshInterval(settings.BeastPrices.AutoRefreshMinutes.Value));
            DrawSummaryRow("Last price fetch", FormatText(settings.BeastPrices.LastUpdated, "never"));
            DrawSummaryRow("Overlay mode", settings.MapRender.ShowEnabledOnly.Value ? "Only tracked beasts" : "All rare beasts", GetStateColor(settings.MapRender.ShowEnabledOnly.Value));
            ImGui.EndTable();
        }
    }

    private void DrawConfigurationHeader()
    {
        ImGui.Dummy(new System.Numerics.Vector2(0, 12));
        ImGui.TextColored(SummaryAccentColor, "Configuration");
        ImGui.Separator();
    }

    private void DrawBeastPricesActionsRow()
    {
        if (ImGui.Button("Refresh Prices"))
            QueuePriceFetch();
        ImGui.SameLine();
        if (ImGui.Button("Select 15c+"))
            SelectPriceDataBeastsWorth15ChaosOrMore();
        ImGui.SameLine();
        if (ImGui.Button("Select All"))
            SelectAllPriceDataBeasts();
        ImGui.SameLine();
        if (ImGui.Button("Clear Selection"))
            DeselectAllPriceDataBeasts();
    }

    private static void DrawSummaryRow(string label, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextColored(SummaryMutedColor, label);
        ImGui.TableNextColumn();
        ImGui.Text(value ?? string.Empty);
    }

    private static void DrawSummaryRow(string label, string value, Vector4 valueColor)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextColored(SummaryMutedColor, label);
        ImGui.TableNextColumn();
        ImGui.TextColored(valueColor, value ?? string.Empty);
    }

    private static Vector4 GetStateColor(bool enabled) => enabled ? SummaryOkColor : SummaryMutedColor;

    private static string FormatText(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string FormatRefreshInterval(int minutes) =>
        minutes <= 0 ? "manual only" : $"every {minutes} min";
}
