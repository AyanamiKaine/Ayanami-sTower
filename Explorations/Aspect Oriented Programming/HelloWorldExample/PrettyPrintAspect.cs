using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace HelloWorldExample;

/// <summary>
/// Implements a basic to string method
/// </summary>
internal class PrettyPrintAttribute : TypeAspect
{
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