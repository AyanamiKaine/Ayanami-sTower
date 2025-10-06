"""
ActorRuntime - Higher-level abstraction that owns the loop behavior.

Like Erlang/Elixir's OTP, the runtime owns the scheduling loop and calls
the actor's handle_message() callback. The actor doesn't manage its own loop.
"""

import threading
import time
from typing import List, Callable, Optional
from src.VMActor import VMActor


class ActorRuntime:
    """Scheduler/runtime that manages actor message processing loops.
    
    This is like Erlang's VM or Elixir's OTP - it owns the loop and calls
    the actor's handle_message() callback. You can customize loop behavior:
    - Round-robin scheduling
    - Priority scheduling
    - Batch processing
    - Rate limiting
    - etc.
    """
    
    def __init__(self):
        self.actors = []
        self.running = False
        self._thread = None
    
    def register(self, actor: VMActor):
        """Register an actor with this runtime."""
        self.actors.append(actor)
        actor.running = True
    
    def unregister(self, actor: VMActor):
        """Unregister an actor from this runtime."""
        if actor in self.actors:
            self.actors.remove(actor)
            actor.running = False
    
    def simple_loop(self):
        """Simple loop: process one message per actor, round-robin."""
        while self.running:
            any_processed = False
            
            for actor in self.actors:
                if actor.handle_message():
                    any_processed = True
            
            if not any_processed:
                # No messages processed, sleep briefly
                time.sleep(0.01)
    
    def greedy_loop(self):
        """Greedy loop: process all messages from first actor before moving to next."""
        while self.running:
            any_processed = False
            
            for actor in self.actors:
                # Process all available messages from this actor
                while actor.handle_message():
                    any_processed = True
            
            if not any_processed:
                time.sleep(0.01)
    
    def fair_loop(self, messages_per_actor=10):
        """Fair loop: process up to N messages per actor per round."""
        while self.running:
            any_processed = False
            
            for actor in self.actors:
                for _ in range(messages_per_actor):
                    if actor.handle_message():
                        any_processed = True
                    else:
                        break
            
            if not any_processed:
                time.sleep(0.01)
    
    def custom_loop(self, loop_fn: Callable):
        """Use a custom loop function.
        
        Args:
            loop_fn: Function that takes (runtime, actors) and implements the loop
        """
        while self.running:
            loop_fn(self, self.actors)
    
    def start(self, loop_type='simple', blocking=False, **kwargs):
        """Start the runtime with specified loop behavior.
        
        Args:
            loop_type: 'simple', 'greedy', 'fair', or a custom function
            blocking: If True, run in current thread
            **kwargs: Additional arguments for loop (e.g., messages_per_actor for fair)
        """
        if self.running:
            raise RuntimeError("Runtime already running")
        
        self.running = True
        
        # Select loop function
        if loop_type == 'simple':
            loop_fn = self.simple_loop
        elif loop_type == 'greedy':
            loop_fn = self.greedy_loop
        elif loop_type == 'fair':
            messages_per_actor = kwargs.get('messages_per_actor', 10)
            loop_fn = lambda: self.fair_loop(messages_per_actor)
        elif callable(loop_type):
            loop_fn = lambda: self.custom_loop(loop_type)
        else:
            raise ValueError(f"Unknown loop_type: {loop_type}")
        
        if blocking:
            loop_fn()
        else:
            self._thread = threading.Thread(target=loop_fn, daemon=True)
            self._thread.start()
    
    def stop(self):
        """Stop the runtime."""
        self.running = False
        if self._thread and self._thread.is_alive():
            self._thread.join(timeout=1.0)


class SimpleRuntime:
    """Minimal runtime for a single actor - even simpler interface."""
    
    def __init__(self, actor: VMActor):
        self.actor = actor
        self.running = False
        self._thread = None
        actor.running = True
    
    def loop_until_empty(self):
        """Process all messages until queue is empty."""
        while self.actor.handle_message():
            pass
    
    def loop_forever(self):
        """Process messages forever until stopped."""
        while self.running:
            if not self.actor.handle_message():
                time.sleep(0.01)
    
    def loop_n_messages(self, n: int):
        """Process exactly N messages."""
        for _ in range(n):
            if not self.actor.handle_message():
                break
    
    def start(self, blocking=False):
        """Start processing messages forever."""
        if self.running:
            raise RuntimeError("Runtime already running")
        
        self.running = True
        
        if blocking:
            self.loop_forever()
        else:
            self._thread = threading.Thread(target=self.loop_forever, daemon=True)
            self._thread.start()
    
    def stop(self):
        """Stop the runtime."""
        self.running = False
        if self._thread and self._thread.is_alive():
            self._thread.join(timeout=1.0)


# Example custom loop strategies

def priority_loop(runtime: ActorRuntime, actors: List[VMActor]):
    """Custom loop that prioritizes actors with more messages."""
    # Sort actors by number of pending messages (most first)
    sorted_actors = sorted(
        actors,
        key=lambda a: len(a.bytecode) - a.ip,
        reverse=True
    )
    
    any_processed = False
    for actor in sorted_actors:
        if actor.handle_message():
            any_processed = True
    
    if not any_processed:
        time.sleep(0.01)


def batched_loop(runtime: ActorRuntime, actors: List[VMActor], batch_size=5):
    """Process messages in batches."""
    for actor in actors:
        batch_count = 0
        while batch_count < batch_size and actor.handle_message():
            batch_count += 1
    
    if all(len(a.bytecode) <= a.ip for a in actors):
        time.sleep(0.01)


def rate_limited_loop(runtime: ActorRuntime, actors: List[VMActor], 
                      messages_per_second=100):
    """Rate-limit message processing."""
    sleep_time = 1.0 / messages_per_second
    
    for actor in actors:
        if actor.handle_message():
            time.sleep(sleep_time)
