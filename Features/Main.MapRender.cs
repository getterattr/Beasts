using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using GameOffsets.Native;
using ImGuiNET;
using SharpDX;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace Beasts;

public partial class Main
{
    private const int TileToGridConversion = 23;
    private const int TileToWorldConversion = 250;
    private const string CaptureMonsterTrappedBuffName = "capture_monster_trapped";
    private const string CaptureMonsterCapturedBuffName = "capture_monster_captured";
    private const string ItemizedCapturedMonsterMetadata = "Metadata/Items/Currency/CurrencyItemisedCapturedMonster";
    private const float GridToWorldMultiplier = TileToWorldConversion / (float)TileToGridConversion;
    private const double CameraAngle = 38.7 * Math.PI / 180;
    private static readonly float CameraAngleCos = (float)Math.Cos(CameraAngle);
    private static readonly float CameraAngleSin = (float)Math.Sin(CameraAngle);
    private static readonly Vector2[] WorldCirclePoints = BeastsHelpers.CreateUnitCirclePoints(15, closeLoop: false);

    private Vector2[] _worldCircleScreenPoints => MapRenderRuntime.WorldCircleScreenPoints;

    private void DrawInWorldBeasts(IReadOnlyList<TrackedBeastRenderInfo> beasts) => MapRenderBeastOverlays.DrawInWorldBeasts(beasts);

    private void DrawPreviewWorldLabel(string beastName, BeastCaptureState captureState) => MapRenderLabels.DrawPreviewWorldLabel(beastName, captureState);

    private void DrawPreviewMapLabel(string beastName, BeastCaptureState captureState) => MapRenderLabels.DrawPreviewMapLabel(beastName, captureState);

    private void DrawTrackedBeastPreviewRow(string priceText, string beastName, BeastCaptureState captureState) => MapRenderLabels.DrawTrackedBeastPreviewRow(priceText, beastName, captureState);

    private void DrawPreviewCircles() => MapRenderLabels.DrawPreviewCircles();

    private void BuildMarkerTexts(string label, BeastCaptureState captureState, out string primaryText, out string secondaryText) =>
        MapRenderPresentation.BuildMarkerTexts(label, captureState, out primaryText, out secondaryText);

    private void BuildPreviewMapMarkerTexts(string beastName, BeastCaptureState captureState, out string primaryText, out string secondaryText) =>
        MapRenderPresentation.BuildPreviewMapMarkerTexts(beastName, captureState, out primaryText, out secondaryText);

    private void DrawTrackedBeastsWindow(IReadOnlyList<TrackedBeastMapMarkerInfo> beasts) => MapRenderImGuiOverlays.DrawTrackedBeastsWindow(beasts);

    private void DrawInventoryBeasts()
    {
        var inventory = GameController?.Game?.IngameState?.IngameUi?.InventoryPanel?[InventoryIndex.PlayerInventory];
        if (inventory?.IsVisible != true) return;
        DrawCapturedBeastItems(inventory.VisibleInventoryItems);
    }

    private void DrawVisibleStashBeasts(StashElement stash)
    {
        if (stash?.IsVisible != true) return;

        var items = stash.VisibleStash?.VisibleInventoryItems;
        if (items != null)
        {
            DrawCapturedBeastItems(items);
        }
    }

    private void DrawStashBeasts() => DrawVisibleStashBeasts(GameController?.Game?.IngameState?.IngameUi?.StashElement);

    private void DrawMerchantBeasts() => DrawVisibleStashBeasts(GameController?.Game?.IngameState?.IngameUi?.OfflineMerchantPanel);

    private void DrawCapturedBeastItems(IList<NormalInventoryItem> items) =>
        MapRenderPanelOverlays.DrawCapturedBeastItems(items, ItemizedCapturedMonsterMetadata);

    private void DrawBestiaryPanelPrices()
    {
        if (BestiaryChallengesPanel?.IsVisible != true)
        {
            return;
        }

        MapRenderPanelOverlays.DrawBestiaryPanelPrices();
    }

    private bool TryGetBestiaryCapturedBeastsDisplay(out Element beastsDisplay, out RectangleF visibleRect) =>
        BestiaryCapturedBeastsView.TryGetCapturedBeastsDisplay(out beastsDisplay, out visibleRect);

    private BeastCaptureState GetBeastCaptureState(Entity entity) => BeastLookup.GetBeastCaptureState(entity);

    private bool TryGetBeastPriceText(string beastName, out string priceText) => BeastLookup.TryGetBeastPriceText(beastName, out priceText);

    private string GetDisplayedCaptureStatusText(BeastCaptureState captureState) => MapRenderPresentation.GetDisplayedCaptureStatusText(captureState);

    private Color GetDisplayedCaptureStatusColor(BeastCaptureState captureState) => MapRenderPresentation.GetDisplayedCaptureStatusColor(captureState);

    private void BuildMapMarkerTexts(string beastName, BeastCaptureState captureState, out string primaryText, out string secondaryText) =>
        MapRenderPresentation.BuildMapMarkerTexts(beastName, captureState, out primaryText, out secondaryText);

    private Color GetWorldBeastColor(BeastCaptureState captureState) => MapRenderPresentation.GetWorldBeastColor(captureState);

    private Color GetWorldBeastCircleColor(BeastCaptureState captureState) => MapRenderPresentation.GetWorldBeastCircleColor(captureState);

    private Color GetTrackedWindowBeastColor() => MapRenderPresentation.GetTrackedWindowBeastColor();

}

