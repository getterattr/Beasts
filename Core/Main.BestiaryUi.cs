using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory;

namespace Beasts;

public partial class Main
{
    private static readonly int[] BestiaryPanelPath = [2, 0, 1, 1, 15];
    private static readonly int[] BestiaryCapturedBeastsTabPath = [2, 0, 1, 1, 15, 0, 18];

    private Element BestiaryChallengesPanel => GameController?.IngameState?.IngameUi?.ChallengesPanel;

    private bool IsBestiaryCapturedBeastsTabVisible()
    {
        if (BestiaryChallengesPanel?.IsVisible != true)
            return false;

        return TryGetBestiaryCapturedBeastsTab()?.IsVisible == true;
    }

    private Element TryGetBestiaryPanel()
    {
        var fixedPanel = TryGetChildFromIndicesQuietly(BestiaryChallengesPanel, BestiaryPanelPath);
        if (fixedPanel?.IsVisible == true)
            return fixedPanel;

        var container = TryGetChildFromIndicesQuietly(BestiaryChallengesPanel, BestiaryPanelPath.Take(BestiaryPanelPath.Length - 1).ToArray());
        if (container?.Children == null)
            return fixedPanel;

        foreach (var candidate in container.Children)
        {
            if (candidate?.IsVisible == true &&
                TryGetChildFromIndicesQuietly(candidate, BestiaryCapturedBeastsTabPath.Skip(BestiaryPanelPath.Length).ToArray()) != null)
            {
                return candidate;
            }
        }

        return fixedPanel;
    }

    private Element TryGetBestiaryCapturedBeastsTab()
    {
        var panel = TryGetBestiaryPanel();
        if (panel == null)
            return null;

        var fixedTab = TryGetChildFromIndicesQuietly(panel, BestiaryCapturedBeastsTabPath.Skip(BestiaryPanelPath.Length).ToArray());
        if (LooksLikeBestiaryCapturedBeastsTab(fixedTab))
            return fixedTab;

        return FindBestiaryCapturedBeastsTabDynamically(panel);
    }

    private static bool LooksLikeBestiaryCapturedBeastsTab(Element candidate)
    {
        if (candidate?.IsVisible != true)
            return false;

        var rect = candidate.GetClientRect();
        if (rect.Width < 300 || rect.Height < 300)
            return false;

        var footer = candidate.GetChildAtIndex(0);
        var scrollbar = candidate.GetChildAtIndex(2);
        return footer?.IsVisible == true && scrollbar?.IsVisible == true;
    }

    private static Element FindBestiaryCapturedBeastsTabDynamically(Element panel)
    {
        var innerRoot = panel?.GetChildAtIndex(0);
        return innerRoot?.Children?
            .Where(candidate => candidate?.IsVisible == true)
            .Where(candidate =>
            {
                var rect = candidate.GetClientRect();
                return rect.Width >= 300 && rect.Height >= 300;
            })
            .OrderByDescending(candidate =>
            {
                var rect = candidate.GetClientRect();
                return rect.Width * rect.Height;
            })
            .FirstOrDefault(LooksLikeBestiaryCapturedBeastsTab);
    }

    private List<Element> GetVisibleBestiaryCapturedBeasts() => BestiaryCapturedBeastsView.GetVisibleCapturedBeasts();

    private string GetBestiaryBeastLabel(Element beastElement) => BestiaryCapturedBeastsView.GetBestiaryBeastLabel(beastElement);

    private static Element GetChildAtOrDefault(Element parent, int childIndex) =>
        parent?.Children is { } children && childIndex >= 0 && childIndex < children.Count ? children[childIndex] : null;

    private static Element TryGetChildFromIndicesQuietly(Element root, IReadOnlyList<int> path)
    {
        var current = root;
        if (current == null || path == null)
            return null;

        foreach (var index in path)
        {
            current = GetChildAtOrDefault(current, index);
            if (current == null)
                return null;
        }

        return current;
    }

    private static string TryGetElementText(Element element)
    {
        try { return element?.Text?.Trim() ?? element?.GetText(16)?.Trim(); }
        catch { return null; }
    }

    private static string GetElementTextRecursive(Element element, int maxDepth = 3)
    {
        if (element == null)
            return null;

        var text = TryGetElementText(element);
        if (!string.IsNullOrWhiteSpace(text) || maxDepth <= 0 || element.Children == null)
            return text;

        foreach (var child in element.Children)
        {
            text = GetElementTextRecursive(child, maxDepth - 1);
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return null;
    }

    private static IEnumerable<Element> EnumerateDescendants(Element root, bool includeSelf = false)
    {
        if (root == null)
            yield break;

        var stack = new Stack<Element>();
        if (includeSelf)
        {
            stack.Push(root);
        }
        else if (root.Children != null)
        {
            for (var i = root.Children.Count - 1; i >= 0; i--)
            {
                if (root.Children[i] != null)
                    stack.Push(root.Children[i]);
            }
        }

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            if (current?.Children == null)
                continue;

            for (var i = current.Children.Count - 1; i >= 0; i--)
            {
                if (current.Children[i] != null)
                    stack.Push(current.Children[i]);
            }
        }
    }
}
