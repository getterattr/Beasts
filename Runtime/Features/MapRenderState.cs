using Vector2 = System.Numerics.Vector2;

namespace Beasts.Runtime.Features;

internal sealed class MapRenderState
{
    public Vector2[] WorldCircleScreenPoints { get; } = new Vector2[WorldCirclePointsLength];

    public const int WorldCirclePointsLength = 15;
}
