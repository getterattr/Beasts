using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using SharpDX;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;
using Vector2 = System.Numerics.Vector2;

namespace Beasts;

public partial class Main : BaseSettingsPlugin<Settings>
{
    private static readonly TimeSpan CachedCapturingOverlayLifetime = TimeSpan.FromSeconds(2);
    private const string BestiaryScarabOfDuplicatingName = "Bestiary Scarab of Duplicating";
    private const string IsinMirageMapStatKeyPart = "MapMirageChosenWish";
    private const string MissingTrackedBeastName = "\0";
    private static readonly GameStat? IsCapturableMonsterStat = TryGetCapturableMonsterStat();

    private static readonly TrackedBeast[] AllRedBeasts = BeastData.AllRedBeasts;

    private readonly HashSet<long> _capturedBeastIds = new();
    private readonly Dictionary<long, Entity> _trackedBeastEntities = new();
    private readonly Dictionary<long, TrackedBeastMapMarkerInfo> _trackedBeastOverlayCacheById = new();
    private readonly Dictionary<string, string> _trackedBeastNameCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<TrackedBeastRenderInfo> _trackedBeastRenderBuffer = new();
    private readonly List<TrackedBeastMapMarkerInfo> _trackedBeastOverlayBuffer = new();
    private IReadOnlyList<TrackedBeastMapMarkerInfo> _trackedBeastOverlayThrottleBuffer = Array.Empty<TrackedBeastMapMarkerInfo>();
    private bool _wasBestiaryTabVisible;
    private bool _isBestiaryClipboardPasteRunning;
    private string _trackedBeastOverlayCacheAreaHash = string.Empty;
    private int _trackedBeastOverlayCacheAreaInstanceId = -1;
    private DateTime _trackedBeastOverlayThrottleLastRefreshUtc = DateTime.MinValue;
    private int _trackedBeastOverlayThrottleRefreshMs = -1;
    private bool _trackedBeastOverlayThrottleShowEnabledOnly;

    public Main()
    {
        Name = "Beasts";
    }

    public override void OnLoad()
    {
        Core.Initialize(this);

        var now = DateTime.UtcNow;
        Runtime.Initialize(now, CreateSettingsBindingTargets());

        InitializeCurrentAreaTracking(now);

        LoadPersistedBeastPriceSettings();
        QueuePriceFetch();
    }

    public override void OnClose()
    {
        base.OnClose();
        SavePersistedBeastPriceSettings();
        Runtime.Shutdown();
        _mapRenderState = null;
        _bestiaryCapturedBeastsViewService = null;
        _beastLookupService = null;
        _mapRenderPresentationService = null;
        _mapRenderImGuiOverlayService = null;
        _mapRenderBeastOverlayService = null;
        _mapRenderLabelService = null;
        _mapRenderDrawingPrimitivesService = null;
        _mapRenderPanelOverlayService = null;
        _runtime = null;
        Core.Shutdown();
    }

    private const string MenagerieAreaName = "The Menagerie";
    private const string SettingsFileName = "Beasts_settings.json";

    private void LogDebug(string message)
    {
        if (Settings?.DebugLogging?.Value == true)
        {
            try { DebugWindow.LogMsg($"[Beasts] {message}"); } catch { }
        }
    }

    private void LogError(string message, Exception ex = null)
    {
        var full = ex == null ? message : $"{message} {ex.GetType().Name}: {ex.Message}";
        try { DebugWindow.LogMsg($"[Beasts] ERROR: {full}"); } catch { }
    }

    private void Log(string message)
    {
        try { DebugWindow.LogMsg($"[Beasts] {message}"); } catch { }
    }

    private static bool IsHideoutLikeArea(AreaInstance area)
    {
        return area?.IsHideout == true ||
               area?.Name.EqualsIgnoreCase(MenagerieAreaName) == true;
    }

    private static bool IsOverlayHideInHideoutArea(AreaInstance area)
    {
        return area?.IsTown == true ||
               area?.IsPeaceful == true ||
               IsHideoutLikeArea(area);
    }

    private static bool IsRunnableMapArea(AreaInstance area)
    {
        return area is { IsTown: false } && !IsHideoutLikeArea(area);
    }

    private void InitializeCurrentAreaTracking(DateTime now)
    {
        var currentArea = GameController?.Area?.CurrentArea;
        _isCurrentAreaTrackable = IsRunnableMapArea(currentArea);
        if (_isCurrentAreaTrackable)
        {
            _activeMapAreaHash = BeastsHelpers.TryGetAreaHashText(currentArea);
            _activeMapAreaName = BeastsHelpers.TryGetAreaNameText(currentArea);
            _activeMapInstanceId = BeastsHelpers.TryGetAreaInstanceId(currentArea);
        }
    }

    private void LoadPersistedBeastPriceSettings()
    {
        try
        {
            var settingsPath = GetBeastsSettingsFilePath();
            if (!File.Exists(settingsPath))
            {
                return;
            }

            var root = JObject.Parse(File.ReadAllText(settingsPath));
            if (root["BeastPrices"] is not JObject beastPricesSection)
            {
                return;
            }

            Settings.BeastPrices.LastUpdated = beastPricesSection["LastUpdated"]?.Value<string>() ?? Settings.BeastPrices.LastUpdated;

            if (beastPricesSection["EnabledBeasts"] is JArray enabledBeastsArray)
            {
                Settings.BeastPrices.EnabledBeasts = new HashSet<string>(
                    enabledBeastsArray.Values<string>().Where(x => !string.IsNullOrWhiteSpace(x)),
                    StringComparer.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex)
        {
            LogError("Failed to load persisted beast price settings", ex);
        }
    }

    private void SavePersistedBeastPriceSettings()
    {
        try
        {
            var settingsPath = GetBeastsSettingsFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            var content = File.Exists(settingsPath) ? File.ReadAllText(settingsPath) : null;
            var root = !string.IsNullOrWhiteSpace(content) ? JObject.Parse(content) : new JObject();
            var beastPricesSection = root["BeastPrices"] as JObject ?? new JObject();
            beastPricesSection["LastUpdated"] = Settings.BeastPrices.LastUpdated;
            beastPricesSection["EnabledBeasts"] = new JArray(Settings.BeastPrices.EnabledBeasts.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
            root["BeastPrices"] = beastPricesSection;
            File.WriteAllText(settingsPath, root.ToString());
        }
        catch (Exception ex)
        {
            LogError("Failed to save persisted beast price settings", ex);
        }
    }

    private static string GetBeastsSettingsFilePath()
    {
        return Path.Combine(Directory.GetCurrentDirectory(), "config", "global", SettingsFileName);
    }

    private void QueuePriceFetch()
    {
        _ = Task.Run(FetchBeastPricesAsync);
    }

    public override void AreaChange(AreaInstance area)
    {
        Core.AreaChanged(area);

        var decision = Runtime.AreaTransitions.Evaluate(area);

        _trackedBeastEntities.Clear();
        InvalidateTrackedBeastOverlayThrottle();

        switch (decision.Kind)
        {
            case global::Beasts.Runtime.Lifecycle.AreaTransitionKind.EnteredNewTrackableMap:
                ClearTrackedBeastOverlayCache();
                SetTrackedBeastOverlayCacheScope(decision.NewAreaHash, decision.NewAreaInstanceId);
                break;

            case global::Beasts.Runtime.Lifecycle.AreaTransitionKind.ReenteredActiveMap:
                SetTrackedBeastOverlayCacheScope(decision.NewAreaHash, decision.NewAreaInstanceId);
                break;
        }

        if (decision.Kind != global::Beasts.Runtime.Lifecycle.AreaTransitionKind.EnteredNewTrackableMap)
        {
            return;
        }

        _capturedBeastIds.Clear();
    }

    private void ClearTrackedBeastOverlayCache()
    {
        _trackedBeastOverlayCacheById.Clear();
        _trackedBeastOverlayCacheAreaHash = string.Empty;
        _trackedBeastOverlayCacheAreaInstanceId = -1;
        InvalidateTrackedBeastOverlayThrottle();
    }

    private void InvalidateTrackedBeastOverlayThrottle()
    {
        _trackedBeastOverlayThrottleBuffer = Array.Empty<TrackedBeastMapMarkerInfo>();
        _trackedBeastOverlayThrottleLastRefreshUtc = DateTime.MinValue;
        _trackedBeastOverlayThrottleRefreshMs = -1;
    }

    private void SetTrackedBeastOverlayCacheScope(string areaHash, int areaInstanceId)
    {
        _trackedBeastOverlayCacheAreaHash = areaHash ?? string.Empty;
        _trackedBeastOverlayCacheAreaInstanceId = areaInstanceId;
    }

    private void EnsureTrackedBeastOverlayCacheScopeCurrentArea()
    {
        if (!string.IsNullOrWhiteSpace(_trackedBeastOverlayCacheAreaHash) || _trackedBeastOverlayCacheAreaInstanceId >= 0)
        {
            return;
        }

        var currentArea = GameController?.Area?.CurrentArea;
        if (currentArea == null)
        {
            return;
        }

        SetTrackedBeastOverlayCacheScope(
            BeastsHelpers.TryGetAreaHashText(currentArea),
            BeastsHelpers.TryGetAreaInstanceId(currentArea));
    }

    private bool IsTrackedBeastOverlayCacheInCurrentAreaScope()
    {
        var currentArea = GameController?.Area?.CurrentArea;
        if (currentArea == null)
        {
            return false;
        }

        var currentAreaHash = BeastsHelpers.TryGetAreaHashText(currentArea) ?? string.Empty;
        var currentAreaInstanceId = BeastsHelpers.TryGetAreaInstanceId(currentArea);
        var hashMatches = !string.IsNullOrWhiteSpace(_trackedBeastOverlayCacheAreaHash) &&
                          !string.IsNullOrWhiteSpace(currentAreaHash) &&
                          string.Equals(currentAreaHash, _trackedBeastOverlayCacheAreaHash, StringComparison.Ordinal);
        var instanceMatches = _trackedBeastOverlayCacheAreaInstanceId >= 0 && currentAreaInstanceId >= 0 &&
                              currentAreaInstanceId == _trackedBeastOverlayCacheAreaInstanceId;
        return hashMatches || instanceMatches;
    }

    public override void EntityAdded(Entity entity)
    {
        if (!IsRareBeast(entity)) return;
        if (TryGetTrackedBeastNameCached(entity.Metadata, out _))
        {
            _trackedBeastEntities[entity.Id] = entity;
            UpdateTrackedBeastOverlayCache(entity, isLive: true);
        }
    }

    public override void EntityRemoved(Entity entity)
    {
        _trackedBeastEntities.Remove(entity.Id);

        if (_capturedBeastIds.Contains(entity.Id))
        {
            _trackedBeastOverlayCacheById.Remove(entity.Id);
            return;
        }

        UpdateTrackedBeastOverlayCache(entity, isLive: false);
    }

    private void TrackBeastCaptureStates()
    {
        var liveTrackedBeasts = BuildLiveTrackedBeastEntityMap();
        var staleTrackedIds = new List<long>();

        foreach (var (id, entity) in _trackedBeastEntities)
        {
            if (liveTrackedBeasts.ContainsKey(id))
            {
                continue;
            }

            staleTrackedIds.Add(id);

            if (_trackedBeastOverlayCacheById.TryGetValue(id, out var cachedOverlay) && cachedOverlay.IsLive)
            {
                _trackedBeastOverlayCacheById[id] = cachedOverlay with
                {
                    IsLive = false,
                    LastUpdatedUtc = DateTime.UtcNow,
                };
            }
        }

        foreach (var staleTrackedId in staleTrackedIds)
        {
            _trackedBeastEntities.Remove(staleTrackedId);
        }

        foreach (var (id, entity) in liveTrackedBeasts)
        {
            _trackedBeastEntities[id] = entity;

            UpdateTrackedBeastOverlayCache(entity, isLive: true);

            if (_capturedBeastIds.Contains(id)) continue;
            if (GetBeastCaptureState(entity) != BeastCaptureState.Captured) continue;
            if (!TryGetTrackedBeastNameCached(entity.Metadata, out var beastName)) continue;

            MarkTrackedBeastCaptured(id, beastName, DateTime.UtcNow);
        }
    }

    private void MarkTrackedBeastCaptured(long entityId, string beastName, DateTime now)
    {
        _trackedBeastOverlayCacheById.Remove(entityId);
        _capturedBeastIds.Add(entityId);
    }

    private Dictionary<long, Entity> BuildLiveTrackedBeastEntityMap()
    {
        var liveEntities = GameController?.EntityListWrapper?.Entities;
        if (liveEntities == null)
        {
            return [];
        }

        var trackedBeasts = new Dictionary<long, Entity>();
        foreach (var liveEntity in liveEntities)
        {
            if (liveEntity?.IsValid != true)
            {
                continue;
            }

            if (!IsRareBeast(liveEntity))
            {
                continue;
            }

            if (!TryGetTrackedBeastNameCached(liveEntity.Metadata, out _))
            {
                continue;
            }

            trackedBeasts[liveEntity.Id] = liveEntity;
        }

        return trackedBeasts;
    }

    private IReadOnlyList<TrackedBeastRenderInfo> CollectTrackedBeastRenderInfo()
    {
        _trackedBeastRenderBuffer.Clear();
        var showEnabledOnly = Settings.MapRender.ShowEnabledOnly.Value;
        var enabledBeasts = Settings.BeastPrices.EnabledBeasts;

        foreach (var (_, entity) in _trackedBeastEntities)
        {
            if (!entity.IsValid) continue;
            if (!TryGetTrackedBeastNameCached(entity.Metadata, out var beastName)) continue;
            if (showEnabledOnly && !enabledBeasts.Contains(beastName)) continue;

            var captureState = GetBeastCaptureState(entity);
            var positioned = entity.GetComponent<Positioned>();
            if (positioned == null) continue;

            _trackedBeastRenderBuffer.Add(new TrackedBeastRenderInfo(
                entity,
                positioned,
                beastName,
                captureState));
        }

        return _trackedBeastRenderBuffer;
    }

    private IReadOnlyList<TrackedBeastMapMarkerInfo> CollectTrackedBeastOverlayInfo()
    {
        _trackedBeastOverlayBuffer.Clear();
        var showEnabledOnly = Settings.MapRender.ShowEnabledOnly.Value;
        var enabledBeasts = Settings.BeastPrices.EnabledBeasts;
        var now = DateTime.UtcNow;
        var shouldIncludeCachedOverlays = IsTrackedBeastOverlayCacheInCurrentAreaScope();
        var liveOverlayIds = new HashSet<long>();
        List<long> expiredOverlayIds = null;

        foreach (var (id, entity) in _trackedBeastEntities)
        {
            if (entity?.IsValid != true)
            {
                continue;
            }

            if (!TryBuildTrackedBeastOverlayInfo(entity, isLive: true, out var liveOverlayInfo))
            {
                continue;
            }

            if (showEnabledOnly && !enabledBeasts.Contains(liveOverlayInfo.BeastName))
            {
                continue;
            }

            _trackedBeastOverlayBuffer.Add(liveOverlayInfo);
            liveOverlayIds.Add(id);
        }

        foreach (var overlayInfo in _trackedBeastOverlayCacheById.Values)
        {
            if (liveOverlayIds.Contains(overlayInfo.EntityId))
            {
                continue;
            }

            if (_capturedBeastIds.Contains(overlayInfo.EntityId))
            {
                continue;
            }

            var isCapturingExpired = overlayInfo.CaptureState == BeastCaptureState.Capturing &&
                                     now - overlayInfo.LastUpdatedUtc > CachedCapturingOverlayLifetime;

            if (overlayInfo.CaptureState == BeastCaptureState.Captured)
            {
                continue;
            }

            if (isCapturingExpired)
            {
                MarkTrackedBeastCaptured(overlayInfo.EntityId, overlayInfo.BeastName, now);
                expiredOverlayIds ??= [];
                expiredOverlayIds.Add(overlayInfo.EntityId);
                continue;
            }

            if (!shouldIncludeCachedOverlays)
            {
                continue;
            }

            if (showEnabledOnly && !enabledBeasts.Contains(overlayInfo.BeastName))
            {
                continue;
            }

            _trackedBeastOverlayBuffer.Add(overlayInfo);
        }

        if (expiredOverlayIds != null)
        {
            foreach (var overlayId in expiredOverlayIds)
            {
                _trackedBeastOverlayCacheById.Remove(overlayId);
            }
        }

        return _trackedBeastOverlayBuffer;
    }

    private const int TrackedBeastOverlayRefreshMs = 75;

    private IReadOnlyList<TrackedBeastMapMarkerInfo> CollectTrackedBeastOverlayInfoThrottled(DateTime now)
    {
        var mapRender = Settings.MapRender;
        var refreshMs = TrackedBeastOverlayRefreshMs;
        var showEnabledOnly = mapRender.ShowEnabledOnly.Value;

        var settingsChanged = refreshMs != _trackedBeastOverlayThrottleRefreshMs ||
                              showEnabledOnly != _trackedBeastOverlayThrottleShowEnabledOnly;
        var refreshDue = refreshMs <= 0 ||
                         now - _trackedBeastOverlayThrottleLastRefreshUtc >= TimeSpan.FromMilliseconds(refreshMs);

        if (!settingsChanged && !refreshDue)
        {
            return _trackedBeastOverlayThrottleBuffer;
        }

        _trackedBeastOverlayThrottleBuffer = CollectTrackedBeastOverlayInfo();
        _trackedBeastOverlayThrottleLastRefreshUtc = now;
        _trackedBeastOverlayThrottleRefreshMs = refreshMs;
        _trackedBeastOverlayThrottleShowEnabledOnly = showEnabledOnly;
        return _trackedBeastOverlayThrottleBuffer;
    }

    private void UpdateTrackedBeastOverlayCache(Entity entity, bool isLive)
    {
        if (entity == null)
        {
            return;
        }

        EnsureTrackedBeastOverlayCacheScopeCurrentArea();

        if (TryBuildTrackedBeastOverlayInfo(entity, isLive, out var overlayInfo))
        {
            if (overlayInfo.CaptureState == BeastCaptureState.Captured)
            {
                _trackedBeastOverlayCacheById.Remove(entity.Id);
                return;
            }

            _trackedBeastOverlayCacheById[entity.Id] = overlayInfo;
            return;
        }

        if (_trackedBeastOverlayCacheById.TryGetValue(entity.Id, out var cached))
        {
            var shouldRefreshTimestamp = isLive || cached.IsLive != isLive;
            _trackedBeastOverlayCacheById[entity.Id] = cached with
            {
                IsLive = isLive,
                LastUpdatedUtc = shouldRefreshTimestamp ? DateTime.UtcNow : cached.LastUpdatedUtc,
            };
        }
    }

    private bool TryBuildTrackedBeastOverlayInfo(Entity entity, bool isLive, out TrackedBeastMapMarkerInfo overlayInfo)
    {
        overlayInfo = default;

        if (entity == null)
        {
            return false;
        }

        if (!TryGetTrackedBeastNameCached(entity.Metadata, out var beastName))
        {
            return false;
        }

        var positioned = entity.GetComponent<Positioned>();
        if (positioned == null)
        {
            return false;
        }

        overlayInfo = new TrackedBeastMapMarkerInfo(
            entity.Id,
            positioned.GridPosNum,
            beastName,
            GetBeastCaptureState(entity),
            isLive,
            DateTime.UtcNow);
        return true;
    }

    public override void Render()
    {
        var now = DateTime.UtcNow;

        if (_isCurrentAreaTrackable)
        {
            TrackBeastCaptureStates();
        }

        HandleBestiaryClipboardAutoCopy();

        var beastPrices = Settings.BeastPrices;
        var mapRender = Settings.MapRender;

        TryScheduleAutoPriceRefresh(now, beastPrices);

        if (!AreOverlaysVisible())
            return;

        if (IsinMirage())
            return;

        IReadOnlyList<TrackedBeastRenderInfo> liveTrackedBeasts = Array.Empty<TrackedBeastRenderInfo>();
        if (mapRender.ShowBeastLabelsInWorld.Value)
        {
            liveTrackedBeasts = CollectTrackedBeastRenderInfo();
        }

        IReadOnlyList<TrackedBeastMapMarkerInfo> trackedBeastOverlays = Array.Empty<TrackedBeastMapMarkerInfo>();
        if (mapRender.ShowTrackedBeastsWindow.Value)
        {
            trackedBeastOverlays = CollectTrackedBeastOverlayInfoThrottled(now);
        }

        if (mapRender.ShowBeastLabelsInWorld.Value && liveTrackedBeasts.Count > 0)
            DrawInWorldBeasts(liveTrackedBeasts);

        if (mapRender.ShowTrackedBeastsWindow.Value && trackedBeastOverlays.Count > 0)
            DrawTrackedBeastsWindow(trackedBeastOverlays);

        if (mapRender.ShowPricesOnCapturedBeasts.Value)
        {
            DrawInventoryBeasts();
            DrawStashBeasts();
            DrawMerchantBeasts();
            DrawBestiaryPanelPrices();
        }
    }

    private bool AreOverlaysVisible()
    {
        if (!Settings.AutoHideOverlays.Value)
            return true;

        var area = GameController?.Area?.CurrentArea;
        if (IsOverlayHideInHideoutArea(area))
            return false;

        var ingameUi = GameController?.IngameState?.IngameUi;
        if (ingameUi == null)
            return true;

        if (ingameUi.FullscreenPanels.Any(p => p.IsVisible))
            return false;

        if (ingameUi.OpenLeftPanel?.IsVisible == true)
            return false;

        if (ingameUi.OpenRightPanel?.IsVisible == true)
            return false;

        return true;
    }

    private void TryScheduleAutoPriceRefresh(DateTime now, BeastPricesSettings beastPrices)
    {
        var autoRefreshMinutes = beastPrices.AutoRefreshMinutes.Value;
        if (autoRefreshMinutes <= 0 || _isFetchingPrices ||
            (now - _lastPriceFetchAttempt).TotalMinutes < autoRefreshMinutes)
        {
            return;
        }

        QueuePriceFetch();
    }

    private bool IsinMirage()
    {
        var mapStats = GameController?.IngameState?.Data?.MapStats;
        if (mapStats == null || mapStats.Count == 0)
        {
            return false;
        }

        return mapStats.Any(stat =>
            stat.Key.ToString().IndexOf(IsinMirageMapStatKeyPart, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void DrawOverlayWindow(
        string windowId,
        string text,
        float xPosPercent,
        float yPosPercent,
        float padding,
        int borderThickness,
        float borderRounding,
        float textScale,
        Color textColor,
        Color borderColor,
        Color backgroundColor)
    {
        var windowRect = GameController.Window.GetWindowRectangle();
        var anchor = new Vector2(
            windowRect.Width * (xPosPercent / 100f),
            windowRect.Height * (yPosPercent / 100f));

        var baseTextSize = ImGui.CalcTextSize(text);
        var estimatedWindowSize = new Vector2(
            baseTextSize.X * textScale + padding * 2,
            baseTextSize.Y * textScale + padding * 2);

        var position = new Vector2(anchor.X - estimatedWindowSize.X / 2f, anchor.Y);

        ImGui.SetNextWindowPos(position, ImGuiCond.Always);
        ImGui.SetNextWindowSize(estimatedWindowSize, ImGuiCond.Always);

        ImGui.PushStyleColor(ImGuiCol.WindowBg, BeastsHelpers.ToImGuiColor(backgroundColor));
        ImGui.PushStyleColor(ImGuiCol.Border, BeastsHelpers.ToImGuiColor(borderColor));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, borderRounding);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, borderThickness);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(padding, padding));

        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoInputs |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoMove;

        ImGui.Begin(windowId, flags);
        ImGui.SetWindowFontScale(textScale);
        ImGui.TextColored(BeastsHelpers.ToImGuiColor(textColor), text);
        ImGui.SetWindowFontScale(1f);
        ImGui.End();

        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor(2);
    }

    private bool TryGetTrackedBeastNameCached(string metadata, out string beastName)
    {
        beastName = null;
        if (string.IsNullOrWhiteSpace(metadata))
            return false;

        if (_trackedBeastNameCache.TryGetValue(metadata, out var cached))
        {
            if (cached == MissingTrackedBeastName)
                return false;

            beastName = cached;
            return true;
        }

        foreach (var tracked in AllRedBeasts)
        {
            if (tracked.MetadataPatterns.Any(pattern => string.Equals(metadata, pattern, StringComparison.OrdinalIgnoreCase)))
            {
                beastName = tracked.Name;
                _trackedBeastNameCache[metadata] = beastName;
                return true;
            }
        }

        _trackedBeastNameCache[metadata] = MissingTrackedBeastName;
        return false;
    }

    private static bool IsRareBeast(Entity entity)
    {
        return entity.Rarity == MonsterRarity.Rare &&
               IsCapturableMonsterStat is { } capturableStat &&
               entity.Stats?.ContainsKey(capturableStat) == true;
    }

    private static GameStat? TryGetCapturableMonsterStat()
    {
        return Enum.TryParse<GameStat>("IsCapturableMonster", out var stat) ? stat : null;
    }


    private void HandleBestiaryClipboardAutoCopy()
    {
        if (!Settings.BestiaryClipboard.EnableAutoCopy.Value)
        {
            _wasBestiaryTabVisible = false;
            return;
        }

        var isVisible = IsBestiaryTabVisible();
        if (isVisible && !_wasBestiaryTabVisible)
        {
            var regex = GetConfiguredBestiaryRegex();
            ImGui.SetClipboardText(regex);

            if (Settings.BestiaryClipboard.AutoPasteAfterCopy.Value &&
                !_isBestiaryClipboardPasteRunning &&
                !string.IsNullOrWhiteSpace(regex))
            {
                _isBestiaryClipboardPasteRunning = true;
                _ = PasteBestiaryRegexAsync();
            }
        }

        _wasBestiaryTabVisible = isVisible;
    }

    private async Task PasteBestiaryRegexAsync()
    {
        try
        {
            await Task.Delay(60);
            if (!IsBestiaryTabVisible())
                return;

            await TapKeyWithModifierAsync(Keys.ControlKey, Keys.F);
            await Task.Delay(80);
            if (!IsBestiaryTabVisible())
                return;

            await TapKeyWithModifierAsync(Keys.ControlKey, Keys.V);
            await Task.Delay(60);
            await TapKeyAsync(Keys.Enter);
        }
        catch (Exception ex)
        {
            LogDebug($"Bestiary clipboard auto-paste skipped. {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _isBestiaryClipboardPasteRunning = false;
        }
    }

    private static async Task TapKeyAsync(Keys key)
    {
        Input.KeyDown(key);
        await Task.Delay(30);
        Input.KeyUp(key);
    }

    private static async Task TapKeyWithModifierAsync(Keys modifier, Keys key)
    {
        Input.KeyDown(modifier);
        await Task.Delay(20);
        Input.KeyDown(key);
        await Task.Delay(30);
        Input.KeyUp(key);
        await Task.Delay(20);
        Input.KeyUp(modifier);
    }

    private string GetConfiguredBestiaryRegex()
    {
        return Settings.BestiaryClipboard.UseAutoRegex.Value
            ? BuildAutoRegexFromEnabledBeasts()
            : (Settings.BestiaryClipboard.BeastRegex.Value ?? string.Empty);
    }

    private string BuildAutoRegexFromEnabledBeasts()
    {
        var enabledBeasts = Settings.BeastPrices.EnabledBeasts;
        if (enabledBeasts.Count == 0) return string.Empty;
        return string.Join('|', AllRedBeasts
            .Where(b => enabledBeasts.Contains(b.Name) && !string.IsNullOrEmpty(b.RegexFragment))
            .Select(b => b.RegexFragment));
    }

}
