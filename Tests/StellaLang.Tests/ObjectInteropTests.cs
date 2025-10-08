using System;

namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class ObjectInteropTests
{
    private sealed class Circle(double radius)
    {
        public double Radius { get; } = radius;

        public double Area() => Math.PI * Radius * Radius;
        public void Scale(double factor) => _scale *= factor;
        private double _scale = 1.0;
        public double ScaledRadius => Radius * _scale;
    }

    [Fact]
    public void ConstructObject_PushHandle_CallInstanceMethods()
    {
        var vm = new VMActor();

        // Define constructor and instance methods by handle
        vm.DefineConstructor("NEW-CIRCLE", typeof(Circle), typeof(double));
        vm.DefineInstanceByHandle("AREA", typeof(Circle), nameof(Circle.Area));
        vm.DefineInstanceByHandle("SCALE", typeof(Circle), nameof(Circle.Scale), typeof(double));
        vm.DefineInstanceByHandle("SCALED-R", typeof(Circle), nameof(Circle.ScaledRadius));

        // Create circle with radius 3.0 -> handle on stack
        new ProgramBuilder()
            .Push(3)          // radius
            .Word("NEW-CIRCLE")
            .RunOn(vm);
        var handle = vm.DataStack.First().AsPointer();

        // Compute area: (handle -- area)
        new ProgramBuilder()
            .Push(handle)
            .Word("AREA")
            .RunOn(vm);
        var area = vm.DataStack.First().AsFloat();
        Assert.InRange(area, 28.27, 28.28); // ~pi*9

        // Scale by 2: (handle 2 -- ) then query scaled radius
        new ProgramBuilder()
            .Push(handle)
            .Push(2)
            .Word("SCALE")
            .Push(handle)
            .Word("SCALED-R")
            .RunOn(vm);

        var scaled = vm.DataStack.First().AsFloat();
        Assert.Equal(6.0, Math.Round(scaled, 1));
    }
}
