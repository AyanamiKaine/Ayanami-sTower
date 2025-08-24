using System;
using System.Collections.Generic;
using MoonWorks.Input;

namespace AyanamisTower.StellaEcs.StellaInvicta
{
    internal sealed class InputManager
    {
        private readonly HashSet<KeyCode> _prevKeys = new();
        private readonly HashSet<KeyCode> _currKeys = new();

        private bool _prevLeftMouse = false;
        private bool _currLeftMouse = false;

        private enum TriggerType { Pressed, Held, LeftMousePressed, Custom }

        private sealed class Check
        {
            public TriggerType Type;
            public KeyCode Key;
            public Func<Inputs, bool>? Predicate;
            public Action Action = () => { };
            public string Name = string.Empty;
        }

        private readonly List<Check> _checks = new();

        public void Update(Inputs inputs)
        {
            // rotate key sets
            _prevKeys.Clear();
            foreach (var k in _currKeys) _prevKeys.Add(k);
            _currKeys.Clear();

            // snapshot current keyboard state for all KeyCode values
            foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
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

            // Evaluate registered checks
            foreach (var c in _checks)
            {
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
        public bool IsLeftMouseHeld() => _currLeftMouse;

        // Registration helpers
        public void RegisterKeyPressed(KeyCode key, Action action, string name = "") => _checks.Add(new Check { Type = TriggerType.Pressed, Key = key, Action = action, Name = name });
        public void RegisterKeyHeld(KeyCode key, Action action, string name = "") => _checks.Add(new Check { Type = TriggerType.Held, Key = key, Action = action, Name = name });
        public void RegisterLeftMousePressed(Action action, string name = "") => _checks.Add(new Check { Type = TriggerType.LeftMousePressed, Action = action, Name = name });
        public void RegisterCustom(Func<Inputs, bool> predicate, Action action, string name = "") => _checks.Add(new Check { Type = TriggerType.Custom, Predicate = predicate, Action = action, Name = name });
    }
}
