using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HookMeUp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Hookr.Strut {
	internal class Program {
		private static void Main(string[] args) {
			Console.WriteLine("Attach debgger now");
			Console.ReadLine();

			var compileFile = args[0];
			var originalContent = File.ReadAllText(compileFile);
			var syntax = SourceText.From(originalContent);
			var root = SyntaxFactory.ParseSyntaxTree(syntax).GetRoot() as CompilationUnitSyntax;

			root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("HookMeUp"))).NormalizeWhitespace();
			root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Reflection"))).NormalizeWhitespace();

			var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
			var replacements = new Dictionary<MethodDeclarationSyntax, MethodDeclarationSyntax>();

			foreach (var method in methods) {
				var thisArgument = SyntaxFactory.Argument(SyntaxFactory.ThisExpression());
				var nullArgument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("null"));
				var currentMethodArgument = SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(nameof(MethodBase)), SyntaxFactory.IdentifierName(nameof(MethodBase.GetCurrentMethod)))));

				var hookContext = SyntaxFactory.LocalDeclarationStatement(
					SyntaxFactory.VariableDeclaration(
						SyntaxFactory.IdentifierName("var"))
					.WithVariables(
						SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
							SyntaxFactory.VariableDeclarator(
								SyntaxFactory.Identifier("hookContext"))
							.WithInitializer(
								SyntaxFactory.EqualsValueClause(
									SyntaxFactory.ObjectCreationExpression(
										SyntaxFactory.IdentifierName(nameof(HookingContext)))
									.WithArgumentList(
										SyntaxFactory.ArgumentList(
											SyntaxFactory.SeparatedList(
												new[]{
													method.Modifiers.Any(x => x.ValueText == "static") ? nullArgument : thisArgument,
													currentMethodArgument
												})
											)
										)
									)
								)
							)
						)
					)
					.NormalizeWhitespace()
					.WithLeadingTrivia(SyntaxFactory.Trivia(SyntaxFactory.LineDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.HiddenKeyword), true).NormalizeWhitespace().WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)))
					;

				var HookManager_OnEntry = SyntaxFactory.ExpressionStatement(
					SyntaxFactory.InvocationExpression(
						SyntaxFactory.MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							SyntaxFactory.IdentifierName(nameof(Pimp)),
							SyntaxFactory.IdentifierName(nameof(Pimp.OnEnter))),
						SyntaxFactory.ArgumentList(
							SyntaxFactory.SeparatedList(
								new[] {
									SyntaxFactory.Argument(SyntaxFactory.IdentifierName("hookContext"))
								})))).NormalizeWhitespace()
					.WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)
					;

				var HookManager_OnExit = SyntaxFactory.ExpressionStatement(
					SyntaxFactory.InvocationExpression(
						SyntaxFactory.MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							SyntaxFactory.IdentifierName(nameof(Pimp)),
							SyntaxFactory.IdentifierName(nameof(Pimp.OnExit))),
						SyntaxFactory.ArgumentList(
							SyntaxFactory.SeparatedList(
								new[] {
									SyntaxFactory.Argument(SyntaxFactory.IdentifierName("hookContext"))
								}))))
					.WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)
					;

				var HookManager_OnException = SyntaxFactory.ExpressionStatement(
					SyntaxFactory.InvocationExpression(
						SyntaxFactory.MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							SyntaxFactory.IdentifierName(nameof(Pimp)),
							SyntaxFactory.IdentifierName(nameof(Pimp.OnException))),
						SyntaxFactory.ArgumentList(
							SyntaxFactory.SeparatedList(
								new[] {
									SyntaxFactory.Argument(SyntaxFactory.IdentifierName("hookContext")),
									SyntaxFactory.Argument(SyntaxFactory.IdentifierName("ex"))
								}))))
					.NormalizeWhitespace()
					.WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)
					;

				CSharpSyntaxNode lamdbaExpression;
				if (method.Body?.Statements.Any() ?? false) {
					lamdbaExpression = SyntaxFactory.Block(method.Body.Statements)
						.WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(SyntaxFactory.Trivia(SyntaxFactory.LineDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DefaultKeyword), true).NormalizeWhitespace().WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed))))
						.WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(SyntaxFactory.Trivia(SyntaxFactory.LineDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.HiddenKeyword), true).NormalizeWhitespace().WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed))))
						;

				} else {
					lamdbaExpression = method.ExpressionBody.Expression;
				}
				var actionOrFunc = SyntaxFactory.LocalDeclarationStatement(
					SyntaxFactory.VariableDeclaration(
						(method.ReturnType as PredefinedTypeSyntax).Keyword.Kind() == SyntaxKind.VoidKeyword ? SyntaxFactory.IdentifierName("Action") : SyntaxFactory.IdentifierName("Func"),
						SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.VariableDeclarator("body").WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParenthesizedLambdaExpression(lamdbaExpression)))
						))
						).NormalizeWhitespace()
					.WithLeadingTrivia(SyntaxFactory.Trivia(SyntaxFactory.LineDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.HiddenKeyword), true).NormalizeWhitespace().WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)))
					;
				var tryStatement = SyntaxFactory.TryStatement(
					SyntaxFactory.Block(
						SyntaxFactory.ExpressionStatement(
							SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("body")))
							.NormalizeWhitespace()
							.WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)
							.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
						),
					SyntaxFactory.List<CatchClauseSyntax>(
						new[] {
				SyntaxFactory.CatchClause(
					SyntaxFactory.CatchDeclaration(
						SyntaxFactory.IdentifierName(nameof(Exception)),
						SyntaxFactory.Identifier("ex")).NormalizeWhitespace(),
					null,
					SyntaxFactory.Block(HookManager_OnException.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)))
							}),
					SyntaxFactory.FinallyClause(SyntaxFactory.Block(HookManager_OnExit.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed))))
					.WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)
					.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
					;

				BlockSyntax newMethodBody = SyntaxFactory.Block(actionOrFunc, hookContext, HookManager_OnEntry, tryStatement).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
				var newMethod = SyntaxFactory.MethodDeclaration(method.AttributeLists, method.Modifiers, method.ReturnType, method.ExplicitInterfaceSpecifier, method.Identifier, method.TypeParameterList, method.ParameterList, method.ConstraintClauses, newMethodBody, null);
				replacements[method] = newMethod;
			}
			root = root.ReplaceNodes(methods, (o, n) => replacements[o]);

			var generatedFile = Regex.Replace(compileFile, ".cs$", ".g.cs");
			File.WriteAllText(generatedFile, root.ToFullString());

			//TODO get text from root and write to .g.cs file
		}
	}
}
