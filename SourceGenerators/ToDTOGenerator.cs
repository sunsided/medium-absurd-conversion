using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        // Fetch all ToDTO methods.
        var extensionMethods = syntaxReceiver.CandidateMethods
            .Select(methodDeclaration => compilation
                .GetSemanticModel(methodDeclaration.SyntaxTree)
                .GetDeclaredSymbol(methodDeclaration)!)
            .Where(declaredSymbol => declaredSymbol.IsExtensionMethod)
            .ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        // Fetch type type arguments of all calls to the ToDTO methods.
        var usedTypeArguments = syntaxReceiver.CandidateInvocations
            .Select(methodDeclaration => compilation
               .GetSemanticModel(methodDeclaration.SyntaxTree)
               .GetSymbolInfo(methodDeclaration).Symbol as IMethodSymbol)
            .Where(symbol => symbol?.TypeParameters.Length == 2)
            .Select(symbol => new InputOutputPair(symbol!.TypeArguments[0], symbol.TypeArguments[1]))
            .ToImmutableHashSet();

        var code = GenerateConversionMethodCode(extensionMethods, usedTypeArguments);
        context.AddSource("GenericPeopleConversion.Generated.cs", code);
    }

private static string GenerateConversionMethodCode(
    ImmutableHashSet<IMethodSymbol> extensionMethods,
    ImmutableHashSet<InputOutputPair> usedTypeArguments)
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
        if (!usedTypeArguments.Contains(new InputOutputPair(methodSymbol.ReturnType, methodSymbol.Parameters.Single().Type)))
        {
            continue;
        }

        var methodName = GetMethodFullName(methodSymbol);
        var parameterType = GetParameterTypeFullName(methodSymbol);
        var returnType = GetReturnTypeFullName(methodSymbol);

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

    return sb.ToString();
}

    private static string GetMethodFullName(IMethodSymbol methodSymbol)
    {
        var methodReceiverType = methodSymbol.ReceiverType!;
        return
            $"{methodReceiverType.ContainingNamespace.Name}.{methodReceiverType.Name}.{methodSymbol.Name}";
    }

    private static string GetReturnTypeFullName(IMethodSymbol methodSymbol)
    {
        var returnTypeNamespace = methodSymbol.ReturnType.ContainingNamespace.Name;
        var returnTypeName = methodSymbol.ReturnType.Name;
        return $"{returnTypeNamespace}.{returnTypeName}";
    }

    private static string GetParameterTypeFullName(IMethodSymbol methodSymbol)
    {
        var parameterTypeNamespace = methodSymbol.Parameters.Single().Type.ContainingNamespace.Name;
        var parameterTypeName = methodSymbol.Parameters.Single().Type.Name;
        return $"{parameterTypeNamespace}.{parameterTypeName}";
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

    private readonly struct InputOutputPair : IEquatable<InputOutputPair>
    {
        public InputOutputPair(ITypeSymbol dtoType, ITypeSymbol dataType)
        {
            DtoType = dtoType;
            DataType = dataType;
        }

        public ITypeSymbol DtoType { get; }
        public ITypeSymbol DataType { get; }

        /// <inheritdoc />
        public bool Equals(InputOutputPair other) =>
            DtoType.Equals(other.DtoType, SymbolEqualityComparer.Default) && DataType.Equals(other.DataType, SymbolEqualityComparer.Default);

        /// <inheritdoc />
        public override bool Equals(object? obj) =>
            obj is InputOutputPair other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (SymbolEqualityComparer.Default.GetHashCode(DtoType) * 397) ^
                       SymbolEqualityComparer.Default.GetHashCode(DataType);
            }
        }
    }
}
