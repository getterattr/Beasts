using System;
using Beasts.Runtime.Lifecycle;
using Beasts.Runtime.State;

namespace Beasts.Runtime;

internal sealed class BeastsRuntime
{
    private readonly Main _plugin;

    public BeastsRuntime(Main plugin)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        State = new BeastsRuntimeState();
        AreaTransitions = new AreaTransitionCoordinator(State);
    }

    public BeastsRuntimeState State { get; }

    public AreaTransitionCoordinator AreaTransitions { get; }

    public void Initialize(DateTime now, MainSettingsBindingTargets bindingTargets)
    {
        MainSettingsBindings.Bind(_plugin.Settings, bindingTargets);
    }

    public void Shutdown()
    {
    }
}