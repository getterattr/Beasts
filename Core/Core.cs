using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using Graphics = ExileCore.Graphics;

namespace Beasts;

internal static class Core
{
    public static Main Plugin { get; private set; }

    public static GameController GameController => Plugin?.GameController;

    public static Settings Settings => Plugin?.Settings;

    public static Graphics Graphics => Plugin?.Graphics;

    public static AreaInstance CurrentArea { get; private set; }

    public static bool IsReady => Plugin?.GameController != null;

    public static void Initialize(Main plugin)
    {
        Plugin = plugin;
        CurrentArea = plugin.GameController?.Area?.CurrentArea;
    }

    public static void AreaChanged(AreaInstance area)
    {
        CurrentArea = area;
    }

    public static void Shutdown()
    {
        CurrentArea = null;
        Plugin = null;
    }
}
