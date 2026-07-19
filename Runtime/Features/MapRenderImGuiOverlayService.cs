using System;
using System.Collections.Generic;
using ImGuiNET;
using SharpDX;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;

namespace Beasts.Runtime.Features;

internal sealed record MapRenderImGuiOverlayCallbacks(
    Action<string, BeastCaptureState> DrawPreviewWorldLabel,
    Action<string, BeastCaptureState> DrawPreviewMapLabel,
    Action<string, string, BeastCaptureState> DrawTrackedBeastPreviewRow,
    Action DrawPreviewCircles,
    Func<Color> GetTrackedWindowBeastColor,
    Func<string, string> GetBeastPriceTextOrNull,
    Func<BeastCaptureState, Color> GetDisplayedCaptureStatusColor,
    Func<BeastCaptureState, string> GetDisplayedCaptureStatusText);

internal sealed class MapRenderImGuiOverlayService
{
    private const string PreviewBeastName = "Craicic Chimeral";
    private readonly MapRenderImGuiOverlayCallbacks _callbacks;

    public MapRenderImGuiOverlayService(MapRenderImGuiOverlayCallbacks callbacks)
    {
        _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
    }

    public void DrawStylePreviewWindow()
    {
        ImGui.SetNextWindowBgAlpha(0.9f);
        if (!ImGui.Begin("Beast Style Preview##BeastsStylePreview",
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.End();
            return;
        }

        ImGui.Text("World Label Preview");
        _callbacks.DrawPreviewWorldLabel(PreviewBeastName, BeastCaptureState.None);
        _callbacks.DrawPreviewWorldLabel(PreviewBeastName, BeastCaptureState.Capturing);
        _callbacks.DrawPreviewWorldLabel(PreviewBeastName, BeastCaptureState.Captured);

        ImGui.Separator();
        ImGui.Text("Large Map Label Preview");
        _callbacks.DrawPreviewMapLabel(PreviewBeastName, BeastCaptureState.None);
        _callbacks.DrawPreviewMapLabel(PreviewBeastName, BeastCaptureState.Capturing);
        _callbacks.DrawPreviewMapLabel(PreviewBeastName, BeastCaptureState.Captured);

        ImGui.Separator();
        ImGui.Text("Tracked Beasts Window Preview");
        if (ImGui.BeginTable("##TrackedWindowPreviewTable", 2,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV))
        {
            ImGui.TableSetupColumn("Price", ImGuiTableColumnFlags.WidthFixed, 52);
            ImGui.TableSetupColumn("Beast", ImGuiTableColumnFlags.WidthStretch);

            _callbacks.DrawTrackedBeastPreviewRow("1c", PreviewBeastName, BeastCaptureState.None);
            _callbacks.DrawTrackedBeastPreviewRow("1c", PreviewBeastName, BeastCaptureState.Capturing);
            _callbacks.DrawTrackedBeastPreviewRow("1c", PreviewBeastName, BeastCaptureState.Captured);

            ImGui.EndTable();
        }

        ImGui.Separator();
        ImGui.Text("Circle Preview");
        _callbacks.DrawPreviewCircles();

        ImGui.End();
    }

    public void DrawTrackedBeastsWindow(IReadOnlyList<TrackedBeastMapMarkerInfo> beasts)
    {
        if (beasts.Count == 0)
        {
            return;
        }

        var trackedWindowBeastColor = BeastsHelpers.ToImGuiColor(_callbacks.GetTrackedWindowBeastColor());

        ImGui.SetNextWindowBgAlpha(0.6f);
        ImGui.Begin("##RareBeastTrackerWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize);

        if (ImGui.BeginTable("##TrackerTable", 2,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV))
        {
            ImGui.TableSetupColumn("Price", ImGuiTableColumnFlags.WidthFixed, 52);
            ImGui.TableSetupColumn("Beast", ImGuiTableColumnFlags.WidthStretch);

            foreach (var beast in beasts)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(_callbacks.GetBeastPriceTextOrNull(beast.BeastName) ?? "?");
                ImGui.TableNextColumn();
                ImGui.TextColored(trackedWindowBeastColor, beast.BeastName);
                if (beast.CaptureState != BeastCaptureState.None)
                {
                    ImGui.SameLine(0, 0);
                    ImGui.TextColored(BeastsHelpers.ToImGuiColor(_callbacks.GetDisplayedCaptureStatusColor(beast.CaptureState)),
                        $" {_callbacks.GetDisplayedCaptureStatusText(beast.CaptureState)}");
                }
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }
}