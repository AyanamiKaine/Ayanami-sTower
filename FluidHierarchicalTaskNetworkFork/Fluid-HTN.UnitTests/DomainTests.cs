﻿using System;
using FluidHTN;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.Contexts;
using FluidHTN.Effects;
using FluidHTN.PrimitiveTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class DomainTests
    {
        [TestMethod]
        public void DomainHasRootWithDomainName_ExpectedBehavior()
        {
            var domain = new Domain<MyContext>("Test");
            Assert.IsTrue(domain.Root != null);
            Assert.IsTrue(domain.Root.Name == "Test");
        }

        [TestMethod]
        public void AddSubtaskToParent_ExpectedBehavior()
        {
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Test" };
            domain.Add(task1, task2);
            Assert.IsTrue(task1.Subtasks.Contains(task2));
            Assert.IsTrue(task2.Parent == task1);
        }

        /*
        Yes this will throw but we cannot give it an null context?
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException), AllowDerivedTypes = false)]
        public void FindPlanNoCtxThrowsNRE_ExpectedBehavior()
        {
            var domain = new Domain<MyContext>("Test");
            var status = domain.FindPlan(null, out var plan);
        }
        */
        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void FindPlanUninitializedContextThrowsException_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var domain = new Domain<MyContext>("Test");
            var status = domain.FindPlan(ctx, out var plan);
            Assert.IsTrue(status == DecompositionStatus.Failed);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        [TestMethod]
        public void FindPlanNoTasksThenNullPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var status = domain.FindPlan(ctx, out var plan);
            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void MTRNullThrowsException_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.MethodTraversalRecord = null;

            var domain = new Domain<MyContext>("Test");
            var status = domain.FindPlan(ctx, out var plan);
        }

        [TestMethod]
        public void AfterFindPlanContextStateIsExecuting_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var status = domain.FindPlan(ctx, out var plan);
            Assert.IsTrue(ctx.ContextState == ContextState.Executing);
        }

        [TestMethod]
        public void FindPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);
            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.IsTrue(plan.Peek().Name == "Sub-task");
        }

        [TestMethod]
        public void FindPlanTrimsNonPermanentStateChange_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Sequence() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task1" }.AddEffect(
                new ActionEffect<MyContext>(
                    "TestEffect1",
                    EffectType.PlanOnly,
                    (context, type) => context.SetState(MyWorldState.HasA, true, type)
                )
            );
            var task3 = new PrimitiveTask() { Name = "Sub-task2" }.AddEffect(
                new ActionEffect<MyContext>(
                    "TestEffect2",
                    EffectType.PlanAndExecute,
                    (context, type) => context.SetState(MyWorldState.HasB, true, type)
                )
            );
            var task4 = new PrimitiveTask() { Name = "Sub-task3" }.AddEffect(
                new ActionEffect<MyContext>(
                    "TestEffect3",
                    EffectType.Permanent,
                    (context, type) => context.SetState(MyWorldState.HasC, true, type)
                )
            );
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);
            domain.Add(task1, task3);
            domain.Add(task1, task4);
            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int)MyWorldState.HasA].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int)MyWorldState.HasB].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int)MyWorldState.HasC].Count == 0);
            Assert.IsTrue(ctx.WorldState[(int)MyWorldState.HasA] == 0);
            Assert.IsTrue(ctx.WorldState[(int)MyWorldState.HasB] == 0);
            Assert.IsTrue(ctx.WorldState[(int)MyWorldState.HasC] == 1);
            Assert.IsTrue(plan.Count == 3);
        }

        [TestMethod]
        public void FindPlanClearsStateChangeWhenPlanIsNull_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Sequence() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task1" }.AddEffect(
                new ActionEffect<MyContext>(
                    "TestEffect1",
                    EffectType.PlanOnly,
                    (context, type) => context.SetState(MyWorldState.HasA, true, type)
                )
            );
            var task3 = new PrimitiveTask() { Name = "Sub-task2" }.AddEffect(
                new ActionEffect<MyContext>(
                    "TestEffect2",
                    EffectType.PlanAndExecute,
                    (context, type) => context.SetState(MyWorldState.HasB, true, type)
                )
            );
            var task4 = new PrimitiveTask() { Name = "Sub-task3" }.AddEffect(
                new ActionEffect<MyContext>(
                    "TestEffect3",
                    EffectType.Permanent,
                    (context, type) => context.SetState(MyWorldState.HasC, true, type)
                )
            );
            var task5 = new PrimitiveTask() { Name = "Sub-task4" }.AddCondition(
                new FuncCondition<MyContext>("TestCondition", context => context.Done == true)
            );
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);
            domain.Add(task1, task3);
            domain.Add(task1, task4);
            domain.Add(task1, task5);
            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int)MyWorldState.HasA].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int)MyWorldState.HasB].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int)MyWorldState.HasC].Count == 0);
            Assert.IsTrue(ctx.WorldState[(int)MyWorldState.HasA] == 0);
            Assert.IsTrue(ctx.WorldState[(int)MyWorldState.HasB] == 0);
            Assert.IsTrue(ctx.WorldState[(int)MyWorldState.HasC] == 0);
            Assert.IsTrue(plan == null);
        }

        [TestMethod]
        public void FindPlanIfMTRsAreEqualThenReturnNullPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.LastMTR.Add(1);
            ctx.LastMTR.Add(0);

            // Root is a Selector that branch off into task1 selector or task2 sequence.
            // MTR tracks decomposition of compound tasks and priary tasks that are subtasks of selectors,
            // so our MTR is 2 layer deep.
            var domain = new Domain<MyContext>("Test");
            var task1 = new Sequence() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(
                new FuncCondition<MyContext>("TestCondition", context => context.Done == true)
            );
            var task4 = new PrimitiveTask() { Name = "Sub-task1" };
            var task5 = new PrimitiveTask() { Name = "Sub-task2" }.AddCondition(
                new FuncCondition<MyContext>("TestCondition", context => context.Done == true)
            );

            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);
            domain.Add(task2, task5);
            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.AreEqual(ctx.MethodTraversalRecord[0], ctx.LastMTR[0]);
            Assert.AreEqual(ctx.MethodTraversalRecord[1], ctx.LastMTR[1]);
        }

        [TestMethod]
        public void FindPlanIfPlansAreDifferentButMTRsAreEqualThenReturnNullPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.LastMTR.Add(1);
            ctx.LastMTR.Add(0);

            // Root is a Selector that branch off into task1 selector or task2 sequence.
            // MTR tracks decomposition of compound tasks and priary tasks that are subtasks of selectors,
            // so our MTR is 2 layer deep.
            var domain = new Domain<MyContext>("Test");
            var task1 = new Sequence() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(
                new FuncCondition<MyContext>("TestCondition", context => context.Done == true)
            );
            var task4 = new PrimitiveTask() { Name = "Sub-task1" };
            var task5 = new PrimitiveTask() { Name = "Sub-task2" }.AddCondition(
                new FuncCondition<MyContext>("TestCondition", context => context.Done == true)
            );

            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);
            domain.Add(task2, task5);
            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == ctx.LastMTR[0]);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == ctx.LastMTR[1]);
        }

        [TestMethod]
        public void FindPlanIfSelectorFindBetterPrimaryTaskMTRChangeSuccessfully_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.LastMTR.Add(0);
            ctx.LastMTR.Add(1);

            // Root is a Selector that branch off into two primary tasks.
            // We intend for task3 (Test Action B) to be selected in the first run,
            // but it will be a rejected plan because of LastMTR equality.
            // We then change the Done state to true before we do a replan,
            // and now we intend task 2 (Test Action A) to be selected, since its MTR beast LastMTR score.
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test Select" };
            var task2 = new PrimitiveTask() { Name = "Test Action A" }.AddCondition(
                new FuncCondition<MyContext>("Can choose A", context => context.Done == true)
            );
            var task3 = new PrimitiveTask() { Name = "Test Action B" }.AddCondition(
                new FuncCondition<MyContext>("Can not choose A", context => context.Done == false)
            );
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);
            domain.Add(task1, task3);

            // We expect this to first get rejected, because LastMTR holds [0, 1] which is what we'll get back from the planner.
            var status = domain.FindPlan(ctx, out var plan);
            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == ctx.LastMTR[0]);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == ctx.LastMTR[1]);

            // When we change the condition to Done = true, we should now be able to find a better plan!
            ctx.Done = true;
            status = domain.FindPlan(ctx, out plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == ctx.LastMTR[0]);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] < ctx.LastMTR[1]);
        }

        [TestMethod]
        public void PausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var task = new Sequence() { Name = "Test" };
            domain.Add(domain.Root, task);
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task1" });
            domain.Add(task, new PausePlanTask());
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task2" });

            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Partial);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task1", plan.Peek().Name);
            Assert.IsTrue(ctx.HasPausedPartialPlan);
            Assert.IsTrue(ctx.PartialPlanQueue.Count == 1);
            Assert.AreEqual(task, ctx.PartialPlanQueue.Peek().Task);
            Assert.AreEqual(2, ctx.PartialPlanQueue.Peek().TaskIndex);
        }

        [TestMethod]
        public void ContinuePausedPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var domain = new Domain<MyContext>("Test");
            var task = new Sequence() { Name = "Test" };
            domain.Add(domain.Root, task);
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task1" });
            domain.Add(task, new PausePlanTask());
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task2" });

            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Partial);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task1", plan.Dequeue().Name);
            Assert.IsTrue(ctx.HasPausedPartialPlan);
            Assert.IsTrue(ctx.PartialPlanQueue.Count == 1);
            Assert.AreEqual(task, ctx.PartialPlanQueue.Peek().Task);
            Assert.AreEqual(2, ctx.PartialPlanQueue.Peek().TaskIndex);

            status = domain.FindPlan(ctx, out plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task2", plan.Peek().Name);
        }

        [TestMethod]
        public void NestedPausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var domain = new Domain<MyContext>("Test");
            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Sequence() { Name = "Test3" };

            domain.Add(domain.Root, task);
            domain.Add(task, task2);
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task4" });

            domain.Add(task2, task3);
            domain.Add(task2, new PrimitiveTask() { Name = "Sub-task3" });

            domain.Add(task3, new PrimitiveTask() { Name = "Sub-task1" });
            domain.Add(task3, new PausePlanTask());
            domain.Add(task3, new PrimitiveTask() { Name = "Sub-task2" });

            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Partial);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task1", plan.Peek().Name);
            Assert.IsTrue(ctx.HasPausedPartialPlan);
            Assert.IsTrue(ctx.PartialPlanQueue.Count == 2);
            var queueAsArray = ctx.PartialPlanQueue.ToArray();
            Assert.AreEqual(task3, queueAsArray[0].Task);
            Assert.AreEqual(2, queueAsArray[0].TaskIndex);
            Assert.AreEqual(task, queueAsArray[1].Task);
            Assert.AreEqual(1, queueAsArray[1].TaskIndex);
        }

        [TestMethod]
        public void ContinueNestedPausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");

            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Sequence() { Name = "Test3" };

            domain.Add(domain.Root, task);
            domain.Add(task, task2);
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task4" });

            domain.Add(task2, task3);
            domain.Add(task2, new PrimitiveTask() { Name = "Sub-task3" });

            domain.Add(task3, new PrimitiveTask() { Name = "Sub-task1" });
            domain.Add(task3, new PausePlanTask());
            domain.Add(task3, new PrimitiveTask() { Name = "Sub-task2" });

            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Partial);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task1", plan.Dequeue().Name);
            Assert.IsTrue(ctx.HasPausedPartialPlan);
            Assert.IsTrue(ctx.PartialPlanQueue.Count == 2);
            var queueAsArray = ctx.PartialPlanQueue.ToArray();
            Assert.AreEqual(task3, queueAsArray[0].Task);
            Assert.AreEqual(2, queueAsArray[0].TaskIndex);
            Assert.AreEqual(task, queueAsArray[1].Task);
            Assert.AreEqual(1, queueAsArray[1].TaskIndex);

            status = domain.FindPlan(ctx, out plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 2);
            Assert.AreEqual("Sub-task2", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task4", plan.Dequeue().Name);
        }

        [TestMethod]
        public void ContinueMultipleNestedPausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");

            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Sequence() { Name = "Test3" };
            var task4 = new Sequence() { Name = "Test4" };

            domain.Add(domain.Root, task);

            domain.Add(task3, new PrimitiveTask() { Name = "Sub-task1" });
            domain.Add(task3, new PausePlanTask());
            domain.Add(task3, new PrimitiveTask() { Name = "Sub-task2" });

            domain.Add(task2, task3);
            domain.Add(task2, new PrimitiveTask() { Name = "Sub-task3" });

            domain.Add(task4, new PrimitiveTask() { Name = "Sub-task5" });
            domain.Add(task4, new PausePlanTask());
            domain.Add(task4, new PrimitiveTask() { Name = "Sub-task6" });

            domain.Add(task, task2);
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task4" });
            domain.Add(task, task4);
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task7" });

            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Partial);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task1", plan.Dequeue().Name);
            Assert.IsTrue(ctx.HasPausedPartialPlan);
            Assert.IsTrue(ctx.PartialPlanQueue.Count == 2);
            var queueAsArray = ctx.PartialPlanQueue.ToArray();
            Assert.AreEqual(task3, queueAsArray[0].Task);
            Assert.AreEqual(2, queueAsArray[0].TaskIndex);
            Assert.AreEqual(task, queueAsArray[1].Task);
            Assert.AreEqual(1, queueAsArray[1].TaskIndex);

            status = domain.FindPlan(ctx, out plan);

            Assert.IsTrue(status == DecompositionStatus.Partial);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 3);
            Assert.AreEqual("Sub-task2", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task4", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task5", plan.Dequeue().Name);

            status = domain.FindPlan(ctx, out plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 2);
            Assert.AreEqual("Sub-task6", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task7", plan.Dequeue().Name);
        }
    }
}
