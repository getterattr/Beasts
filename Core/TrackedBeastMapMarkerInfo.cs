using System;
using Vector2 = System.Numerics.Vector2;

namespace Beasts;

internal readonly record struct TrackedBeastMapMarkerInfo(
	long EntityId,
	Vector2 GridPos,
	string BeastName,
	BeastCaptureState CaptureState,
	bool IsLive = true,
	DateTime LastUpdatedUtc = default);