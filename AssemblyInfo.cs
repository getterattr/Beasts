using System.Diagnostics;

// The HUD compiles source plugins in Debug, which marks the assembly DisableOptimizations
// and makes the JIT run every method unoptimized. Roslyn adds that marker only when the
// assembly declares no Debuggable attribute of its own, so declaring one without the flag
// lets the JIT optimize this plugin: Tick and Render run several times faster.
// Breakpoints still bind; some locals show as optimized away. Delete the file to step through.
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
