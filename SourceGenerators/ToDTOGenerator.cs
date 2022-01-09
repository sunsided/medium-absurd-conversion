using System.Linq.Expressions;
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
        // context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    /// <inheritdoc />
    public void Execute(GeneratorExecutionContext context)
    {
        var compilation = context.Compilation;

        // var notifyInterface = compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");
        var extensionMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // Find all ToDTO extension methods.
            var methods = syntaxTree.GetRoot().DescendantNodesAndSelf()
                .OfType<MethodDeclarationSyntax>()
                .Select(x => semanticModel.GetDeclaredSymbol(x)!)
                .Where(m => "ToDTO".Equals(m?.Name, StringComparison.Ordinal))
                .Where(m => m.IsExtensionMethod && m.Parameters.Length == 1);
            extensionMethods.UnionWith(methods);

            // TODO: Check for InvocationExpression of the method!
        }

        var names = extensionMethods.Select(m => $@"""{m.ReturnType.Name} <= {m.ReceiverType}.{m.Name}({m.Parameters.Single().Type.Name})""");

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

        context.AddSource(@"lol.cs", sb.ToString());
    }

#if false
    private sealed class SyntaxReceiver : ISyntaxReceiver
    {
        public List<MethodDeclarationSyntax> CandidateMethods { get; } = new();

        /// <inheritdoc />
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // We're only interested in syntax events related to method declarations
            // that have exactly one input parameter as well as a "static" and a "partial" modifier.
            if (syntaxNode is not MethodDeclarationSyntax
                {
                    ParameterList.Parameters.Count: 1,
                    Modifiers:
                    {
                        Count: >= 2
                    } modifiers
                } mds)
            {
                return;
            }

            // We cannot process non-partial and non-static methods.
            if (!modifiers.Any(st => st.ValueText.Equals("partial")) || !modifiers.Any(st => st.ValueText.Equals("static")))
            {
                return;
            }

            CandidateMethods.Add(mds);
        }
    }
#endif
}
