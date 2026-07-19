using System;
using System.Collections.Generic;
using System.Globalization;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;
using Vector2 = System.Numerics.Vector2;

namespace Beasts.Runtime.Features;

internal sealed record MapRenderPanelOverlayCallbacks(
    Func<string, float?> TryGetBeastPriceChaos,
    Action<RectangleF, Color> DrawBox,
    Action<RectangleF, Color, int> DrawFrame,
    Action<string, Vector2, Color> DrawCenteredText,
    Func<RectangleF?> TryGetBestiaryCapturedBeastsVisibleRect,
    Func<List<Element>> GetVisibleBestiaryCapturedBeasts,
    Func<Element, string> GetBestiaryBeastLabel);

internal sealed class MapRenderPanelOverlayService
{
    private readonly MapRenderPanelOverlayCallbacks _callbacks;

    public MapRenderPanelOverlayService(MapRenderPanelOverlayCallbacks callbacks)
    {
        _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
    }

    public void DrawCapturedBeastItems(IList<NormalInventoryItem> items, string itemizedCapturedMonsterMetadata)
    {
        foreach (var item in items)
        {
            if (item?.Item == null || item.Item.Metadata != itemizedCapturedMonsterMetadata)
            {
                continue;
            }

            var monster = item.Item.GetComponent<CapturedMonster>();
            var monsterName = monster?.MonsterVariety?.MonsterName;
            var rect = item.GetClientRect();
            var price = string.IsNullOrEmpty(monsterName) ? null : _callbacks.TryGetBeastPriceChaos(monsterName);

            if (price.HasValue && price.Value >= 0)
            {
                _callbacks.DrawBox(rect, new Color(0, 0, 0, 25));
                _callbacks.DrawCenteredText($"{price.Value.ToString(CultureInfo.InvariantCulture)}c", new Vector2(rect.Center.X, rect.Center.Y), Color.White);
            }
            else
            {
                _callbacks.DrawBox(rect, new Color(255, 255, 0, 25));
                _callbacks.DrawFrame(rect, new Color(255, 255, 0, 50), 1);
            }
        }
    }

    public void DrawBestiaryPanelPrices()
    {
        var visibleRect = _callbacks.TryGetBestiaryCapturedBeastsVisibleRect();
        if (!visibleRect.HasValue)
        {
            return;
        }

        var visibleBeasts = _callbacks.GetVisibleBestiaryCapturedBeasts();
        foreach (var beastEl in visibleBeasts)
        {
            try
            {
                var nameText = beastEl?.Tooltip?.GetChildAtIndex(1)?.GetChildAtIndex(0)?.Text
                    ?.Replace("-", string.Empty).Trim();
                nameText ??= _callbacks.GetBestiaryBeastLabel(beastEl)?.Trim();
                if (string.IsNullOrEmpty(nameText))
                {
                    continue;
                }

                var price = _callbacks.TryGetBeastPriceChaos(nameText);
                if (!price.HasValue || price.Value < 0)
                {
                    continue;
                }

                var rect = beastEl.GetClientRect();
                if (!IsRectMostlyInside(rect, visibleRect.Value))
                {
                    continue;
                }

                var center = new Vector2(rect.Center.X, rect.Center.Y);
                _callbacks.DrawBox(rect, new Color(0, 0, 0, 0.5f));
                _callbacks.DrawFrame(rect, Color.White, 2);
                _callbacks.DrawCenteredText(nameText, center, Color.White);
                _callbacks.DrawCenteredText($"{price.Value.ToString(CultureInfo.InvariantCulture)}c", center + new Vector2(0, 20), Color.White);
            }
            catch
            {
            }
        }
    }

    private static bool IsRectMostlyInside(RectangleF rect, RectangleF bounds)
    {
        var overlapLeft = Math.Max(rect.Left, bounds.Left);
        var overlapTop = Math.Max(rect.Top, bounds.Top);
        var overlapRight = Math.Min(rect.Right, bounds.Right);
        var overlapBottom = Math.Min(rect.Bottom, bounds.Bottom);

        var overlapWidth = overlapRight - overlapLeft;
        var overlapHeight = overlapBottom - overlapTop;
        if (overlapWidth <= 0 || overlapHeight <= 0)
        {
            return false;
        }

        var rectArea = rect.Width * rect.Height;
        if (rectArea <= 0)
        {
            return false;
        }

        var visibleAreaRatio = overlapWidth * overlapHeight / rectArea;
        return visibleAreaRatio >= 0.6f;
    }
}