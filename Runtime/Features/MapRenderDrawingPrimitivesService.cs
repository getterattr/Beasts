using System;
using System.Collections.Generic;
using SharpDX;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace Beasts.Runtime.Features;

internal sealed record MapRenderDrawingPrimitivesCallbacks(
    Func<Vector3, Vector2> WorldToScreen,
    Func<float> GetWorldBeastCircleFillOpacityPercent,
    Func<float> GetWorldBeastCircleOutlineThickness,
    Func<Color> GetWorldTextOutlineColor,
    Func<Vector2[]> GetWorldCircleScreenPoints,
    IReadOnlyList<Vector2> WorldCirclePoints,
    Action<string, Vector2, Color> DrawCenteredText,
    Action<Vector2[], Color> DrawConvexPolyFilled,
    Action<Vector2[], Color, float> DrawPolyLine);

internal sealed class MapRenderDrawingPrimitivesService
{
    private readonly MapRenderDrawingPrimitivesCallbacks _callbacks;

    public MapRenderDrawingPrimitivesService(MapRenderDrawingPrimitivesCallbacks callbacks)
    {
        _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
    }

    public void DrawCenteredText(string text, Vector2 position, Color color)
    {
        _callbacks.DrawCenteredText(text, position, color);
    }

    public void DrawOutlinedText(string text, Vector2 position, Color color)
    {
        var outlineColor = _callbacks.GetWorldTextOutlineColor();
        _callbacks.DrawCenteredText(text, position + new Vector2(-1, -1), outlineColor);
        _callbacks.DrawCenteredText(text, position + new Vector2(-1, 1), outlineColor);
        _callbacks.DrawCenteredText(text, position + new Vector2(1, -1), outlineColor);
        _callbacks.DrawCenteredText(text, position + new Vector2(1, 1), outlineColor);
        _callbacks.DrawCenteredText(text, position, color);
    }

    public void DrawFilledCircleInWorld(Vector3 position, float radius, Color color)
    {
        var screenPoints = _callbacks.GetWorldCircleScreenPoints();
        for (var i = 0; i < _callbacks.WorldCirclePoints.Count; i++)
        {
            var point = _callbacks.WorldCirclePoints[i];
            screenPoints[i] = _callbacks.WorldToScreen(position + new Vector3(point.X * radius, point.Y * radius, 0));
        }

        var fillOpacity = _callbacks.GetWorldBeastCircleFillOpacityPercent() / 100f;
        _callbacks.DrawConvexPolyFilled(screenPoints, color with { A = Color.ToByte((int)(fillOpacity * 255)) });
        _callbacks.DrawPolyLine(screenPoints, color, _callbacks.GetWorldBeastCircleOutlineThickness());
    }
}