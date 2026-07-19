using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ExileCore.PoEMemory;
using SharpDX;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace Beasts;

internal static class BeastsHelpers
{
    public static string FormatDuration(TimeSpan duration) =>
        duration.TotalHours >= 1
            ? duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
            : duration.ToString(@"mm\:ss", CultureInfo.InvariantCulture);

    public static Vector4 ToImGuiColor(Color color) => new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

    public static uint ToImGuiColorU32(Color color) => ImGuiNET.ImGui.ColorConvertFloat4ToU32(ToImGuiColor(color));

    public static string PluralSuffix(int count) => count == 1 ? string.Empty : "s";

    public static bool EqualsIgnoreCase(this string value, string other) =>
        string.Equals(value, other, StringComparison.OrdinalIgnoreCase);

    public static IOrderedEnumerable<T> OrderByScreenPosition<T>(this IEnumerable<T> items, Func<T, RectangleF> rectSelector) =>
        items.OrderBy(item => rectSelector(item).Top)
             .ThenBy(item => rectSelector(item).Left);

    public static Vector2[] CreateUnitCirclePoints(int segments, bool closeLoop = true)
    {
        var pointCount = closeLoop ? segments + 1 : segments;
        var points = new Vector2[pointCount];
        for (var i = 0; i < segments; i++)
        {
            var angle = i * 2f * MathF.PI / segments;
            points[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }

        if (closeLoop)
        {
            points[segments] = points[0];
        }

        return points;
    }

    public static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    public static Element GetChildAtIndices(Element root, params int[] indices)
    {
        var current = root;
        if (current == null || indices == null)
        {
            return null;
        }

        foreach (var index in indices)
        {
            current = current.GetChildAtIndex(index);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    public static bool TryGetTerrainHeight(float[][] heightData, int x, int y, out float height)
    {
        height = 0;
        if (heightData == null || y < 0 || y >= heightData.Length || x < 0 || x >= heightData[y].Length)
        {
            return false;
        }

        height = heightData[y][x];
        return true;
    }

    public static string TryGetAreaHashText(object area)
    {
        if (area == null)
        {
            return null;
        }

        static string TryReadPropertyString(object value, string propertyName) => value.GetType().GetProperty(propertyName)?.GetValue(value)?.ToString();

        return TryReadPropertyString(area, "AreaHash") ?? TryReadPropertyString(area, "Hash");
    }

    public static int TryGetAreaInstanceId(object area)
    {
        if (area == null) return -1;
        var val = area.GetType().GetProperty("InstanceId")?.GetValue(area);
        if (val is int id) return id;
        if (val != null && int.TryParse(val.ToString(), out var parsed)) return parsed;
        return -1;
    }

    public static string TryGetAreaNameText(object area)
    {
        if (area == null)
        {
            return string.Empty;
        }

        static string TryReadPropertyString(object value, string propertyName) => value.GetType().GetProperty(propertyName)?.GetValue(value)?.ToString();

        return TryReadPropertyString(area, "Name")
               ?? TryReadPropertyString(area, "DisplayName")
               ?? TryReadPropertyString(area, "RawName")
               ?? string.Empty;
    }
}

