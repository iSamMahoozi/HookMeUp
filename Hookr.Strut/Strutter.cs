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
	internal class Strutter : IDisposable {
		private readonly string _inputFilename;
		private CompilationUnitSyntax _root;
		private Dictionary<MethodDeclarationSyntax, MethodDeclarationSyntax> _methodReplacements;
		private readonly string _hookContextName;
		private readonly string _orgMethodLambdaName;

		public Strutter() {
			_hookContextName = "hookContext";
			_orgMethodLambdaName = "hookBody";
			_methodReplacements = new Dictionary<MethodDeclarationSyntax, MethodDeclarationSyntax>();
		}

		public Strutter(string inputFilename) : this() {
			_inputFilename = inputFilename;
		}

		internal Strutter(SourceText syntax) : this() {
			_root = SyntaxFactory.ParseSyntaxTree(syntax).GetRoot() as CompilationUnitSyntax;
		}


		public Strutter ParseFile() {
			var originalContent = File.ReadAllText(_inputFilename);
			var syntax = SourceText.From(originalContent);
			_root = SyntaxFactory.ParseSyntaxTree(syntax).GetRoot() as CompilationUnitSyntax;

			return this;
		}

		private ExpressionStatementSyntax InvokeHookingContextStatement(string methodName, string ex = null) {
			var args = new List<ArgumentSyntax> {
				SyntaxFactory.Argument(SyntaxFactory.IdentifierName(_hookContextName))
			};
			if (ex != null) {
				args.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(ex)));
			}

			return SyntaxFactory.ExpressionStatement(
				SyntaxFactory.InvocationExpression(
					SyntaxFactory.MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						SyntaxFactory.IdentifierName(nameof(Pimp)),
						SyntaxFactory.IdentifierName(methodName)),
					SyntaxFactory.ArgumentList(
						SyntaxFactory.SeparatedList(args)
						)
					)
				)
				.NormalizeWhitespace()
				.WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed);
		}

		private void InjectIntoMethod(MethodDeclarationSyntax method) {
			var thisArgument = SyntaxFactory.Argument(SyntaxFactory.ThisExpression());
			var nullArgument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("null"));
			var currentMethodArgument = SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(nameof(MethodBase)), SyntaxFactory.IdentifierName(nameof(MethodBase.GetCurrentMethod)))));

			var hookContext = SyntaxFactory.LocalDeclarationStatement(
				SyntaxFactory.VariableDeclaration(
					SyntaxFactory.IdentifierName("var"))
				.WithVariables(
					SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
						SyntaxFactory.VariableDeclarator(
							SyntaxFactory.Identifier(_hookContextName))
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
					(method.ReturnType as PredefinedTypeSyntax)?.Keyword.Kind() == SyntaxKind.VoidKeyword ? (SimpleNameSyntax)SyntaxFactory.IdentifierName("Action") : (SimpleNameSyntax)SyntaxFactory.GenericName(
						SyntaxFactory.Identifier("Func"))
					.WithTypeArgumentList(
						SyntaxFactory.TypeArgumentList(
							SyntaxFactory.SingletonSeparatedList<TypeSyntax>(method.ReturnType))),
					SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.VariableDeclarator("body").WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParenthesizedLambdaExpression(lamdbaExpression)))
						)))
				.NormalizeWhitespace()
				.WithLeadingTrivia(SyntaxFactory.Trivia(SyntaxFactory.LineDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.HiddenKeyword), true).NormalizeWhitespace().WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)));

			var tryStatement = SyntaxFactory.TryStatement(
				SyntaxFactory.Block(
					(method.ReturnType as PredefinedTypeSyntax)?.Keyword.Kind() == SyntaxKind.VoidKeyword ?
						(StatementSyntax)SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(_orgMethodLambdaName))) :
						(StatementSyntax)SyntaxFactory.ReturnStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(_orgMethodLambdaName)))
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
					SyntaxFactory.Block(InvokeHookingContextStatement(nameof(Pimp.OnException), "ex").WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)))
						}),
				SyntaxFactory.FinallyClause(SyntaxFactory.Block(InvokeHookingContextStatement(nameof(Pimp.OnExit)).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed))))
				.WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)
				.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
				;

			BlockSyntax newMethodBody = SyntaxFactory.Block(actionOrFunc, hookContext, InvokeHookingContextStatement(nameof(Pimp.OnEnter)), tryStatement).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
			var newMethod = SyntaxFactory.MethodDeclaration(method.AttributeLists, method.Modifiers, method.ReturnType, method.ExplicitInterfaceSpecifier, method.Identifier, method.TypeParameterList, method.ParameterList, method.ConstraintClauses, newMethodBody, null);

			_methodReplacements[method] = newMethod;
		}

		private IEnumerable<MethodDeclarationSyntax> _Methods => _root.DescendantNodes().OfType<MethodDeclarationSyntax>();
		public Strutter InjectIntoMethods() {
			foreach (var method in _Methods) {
				InjectIntoMethod(method);
			}

			return this;
		}
		public Strutter ReplaceMethods() {
			if (_methodReplacements.Any()) {
				_root = _root.ReplaceNodes(_Methods, (o, n) => _methodReplacements[o]);
				_root = _root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("HookMeUp"))).NormalizeWhitespace();
				_root = _root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Reflection"))).NormalizeWhitespace();
			}

			return this;
		}
		public Strutter WriteAllToFile() {
			var generatedFile = Regex.Replace(_inputFilename, ".cs$", ".g.cs");
			File.WriteAllText(generatedFile, _root.ToFullString());

			return this;
		}

		private object ToDump() => _root.ToFullString();

		public void Dispose() {
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
			}
		}
	}
}
