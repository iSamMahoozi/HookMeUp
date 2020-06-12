using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hookr.Strut {
	internal sealed class Strutter : IDisposable {
		private readonly string _fileName;
		public Strutter(string fileName) {
			_fileName = fileName;
		}

		public void Strut() {
			if (File.Exists(_fileName)) {
				var fileContent = File.ReadAllText(_fileName);
				var node = new SyntaxRewriter(_fileName, fileContent).Visit();
				var generatedFile = Regex.Replace(_fileName, ".cs$", ".g.cs");
				File.WriteAllText(generatedFile, node.ToFullString());
			}
		}

		#region dispose pattern
		#region IDisposable
		public void Dispose() => Dispose(true);
		#endregion

		private volatile bool _disposed = false;
		private void Dispose(bool disposing) {
			if (_disposed) {
				return;
			}

			if (disposing) {
			}

			_disposed = true;
		}
		#endregion

		private class SyntaxRewriter : CSharpSyntaxRewriter {
			private readonly string _fileName;
			private readonly SyntaxTree _tree;

			private readonly string _hookContextName = "hookContext";

			public SyntaxRewriter(string filename, string fileContent) {
				_fileName = filename;

				var syntax = SourceText.From(fileContent);
				_tree = ParseSyntaxTree(syntax);
			}

			public SyntaxNode Visit() => base.Visit(_tree.GetRoot());

			public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node) {
				base.VisitMethodDeclaration(node);

				var localMethodName = $"{node.Identifier.Text}_{Guid.NewGuid().ToString("N")}";

				var tryStatement = TryStatement(
					Block(
						(node.ReturnType as PredefinedTypeSyntax)?.Keyword.Kind() == SyntaxKind.VoidKeyword ?
							(StatementSyntax)ExpressionStatement(InvocationExpression(IdentifierName(localMethodName))) :
							(StatementSyntax)ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(_hookContextName), IdentifierName(nameof(HookMeUp.HookingContext.WithReturnValue)))).WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(InvocationExpression(IdentifierName(localMethodName)))))))
						),
					List(
						new[] {
				CatchClause(
					CatchDeclaration(
						IdentifierName(nameof(Exception)),
						Identifier("ex")).NormalizeWhitespace(),
					null,
					Block(InvokePimpMethod(nameof(HookMeUp.Pimp.OnException), "ex").WithTrailingTrivia(CarriageReturnLineFeed)))
							}),
					FinallyClause(Block(InvokePimpMethod(nameof(HookMeUp.Pimp.OnExit)).WithTrailingTrivia(CarriageReturnLineFeed))))
					;

				var statements = new List<StatementSyntax> {
					GetHookContextDeclaration(node).WithLeadingTrivia(Trivia(LineDirectiveTrivia(Token(SyntaxKind.HiddenKeyword), true))),
					InvokePimpMethod(nameof(HookMeUp.Pimp.OnEnter)),
					tryStatement,
				};
				if ((node.ReturnType as PredefinedTypeSyntax)?.Keyword.Kind() != SyntaxKind.VoidKeyword) {
					statements.Add(ReturnStatement(CastExpression(node.ReturnType, MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(_hookContextName), IdentifierName(nameof(HookMeUp.HookingContext.ReturnValue))))));
				}
				statements.Add(GetLocalFunc(node, localMethodName));

				return node
					.WithExpressionBody(null)
					.WithBody(Block(statements).NormalizeWhitespace())
					.WithSemicolonToken(MissingToken(SyntaxKind.SemicolonToken))
					;
			}

			private LocalFunctionStatementSyntax GetLocalFunc(MethodDeclarationSyntax method, string localMethodName) {
				var lineNumber = _tree.GetLineSpan(method.FullSpan).StartLinePosition.Line;

				return LocalFunctionStatement(method.ReturnType, Identifier(localMethodName))
					.WithExpressionBody(method.ExpressionBody)
					.WithSemicolonToken(method.SemicolonToken)
					.WithBody(method.Body?.WithLeadingTrivia(Trivia(LineDirectiveTrivia(Literal(lineNumber), Literal(_fileName), true))))
					;
			}
			private LocalDeclarationStatementSyntax GetHookContextDeclaration(MethodDeclarationSyntax method) {
				var thisArgument = Argument(ThisExpression());
				var nullArgument = Argument(LiteralExpression(SyntaxKind.NullLiteralExpression));
				var currentMethodArgument = Argument(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(typeof(System.Reflection.MethodBase).FullName), IdentifierName(nameof(System.Reflection.MethodBase.GetCurrentMethod)))));

				return LocalDeclarationStatement(
					VariableDeclaration(
						IdentifierName("var"))
						.WithVariables(
						SingletonSeparatedList(
					VariableDeclarator(
						Identifier(_hookContextName))
					.WithInitializer(
						EqualsValueClause(
							ObjectCreationExpression(
								IdentifierName(typeof(HookMeUp.HookingContext).FullName))
							.WithArgumentList(
								ArgumentList(
									SeparatedList(
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
			);

			}
			private ExpressionStatementSyntax InvokePimpMethod(string methodName, string ex = null) {
				var args = new List<ArgumentSyntax> {
				Argument(IdentifierName(_hookContextName))
			};
				if (ex != null) {
					args.Add(Argument(IdentifierName(ex)));
				}

				return ExpressionStatement(
					InvocationExpression(
						MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							IdentifierName(typeof(HookMeUp.Pimp).FullName),
							IdentifierName(methodName)),
						ArgumentList(
							SeparatedList(args)
							)
						)
					);
			}
		}
	}
}