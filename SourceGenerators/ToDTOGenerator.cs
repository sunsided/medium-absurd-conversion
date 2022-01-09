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
            .Where(declaredSymbol => declaredSymbol.IsExtensionMethod)
            .ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        // TODO: Check for InvocationExpression of the method!
        var calls = syntaxReceiver.CandidateInvocations
            //.Select(methodDeclaration => compilation
            //   .GetSemanticModel(methodDeclaration.SyntaxTree)
            ///   .GetSymbolInfo(methodDeclaration).Symbol as IMethodSymbol)
            //.Select(symbol => symbol?.ToDisplayString())
            //.Where(name => name?.Contains("ToDTO") == true)
            .Select(invocation => invocation.Expression.GetType() + " - " + invocation)
            // .Where(name => name?.Contains("ToDTO") == true)
            .ToList();

        var lol = "/* " + calls.Count + ": " + string.Join(Environment.NewLine + "// ", calls) + " */";
        context.AddSource("GenericPeopleConversionCalls.Generated.cs", lol);

        var code = GenerateConversionMethodCode(extensionMethods);
        context.AddSource("GenericPeopleConversion.Generated.cs", code);
    }

    private static string GenerateConversionMethodCode(ImmutableHashSet<IMethodSymbol> extensionMethods)
    {
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
            var methodName =
                $"{methodReceiverType.ContainingNamespace.Name}.{methodReceiverType.Name}.{methodSymbol.Name}";

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

        // TODO: Add an analyzer that prevents this from happening.
        sb.Append(
            @"        throw new InvalidOperationException(""No method found to convert from type {typeof(TData)} to {typeof{TDTO}}"");");
        sb.AppendLine(@"
    }
}");

        var code = sb.ToString();
        return code;
    }

    private sealed class SyntaxReceiver : ISyntaxReceiver
    {
        public HashSet<MethodDeclarationSyntax> CandidateMethods { get; } = new();
        public HashSet<InvocationExpressionSyntax> CandidateInvocations { get; } = new();

        /// <inheritdoc />
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Find candidates for "ToDTO" extension methods.
            // We expect exactly one input parameter as well as a "static" modifier.
            if (syntaxNode is MethodDeclarationSyntax
                {
                    Identifier.Text: "ToDTO",
                    ParameterList.Parameters.Count: 1,
                    Modifiers:
                    {
                        Count: >= 1
                    } modifiers
                } mds &&
                modifiers.Any(st => st.ValueText.Equals("static")))
            {
                CandidateMethods.Add(mds);
            }

            // Likewise, the method invocations must be to a "ToDTO" method with exactly one argument.
            if (syntaxNode is InvocationExpressionSyntax
                {
                    ArgumentList.Arguments.Count: 1,
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name.Identifier.ValueText: "ToDTO",
                    }
                } ie)
            {
                CandidateInvocations.Add(ie);
            }
        }
    }
}
