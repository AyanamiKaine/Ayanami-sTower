namespace InformationHidingAndECS.Components;

public record struct Health(double Amount);
public record struct AttackPower(double Amount);
public record struct JumpStrength(float Amount);
public class UserInput()
{
    public void CurrentUserInput() { }
};

public struct Position3D;
public struct Velocity3D;