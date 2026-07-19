using System;
using SharpDX;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;

namespace Beasts.Runtime.Features;

internal sealed record MapRenderPresentationCallbacks(
    Func<bool> GetReplaceNameAndPriceWithStatusText,
    Func<string> GetCapturingStatusText,
    Func<string> GetCapturedStatusText,
    Func<Color> GetCaptureTextColor,
    Func<Color> GetCapturedTextColor,
    Func<string, string> GetBeastPriceTextOrNull,
    Func<Color> GetWorldCapturedBeastColor,
    Func<Color> GetWorldBeastColor,
    Func<Color> GetWorldCapturedCircleColor,
    Func<Color> GetWorldCaptureRingColor,
    Func<Color> GetWorldBeastCircleColor,
    Func<Color> GetTrackedWindowBeastColor);

internal sealed class MapRenderPresentationService
{
    private readonly MapRenderPresentationCallbacks _callbacks;

    public MapRenderPresentationService(MapRenderPresentationCallbacks callbacks)
    {
        _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
    }

    public string GetDisplayedCaptureStatusText(BeastCaptureState captureState)
    {
        var (setting, fallback) = captureState == BeastCaptureState.Captured
            ? (_callbacks.GetCapturedStatusText(), "Caught")
            : (_callbacks.GetCapturingStatusText(), "Capturing");
        return string.IsNullOrWhiteSpace(setting) ? fallback : setting;
    }

    public Color GetDisplayedCaptureStatusColor(BeastCaptureState captureState)
    {
        return captureState == BeastCaptureState.Captured
            ? _callbacks.GetCapturedTextColor()
            : _callbacks.GetCaptureTextColor();
    }

    public void BuildMarkerTexts(string label, BeastCaptureState captureState, out string primaryText, out string secondaryText)
    {
        if (captureState == BeastCaptureState.None)
        {
            primaryText = label;
            secondaryText = null;
            return;
        }

        if (_callbacks.GetReplaceNameAndPriceWithStatusText())
        {
            primaryText = GetDisplayedCaptureStatusText(captureState);
            secondaryText = null;
            return;
        }

        primaryText = label;
        secondaryText = GetDisplayedCaptureStatusText(captureState);
    }

    public void BuildPreviewMapMarkerTexts(string beastName, BeastCaptureState captureState, out string primaryText, out string secondaryText)
    {
        BuildMarkerTexts($"{beastName} 1c", captureState, out primaryText, out secondaryText);
    }

    public void BuildMapMarkerTexts(string beastName, BeastCaptureState captureState, out string primaryText, out string secondaryText)
    {
        var priceText = _callbacks.GetBeastPriceTextOrNull(beastName);
        primaryText = !string.IsNullOrEmpty(priceText) ? $"{beastName} {priceText}" : beastName;
        secondaryText = null;
    }

    public Color GetWorldBeastColor(BeastCaptureState captureState)
    {
        return captureState != BeastCaptureState.None ? _callbacks.GetWorldCapturedBeastColor() : _callbacks.GetWorldBeastColor();
    }

    public Color GetWorldBeastCircleColor(BeastCaptureState captureState)
    {
        return captureState switch
        {
            BeastCaptureState.Captured => _callbacks.GetWorldCapturedCircleColor(),
            BeastCaptureState.Capturing => _callbacks.GetWorldCaptureRingColor(),
            _ => _callbacks.GetWorldBeastCircleColor(),
        };
    }

    public Color GetTrackedWindowBeastColor()
    {
        return _callbacks.GetTrackedWindowBeastColor();
    }
}