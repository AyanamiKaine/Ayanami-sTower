TrafficLight light = new();
light.TimerElapsed(); // Green
light.TimerElapsed(); // Yellow
light.SensorTriggered(); // Red (Sensor)


public interface ITrafficLightState
{
    void HandleTimerElapsed(TrafficLight context);
    void HandleSensorTriggered(TrafficLight context);
}

public class RedState : ITrafficLightState
{
    public void HandleTimerElapsed(TrafficLight context)
    {
        context.ChangeState(new GreenState());
        Console.WriteLine("Red -> Green");
    }

    public void HandleSensorTriggered(TrafficLight context)
    {
        // Optional: Do nothing or handle special cases
    }
}

public class GreenState : ITrafficLightState
{
    public void HandleTimerElapsed(TrafficLight context)
    {
        context.ChangeState(new YellowState());
        Console.WriteLine("Green -> Yellow");
    }

    public void HandleSensorTriggered(TrafficLight context)
    {
        // Optional: Do nothing or handle special cases
    }
}

public class YellowState : ITrafficLightState
{
    public void HandleTimerElapsed(TrafficLight context)
    {
        context.ChangeState(new RedState());
        Console.WriteLine("Yellow -> Red");
    }

    public void HandleSensorTriggered(TrafficLight context)
    {
        context.ChangeState(new RedState());
        Console.WriteLine("Yellow -> Red (Sensor)");
    }
}

public class TrafficLight
{
    public ITrafficLightState CurrentState { get; private set; }

    public TrafficLight()
    {
        CurrentState = new RedState(); // Initial state
    }

    public void ChangeState(ITrafficLightState newState)
    {
        CurrentState = newState;
    }

    public void TimerElapsed()
    {
        CurrentState.HandleTimerElapsed(this);
    }

    public void SensorTriggered()
    {
        CurrentState.HandleSensorTriggered(this);
    }
}
