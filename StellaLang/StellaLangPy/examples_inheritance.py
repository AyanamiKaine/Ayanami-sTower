"""Example demonstrating VMActor inheritance and runtime behavior modification.

This shows:
1. How to inherit from VMActor to create specialized actors
2. Actors running custom loops with their own behavior
3. Other actors modifying running behavior by replacing instructions
4. Dynamic hot-swapping of functionality while actor is running
"""

from src.VMActor import VMActor
from src.ActorRuntime import SimpleRuntime
import time
import threading


class WorkerActor(VMActor):
    """A worker actor that processes tasks in a loop."""
    
    def __init__(self, name):
        super().__init__()
        self.name = name
        self.task_count = 0
        self.running_loop = False
        
        # Define custom instructions for this worker
        def process_task(vm):
            """Process a task from the queue."""
            if len(vm.stack) > 0:
                task = vm.stack.pop()
                vm.task_count += 1
                print(f"  [{vm.name}] Processing task: {task} (total: {vm.task_count})")
                time.sleep(0.1)  # Simulate work
        
        def status_report(vm):
            """Report current status."""
            print(f"  [{vm.name}] Status: {vm.task_count} tasks processed")
        
        self.define_new_instruction("OP_PROCESS_TASK", process_task)
        self.define_new_instruction("OP_STATUS", status_report)
    
    def work_loop(self, duration=2.0):
        """Run a work loop that processes tasks."""
        print(f"[{self.name}] Starting work loop for {duration}s")
        self.running_loop = True
        start_time = time.time()
        
        while self.running_loop and (time.time() - start_time) < duration:
            # Process any pending messages
            if self.handle_message():
                pass  # Message was processed
            else:
                # No messages, brief sleep
                time.sleep(0.05)
        
        print(f"[{self.name}] Work loop finished")


class MonitorActor(VMActor):
    """A monitor actor that watches and controls other actors."""
    
    def __init__(self, name):
        super().__init__()
        self.name = name
        self.watched_actors = {}
    
    def watch(self, actor_name, actor):
        """Add an actor to watch list."""
        self.watched_actors[actor_name] = actor
    
    def modify_worker_behavior(self, worker_name, new_behavior):
        """Hot-swap a worker's task processing behavior."""
        worker = self.watched_actors.get(worker_name)
        if worker:
            print(f"[{self.name}] Modifying {worker_name}'s behavior...")
            worker.replace_existing_instruction("OP_PROCESS_TASK", new_behavior)
            print(f"[{self.name}] Behavior modified!")


def example1_basic_inheritance():
    """Example 1: Basic inheritance and custom loop."""
    print("\n=== Example 1: Basic Inheritance ===")
    
    worker = WorkerActor("Worker-1")
    
    # Send some tasks
    worker.send("OP_CONSTANT", "Task-A", "OP_PROCESS_TASK")
    worker.send("OP_CONSTANT", "Task-B", "OP_PROCESS_TASK")
    worker.send("OP_CONSTANT", "Task-C", "OP_PROCESS_TASK")
    
    # Run the worker loop
    worker.work_loop(duration=1.0)
    
    print("✓ Inherited actor with custom loop behavior")


def example2_runtime_modification():
    """Example 2: Modify behavior while actor is running."""
    print("\n=== Example 2: Runtime Behavior Modification ===")
    
    worker = WorkerActor("Worker-2")
    monitor = MonitorActor("Monitor")
    monitor.watch("Worker-2", worker)
    
    # Send initial tasks
    worker.send("OP_CONSTANT", "Task-1", "OP_PROCESS_TASK")
    worker.send("OP_CONSTANT", "Task-2", "OP_PROCESS_TASK")
    
    # Start worker in background thread
    worker_thread = threading.Thread(target=worker.work_loop, args=(3.0,))
    worker_thread.start()
    
    # Let it process some tasks
    time.sleep(0.5)
    
    # Now modify the behavior while it's running!
    def new_process_task(vm):
        """Enhanced task processor with validation."""
        if len(vm.stack) > 0:
            task = vm.stack.pop()
            vm.task_count += 1
            print(f"  [{vm.name}] ⚡ ENHANCED: Processing {task} (total: {vm.task_count})")
            time.sleep(0.1)
    
    monitor.modify_worker_behavior("Worker-2", new_process_task)
    
    # Send more tasks - they'll use the new behavior
    worker.send("OP_CONSTANT", "Task-3", "OP_PROCESS_TASK")
    worker.send("OP_CONSTANT", "Task-4", "OP_PROCESS_TASK")
    
    worker_thread.join()
    
    print("✓ Successfully modified behavior while running")


def example3_supervisor_controlling_workers():
    """Example 3: Supervisor actor controlling multiple workers."""
    print("\n=== Example 3: Supervisor Controls Workers ===")
    
    # Create workers
    worker1 = WorkerActor("Worker-A")
    worker2 = WorkerActor("Worker-B")
    
    # Create supervisor
    supervisor = MonitorActor("Supervisor")
    supervisor.watch("Worker-A", worker1)
    supervisor.watch("Worker-B", worker2)
    
    # Start workers in background
    thread1 = threading.Thread(target=worker1.work_loop, args=(2.5,))
    thread2 = threading.Thread(target=worker2.work_loop, args=(2.5,))
    
    # Send tasks to workers
    worker1.send("OP_CONSTANT", "Job-1", "OP_PROCESS_TASK")
    worker2.send("OP_CONSTANT", "Job-2", "OP_PROCESS_TASK")
    
    thread1.start()
    thread2.start()
    
    # Let them process
    time.sleep(0.5)
    
    # Supervisor modifies Worker-A to be verbose
    def verbose_processor(vm):
        if len(vm.stack) > 0:
            task = vm.stack.pop()
            vm.task_count += 1
            print(f"  [{vm.name}] 🔊 VERBOSE: Starting {task}")
            time.sleep(0.1)
            print(f"  [{vm.name}] 🔊 VERBOSE: Completed {task} (total: {vm.task_count})")
    
    supervisor.modify_worker_behavior("Worker-A", verbose_processor)
    
    # Supervisor modifies Worker-B to be quiet
    def quiet_processor(vm):
        if len(vm.stack) > 0:
            task = vm.stack.pop()
            vm.task_count += 1
            # Silent processing
            time.sleep(0.1)
    
    supervisor.modify_worker_behavior("Worker-B", quiet_processor)
    
    # Send more tasks
    worker1.send("OP_CONSTANT", "Job-3", "OP_PROCESS_TASK")
    worker2.send("OP_CONSTANT", "Job-4", "OP_PROCESS_TASK")
    
    thread1.join()
    thread2.join()
    
    print(f"Worker-A processed: {worker1.task_count} tasks")
    print(f"Worker-B processed: {worker2.task_count} tasks")
    print("✓ Supervisor successfully controlled worker behaviors")


def example4_progressive_enhancement():
    """Example 4: Progressively enhance actor capabilities."""
    print("\n=== Example 4: Progressive Enhancement ===")
    
    worker = WorkerActor("Learner")
    
    # Start with basic behavior
    worker.send("OP_CONSTANT", 10, "OP_PROCESS_TASK")
    
    thread = threading.Thread(target=worker.work_loop, args=(3.0,))
    thread.start()
    
    time.sleep(0.3)
    
    # Upgrade: Add caching
    print("[System] Upgrading to version 2.0 - adding cache...")
    cache = {}
    
    def cached_processor(vm):
        if len(vm.stack) > 0:
            task = vm.stack.pop()
            if task in cache:
                print(f"  [{vm.name}] 💾 CACHED: {task} -> {cache[task]}")
            else:
                vm.task_count += 1
                result = f"Result-{task}"
                cache[task] = result
                print(f"  [{vm.name}] 🆕 COMPUTED: {task} -> {result}")
            time.sleep(0.1)
    
    worker.replace_existing_instruction("OP_PROCESS_TASK", cached_processor)
    
    # Send same task again - should be cached
    worker.send("OP_CONSTANT", 10, "OP_PROCESS_TASK")
    worker.send("OP_CONSTANT", 20, "OP_PROCESS_TASK")
    worker.send("OP_CONSTANT", 10, "OP_PROCESS_TASK")  # Cached!
    
    time.sleep(0.8)
    
    # Upgrade: Add metrics
    print("[System] Upgrading to version 3.0 - adding metrics...")
    metrics = {'cache_hits': 0, 'cache_misses': 0}
    
    def metrics_processor(vm):
        if len(vm.stack) > 0:
            task = vm.stack.pop()
            if task in cache:
                metrics['cache_hits'] += 1
                print(f"  [{vm.name}] 📊 HIT: {task} (hits: {metrics['cache_hits']})")
            else:
                metrics['cache_misses'] += 1
                vm.task_count += 1
                result = f"Result-{task}"
                cache[task] = result
                print(f"  [{vm.name}] 📊 MISS: {task} (misses: {metrics['cache_misses']})")
            time.sleep(0.1)
    
    worker.replace_existing_instruction("OP_PROCESS_TASK", metrics_processor)
    
    worker.send("OP_CONSTANT", 10, "OP_PROCESS_TASK")  # Hit
    worker.send("OP_CONSTANT", 30, "OP_PROCESS_TASK")  # Miss
    worker.send("OP_CONSTANT", 20, "OP_PROCESS_TASK")  # Hit
    
    thread.join()
    
    print(f"Final metrics: {metrics}")
    print("✓ Actor capabilities progressively enhanced")


def example5_behavior_switching():
    """Example 5: Switch between different operational modes."""
    print("\n=== Example 5: Behavior Mode Switching ===")
    
    worker = WorkerActor("ModeWorker")
    
    # Define different modes
    def normal_mode(vm):
        if len(vm.stack) > 0:
            task = vm.stack.pop()
            vm.task_count += 1
            print(f"  [{vm.name}] NORMAL: {task}")
            time.sleep(0.1)
    
    def turbo_mode(vm):
        if len(vm.stack) > 0:
            task = vm.stack.pop()
            vm.task_count += 1
            print(f"  [{vm.name}] 🚀 TURBO: {task}")
            time.sleep(0.05)  # Faster!
    
    def debug_mode(vm):
        if len(vm.stack) > 0:
            task = vm.stack.pop()
            vm.task_count += 1
            print(f"  [{vm.name}] 🐛 DEBUG: {task}")
            print(f"  [{vm.name}]   Stack: {list(vm.stack)}")
            print(f"  [{vm.name}]   Vars: {vm.variables}")
            time.sleep(0.15)
    
    # Start in normal mode
    worker.replace_existing_instruction("OP_PROCESS_TASK", normal_mode)
    
    thread = threading.Thread(target=worker.work_loop, args=(4.0,))
    thread.start()
    
    worker.send("OP_CONSTANT", "Task-1", "OP_PROCESS_TASK")
    time.sleep(0.5)
    
    # Switch to turbo mode
    print("\n[System] Switching to TURBO mode...")
    worker.replace_existing_instruction("OP_PROCESS_TASK", turbo_mode)
    worker.send("OP_CONSTANT", "Task-2", "OP_PROCESS_TASK")
    worker.send("OP_CONSTANT", "Task-3", "OP_PROCESS_TASK")
    time.sleep(0.5)
    
    # Switch to debug mode
    print("\n[System] Switching to DEBUG mode...")
    worker.replace_existing_instruction("OP_PROCESS_TASK", debug_mode)
    worker.variables['mode'] = 'debug'
    worker.send("OP_CONSTANT", "Task-4", "OP_PROCESS_TASK")
    time.sleep(0.5)
    
    # Back to normal
    print("\n[System] Switching back to NORMAL mode...")
    worker.replace_existing_instruction("OP_PROCESS_TASK", normal_mode)
    worker.send("OP_CONSTANT", "Task-5", "OP_PROCESS_TASK")
    
    thread.join()
    
    print("✓ Successfully switched between operational modes")


def example6_defun_with_inheritance():
    """Example 6: Combine defun with inheritance for Lisp-like DSL."""
    print("\n=== Example 6: Defun with Inheritance ===")
    
    class LispActor(VMActor):
        """Actor with Lisp-like evaluation loop."""
        
        def __init__(self, name):
            super().__init__()
            self.name = name
            self.expression_count = 0
        
        def eval_loop(self, expressions, duration=2.0):
            """Evaluate s-expressions in a loop."""
            print(f"[{self.name}] Starting eval loop...")
            
            for expr in expressions:
                print(f"[{self.name}] Evaluating: {expr}")
                bytecode = self.s_expression_to_bytecode(expr)
                self.send(*bytecode)
                
                # Process the expression
                while self.handle_message():
                    pass
                
                self.expression_count += 1
                time.sleep(0.2)
            
            print(f"[{self.name}] Evaluated {self.expression_count} expressions")
    
    # Create Lisp actor
    lisp = LispActor("LispEval")
    
    # Define custom functions
    def show(vm):
        value = vm.stack.pop()
        print(f"  => {value}")
    
    def square(vm):
        value = vm.stack.pop()
        vm.stack.push(value * value)
    
    lisp.defun("show", show)
    lisp.defun("square", square)
    
    # Evaluate expressions in background
    expressions = [
        '(define x 5)',
        '(show x)',
        '(define y (square x))',
        '(show y)',
        '(show (+ x y))',
    ]
    
    thread = threading.Thread(target=lisp.eval_loop, args=(expressions,))
    thread.start()
    
    # While it's running, add a new function!
    time.sleep(0.5)
    print("\n[System] Hot-adding 'double' function...")
    
    def double(vm):
        value = vm.stack.pop()
        vm.stack.push(value * 2)
    
    lisp.defun("double", double)
    
    thread.join()
    
    # Now use the new function
    print("\n[System] Using newly added 'double' function:")
    lisp.eval_loop(['(show (double 7))'])
    
    print("✓ Combined defun with inherited eval loop")


def example7_actor_hot_reload():
    """Example 7: Hot-reload entire instruction set."""
    print("\n=== Example 7: Hot Reload Instruction Set ===")
    
    worker = WorkerActor("HotReload")
    
    thread = threading.Thread(target=worker.work_loop, args=(3.0,))
    thread.start()
    
    # Original instruction set
    worker.send("OP_CONSTANT", "Original-1", "OP_PROCESS_TASK")
    time.sleep(0.3)
    
    # Hot reload: Replace multiple instructions at once
    print("\n[System] 🔥 HOT RELOADING instruction set...")
    
    def new_task_processor(vm):
        if len(vm.stack) > 0:
            task = vm.stack.pop()
            vm.task_count += 1
            print(f"  [{vm.name}] ✨ NEW VERSION: {task} (v2.0)")
            time.sleep(0.1)
    
    def new_status_reporter(vm):
        print(f"  [{vm.name}] 📈 NEW STATUS: {vm.task_count} tasks, stack: {len(vm.stack)}")
    
    # Replace both at once
    worker.replace_existing_instruction("OP_PROCESS_TASK", new_task_processor)
    worker.replace_existing_instruction("OP_STATUS", new_status_reporter)
    
    # Test new instructions
    worker.send("OP_CONSTANT", "New-1", "OP_PROCESS_TASK")
    worker.send("OP_STATUS")
    worker.send("OP_CONSTANT", "New-2", "OP_PROCESS_TASK")
    
    thread.join()
    
    print("✓ Successfully hot-reloaded instruction set")


if __name__ == '__main__':
    print("=" * 70)
    print("VMACTOR INHERITANCE & RUNTIME BEHAVIOR MODIFICATION")
    print("=" * 70)
    print("\nKey Concepts:")
    print("  • Inherit from VMActor to create specialized actors")
    print("  • Actors can run custom loops with their own behavior")
    print("  • Other actors can modify running behavior dynamically")
    print("  • Hot-swap functionality while actor is running")
    print("  • No need to stop/restart actor to change behavior")
    print()
    
    example1_basic_inheritance()
    example2_runtime_modification()
    example3_supervisor_controlling_workers()
    example4_progressive_enhancement()
    example5_behavior_switching()
    example6_defun_with_inheritance()
    example7_actor_hot_reload()
    
    print("\n" + "=" * 70)
    print("Key Takeaways:")
    print("  ✓ Inheritance enables specialized actor types")
    print("  ✓ Actors can run custom loops while processing messages")
    print("  ✓ Behavior can be modified while actor is running")
    print("  ✓ Supervisors can control worker behaviors dynamically")
    print("  ✓ Progressive enhancement: add features without restart")
    print("  ✓ Mode switching: change operational modes on-the-fly")
    print("  ✓ Hot-reload: upgrade running systems without downtime")
    print("  ✓ Combines with defun for powerful Lisp-like DSLs")
    print("=" * 70)
