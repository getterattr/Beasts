using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using SharpDX;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;
using Element = ExileCore.PoEMemory.Element;

namespace Beasts.Runtime.Features;

internal sealed record BestiaryCapturedBeastsViewCallbacks(
    Func<Element> TryGetBestiaryPanel,
    Func<Element> TryGetBestiaryCapturedBeastsTab,
    Func<Element, string> GetBestiaryBeastFallbackLabel,
    Func<Element, IEnumerable<Element>> EnumerateDescendants);

internal sealed class BestiaryCapturedBeastsViewService
{
    private readonly BestiaryCapturedBeastsViewCallbacks _callbacks;

    public BestiaryCapturedBeastsViewService(BestiaryCapturedBeastsViewCallbacks callbacks)
    {
        _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
    }

    public bool TryGetCapturedBeastsDisplay(out Element beastsDisplay, out RectangleF visibleRect)
    {
        beastsDisplay = null!;
        visibleRect = default;

        var bestiaryPanel = _callbacks.TryGetBestiaryPanel();
        if (bestiaryPanel == null || !bestiaryPanel.IsVisible)
        {
            return false;
        }

        var capturedPanel = _callbacks.TryGetBestiaryCapturedBeastsTab();
        if (capturedPanel == null || !capturedPanel.IsVisible)
        {
            return false;
        }

        var viewport = TryGetViewport(capturedPanel);
        if (viewport == null || !viewport.IsVisible)
        {
            return false;
        }

        visibleRect = viewport.GetClientRect();
        beastsDisplay = TryGetDisplayRoot(viewport);
        return beastsDisplay != null;
    }

    public string GetBestiaryBeastLabel(Element beastElement)
    {
        if (beastElement == null)
        {
            return null;
        }

        var entityName = beastElement.Entity?.GetComponent<Base>()?.Name?.Trim();
        if (!string.IsNullOrWhiteSpace(entityName))
        {
            return entityName;
        }

        var entityMetadata = beastElement.Entity?.Metadata?.Trim();
        if (!string.IsNullOrWhiteSpace(entityMetadata))
        {
            return entityMetadata;
        }

        return _callbacks.GetBestiaryBeastFallbackLabel(beastElement);
    }

    public List<Element> GetVisibleCapturedBeasts()
    {
        if (!TryGetCapturedBeastsDisplay(out var beastsDisplay, out var visibleRect))
        {
            return [];
        }

        var visibleBeasts = new List<Element>();
        foreach (var familyGroup in GetVisibleFamilyGroups(beastsDisplay))
        {
            AddCapturedBeastCandidates(GetFamilyRowsRoot(familyGroup)?.Children, visibleRect, visibleBeasts);
        }

        return DistinctAndOrderCapturedBeasts(visibleBeasts);
    }

    public int GetTotalCapturedBeastCount()
    {
        if (!TryGetCapturedBeastsDisplay(out var beastsDisplay, out _))
        {
            return 0;
        }

        var displayedBeasts = new List<Element>();
        foreach (var familyGroup in GetVisibleFamilyGroups(beastsDisplay))
        {
            AddDisplayedCapturedBeastCandidates(GetFamilyRowsRoot(familyGroup)?.Children, displayedBeasts);
        }

        return DistinctAndOrderCapturedBeasts(displayedBeasts).Count;
    }

    private IEnumerable<Element> GetVisibleFamilyGroups(Element beastsDisplay) =>
        beastsDisplay?.Children?.Where(element => element?.IsVisible == true) ?? Enumerable.Empty<Element>();

    private static Element GetFamilyRowsRoot(Element familyGroup)
    {
        var fixedRowsRoot = familyGroup?.GetChildAtIndex(1);
        if (fixedRowsRoot != null)
        {
            return fixedRowsRoot;
        }

        return familyGroup?.Children?
            .Where(child => child != null)
            .OrderByDescending(child => child.Children?.Count ?? 0)
            .FirstOrDefault();
    }

    private static Element TryGetViewport(Element capturedPanel)
    {
        var fixedViewport = capturedPanel?.GetChildAtIndex(1);
        if (fixedViewport?.IsVisible == true)
        {
            return fixedViewport;
        }

        return capturedPanel?.Children?
            .Where(child => child?.IsVisible == true)
            .OrderByDescending(child =>
            {
                var rect = child.GetClientRect();
                return rect.Width * rect.Height;
            })
            .FirstOrDefault();
    }

    private static Element TryGetDisplayRoot(Element viewport)
    {
        var fixedDisplay = viewport?.GetChildAtIndex(0);
        if (fixedDisplay != null)
        {
            return fixedDisplay;
        }

        return viewport?.Children?
            .Where(child => child?.IsVisible == true)
            .OrderByDescending(child => child.Children?.Count ?? 0)
            .ThenByDescending(child =>
            {
                var rect = child.GetClientRect();
                return rect.Width * rect.Height;
            })
            .FirstOrDefault();
    }

    private bool IsDisplayedCapturedBeastCandidate(Element beastElement)
    {
        if (beastElement?.IsVisible != true)
        {
            return false;
        }

        var rect = beastElement.GetClientRect();
        if (rect.Width < 16 || rect.Height < 16)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(GetBestiaryBeastLabel(beastElement)))
        {
            return false;
        }

        return beastElement.Entity != null || _callbacks.EnumerateDescendants(beastElement).Any(child => child?.Entity != null);
    }

    private bool IsCapturedBeastCandidate(Element beastElement, RectangleF visibleRect)
    {
        if (!IsDisplayedCapturedBeastCandidate(beastElement))
        {
            return false;
        }

        return IsRectMostlyInside(beastElement.GetClientRect(), visibleRect);
    }

    private void AddCapturedBeastCandidates(IEnumerable<Element> source, RectangleF visibleRect, ICollection<Element> destination)
    {
        if (source == null || destination == null)
        {
            return;
        }

        foreach (var beastElement in source)
        {
            if (IsCapturedBeastCandidate(beastElement, visibleRect))
            {
                destination.Add(beastElement);
            }
        }
    }

    private void AddDisplayedCapturedBeastCandidates(IEnumerable<Element> source, ICollection<Element> destination)
    {
        if (source == null || destination == null)
        {
            return;
        }

        foreach (var beastElement in source)
        {
            if (IsDisplayedCapturedBeastCandidate(beastElement))
            {
                destination.Add(beastElement);
            }
        }
    }

    private static List<Element> DistinctAndOrderCapturedBeasts(IEnumerable<Element> beasts)
    {
        return beasts
            .GroupBy(element =>
            {
                var rect = element.GetClientRect();
                return new { rect.Left, rect.Top, rect.Right, rect.Bottom };
            })
            .Select(group => group
                .OrderByDescending(element => element.Entity != null)
                .ThenByDescending(element => element.Children?.Count ?? 0)
                .First())
            .OrderByScreenPosition(element => element.GetClientRect())
            .ToList();
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