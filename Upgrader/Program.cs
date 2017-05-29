using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdWordsUpgrader
{
    class Asyncer : CSharpSyntaxRewriter
    {

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            foreach(var mt in node.Members.Select(x => x as MethodDeclarationSyntax).Where(x => x!= null))
            {
                var id = mt.Identifier.ToString();

                if (id != "get" && id != "mutate" && id != "query" && id != "getResult")
                    continue;

                var asyncMethod = ConvertToAsync(mt);

                node = node.AddMembers(asyncMethod);

            }

            return node;
        }

        private MethodDeclarationSyntax ConvertToAsync(MethodDeclarationSyntax node)
        {
            var nm = CSharpSyntaxTree.ParseText(@"public void m3()
            { 
                Task<object[]> results = this.InvokeAsync(""" + node.Identifier.ToString() +
                @""", new object[]
              {" +

                string.Join(",", node.ParameterList.Parameters.Select(x => x.Identifier.ToString()))
               +
              @"}); 
              return ((" + node.ReturnType.GetText().ToString().Trim() + @")((await results)[0]));
            }
            ").GetRoot().DescendantNodes().First() as MethodDeclarationSyntax;


            var asyncMathod = SyntaxFactory.MethodDeclaration(node.AttributeLists, node.Modifiers,
                SyntaxFactory.ParseTypeName(" Task<" + node.ReturnType.GetText().ToString().Trim() + "> "),
                node.ExplicitInterfaceSpecifier,
                SyntaxFactory.Identifier(node.Identifier.Text + "Async"), node.TypeParameterList, node.ParameterList, node.ConstraintClauses,
                nm.Body
                    , node.ExpressionBody
                )
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
              ;

            return asyncMathod;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string csFile = args[0];
                
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(csFile));

            var rewriter = new Asyncer();
            var result = rewriter.Visit(tree.GetRoot());

            File.Move(csFile, csFile + ".origin");

            File.WriteAllText(csFile, result.ToFullString());

        }

    }
}
