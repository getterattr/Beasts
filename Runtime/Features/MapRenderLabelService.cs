using System;
using ImGuiNET;
using SharpDX;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;
using Vector2 = System.Numerics.Vector2;

namespace Beasts.Runtime.Features;

internal sealed record MapRenderLabelCallbacks(
    Func<float> GetWorldTextLineSpacing,
    Func<Color> GetWorldPriceTextColor,
    Func<Color> GetMapMarkerBackgroundColor,
    Func<Color> GetMapMarkerTextColor,
    Func<float> GetMapLabelPaddingX,
    Func<float> GetMapLabelPaddingY,
    Func<bool> GetReplaceNameAndPriceWithStatusText,
    Func<BeastCaptureState, Color> GetWorldBeastColor,
    Func<BeastCaptureState, string> GetDisplayedCaptureStatusText,
    Func<BeastCaptureState, Color> GetDisplayedCaptureStatusColor,
    Func<BeastCaptureState, Color> GetWorldBeastCircleColor,
    Func<Color> GetTrackedWindowBeastColor,
    Func<string, BeastCaptureState, (string PrimaryText, string SecondaryText)> BuildPreviewMapMarkerTexts,
    Func<string, BeastCaptureState, (string PrimaryText, string SecondaryText)> BuildMapMarkerTexts,
    Func<float> GetWorldBeastCircleRadius,
    Func<float> GetWorldBeastCircleFillOpacityPercent,
    Func<float> GetWorldBeastCircleOutlineThickness,
    Func<Color> GetWorldTextOutlineColor);

internal sealed class MapRenderLabelService
{
    private readonly MapRenderLabelCallbacks _callbacks;

    public MapRenderLabelService(MapRenderLabelCallbacks callbacks)
    {
        _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
    }

    public void DrawMapMarker(string beastName, BeastCaptureState captureState, Vector2 pos)
    {
        var (primaryText, secondaryText) = _callbacks.BuildMapMarkerTexts(beastName, captureState);

        DrawMapLabel(
            primaryText,
            secondaryText,
            pos,
            captureState != BeastCaptureState.None && _callbacks.GetReplaceNameAndPriceWithStatusText()
                ? _callbacks.GetDisplayedCaptureStatusColor(captureState)
                : _callbacks.GetMapMarkerTextColor(),
            _callbacks.GetDisplayedCaptureStatusColor(captureState));
    }

    public void DrawPreviewWorldLabel(string beastName, BeastCaptureState captureState)
    {
        var drawList = ImGui.GetWindowDrawList();
        var size = new Vector2(280, 88);
        var origin = ImGui.GetCursorScreenPos();
        ImGui.InvisibleButton($"##WorldPreview{beastName}{captureState}", size);

        var centerX = origin.X + size.X / 2f;
        var lineSpacing = _callbacks.GetWorldTextLineSpacing();
        var worldBeastColor = _callbacks.GetWorldBeastColor(captureState);
        var captureTextColor = _callbacks.GetDisplayedCaptureStatusColor(captureState);
        var statusText = _callbacks.GetDisplayedCaptureStatusText(captureState);
        var useCaptureTextOnly = captureState != BeastCaptureState.None && _callbacks.GetReplaceNameAndPriceWithStatusText();

        if (useCaptureTextOnly)
        {
            DrawPreviewOutlinedText(drawList, statusText, new Vector2(centerX, origin.Y + 14), captureTextColor);
        }
        else
        {
            DrawPreviewOutlinedText(drawList, beastName, new Vector2(centerX, origin.Y + 8), worldBeastColor);
            DrawPreviewOutlinedText(drawList, "1c", new Vector2(centerX, origin.Y + 8 + lineSpacing), _callbacks.GetWorldPriceTextColor());
            if (captureState != BeastCaptureState.None)
            {
                DrawPreviewOutlinedText(drawList, statusText, new Vector2(centerX, origin.Y + 8 + lineSpacing * 2), captureTextColor);
            }
        }

        var circleCenter = new Vector2(origin.X + 24, origin.Y + size.Y - 22);
        DrawPreviewCircle(drawList, circleCenter, _callbacks.GetWorldBeastCircleRadius(), captureState);
    }

    public void DrawPreviewMapLabel(string beastName, BeastCaptureState captureState)
    {
        var drawList = ImGui.GetWindowDrawList();
        var size = new Vector2(280, 72);
        var origin = ImGui.GetCursorScreenPos();
        ImGui.InvisibleButton($"##MapPreview{beastName}{captureState}", size);

        var (primaryText, secondaryText) = _callbacks.BuildPreviewMapMarkerTexts(beastName, captureState);

        DrawCenteredLabel(
            drawList,
            primaryText,
            secondaryText,
            origin + size / 2f,
            _callbacks.GetMapMarkerBackgroundColor(),
            captureState != BeastCaptureState.None && _callbacks.GetReplaceNameAndPriceWithStatusText()
                ? _callbacks.GetDisplayedCaptureStatusColor(captureState)
                : _callbacks.GetMapMarkerTextColor(),
            _callbacks.GetDisplayedCaptureStatusColor(captureState));
    }

    public void DrawTrackedBeastPreviewRow(string priceText, string beastName, BeastCaptureState captureState)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(priceText);
        ImGui.TableNextColumn();
        ImGui.TextColored(BeastsHelpers.ToImGuiColor(_callbacks.GetTrackedWindowBeastColor()), beastName);
        if (captureState != BeastCaptureState.None)
        {
            ImGui.SameLine(0, 0);
            ImGui.TextColored(BeastsHelpers.ToImGuiColor(_callbacks.GetDisplayedCaptureStatusColor(captureState)),
                $" {_callbacks.GetDisplayedCaptureStatusText(captureState)}");
        }
    }

    public void DrawPreviewCircles()
    {
        var drawList = ImGui.GetWindowDrawList();
        var size = new Vector2(280, 58);
        var origin = ImGui.GetCursorScreenPos();
        ImGui.InvisibleButton("##CirclePreview", size);

        var normalCenter = new Vector2(origin.X + 46, origin.Y + size.Y / 2f);
        var capturingCenter = new Vector2(origin.X + 140, origin.Y + size.Y / 2f);
        var capturedCenter = new Vector2(origin.X + 234, origin.Y + size.Y / 2f);
        DrawPreviewCircle(drawList, normalCenter, _callbacks.GetWorldBeastCircleRadius(), BeastCaptureState.None);
        DrawPreviewCircle(drawList, capturingCenter, _callbacks.GetWorldBeastCircleRadius(), BeastCaptureState.Capturing);
        DrawPreviewCircle(drawList, capturedCenter, _callbacks.GetWorldBeastCircleRadius(), BeastCaptureState.Captured);
        drawList.AddText(new Vector2(normalCenter.X - 26, normalCenter.Y + 18), 0xFFFFFFFF, "Normal");
        drawList.AddText(new Vector2(capturingCenter.X - 30, capturingCenter.Y + 18), 0xFFFFFFFF, _callbacks.GetDisplayedCaptureStatusText(BeastCaptureState.Capturing));
        drawList.AddText(new Vector2(capturedCenter.X - 26, capturedCenter.Y + 18), 0xFFFFFFFF, _callbacks.GetDisplayedCaptureStatusText(BeastCaptureState.Captured));
    }

    private void DrawPreviewCircle(ImDrawListPtr drawList, Vector2 center, float configuredRadius, BeastCaptureState captureState)
    {
        var radius = 8f + configuredRadius / 200f * 18f;
        var circleColor = _callbacks.GetWorldBeastCircleColor(captureState);

        var outlineColor = BeastsHelpers.ToImGuiColorU32(circleColor);
        var fillOpacity = _callbacks.GetWorldBeastCircleFillOpacityPercent() / 100f;
        var fillColor = BeastsHelpers.ToImGuiColorU32(circleColor with { A = Color.ToByte((int)(fillOpacity * 255)) });
        drawList.AddCircleFilled(center, radius, fillColor, 24);
        drawList.AddCircle(center, radius, outlineColor, 24, _callbacks.GetWorldBeastCircleOutlineThickness());
    }

    private void DrawPreviewOutlinedText(ImDrawListPtr drawList, string text, Vector2 centerPosition, Color color)
    {
        var textSize = ImGui.CalcTextSize(text);
        var topLeft = centerPosition - textSize / 2f;
        var outlineU32 = BeastsHelpers.ToImGuiColorU32(_callbacks.GetWorldTextOutlineColor());
        var textU32 = BeastsHelpers.ToImGuiColorU32(color);

        drawList.AddText(topLeft + new Vector2(-1, -1), outlineU32, text);
        drawList.AddText(topLeft + new Vector2(-1, 1), outlineU32, text);
        drawList.AddText(topLeft + new Vector2(1, -1), outlineU32, text);
        drawList.AddText(topLeft + new Vector2(1, 1), outlineU32, text);
        drawList.AddText(topLeft, textU32, text);
    }

    private void DrawCenteredLabel(ImDrawListPtr drawList, string primaryText, string secondaryText, Vector2 pos, Color backgroundColor, Color primaryColor, Color secondaryColor)
    {
        var lineSpacing = _callbacks.GetWorldTextLineSpacing();
        var hasSecondaryText = !string.IsNullOrEmpty(secondaryText);
        var primarySize = ImGui.CalcTextSize(primaryText);
        var secondarySize = hasSecondaryText ? ImGui.CalcTextSize(secondaryText) : Vector2.Zero;
        var width = Math.Max(primarySize.X, secondarySize.X);
        var height = primarySize.Y + (hasSecondaryText ? secondarySize.Y + lineSpacing * 0.25f : 0f);
        var half = new Vector2(width / 2f, height / 2f);
        var pad = new Vector2(_callbacks.GetMapLabelPaddingX(), _callbacks.GetMapLabelPaddingY());

        drawList.AddRectFilled(pos - half - pad, pos + half + pad, BeastsHelpers.ToImGuiColorU32(backgroundColor));

        var primaryPos = new Vector2(pos.X - primarySize.X / 2f, hasSecondaryText ? pos.Y - height / 2f : pos.Y - primarySize.Y / 2f);
        drawList.AddText(primaryPos, BeastsHelpers.ToImGuiColorU32(primaryColor), primaryText);

        if (!hasSecondaryText)
        {
            return;
        }

        var secondaryPos = new Vector2(pos.X - secondarySize.X / 2f, primaryPos.Y + primarySize.Y + lineSpacing * 0.25f);
        drawList.AddText(secondaryPos, BeastsHelpers.ToImGuiColorU32(secondaryColor), secondaryText);
    }

    private void DrawMapLabel(string primaryText, string secondaryText, Vector2 pos, Color primaryColor, Color secondaryColor)
    {
        DrawCenteredLabel(ImGui.GetForegroundDrawList(), primaryText, secondaryText, pos, _callbacks.GetMapMarkerBackgroundColor(), primaryColor, secondaryColor);
    }
}