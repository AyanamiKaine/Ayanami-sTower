using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace AyanamisTower.Utilities.Aspects;

/// <summary>
/// Aspect that adds a pretty-printing capability to a type by overriding its ToString method
/// to display the type name and all public properties. 
/// Does work for classes, structs.
/// Does not work for record structs.
/// </summary>
public class PrettyPrintAttribute : TypeAspect
{

    /// <summary>
    /// Provides a string representation of the target object, including its type name and property values.
    /// </summary>
    /// <returns>A formatted string representation of the object.</returns>
    [Introduce(WhenExists = OverrideStrategy.Override, Name = "ToString")]
    public string IntroducedToString()
    {
        var stringBuilder = new InterpolatedStringBuilder();
        stringBuilder.AddText("{ ");
        stringBuilder.AddText(meta.Target.Type.Name);
        stringBuilder.AddText(" ");

        var properties = meta.Target.Type.AllFieldsAndProperties
            .Where(
                f => f is
                {
                    IsStatic: false, IsImplicitlyDeclared: false, Accessibility: Accessibility.Public
                })
            .OrderBy(f => f.Name);

        // 
        var i = meta.CompileTime(0);

        // 

        foreach (var property in properties)
        {
            if (i > 0)
            {
                stringBuilder.AddText(", ");
            }

            stringBuilder.AddText(property.Name);
            stringBuilder.AddText("=");
            stringBuilder.AddExpression(property);

            i++;
        }

        stringBuilder.AddText(" }");

        return stringBuilder.ToValue();
    }
}