using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace ArxRiver.DataImporters.Core.Validation;

/// <summary>
/// Caches compiled expression delegates so each expression is compiled exactly once.
/// </summary>
public static class ExpressionCache
{
    private static readonly ConcurrentDictionary<(string Expression, Type DtoType), object> _cache = new();

    public static Func<T, bool> GetOrCompile<T>(string expression)
    {
        var key = (expression, typeof(T));

        var cached = _cache.GetOrAdd(key, k =>
            {
                var globalsType = typeof(ExpressionGlobals<>).MakeGenericType(k.DtoType);

                var options = ScriptOptions.Default
                    .AddReferences(k.DtoType.Assembly, typeof(object).Assembly)
                    .AddImports("System", "System.Linq");

                var script = CSharpScript.Create<bool>(k.Expression, options, globalsType: globalsType);
                script.Compile();
                return script;
            });

        var typedScript = (Script<bool>)cached;

        return row =>
            {
                var globals = Activator.CreateInstance(
                    typeof(ExpressionGlobals<>).MakeGenericType(typeof(T)),
                    row)!;
                var result = typedScript.RunAsync(globals).GetAwaiter().GetResult();
                return result.ReturnValue;
            };
    }
}

/// <summary>
/// Globals object passed to CSharpScript. Users write <c>row.PropertyName</c> in expressions.
/// </summary>
public sealed class ExpressionGlobals<T>(T row)
{
    public T Row { get; } = row;
}