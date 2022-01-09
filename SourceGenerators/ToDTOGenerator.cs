using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGenerators;

[Generator]
public sealed class ToDTOGenerator : ISourceGenerator
{
    /// <inheritdoc />
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    /// <inheritdoc />
    public void Execute(GeneratorExecutionContext context)
    {
        var compilation = context.Compilation;
        var syntaxReceiver = (SyntaxReceiver)context.SyntaxReceiver!;

        var extensionMethods = syntaxReceiver.CandidateMethods
            .Select(methodDeclaration => compilation
                .GetSemanticModel(methodDeclaration.SyntaxTree)
                .GetDeclaredSymbol(methodDeclaration)!)
            .Where(declaredSymbol =>
                "ToDTO".Equals(declaredSymbol.Name, StringComparison.Ordinal) &&
                declaredSymbol.IsExtensionMethod &&
                declaredSymbol.Parameters.Length == 1)
            .ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        var sb = new StringBuilder();

        sb.Append(@"
using System;

namespace ExtensionMethods70642141;

public static partial class GenericPeopleConversion {
    public static partial TDTO ToDTO<TDTO, TData>(TData data)
    {");

        foreach (var methodSymbol in extensionMethods)
        {
            var methodReceiverType = methodSymbol.ReceiverType!;
            var methodName = $"{methodReceiverType.ContainingNamespace.Name}.{methodReceiverType.Name}.{methodSymbol.Name}";

            var parameterTypeNamespace = methodSymbol.Parameters.Single().Type.ContainingNamespace.Name;
            var parameterTypeName = methodSymbol.Parameters.Single().Type.Name;
            var parameterType = $"{parameterTypeNamespace}.{parameterTypeName}";

            var returnTypeNamespace = methodSymbol.ReturnType.ContainingNamespace.Name;
            var returnTypeName = methodSymbol.ReturnType.Name;
            var returnType = $"{returnTypeNamespace}.{returnTypeName}";

            sb.AppendLine($@"
        if (typeof(TData) == typeof({parameterType}) && typeof(TDTO) == typeof({returnType})) {{
            return (TDTO)(object){methodName}(({parameterType})(object)data);
        }}");
        }

        sb.Append(@"        throw new InvalidOperationException(""No method found to convert from type {typeof(TData)} to {typeof{TDTO}}"");");

        sb.AppendLine(@"
    }
}");

        context.AddSource(@"GenericPeopleConversion.Generated.cs", sb.ToString());
    }

    private sealed class SyntaxReceiver : ISyntaxReceiver
    {
        public HashSet<MethodDeclarationSyntax> CandidateMethods { get; } = new();

        /// <inheritdoc />
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // We're only interested in syntax events related to method declarations
            // that have exactly one input parameter as well as a "static" modifier.
            if (syntaxNode is not MethodDeclarationSyntax
                {
                    ParameterList.Parameters.Count: 1,
                    Modifiers:
                    {
                        Count: >= 1,
                    } modifiers
                } mds)
            {
                return;
            }

            // We only process static methods.
            if (!modifiers.Any(st => st.ValueText.Equals("static")))
            {
                return;
            }

            CandidateMethods.Add(mds);
        }
    }
}
