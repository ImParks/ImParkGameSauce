using ImPark.Shared.Events;

namespace ImPark.Core.Modules;

[Event("mod.loaded")]
public readonly record struct ModuleLoadedEvent(string ModuleId);

[Event("mod.enabled")]
public readonly record struct ModuleEnabledEvent(string ModuleId);

[Event("mod.disabled")]
public readonly record struct ModuleDisabledEvent(string ModuleId);
