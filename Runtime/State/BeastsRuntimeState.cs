namespace Beasts.Runtime.State;

internal sealed class BeastsRuntimeState
{
    public MapTrackingState Map { get; } = new();
}

internal sealed class MapTrackingState
{
    public bool IsCurrentAreaTrackable { get; set; }
    public string ActiveMapAreaHash { get; set; } = string.Empty;
    public string ActiveMapAreaName { get; set; } = string.Empty;
    public int ActiveMapInstanceId { get; set; } = -1;
}
