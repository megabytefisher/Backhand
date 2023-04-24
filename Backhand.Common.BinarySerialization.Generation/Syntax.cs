using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Backhand.Common.BinarySerialization.Generation
{
    internal static class Syntax
    {
        public static T? GetParentSyntax<T>(SyntaxNode? syntaxNode) where T : SyntaxNode
        {
            T? parent = null;

            while (syntaxNode != null)
            {
                syntaxNode = syntaxNode.Parent;

                if (syntaxNode is not T typedNode) continue;

                parent = typedNode;
            }

            return parent;
        }

        public static T? ParseAttribute<T>(SemanticModel semanticModel, ClassDeclarationSyntax classDeclaration) where T : Attribute, new()
        {
            string attributeName = typeof(T).FullName;
            AttributeSyntax? attributeSyntax = classDeclaration.AttributeLists
                .SelectMany(list => list.Attributes)
                .FirstOrDefault(attribute => semanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol methodSymbol
                                             && methodSymbol.ContainingType.ToDisplayString() == attributeName);

            if (attributeSyntax == null) return null;

            T attribute = new T();
            if (attributeSyntax.ArgumentList == null) return attribute;

            foreach (var argument in attributeSyntax.ArgumentList.Arguments)
            {
                PropertyInfo? property = typeof(T).GetProperty(argument.NameEquals!.Name.Identifier.Text);
                if (property == null) continue;
                property.SetValue(attribute, semanticModel.GetConstantValue(argument.Expression).Value);
            }

            return attribute;
        }

        public static class Types
        {
            public static readonly TypeSyntax Byte = ParseTypeName(Names.Types.Byte);
            public static readonly TypeSyntax Bool = ParseTypeName(Names.Types.Bool);
            public static readonly TypeSyntax UInt16 = ParseTypeName(Names.Types.UInt16);
            public static readonly TypeSyntax Int16 = ParseTypeName(Names.Types.Int16);
            public static readonly TypeSyntax UInt32 = ParseTypeName(Names.Types.UInt32);
            public static readonly TypeSyntax Int32 = ParseTypeName(Names.Types.Int32);
            public static readonly TypeSyntax UInt64 = ParseTypeName(Names.Types.UInt64);
            public static readonly TypeSyntax Int64 = ParseTypeName(Names.Types.Int64);
            
            public static readonly TypeSyntax ByteSequenceReader = ParseTypeName(Names.Types.ByteSequenceReader);
            public static readonly TypeSyntax ByteSpanWriter = ParseTypeName(Names.Types.ByteSpanWriter);

            public static readonly TypeSyntax IBinarySerializable = ParseTypeName(Names.Types.IBinarySerializable);

            public static TypeSyntax FromSymbol(ITypeSymbol symbol) => ParseTypeName(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        public static class Expressions
        {
            public static readonly ExpressionSyntax Zero = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0));
            public static readonly ExpressionSyntax One = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1));

            public static readonly ExpressionSyntax True = LiteralExpression(SyntaxKind.TrueLiteralExpression);
            public static readonly ExpressionSyntax False = LiteralExpression(SyntaxKind.FalseLiteralExpression);
            
            public static ExpressionSyntax LiteralBoolean(bool value) => value ? True : False;
            
            public static ExpressionSyntax LiteralNumeric(byte value) => CastExpression(Types.Byte, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value)));
            public static ExpressionSyntax LiteralNumeric(short value) => CastExpression(Types.Int16, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value)));
            public static ExpressionSyntax LiteralNumeric(ushort value) => CastExpression(Types.UInt16, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value)));
            public static ExpressionSyntax LiteralNumeric(int value) => CastExpression(Types.Int32, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value)));
            public static ExpressionSyntax LiteralNumeric(uint value) => CastExpression(Types.UInt32, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value)));
            public static ExpressionSyntax LiteralNumeric(long value) => CastExpression(Types.Int64, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value)));
            public static ExpressionSyntax LiteralNumeric(ulong value) => CastExpression(Types.UInt64, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value)));

            public static MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax target, string memberName) =>
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, target, IdentifierName(memberName));

            public static ExpressionSyntax Index(ExpressionSyntax target, ExpressionSyntax index) =>
                ElementAccessExpression(target, BracketedArgumentList(SingletonSeparatedList(Argument(index))));

            public static ExpressionSyntax Cast(ITypeSymbol typeSymbol, ExpressionSyntax expression) =>
                CastExpression(Types.FromSymbol(typeSymbol), expression);

            public static ExpressionSyntax SizeOf(ITypeSymbol typeSymbol) =>
                SizeOfExpression(Types.FromSymbol(typeSymbol));

            public static InvocationExpressionSyntax MethodCall(ExpressionSyntax target, string methodName, params ExpressionSyntax[] arguments) =>
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        target,
                        IdentifierName(methodName)),
                    ArgumentList(SeparatedList(arguments.Select(Argument))));

            public static ExpressionSyntax LessThan(ExpressionSyntax left, ExpressionSyntax right) =>
                BinaryExpression(SyntaxKind.LessThanExpression, left, right);

            public static class Array
            {
                public static ExpressionSyntax Length(ExpressionSyntax array) => MemberAccess(array, Names.Properties.Array.Length);
            }

            public static class IEnumerable
            {
                public static ExpressionSyntax Sum(ExpressionSyntax enumerable, IdentifierNameSyntax enumeratorName, ExpressionSyntax selector) =>
                    MethodCall(
                        enumerable,
                        Names.Methods.IEnumerable.Sum,
                        SimpleLambdaExpression(
                            Parameter(enumeratorName.Identifier),
                            selector));
            }

            public static class SequenceReader
            {
                public static ExpressionSyntax Consumed(ExpressionSyntax reader) => MemberAccess(reader, Names.Properties.SequenceReader.Consumed);
                public static ExpressionSyntax Advance(ExpressionSyntax reader, ExpressionSyntax count) => MethodCall(reader, Names.Methods.SequenceReader.Advance, count);

                public static ExpressionSyntax Read(ExpressionSyntax reader) => MethodCall(reader, Names.Methods.SequenceReader.Read);
                public static ExpressionSyntax ReadUInt16LittleEndian(ExpressionSyntax reader) => MethodCall(reader, Names.Methods.SequenceReader.ReadUInt16LittleEndian);
                public static ExpressionSyntax ReadUInt16BigEndian(ExpressionSyntax reader) => MethodCall(reader, Names.Methods.SequenceReader.ReadUInt16BigEndian);
                public static ExpressionSyntax ReadInt16LittleEndian(ExpressionSyntax reader) => MethodCall(reader, Names.Methods.SequenceReader.ReadInt16LittleEndian);
                public static ExpressionSyntax ReadInt16BigEndian(ExpressionSyntax reader) => MethodCall(reader, Names.Methods.SequenceReader.ReadInt16BigEndian);
                public static ExpressionSyntax ReadUInt32LittleEndian(ExpressionSyntax reader) => MethodCall(reader, Names.Methods.SequenceReader.ReadUInt32LittleEndian);
                public static ExpressionSyntax ReadUInt32BigEndian(ExpressionSyntax reader) => MethodCall(reader, Names.Methods.SequenceReader.ReadUInt32BigEndian);
                public static ExpressionSyntax ReadInt32LittleEndian(ExpressionSyntax reader) => MethodCall(reader, Names.Methods.SequenceReader.ReadInt32LittleEndian);
                public static ExpressionSyntax ReadInt32BigEndian(ExpressionSyntax reader) => MethodCall(reader, Names.Methods.SequenceReader.ReadInt32BigEndian);
                public static ExpressionSyntax ReadUInt64LittleEndian(ExpressionSyntax reader) => MethodCall(reader, Names.Methods.SequenceReader.ReadUInt64LittleEndian);
                public static ExpressionSyntax ReadUInt64BigEndian(ExpressionSyntax reader) => MethodCall(reader, Names.Methods.SequenceReader.ReadUInt64BigEndian);
                public static ExpressionSyntax ReadInt64LittleEndian(ExpressionSyntax reader) => MethodCall(reader, Names.Methods.SequenceReader.ReadInt64LittleEndian);
                public static ExpressionSyntax ReadInt64BigEndian(ExpressionSyntax reader) => MethodCall(reader, Names.Methods.SequenceReader.ReadInt64BigEndian);
            }

            public static class SpanWriter
            {
                public static ExpressionSyntax Index(ExpressionSyntax writer) => MemberAccess(writer, Names.Properties.SpanWriter.Index);
                public static ExpressionSyntax Advance(ExpressionSyntax writer, ExpressionSyntax count) => MethodCall(writer, Names.Methods.SpanWriter.Advance, count);

                public static ExpressionSyntax Write(ExpressionSyntax writer, ExpressionSyntax value) => MethodCall(writer, Names.Methods.SpanWriter.Write, value);
                public static ExpressionSyntax WriteUInt16LittleEndian(ExpressionSyntax writer, ExpressionSyntax value) => MethodCall(writer, Names.Methods.SpanWriter.WriteUInt16LittleEndian, value);
                public static ExpressionSyntax WriteUInt16BigEndian(ExpressionSyntax writer, ExpressionSyntax value) => MethodCall(writer, Names.Methods.SpanWriter.WriteUInt16BigEndian, value);
                public static ExpressionSyntax WriteInt16LittleEndian(ExpressionSyntax writer, ExpressionSyntax value) => MethodCall(writer, Names.Methods.SpanWriter.WriteInt16LittleEndian, value);
                public static ExpressionSyntax WriteInt16BigEndian(ExpressionSyntax writer, ExpressionSyntax value) => MethodCall(writer, Names.Methods.SpanWriter.WriteInt16BigEndian, value);
                public static ExpressionSyntax WriteUInt32LittleEndian(ExpressionSyntax writer, ExpressionSyntax value) => MethodCall(writer, Names.Methods.SpanWriter.WriteUInt32LittleEndian, value);
                public static ExpressionSyntax WriteUInt32BigEndian(ExpressionSyntax writer, ExpressionSyntax value) => MethodCall(writer, Names.Methods.SpanWriter.WriteUInt32BigEndian, value);
                public static ExpressionSyntax WriteInt32LittleEndian(ExpressionSyntax writer, ExpressionSyntax value) => MethodCall(writer, Names.Methods.SpanWriter.WriteInt32LittleEndian, value);
                public static ExpressionSyntax WriteInt32BigEndian(ExpressionSyntax writer, ExpressionSyntax value) => MethodCall(writer, Names.Methods.SpanWriter.WriteInt32BigEndian, value);
                public static ExpressionSyntax WriteUInt64LittleEndian(ExpressionSyntax writer, ExpressionSyntax value) => MethodCall(writer, Names.Methods.SpanWriter.WriteUInt64LittleEndian, value);
                public static ExpressionSyntax WriteUInt64BigEndian(ExpressionSyntax writer, ExpressionSyntax value) => MethodCall(writer, Names.Methods.SpanWriter.WriteUInt64BigEndian, value);
                public static ExpressionSyntax WriteInt64LittleEndian(ExpressionSyntax writer, ExpressionSyntax value) => MethodCall(writer, Names.Methods.SpanWriter.WriteInt64LittleEndian, value);
                public static ExpressionSyntax WriteInt64BigEndian(ExpressionSyntax writer, ExpressionSyntax value) => MethodCall(writer, Names.Methods.SpanWriter.WriteInt64BigEndian, value);
            }

            public static class IBinarySerializable
            {
                // target.GetSize();
                public static ExpressionSyntax GetSize(ExpressionSyntax target) => MethodCall(target, Names.Methods.IBinarySerializable.GetSize);

                // target.Read(ref reader);
                public static ExpressionSyntax Read(ExpressionSyntax target, ExpressionSyntax reader) =>
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            target,
                            IdentifierName(Names.Methods.IBinarySerializable.Read)),
                        ArgumentList(SingletonSeparatedList(Argument(reader).WithRefKindKeyword(Token(SyntaxKind.RefKeyword)))));

                // target.Write(ref writer);
                public static ExpressionSyntax Write(ExpressionSyntax target, ExpressionSyntax writer) =>
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            target,
                            IdentifierName(Names.Methods.IBinarySerializable.Write)),
                        ArgumentList(SingletonSeparatedList(Argument(writer).WithRefKindKeyword(Token(SyntaxKind.RefKeyword)))));
            }
        }

        public static class Statements
        {
            public static StatementSyntax DeclareVariable(TypeSyntax type, IdentifierNameSyntax value, ExpressionSyntax initial) =>
                LocalDeclarationStatement(
                    VariableDeclaration(type).WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(value.Identifier)
                                .WithInitializer(
                                    EqualsValueClause(initial)))));

            public static StatementSyntax For(IdentifierNameSyntax iterator, ExpressionSyntax initial, ExpressionSyntax condition, ExpressionSyntax step, StatementSyntax body) =>
                ForStatement(
                    VariableDeclaration(
                        IdentifierName("var"),
                        SingletonSeparatedList(
                            VariableDeclarator(iterator.Identifier).WithInitializer(
                                EqualsValueClause(initial)))),
                    default,
                    condition,
                    SingletonSeparatedList(step),
                    body);
        }
    }
}
