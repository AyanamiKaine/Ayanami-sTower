using System;
using System.Linq;
using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class ObserverTests
{
    [Fact]
    public void Typed_Set_PreCanMutate_And_Cancel()
    {
        var w = new World();
        var e = w.CreateEntity();

        // Pre 1: mutate X += 10
        w.OnSetPre<PositionComponent>((Entity ent, ref PositionComponent v, ref bool cancel) =>
            {
                v.X += 10;
            });
        // Pre 2: cancel if Y == 99
        w.OnSetPre<PositionComponent>((Entity ent, ref PositionComponent v, ref bool cancel) =>
            {
                if (v.Y == 99) cancel = true;
            });

        // Post: verify hadPrevious flag and values get delivered
        PositionComponent postPrev = default;
        PositionComponent postCurr = default;
        bool postHadPrev = false;
        w.OnSetPost<PositionComponent>((Entity ent, in PositionComponent prev, in PositionComponent curr, bool hadPrev) =>
            {
                postPrev = prev;
                postCurr = curr;
                postHadPrev = hadPrev;
            });

        // First set (no previous)
        e.Set(new PositionComponent { X = 1, Y = 2 });
        var got = e.GetCopy<PositionComponent>();
        Assert.Equal(11, got.X); // mutated by pre
        Assert.Equal(2, got.Y);
        Assert.Equal(default(PositionComponent), postPrev);
        Assert.Equal(got, postCurr);
        Assert.False(postHadPrev);

        // Second set with cancel path
        e.Set(new PositionComponent { X = 5, Y = 99 });
        // Should remain unchanged due to cancel
        got = e.GetCopy<PositionComponent>();
        Assert.Equal(11, got.X);
        Assert.Equal(2, got.Y);
    }

    [Fact]
    public void Typed_Remove_PreCancel_And_PostProvidesRemoved()
    {
        var w = new World();
        var e = w.CreateEntity();
        e.Set(new PositionComponent { X = 7, Y = 8 });

        // Pre-cancel removal once
        bool first = true;
        w.OnRemovePre<PositionComponent>((Entity ent, ref bool cancel) =>
            {
                if (first)
                {
                    cancel = true;
                    first = false;
                }
            });

        PositionComponent removedSeen = default;
        w.OnRemovePost<PositionComponent>((Entity ent, in PositionComponent removed) =>
            {
                removedSeen = removed;
            });

        // First remove should be canceled
        e.Remove<PositionComponent>();
        Assert.True(e.Has<PositionComponent>());

        // Second remove should succeed and report
        e.Remove<PositionComponent>();
        Assert.False(e.Has<PositionComponent>());
        Assert.Equal(new PositionComponent { X = 7, Y = 8 }, removedSeen);
    }

    [Fact]
    public void Dynamic_Set_PreMutateAndCancel_PostHasPrevCurr()
    {
        var w = new World();
        var e = w.CreateEntity();

        // Pre mutate name 'Tag' value, then cancel on value=="BLOCK"
        w.OnDynamicSetPre("Tag", (Entity ent, string name, ref object? value, ref bool cancel) =>
            {
                if (value is string s)
                {
                    value = s + "!";
                    if (s == "BLOCK") cancel = true;
                }
            });

        object? postPrev = null; object? postCurr = null; bool postHadPrev = false;
        w.OnDynamicSetPost("Tag", (Entity ent, string name, object? prev, object? curr, bool hadPrev) =>
            {
                postPrev = prev; postCurr = curr; postHadPrev = hadPrev;
            });

        // First set
        e.SetDynamic("Tag", "A");
        Assert.True(e.HasDynamic("Tag"));
        Assert.Equal("A!", e.GetDynamic<string>("Tag"));
        Assert.Null(postPrev);
        Assert.Equal("A!", postCurr);
        Assert.False(postHadPrev);

        // Second set with previous
        e.SetDynamic("Tag", "B");
        Assert.Equal("B!", e.GetDynamic<string>("Tag"));
        Assert.Equal("A!", postPrev);
        Assert.Equal("B!", postCurr);
        Assert.True(postHadPrev);

        // Cancel set
        e.SetDynamic("Tag", "BLOCK");
        Assert.Equal("B!", e.GetDynamic<string>("Tag"));
    }

    [Fact]
    public void Dynamic_Remove_PreCancel_And_PostReportsRemoved()
    {
        var w = new World();
        var e = w.CreateEntity();
        e.SetDynamic("Score", 123);

        bool cancelOnce = true;
        w.OnDynamicRemovePre("Score", (Entity ent, string name, ref bool cancel) =>
            {
                if (cancelOnce)
                {
                    cancel = true;
                    cancelOnce = false;
                }
            });

        object? removedValue = null; bool hadValue = false;
        w.OnDynamicRemovePost("Score", (Entity ent, string name, object? v, bool had) => { removedValue = v; hadValue = had; });

        // First remove canceled
        e.RemoveDynamic("Score");
        Assert.True(e.HasDynamic("Score"));

        // Second remove succeeds
        e.RemoveDynamic("Score");
        Assert.False(e.HasDynamic("Score"));
        Assert.Equal(123, removedValue);
        Assert.True(hadValue);
    }

    [Fact]
    public void Multiple_Observers_Execute_In_Registration_Order()
    {
        var w = new World();
        var e = w.CreateEntity();

        var order = new System.Collections.Generic.List<int>();
        w.OnSetPre<PositionComponent>((Entity ent, ref PositionComponent v, ref bool cancel) => order.Add(1));
        w.OnSetPre<PositionComponent>((Entity ent, ref PositionComponent v, ref bool cancel) => order.Add(2));
        w.OnSetPost<PositionComponent>((Entity ent, in PositionComponent prev, in PositionComponent curr, bool hadPrev) => order.Add(3));
        w.OnSetPost<PositionComponent>((Entity ent, in PositionComponent prev, in PositionComponent curr, bool hadPrev) => order.Add(4));

        e.Set(new PositionComponent { X = 0, Y = 0 });
        Assert.True(order.SequenceEqual(new[] { 1, 2, 3, 4 }));
    }
}
