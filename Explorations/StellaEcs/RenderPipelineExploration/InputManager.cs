using System;
using System.Collections.Generic;
using MoonWorks.Input;

namespace AyanamisTower.StellaEcs.StellaInvicta
{
    internal sealed class InputManager
    {
        private readonly HashSet<KeyCode> _prevKeys = new();
        private readonly HashSet<KeyCode> _currKeys = new();
        // Only poll these keys (populated from registrations) to avoid iterating full enum
        private readonly HashSet<KeyCode> _registeredKeys = new();

    private bool _prevLeftMouse = false;
    private bool _currLeftMouse = false;
    private bool _prevRightMouse = false;
    private bool _currRightMouse = false;

    private enum TriggerType { Pressed, Held, LeftMousePressed, RightMousePressed, Custom }

        private sealed class Check
        {
            public TriggerType Type;
            public KeyCode Key;
            public Func<Inputs, bool>? Predicate;
            public Action Action = () => { };
            public string Name = string.Empty;
            public string Context = "global"; // context/layer name
            public bool Enabled = true;
        }

        private readonly List<Check> _checks = new();

        public void Update(Inputs inputs)
        {
            // rotate key sets
            _prevKeys.Clear();
            foreach (var k in _currKeys) _prevKeys.Add(k);
            _currKeys.Clear();

            // snapshot only registered keyboard keys for performance
            foreach (var kc in _registeredKeys)
            {
                try
                {
                    if (inputs.Keyboard.IsPressed(kc)) _currKeys.Add(kc);
                }
                catch
                {
                    // be resilient to unknown/unsupported keycodes
                }
            }

            // left mouse
            _prevLeftMouse = _currLeftMouse;
            _currLeftMouse = inputs.Mouse.LeftButton.IsPressed;
            _prevRightMouse = _currRightMouse;
            _currRightMouse = inputs.Mouse.RightButton.IsPressed;

            // Evaluate registered checks that are enabled and whose context is active
            foreach (var c in _checks)
            {
                if (!c.Enabled) continue;
                if (!_activeContexts.Contains(c.Context)) continue; // skip checks not in active contexts

                switch (c.Type)
                {
                    case TriggerType.Pressed:
                        if (_currKeys.Contains(c.Key) && !_prevKeys.Contains(c.Key)) c.Action();
                        break;
                    case TriggerType.Held:
                        if (_currKeys.Contains(c.Key)) c.Action();
                        break;
                    case TriggerType.LeftMousePressed:
                        if (_currLeftMouse && !_prevLeftMouse) c.Action();
                        break;
                    case TriggerType.RightMousePressed:
                        if (_currRightMouse && !_prevRightMouse) c.Action();
                        break;
                    case TriggerType.Custom:
                        try
                        {
                            if (c.Predicate != null && c.Predicate(inputs)) c.Action();
                        }
                        catch
                        {
                            // ignore predicate errors to keep input loop robust
                        }
                        break;
                }
            }
        }

        public bool WasKeyPressed(KeyCode key) => _currKeys.Contains(key) && !_prevKeys.Contains(key);
        public bool IsKeyHeld(KeyCode key) => _currKeys.Contains(key);
    public bool WasLeftMousePressed() => _currLeftMouse && !_prevLeftMouse;
    public bool WasRightMousePressed() => _currRightMouse && !_prevRightMouse;
        public bool IsLeftMouseHeld() => _currLeftMouse;
        public bool WasLeftMouseReleased() => !_currLeftMouse && _prevLeftMouse;

        // Registration helpers
        public void RegisterKeyPressed(KeyCode key, Action action, string name = "", string context = "global")
        {
            _registeredKeys.Add(key);
            _checks.Add(new Check { Type = TriggerType.Pressed, Key = key, Action = action, Name = name, Context = context });
        }

        public void RegisterKeyHeld(KeyCode key, Action action, string name = "", string context = "global")
        {
            _registeredKeys.Add(key);
            _checks.Add(new Check { Type = TriggerType.Held, Key = key, Action = action, Name = name, Context = context });
        }

        public void RegisterLeftMousePressed(Action action, string name = "", string context = "global")
        {
            _checks.Add(new Check { Type = TriggerType.LeftMousePressed, Action = action, Name = name, Context = context });
        }

        public void RegisterRightMousePressed(Action action, string name = "", string context = "global")
        {
            _checks.Add(new Check { Type = TriggerType.RightMousePressed, Action = action, Name = name, Context = context });
        }

        public void RegisterCustom(Func<Inputs, bool> predicate, Action action, string name = "", string context = "global")
        {
            _checks.Add(new Check { Type = TriggerType.Custom, Predicate = predicate, Action = action, Name = name, Context = context });
        }

        // Unregister / enable / disable helpers
        public void UnregisterByName(string name)
        {
            _checks.RemoveAll(c => c.Name == name);
        }

        public void UnregisterAllInContext(string context)
        {
            _checks.RemoveAll(c => c.Context == context);
        }

        public void SetEnabled(string name, bool enabled)
        {
            foreach (var c in _checks)
            {
                if (c.Name == name) c.Enabled = enabled;
            }
        }

        // Context / layer support: active contexts determine which checks run
        private readonly HashSet<string> _activeContexts = new() { "global" };
        private readonly Stack<string> _contextStack = new();

        public void ActivateContext(string context) => _activeContexts.Add(context);
        public void DeactivateContext(string context) { if (context != "global") _activeContexts.Remove(context); }

        public void PushContext(string context)
        {
            _contextStack.Push(context);
            ActivateContext(context);
        }

        public void PopContext()
        {
            if (_contextStack.Count == 0) return;
            var ctx = _contextStack.Pop();
            DeactivateContext(ctx);
        }
    }
}
