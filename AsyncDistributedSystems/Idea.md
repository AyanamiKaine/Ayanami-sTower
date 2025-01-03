```csharp

public class Cell : BaseCell
{
    public void Run(); // Inherited from a base class

    public void HandleMessage(Message); // The Cell works on messages in a queue, in json format.
    /*
    The basecell has all the code that setups the queue, connection code so other cells can connect to it.

    This could be TCP, IPC, what ever.

    When a cell runs it runs handle message in a async loop, but it should be *reactive* only running when actually new messages are happening.

    Cells should be be able to send their entire source code to others, so it might be spawned by another cell in their same process.
    A cell should be a small computer.

    You cant scale with central control
    */
}


```

## Cells react on the presence of data.

- The "data" is the coin: You insert a coin (the signaling molecule).
- The "receptor" is the coin slot: The coin slot (receptor) is designed to accept a specific type of coin.
- The "response" is the dispensed snack: If the coin is the right size and shape, it triggers a mechanism inside the machine (the cascade of events) that dispenses a snack (the cellular response).

The vending machine doesn't "understand" that you want a snack. It simply reacts to the physical presence of the correct coin in the correct slot.
