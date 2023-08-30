using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WalletWasabi.Fluent.Generators;

internal class AutoNotifyGenerator : GeneratorStep<FieldDeclarationSyntax>
{
	private const string AutoNotifyAttributeDisplayString = "WalletWasabi.Fluent.AutoNotifyAttribute";
	private const string ReactiveObjectDisplayString = "ReactiveUI.ReactiveObject";

	public override bool Filter(FieldDeclarationSyntax field)
	{
		return field.AttributeLists.Count > 0;
	}

	public override void Execute(FieldDeclarationSyntax[] syntaxNodes)
	{
		var fieldSymbols = GetAutoNotifyFields(syntaxNodes).ToArray();

		var attributeSymbol = Context.Compilation.GetTypeByMetadataName(AutoNotifyAttributeDisplayString);
		if (attributeSymbol is null)
		{
			return;
		}

		var notifySymbol = Context.Compilation.GetTypeByMetadataName(ReactiveObjectDisplayString);
		if (notifySymbol is null)
		{
			return;
		}

		// TODO: https://github.com/dotnet/roslyn/issues/49385
#pragma warning disable RS1024
		var groupedFields = fieldSymbols.GroupBy(f => f.ContainingType);
#pragma warning restore RS1024

		foreach (var group in groupedFields)
		{
			var classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol, notifySymbol);
			if (classSource is null)
			{
				continue;
			}

			AddSource($"{group.Key.Name}_AutoNotify.cs", classSource);
		}
	}

	private string? ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol, INamedTypeSymbol notifySymbol)
	{
		if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
		{
			return null;
		}

		string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

		var addNotifyInterface = !classSymbol.Interfaces.Contains(notifySymbol);
		var baseType = classSymbol.BaseType;
		while (true)
		{
			if (baseType is null)
			{
				break;
			}

			if (SymbolEqualityComparer.Default.Equals(baseType, notifySymbol))
			{
				addNotifyInterface = false;
				break;
			}

			baseType = baseType.BaseType;
		}

		var source = new StringBuilder();

		var format = new SymbolDisplayFormat(
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeVariance);

		if (addNotifyInterface)
		{
			source.Append($@"// <auto-generated />
#nullable enable
using ReactiveUI;

namespace {namespaceName}
{{
    public partial class {classSymbol.ToDisplayString(format)} : {notifySymbol.ToDisplayString()}
    {{");
		}
		else
		{
			source.Append($@"// <auto-generated />
#nullable enable
using ReactiveUI;

namespace {namespaceName}
{{
    public partial class {classSymbol.ToDisplayString(format)}
    {{");
		}

		foreach (IFieldSymbol fieldSymbol in fields)
		{
			ProcessField(source, fieldSymbol, attributeSymbol);
		}

		source.Append($@"
    }}
}}");

		return source.ToString();
	}

	private void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
	{
		var fieldName = fieldSymbol.Name;
		var fieldType = fieldSymbol.Type;
		var attributeData = fieldSymbol.GetAttributes().Single(ad => ad?.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) ?? false);
		var overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;
		var propertyName = ChooseName(fieldName, overridenNameOpt);

		if (propertyName is null || propertyName.Length == 0 || propertyName == fieldName)
		{
			// Issue a diagnostic that we can't process this field.
			return;
		}

		var overridenSetterModifierOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "SetterModifier").Value;
		var setterModifier = ChooseSetterModifier(overridenSetterModifierOpt);
		if (setterModifier is null)
		{
			source.Append($@"
        public {fieldType} {propertyName}
        {{
            get => {fieldName};
        }}");
		}
		else
		{
			source.Append($@"
        public {fieldType} {propertyName}
        {{
            get => {fieldName};
            {setterModifier}set => this.RaiseAndSetIfChanged(ref {fieldName}, value);
        }}");
		}

		static string? ChooseSetterModifier(TypedConstant overridenSetterModifierOpt)
		{
			if (!overridenSetterModifierOpt.IsNull && overridenSetterModifierOpt.Value is not null)
			{
				var value = (int)overridenSetterModifierOpt.Value;
				return value switch
				{
					0 => null,// None
					1 => "",// Public
					2 => "protected ",// Protected
					3 => "private ",// Private
					4 => "internal ",// Internal
					_ => ""// Default
				};
			}
			else
			{
				return "";
			}
		}

		static string? ChooseName(string fieldName, TypedConstant overridenNameOpt)
		{
			if (!overridenNameOpt.IsNull)
			{
				return overridenNameOpt.Value?.ToString();
			}

			fieldName = fieldName.TrimStart('_');
			if (fieldName.Length == 0)
			{
				return string.Empty;
			}

			if (fieldName.Length == 1)
			{
				return fieldName.ToUpper();
			}

			return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
		}
	}

	private IEnumerable<IFieldSymbol> GetAutoNotifyFields(FieldDeclarationSyntax[] fieldDeclarations)
	{
		foreach (var fieldDeclaration in fieldDeclarations)
		{
			var semanticModel = GetSemanticModel(fieldDeclaration.SyntaxTree);

			foreach (VariableDeclaratorSyntax variable in fieldDeclaration.Declaration.Variables)
			{
				if (semanticModel.GetDeclaredSymbol(variable) is not IFieldSymbol fieldSymbol)
				{
					continue;
				}

				var attributes = fieldSymbol.GetAttributes();
				if (attributes.Any(ad => ad?.AttributeClass?.ToDisplayString() == AutoNotifyAttributeDisplayString))
				{
					yield return fieldSymbol;
				}
			}
		}
	}
}
