using System;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Base marker class for all system groups.
/// A system group defines a phase of execution in the main loop.
/// </summary>
public abstract class SystemGroup { }

/// <summary>
/// A system group for systems that should run at the very beginning of the frame.
/// Use this for setup, entity spawning, or initialization logic.
/// </summary>
public class InitializationSystemGroup : SystemGroup { }

/// <summary>
/// The main system group for core game logic and simulation.
/// Most systems (Input, AI, Physics, Gameplay) will belong to this group.
/// </summary>
public class SimulationSystemGroup : SystemGroup { }

/// <summary>
/// A system group for systems that run after the main simulation.
/// Use this for logic that prepares data for rendering, such as camera updates,
/// UI updates, and animation post-processing.
/// </summary>
public class PresentationSystemGroup : SystemGroup { }
