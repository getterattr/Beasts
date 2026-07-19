using System.Linq;
using Beasts.Runtime;
using Beasts.Runtime.Features;
using ExileCore.Shared.Enums;
using Vector2 = System.Numerics.Vector2;

namespace Beasts;

public partial class Main
{
    private BeastsRuntime _runtime;
    private MapRenderState _mapRenderState;
    private BestiaryCapturedBeastsViewService _bestiaryCapturedBeastsViewService;
    private BeastLookupService _beastLookupService;
    private MapRenderPresentationService _mapRenderPresentationService;
    private MapRenderImGuiOverlayService _mapRenderImGuiOverlayService;
    private MapRenderBeastOverlayService _mapRenderBeastOverlayService;
    private MapRenderLabelService _mapRenderLabelService;
    private MapRenderDrawingPrimitivesService _mapRenderDrawingPrimitivesService;
    private MapRenderPanelOverlayService _mapRenderPanelOverlayService;

    private BeastsRuntime Runtime => _runtime ??= new BeastsRuntime(this);

    private MapRenderState MapRenderRuntime => _mapRenderState ??= new MapRenderState();

    private MapRenderPresentationService MapRenderPresentation => _mapRenderPresentationService ??= new MapRenderPresentationService(
        new MapRenderPresentationCallbacks(
            () => Settings.MapRender.CapturedText.ReplaceNameAndPriceWithStatusText.Value,
            () => Settings.MapRender.CapturedText.StatusText.Value,
            () => Settings.MapRender.CapturedText.CapturedStatusText.Value,
            () => Settings.MapRender.CapturedText.CaptureTextColor.Value,
            () => Settings.MapRender.CapturedText.CapturedTextColor.Value,
            beastName => BeastLookup.TryGetBeastPriceText(beastName, out var priceText) ? priceText : null,
            () => Settings.MapRender.Colors.WorldCapturedBeastColor.Value,
            () => Settings.MapRender.Colors.WorldBeastColor.Value,
            () => Settings.MapRender.Colors.WorldCapturedCircleColor.Value,
            () => Settings.MapRender.Colors.WorldCaptureRingColor.Value,
            () => Settings.MapRender.Colors.WorldBeastCircleColor.Value,
            () => Settings.MapRender.Colors.TrackedWindowBeastColor.Value));

    private BestiaryCapturedBeastsViewService BestiaryCapturedBeastsView => _bestiaryCapturedBeastsViewService ??= new BestiaryCapturedBeastsViewService(
        new BestiaryCapturedBeastsViewCallbacks(
            TryGetBestiaryPanel,
            TryGetBestiaryCapturedBeastsTab,
            beastElement => GetElementTextRecursive(beastElement, 2),
            beastElement => EnumerateDescendants(beastElement)));

    private BeastLookupService BeastLookup => _beastLookupService ??= new BeastLookupService(
        new BeastLookupCallbacks(
            beastName => _beastPriceTexts.TryGetValue(beastName, out var priceText) ? priceText : null,
            () => CaptureMonsterCapturedBuffName,
            () => CaptureMonsterTrappedBuffName));

    private MapRenderPanelOverlayService MapRenderPanelOverlays => _mapRenderPanelOverlayService ??= new MapRenderPanelOverlayService(
        new MapRenderPanelOverlayCallbacks(
            beastName => _beastPrices.TryGetValue(beastName, out var price) ? price : null,
            (rect, color) => Graphics.DrawBox(rect, color),
            (rect, color, thickness) => Graphics.DrawFrame(rect, color, thickness),
            MapRenderDrawingPrimitives.DrawCenteredText,
            () => TryGetBestiaryCapturedBeastsDisplay(out _, out var visibleRect) ? visibleRect : null,
            GetVisibleBestiaryCapturedBeasts,
            GetBestiaryBeastLabel));

    private MapRenderImGuiOverlayService MapRenderImGuiOverlays => _mapRenderImGuiOverlayService ??= new MapRenderImGuiOverlayService(
        new MapRenderImGuiOverlayCallbacks(
            DrawPreviewWorldLabel,
            DrawPreviewMapLabel,
            DrawTrackedBeastPreviewRow,
            DrawPreviewCircles,
            GetTrackedWindowBeastColor,
            beastName => BeastLookup.TryGetBeastPriceText(beastName, out var priceText) ? priceText : null,
            GetDisplayedCaptureStatusColor,
            GetDisplayedCaptureStatusText));

    private MapRenderBeastOverlayService MapRenderBeastOverlays => _mapRenderBeastOverlayService ??= new MapRenderBeastOverlayService(
        new MapRenderBeastOverlayCallbacks(
            positioned => GameController.IngameState.Data.ToWorldWithTerrainHeight(positioned.GridPosition),
            worldPosition => GameController.IngameState.Camera.WorldToScreen(worldPosition),
            () => Settings.MapRender.Layout.WorldTextLineSpacing.Value,
            () => Settings.MapRender.CapturedText.ReplaceNameAndPriceWithStatusText.Value,
            () => Settings.MapRender.Colors.WorldPriceTextColor.Value,
            beastName => BeastLookup.TryGetBeastPriceText(beastName, out var priceText) ? priceText : null,
            GetWorldBeastColor,
            GetDisplayedCaptureStatusText,
            GetDisplayedCaptureStatusColor,
            GetWorldBeastCircleColor,
            () => Settings.MapRender.Layout.WorldBeastCircleRadius.Value,
            MapRenderDrawingPrimitives.DrawOutlinedText,
            MapRenderDrawingPrimitives.DrawFilledCircleInWorld));

    private MapRenderLabelService MapRenderLabels => _mapRenderLabelService ??= new MapRenderLabelService(
        new MapRenderLabelCallbacks(
            () => Settings.MapRender.Layout.WorldTextLineSpacing.Value,
            () => Settings.MapRender.Colors.WorldPriceTextColor.Value,
            () => Settings.MapRender.Colors.MapMarkerBackgroundColor.Value,
            () => Settings.MapRender.Colors.MapMarkerTextColor.Value,
            () => Settings.MapRender.Layout.MapLabelPaddingX.Value,
            () => Settings.MapRender.Layout.MapLabelPaddingY.Value,
            () => Settings.MapRender.CapturedText.ReplaceNameAndPriceWithStatusText.Value,
            GetWorldBeastColor,
            GetDisplayedCaptureStatusText,
            GetDisplayedCaptureStatusColor,
            GetWorldBeastCircleColor,
            GetTrackedWindowBeastColor,
            (beastName, captureState) =>
            {
                BuildPreviewMapMarkerTexts(beastName, captureState, out var primaryText, out var secondaryText);
                return (primaryText, secondaryText);
            },
            (beastName, captureState) =>
            {
                BuildMapMarkerTexts(beastName, captureState, out var primaryText, out var secondaryText);
                return (primaryText, secondaryText);
            },
            () => Settings.MapRender.Layout.WorldBeastCircleRadius.Value,
            () => Settings.MapRender.Layout.WorldBeastCircleFillOpacityPercent.Value,
            () => Settings.MapRender.Layout.WorldBeastCircleOutlineThickness.Value,
            () => Settings.MapRender.Colors.WorldTextOutlineColor.Value));

    private MapRenderDrawingPrimitivesService MapRenderDrawingPrimitives => _mapRenderDrawingPrimitivesService ??= new MapRenderDrawingPrimitivesService(
        new MapRenderDrawingPrimitivesCallbacks(
            worldPosition => GameController.Game.IngameState.Camera.WorldToScreen(worldPosition),
            () => Settings.MapRender.Layout.WorldBeastCircleFillOpacityPercent.Value,
            () => Settings.MapRender.Layout.WorldBeastCircleOutlineThickness.Value,
            () => Settings.MapRender.Colors.WorldTextOutlineColor.Value,
            () => _worldCircleScreenPoints,
            WorldCirclePoints,
            (text, position, color) => Graphics.DrawText(text, position, color, FontAlign.Center),
            (points, color) => Graphics.DrawConvexPolyFilled(points, color),
            (points, color, thickness) => Graphics.DrawPolyLine(points, color, thickness)));

    private bool _isCurrentAreaTrackable
    {
        get => Runtime.State.Map.IsCurrentAreaTrackable;
        set => Runtime.State.Map.IsCurrentAreaTrackable = value;
    }

    private string _activeMapAreaHash
    {
        get => Runtime.State.Map.ActiveMapAreaHash;
        set => Runtime.State.Map.ActiveMapAreaHash = value ?? string.Empty;
    }

    private string _activeMapAreaName
    {
        get => Runtime.State.Map.ActiveMapAreaName;
        set => Runtime.State.Map.ActiveMapAreaName = value ?? string.Empty;
    }

    private int _activeMapInstanceId
    {
        get => Runtime.State.Map.ActiveMapInstanceId;
        set => Runtime.State.Map.ActiveMapInstanceId = value;
    }

    private MainSettingsBindingTargets CreateSettingsBindingTargets()
    {
        return new MainSettingsBindingTargets(
            DrawSettingsOverviewPanel,
            DrawConfigurationHeader,
            QueuePriceFetch,
            DrawBeastPricesActionsRow,
            DrawBeastPickerPanel);
    }
}
