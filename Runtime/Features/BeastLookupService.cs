using System;
using ExileCore.PoEMemory.MemoryObjects;

namespace Beasts.Runtime.Features;

internal sealed record BeastLookupCallbacks(
    Func<string, string> TryGetPriceTextOrNull,
    Func<string> GetCaptureMonsterCapturedBuffName,
    Func<string> GetCaptureMonsterTrappedBuffName);

internal sealed class BeastLookupService
{
    private readonly BeastLookupCallbacks _callbacks;

    public BeastLookupService(BeastLookupCallbacks callbacks)
    {
        _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
    }

    public BeastCaptureState GetBeastCaptureState(Entity entity)
    {
        if (entity?.Buffs?.Find(buff => buff.Name == _callbacks.GetCaptureMonsterCapturedBuffName()) != null)
        {
            return BeastCaptureState.Captured;
        }

        return entity?.Buffs?.Find(buff => buff.Name == _callbacks.GetCaptureMonsterTrappedBuffName()) != null
            ? BeastCaptureState.Capturing
            : BeastCaptureState.None;
    }

    public bool TryGetBeastPriceText(string beastName, out string priceText)
    {
        priceText = _callbacks.TryGetPriceTextOrNull(beastName);
        return priceText != null;
    }
}