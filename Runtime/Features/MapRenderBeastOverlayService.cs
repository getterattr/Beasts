using System;
using System.Collections.Generic;
using ExileCore.PoEMemory.Components;
using SharpDX;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace Beasts.Runtime.Features;

internal sealed record MapRenderBeastOverlayCallbacks(
    Func<Positioned, Vector3> GetWorldPosition,
    Func<Vector3, Vector2> WorldToScreen,
    Func<float> GetWorldTextLineSpacing,
    Func<bool> GetReplaceNameAndPriceWithStatusText,
    Func<Color> GetWorldPriceTextColor,
    Func<string, string> GetBeastPriceTextOrNull,
    Func<BeastCaptureState, Color> GetWorldBeastColor,
    Func<BeastCaptureState, string> GetDisplayedCaptureStatusText,
    Func<BeastCaptureState, Color> GetDisplayedCaptureStatusColor,
    Func<BeastCaptureState, Color> GetWorldBeastCircleColor,
    Func<float> GetWorldBeastCircleRadius,
    Action<string, Vector2, Color> DrawOutlinedText,
    Action<Vector3, float, Color> DrawFilledCircleInWorld);

internal sealed class MapRenderBeastOverlayService
{
    private readonly MapRenderBeastOverlayCallbacks _callbacks;

    public MapRenderBeastOverlayService(MapRenderBeastOverlayCallbacks callbacks)
    {
        _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
    }

    public void DrawInWorldBeasts(IReadOnlyList<TrackedBeastRenderInfo> beasts)
    {
        var lineSpacing = _callbacks.GetWorldTextLineSpacing();
        var replaceNameAndPriceWithStatusText = _callbacks.GetReplaceNameAndPriceWithStatusText();
        var worldPriceTextColor = _callbacks.GetWorldPriceTextColor();
        var worldBeastCircleRadius = _callbacks.GetWorldBeastCircleRadius();

        foreach (var beast in beasts)
        {
            var worldPos = _callbacks.GetWorldPosition(beast.Positioned);
            var screenPos = _callbacks.WorldToScreen(worldPos);
            var hasCaptureState = beast.CaptureState != BeastCaptureState.None;
            var worldBeastColor = _callbacks.GetWorldBeastColor(beast.CaptureState);
            var capturedStatusText = _callbacks.GetDisplayedCaptureStatusText(beast.CaptureState);
            var capturedStatusColor = _callbacks.GetDisplayedCaptureStatusColor(beast.CaptureState);
            var useStatusOnlyText = hasCaptureState && replaceNameAndPriceWithStatusText;

            if (useStatusOnlyText)
            {
                _callbacks.DrawOutlinedText(capturedStatusText, screenPos, capturedStatusColor);
            }
            else
            {
                _callbacks.DrawOutlinedText(beast.BeastName, screenPos, worldBeastColor);

                var nextLineOffset = lineSpacing;
                var priceText = _callbacks.GetBeastPriceTextOrNull(beast.BeastName);
                if (!string.IsNullOrEmpty(priceText))
                {
                    _callbacks.DrawOutlinedText(priceText, screenPos + new Vector2(0, nextLineOffset), worldPriceTextColor);
                    nextLineOffset += lineSpacing;
                }

                if (hasCaptureState)
                {
                    _callbacks.DrawOutlinedText(capturedStatusText, screenPos + new Vector2(0, nextLineOffset), capturedStatusColor);
                }
            }

            _callbacks.DrawFilledCircleInWorld(worldPos, worldBeastCircleRadius, _callbacks.GetWorldBeastCircleColor(beast.CaptureState));
        }
    }

}