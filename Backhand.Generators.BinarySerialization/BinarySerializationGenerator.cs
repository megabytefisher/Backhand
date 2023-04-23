using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Backhand.Generators.BinarySerialization
{
    [Generator]
    public class BinarySerializationGenerator : IIncrementalGenerator
    {
        private const string GenerateBinarySerializationAttribute =
            "Backhand.Common.BinarySerialization.GenerateBinarySerializationAttribute";
        private const string BinarySerializeAttribute =
            "Backhand.Common.BinarySerialization.BinarySerializeAttribute";
        private const string IBinarySerializable =
            "Backhand.Common.BinarySerialization.IBinarySerializable";

        private static readonly ExpressionSyntax Literal0 = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0));
        private static readonly ExpressionSyntax Literal1 = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1));

        private static readonly ExpressionSyntax Literal0b = CastExpression(
            ParseTypeName("byte"),
            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)));

        private static readonly ExpressionSyntax Literal1b = CastExpression(
            ParseTypeName("byte"),
            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)));

        private static readonly ExpressionSyntax LiteralFalse =
            LiteralExpression(SyntaxKind.FalseLiteralExpression);
        private static readonly ExpressionSyntax LiteralTrue =
            LiteralExpression(SyntaxKind.TrueLiteralExpression);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(m => m != null)!;

            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses
                = context.CompilationProvider.Combine(classDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses,
                (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        {
            return node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0 &&
                   c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        }

        static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes,
            SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (ClassDeclarationSyntax classDeclarationSyntax in classes)
            {
                try
                {
                    CompilationUnitSyntax generatedClass = GenerateSerializationPartialClass(
                        compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree), classDeclarationSyntax);
                    context.AddSource($"{classDeclarationSyntax.Identifier}.generated.cs",
                        generatedClass.NormalizeWhitespace().ToFullString());
                }
                catch (Exception ex)
                {
                    Debugger.Launch();
                }
            }
        }

        static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            if (!(context.Node is ClassDeclarationSyntax classDeclarationSyntax))
                return null;

            foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    string fullName = GetAttributeFullName(context.SemanticModel, attributeSyntax);

                    if (fullName == GenerateBinarySerializationAttribute)
                    {
                        return classDeclarationSyntax;
                    }
                }
            }

            return null;
        }

        static CompilationUnitSyntax GenerateSerializationPartialClass(SemanticModel semanticModel,
            ClassDeclarationSyntax sourceClassDeclaration)
        {
            NamespaceDeclarationSyntax? sourceNamespace =
                GetParentSyntax<NamespaceDeclarationSyntax>(sourceClassDeclaration);

            if (sourceNamespace == null)
            {
                throw new InvalidOperationException("Source class must be in a namespace");
            }

            BinarySerializationGeneratorContext context = new();

            // Get GenerateBinarySerializationAttribute from source class
            AttributeSyntax generateBinarySerializationAttribute = sourceClassDeclaration.AttributeLists
                .SelectMany(a => a.Attributes)
                .First(a => GetAttributeFullName(semanticModel, a) == GenerateBinarySerializationAttribute);

            // Get Endian property from GenerateBinarySerializationAttribute
            AttributeArgumentSyntax? endianArgument = generateBinarySerializationAttribute.ArgumentList?.Arguments
                .FirstOrDefault(a => a.NameEquals!.Name.Identifier.Text == "Endian");

            if (endianArgument != null)
            {
                // Get Endian value from Endian property
                ExpressionSyntax endianExpression = endianArgument.Expression;

                // Check if Endian.Big
                bool isLittleEndian = endianExpression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Little" };
                context.IsBigEndian = !isLittleEndian;
            }

            // Get MinimumLengthProperty property from GenerateBinarySerializationAttribute
            AttributeArgumentSyntax? minimumLengthPropertyArgument = generateBinarySerializationAttribute
                .ArgumentList?.Arguments
                .FirstOrDefault(a => a.NameEquals!.Name.Identifier.Text == "MinimumLengthProperty");

            if (minimumLengthPropertyArgument != null)
            {
                // Get MinimumLengthProperty value from MinimumLengthProperty property
                ExpressionSyntax minimumLengthPropertyExpression = minimumLengthPropertyArgument.Expression;

                // Expression should be a compile time constant - find the value.
                string? minimumLengthProperty = (string?)semanticModel.GetConstantValue(minimumLengthPropertyExpression).Value;

                if (minimumLengthProperty != null)
                {
                    context.MinimumLengthExpression = IdentifierName(minimumLengthProperty);
                }
            }

            // Get properties of source class that are marked with BinarySerializedAttribute
            IEnumerable<PropertyDeclarationSyntax> binarySerializedProperties = sourceClassDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(p => p.AttributeLists.Count > 0)
                .Where(p =>
                    p.AttributeLists.Any(a =>
                        a.Attributes.Any(at => GetAttributeFullName(semanticModel, at) == BinarySerializeAttribute)));

            CompilationUnitSyntax compilationUnit = CompilationUnit();

            // Add using statements
            compilationUnit = compilationUnit.AddUsings(
                UsingDirective(ParseName("System")),
                UsingDirective(ParseName("System.Buffers")),
                UsingDirective(ParseName("System.Linq")),
                UsingDirective(ParseName("Backhand.Common.Buffers")));

            // Use same namespace as source class
            NamespaceDeclarationSyntax namespaceDeclaration = NamespaceDeclaration(sourceNamespace.Name);

            // Create partial class declaration with matching name and access modifiers
            ClassDeclarationSyntax classDeclaration = ClassDeclaration(sourceClassDeclaration.Identifier)
                .WithModifiers(sourceClassDeclaration.Modifiers);

            classDeclaration = classDeclaration.AddMembers(
                GenerateGetSizeMethod(context, semanticModel, binarySerializedProperties),
                GenerateWriteMethod(context, semanticModel, binarySerializedProperties),
                GenerateReadMethod(context, semanticModel, binarySerializedProperties));

            namespaceDeclaration = namespaceDeclaration.AddMembers(classDeclaration);

            compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);

            return compilationUnit;
        }

        static MethodDeclarationSyntax GenerateGetSizeMethod(
            BinarySerializationGeneratorContext context,
            SemanticModel semanticModel,
            IEnumerable<PropertyDeclarationSyntax> binarySerializedProperties)
        {
            // Generate a method which returns the size of the serialized object
            List<StatementSyntax> statements = new()
            {
                // int size = 0
                LocalDeclarationStatement(VariableDeclaration(ParseTypeName("int"))
                    .WithVariables(SingletonSeparatedList(VariableDeclarator("size")
                        .WithInitializer(EqualsValueClause(
                            LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                Literal(0)))))))
            };

            foreach (PropertyDeclarationSyntax propertyDeclarationSyntax in binarySerializedProperties)
            {
                // Resolve property type symbol
                ITypeSymbol? propertyTypeSymbol = semanticModel.GetTypeInfo(propertyDeclarationSyntax.Type).Type;

                if (propertyTypeSymbol == null)
                {
                    throw new InvalidOperationException("Could not resolve property type symbol");
                }

                // Check if property type is a primitive
                ExpressionSyntax? primitiveSizeExpression = GeneratePrimitiveSizeExpression(propertyTypeSymbol);
                if (primitiveSizeExpression != null)
                {
                    // size += sizeof(type)
                    statements.Add(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.AddAssignmentExpression,
                                IdentifierName("size"),
                                primitiveSizeExpression
                            )
                        )
                    );
                    continue;
                }

                // Check if property type implements IBinarySerializable
                if (propertyTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == IBinarySerializable))
                {
                    // size += property.GetSize()
                    statements.Add(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.AddAssignmentExpression,
                                IdentifierName("size"),
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(propertyDeclarationSyntax.Identifier),
                                        IdentifierName("GetSize"))))));

                    continue;
                }

                // Check if property type is an array
                if (propertyTypeSymbol is IArrayTypeSymbol arrayTypeSymbol)
                {
                    // Get array element type
                    ITypeSymbol arrayElementTypeSymbol = arrayTypeSymbol.ElementType;

                    // Check if array element type is a primitive
                    primitiveSizeExpression = GeneratePrimitiveSizeExpression(arrayElementTypeSymbol);
                    if (primitiveSizeExpression != null)
                    {
                        // size += sizeof(type) * array.Length
                        statements.Add(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.AddAssignmentExpression,
                                    IdentifierName("size"),
                                    BinaryExpression(
                                        SyntaxKind.MultiplyExpression,
                                        primitiveSizeExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(propertyDeclarationSyntax.Identifier),
                                            IdentifierName("Length"))))));

                        continue;
                    }

                    // Check if array element type is IBinarySerializable
                    if (arrayElementTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == IBinarySerializable))
                    {
                        // size += array.Sum(e => e.GetSize())
                        statements.Add(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.AddAssignmentExpression,
                                    IdentifierName("size"),
                                    InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName(propertyDeclarationSyntax.Identifier),
                                                IdentifierName("Sum")))
                                        .WithArgumentList(
                                            ArgumentList(
                                                SingletonSeparatedList(
                                                    Argument(
                                                        SimpleLambdaExpression(
                                                            Parameter(
                                                                Identifier("e")),
                                                            InvocationExpression(
                                                                MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    IdentifierName("e"),
                                                                    IdentifierName("GetSize")))))))))));
                    }

                    continue;
                }
            }

            if (context.MinimumLengthExpression != null)
            {
                // if (size < MinimumLength) return MinimumLength
                statements.Add(
                    IfStatement(
                        BinaryExpression(
                            SyntaxKind.LessThanExpression,
                            IdentifierName("size"),
                            context.MinimumLengthExpression),
                        ReturnStatement(context.MinimumLengthExpression)));
            }

            // return size
            statements.Add(ReturnStatement(IdentifierName("size")));

            MethodDeclarationSyntax getSizeMethod = MethodDeclaration(ParseTypeName("int"), "GetSize")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithBody(Block(statements));

            return getSizeMethod;
        }

        static MethodDeclarationSyntax GenerateWriteMethod(
            BinarySerializationGeneratorContext context,
            SemanticModel semanticModel,
            IEnumerable<PropertyDeclarationSyntax> binarySerializedProperties)
        {
            // Build ref SpanWriter<byte> bufferWriter parameter
            ParameterSyntax bufferWriterParameter = Parameter(Identifier("bufferWriter"))
                .WithType(ParseTypeName("SpanWriter<byte>"))
                .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)));

            // Local variables
            ExpressionSyntax startIndex = IdentifierName("startIndex");

            List<StatementSyntax> statements = new()
            {
                // int startIndex = bufferWriter.Index
                LocalDeclarationStatement(VariableDeclaration(ParseTypeName("int"))
                    .WithVariables(SingletonSeparatedList(VariableDeclarator(startIndex.ToString())
                        .WithInitializer(EqualsValueClause(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(bufferWriterParameter.Identifier),
                                IdentifierName("Index"))))))),
            };

            // Generate statements to write each property to the buffer
            foreach (PropertyDeclarationSyntax propertyDeclarationSyntax in binarySerializedProperties)
            {
                // Resolve property type symbol
                ITypeSymbol? propertyTypeSymbol = semanticModel.GetTypeInfo(propertyDeclarationSyntax.Type).Type;
                if (propertyTypeSymbol == null)
                {
                    throw new InvalidOperationException("Could not resolve property type symbol");
                }

                // Check if we can write as a primitive
                ExpressionStatementSyntax? primitiveWriteExpression =
                    GeneratePrimitiveWriteExpression(context, propertyTypeSymbol,
                        IdentifierName(propertyDeclarationSyntax.Identifier),
                        IdentifierName(bufferWriterParameter.Identifier));

                if (primitiveWriteExpression != null)
                {
                    statements.Add(primitiveWriteExpression);
                    continue;
                }

                // Check if property type implements IBinarySerializable
                if (propertyTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == IBinarySerializable))
                {
                    // property.Write(ref bufferWriter)
                    statements.Add(
                        ExpressionStatement(
                            InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(propertyDeclarationSyntax.Identifier),
                                        IdentifierName("Write")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                    IdentifierName(bufferWriterParameter.Identifier))
                                                .WithRefKindKeyword(Token(SyntaxKind.RefKeyword)))))));
                    continue;
                }

                // Check if property type is an array
                if (propertyTypeSymbol is IArrayTypeSymbol arrayTypeSymbol)
                {
                    // Get array element type
                    ITypeSymbol arrayElementTypeSymbol = arrayTypeSymbol.ElementType;

                    // Check if array element type is a primitive
                    primitiveWriteExpression =
                        GeneratePrimitiveWriteExpression(
                            context,
                            arrayElementTypeSymbol,
                            IdentifierName("e"),
                            IdentifierName(bufferWriterParameter.Identifier));

                    if (primitiveWriteExpression != null)
                    {
                        // foreach (var e in array)
                        // {
                        //     bufferWriter.Write(e);
                        // }
                        statements.Add(
                            ForEachStatement(
                                ParseTypeName(arrayElementTypeSymbol.ToDisplayString()),
                                Identifier("e"),
                                IdentifierName(propertyDeclarationSyntax.Identifier),
                                Block(primitiveWriteExpression)));
                        continue;
                    }

                    // Check if array element type is IBinarySerializable
                    if (arrayElementTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == IBinarySerializable))
                    {
                        // foreach (var e in array)
                        // {
                        //     e.Write(ref bufferWriter);
                        // }
                        statements.Add(
                            ForEachStatement(
                                ParseTypeName(arrayElementTypeSymbol.ToDisplayString()),
                                Identifier("e"),
                                IdentifierName(propertyDeclarationSyntax.Identifier),
                                Block(
                                    ExpressionStatement(
                                        InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("e"),
                                                    IdentifierName("Write")))
                                            .WithArgumentList(
                                                ArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(
                                                                IdentifierName(bufferWriterParameter
                                                                    .Identifier))
                                                            .WithRefKindKeyword(
                                                                Token(SyntaxKind.RefKeyword)))))))));
                    }

                    continue;
                }
            }

            if (context.MinimumLengthExpression != null)
            {
                // writtenLength = bufferWriter.Index - startIndex
                // remainingLength = MinimumLength - writtenLength
                // if (writtenLength < MinimumLength) bufferWriter.Advance(RemainingLength)
                ExpressionSyntax writtenLength = ParenthesizedExpression(
                    BinaryExpression(
                        SyntaxKind.SubtractExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(bufferWriterParameter.Identifier),
                            IdentifierName("Index")),
                        startIndex));
                ExpressionSyntax remainingLength = ParenthesizedExpression(
                    BinaryExpression(
                        SyntaxKind.SubtractExpression,
                        context.MinimumLengthExpression,
                        writtenLength));

                statements.Add(
                    IfStatement(
                        BinaryExpression(
                            SyntaxKind.LessThanExpression,
                            writtenLength,
                            context.MinimumLengthExpression),
                        ExpressionStatement(
                            InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(bufferWriterParameter.Identifier),
                                        IdentifierName("Advance")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(remainingLength)))))));
            }

            MethodDeclarationSyntax writeMethod = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "Write")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(bufferWriterParameter)
                .WithBody(Block(statements));

            return writeMethod;
        }

        static MethodDeclarationSyntax GenerateReadMethod(
            BinarySerializationGeneratorContext context,
            SemanticModel semanticModel,
            IEnumerable<PropertyDeclarationSyntax> binarySerializedProperties)
        {
            //Debugger.Launch();
        
            // Build ref SequenceReader<byte> bufferReader parameter
            ParameterSyntax bufferReaderParameter = Parameter(Identifier("bufferReader"))
                .WithType(ParseTypeName("SequenceReader<byte>"))
                .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)));

            // Local variables
            ExpressionSyntax startOffset = IdentifierName("startOffset");

            List<StatementSyntax> statements = new()
            {
                // long startOffset = bufferReader.Consumed
                LocalDeclarationStatement(VariableDeclaration(ParseTypeName("long"))
                    .WithVariables(SingletonSeparatedList(VariableDeclarator(startOffset.ToString())
                        .WithInitializer(EqualsValueClause(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(bufferReaderParameter.Identifier),
                                IdentifierName("Consumed"))))))),
            };

            // Generate statements to read each property from the buffer
            foreach (PropertyDeclarationSyntax propertyDeclarationSyntax in binarySerializedProperties)
            {
                // Resolve property type symbol
                ITypeSymbol? propertyTypeSymbol = semanticModel.GetTypeInfo(propertyDeclarationSyntax.Type).Type;
                if (propertyTypeSymbol == null)
                {
                    throw new InvalidOperationException("Could not resolve property type symbol");
                }

                // Check if we can read as a primitive
                ExpressionStatementSyntax? primitiveReadExpression =
                    GeneratePrimitiveReadExpression(context, propertyTypeSymbol, IdentifierName(propertyDeclarationSyntax.Identifier), IdentifierName(bufferReaderParameter.Identifier));

                if (primitiveReadExpression != null)
                {
                    statements.Add(primitiveReadExpression);
                    continue;
                }

                // Check if property type implements IBinarySerializable
                if (propertyTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == IBinarySerializable))
                {
                    // property.Read(ref bufferReader)
                    statements.Add(
                        ExpressionStatement(
                            InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(propertyDeclarationSyntax.Identifier),
                                        IdentifierName("Read")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                    IdentifierName(bufferReaderParameter.Identifier))
                                                .WithRefKindKeyword(Token(SyntaxKind.RefKeyword)))))));
                    continue;
                }

                // Check if property type is an array
                if (propertyTypeSymbol is IArrayTypeSymbol arrayTypeSymbol)
                {
                    // Get array element type
                    ITypeSymbol arrayElementTypeSymbol = arrayTypeSymbol.ElementType;

                    // Check if array element type is a primitive
                    // (write to property[i])
                    primitiveReadExpression = GeneratePrimitiveReadExpression(
                        context,
                        arrayElementTypeSymbol,
                        ElementAccessExpression(
                            IdentifierName(propertyDeclarationSyntax.Identifier),
                            BracketedArgumentList(
                                SingletonSeparatedList(
                                    Argument(IdentifierName("i"))))),
                        IdentifierName(bufferReaderParameter.Identifier));
                        

                    if (primitiveReadExpression != null)
                    {
                        // for (int i = 0; i < array.Length; i++)
                        // {
                        //     property[i] = bufferReader.Read();
                        // }
                        statements.Add(
                            GenerateRepeatBlock(
                                "i",
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(propertyDeclarationSyntax.Identifier),
                                    IdentifierName("Length")),
                                Block(primitiveReadExpression)));
                        continue;
                    }

                    // Check if array element type is IBinarySerializable
                    if (arrayElementTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == IBinarySerializable))
                    {
                        // foreach (var e in array)
                        // {
                        //     e.Read(ref bufferWriter);
                        // }
                        statements.Add(
                            ForEachStatement(
                                ParseTypeName(arrayElementTypeSymbol.ToDisplayString()),
                                Identifier("e"),
                                IdentifierName(propertyDeclarationSyntax.Identifier),
                                Block(
                                    ExpressionStatement(
                                        InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("e"),
                                                    IdentifierName("Read")))
                                            .WithArgumentList(
                                                ArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(
                                                                IdentifierName(bufferReaderParameter
                                                                    .Identifier))
                                                            .WithRefKindKeyword(
                                                                Token(SyntaxKind.RefKeyword)))))))));
                    }
                    continue;
                }
            }

            if (context.MinimumLengthExpression != null)
            {
                // readLength = bufferReader.Consumed - startOffset
                // remainingLength = context.MinimumLength - readLength
                // if (remainingLength > 0) bufferReader.Advance(remainingLength)
                ExpressionSyntax readLength = ParenthesizedExpression(BinaryExpression(
                    SyntaxKind.SubtractExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(bufferReaderParameter.Identifier),
                        IdentifierName("Consumed")),
                    startOffset));
                ExpressionSyntax remainingLength = ParenthesizedExpression(
                    BinaryExpression(
                        SyntaxKind.SubtractExpression,
                        context.MinimumLengthExpression,
                        readLength));

                statements.Add(
                    IfStatement(
                        BinaryExpression(
                            SyntaxKind.GreaterThanExpression,
                            remainingLength,
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(0))),
                        Block(
                            ExpressionStatement(
                                InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(bufferReaderParameter.Identifier),
                                            IdentifierName("Advance")))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(remainingLength))))))));

            }

            MethodDeclarationSyntax readMethod = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "Read")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(bufferReaderParameter)
                .WithBody(Block(statements));
            
            return readMethod;
        }

        static ExpressionSyntax? GeneratePrimitiveSizeExpression(ITypeSymbol typeSymbol)
        {
            // If typeSymbol is an enum, get the underlying type
            if (typeSymbol is INamedTypeSymbol { EnumUnderlyingType: not null } namedTypeSymbol)
            {
                typeSymbol = namedTypeSymbol.EnumUnderlyingType;
            }

            switch (typeSymbol.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Int64:
                    return SizeOfExpression(ParseTypeName(typeSymbol.ToDisplayString()));
                default:
                    return null;
            }
        }

        // Generate: e.g. bufferWriter.Write(value);
        static ExpressionStatementSyntax? GeneratePrimitiveWriteExpression(
            BinarySerializationGeneratorContext context,
            ITypeSymbol typeSymbol,
            ExpressionSyntax value,
            ExpressionSyntax bufferWriter)
        {
            // If typeSymbol is an enum, get the underlying type
            if (typeSymbol is INamedTypeSymbol { EnumUnderlyingType: not null } namedTypeSymbol)
            {
                typeSymbol = namedTypeSymbol.EnumUnderlyingType;
            }
            
            return typeSymbol.SpecialType switch
            {
                SpecialType.System_Boolean =>
                    GenerateMethodCallStatement(
                        bufferWriter,
                        "Write",
                        ConditionalExpression(value, Literal1b, Literal0b)),
                SpecialType.System_Byte =>
                    GenerateMethodCallStatement(bufferWriter, "Write", CastExpression(ParseTypeName(typeSymbol.ToDisplayString()), value)),
                SpecialType.System_UInt16 => context.IsBigEndian
                    ? GenerateMethodCallStatement(bufferWriter, "WriteUInt16BigEndian", CastExpression(ParseTypeName(typeSymbol.ToDisplayString()), value))
                    : GenerateMethodCallStatement(bufferWriter, "WriteUInt16LittleEndian", CastExpression(ParseTypeName(typeSymbol.Name), value)),
                SpecialType.System_Int16 => context.IsBigEndian
                    ? GenerateMethodCallStatement(bufferWriter, "WriteInt16BigEndian", CastExpression(ParseTypeName(typeSymbol.ToDisplayString()), value))
                    : GenerateMethodCallStatement(bufferWriter, "WriteInt16LittleEndian", CastExpression(ParseTypeName(typeSymbol.Name), value)),
                SpecialType.System_UInt32 => context.IsBigEndian
                    ? GenerateMethodCallStatement(bufferWriter, "WriteUInt32BigEndian", CastExpression(ParseTypeName(typeSymbol.ToDisplayString()), value))
                    : GenerateMethodCallStatement(bufferWriter, "WriteUInt32LittleEndian", CastExpression(ParseTypeName(typeSymbol.Name), value)),
                SpecialType.System_Int32 => context.IsBigEndian
                    ? GenerateMethodCallStatement(bufferWriter, "WriteInt32BigEndian", CastExpression(ParseTypeName(typeSymbol.ToDisplayString()), value))
                    : GenerateMethodCallStatement(bufferWriter, "WriteInt32LittleEndian", CastExpression(ParseTypeName(typeSymbol.Name), value)),
                SpecialType.System_UInt64 => context.IsBigEndian
                    ? GenerateMethodCallStatement(bufferWriter, "WriteUInt64BigEndian", CastExpression(ParseTypeName(typeSymbol.ToDisplayString()), value))
                    : GenerateMethodCallStatement(bufferWriter, "WriteUInt64LittleEndian", CastExpression(ParseTypeName(typeSymbol.Name), value)),
                SpecialType.System_Int64 => context.IsBigEndian
                    ? GenerateMethodCallStatement(bufferWriter, "WriteInt64BigEndian", CastExpression(ParseTypeName(typeSymbol.ToDisplayString()), value))
                    : GenerateMethodCallStatement(bufferWriter, "WriteInt64LittleEndian", CastExpression(ParseTypeName(typeSymbol.Name), value)),
                _ => null
            };
        }

        // Generate: e.g. bufferReader.Read();
        static ExpressionStatementSyntax? GeneratePrimitiveReadExpression(
            BinarySerializationGeneratorContext context,
            ITypeSymbol typeSymbol,
            ExpressionSyntax value,
            ExpressionSyntax bufferReader)
        {
            // If typeSymbol is an enum, get the underlying type
            ITypeSymbol readType = typeSymbol;
            if (typeSymbol is INamedTypeSymbol { EnumUnderlyingType: not null } namedTypeSymbol)
            {
                readType = namedTypeSymbol.EnumUnderlyingType;
            }

            ExpressionSyntax? readExpression = readType.SpecialType switch
            {
                SpecialType.System_Boolean =>
                    ConditionalExpression(
                        ParenthesizedExpression(
                            BinaryExpression(
                                SyntaxKind.EqualsExpression,
                                GenerateMethodCallExpression(bufferReader, "Read"),
                                Literal0b)),
                        LiteralFalse,
                        LiteralTrue),
                SpecialType.System_Byte =>
                    GenerateMethodCallExpression(bufferReader, "Read"),
                SpecialType.System_UInt16 => context.IsBigEndian
                    ? GenerateMethodCallExpression(bufferReader, "ReadUInt16BigEndian")
                    : GenerateMethodCallExpression(bufferReader, "ReadUInt16LittleEndian"),
                SpecialType.System_Int16 => context.IsBigEndian
                    ? GenerateMethodCallExpression(bufferReader, "ReadInt16BigEndian")
                    : GenerateMethodCallExpression(bufferReader, "ReadInt16LittleEndian"),
                SpecialType.System_UInt32 => context.IsBigEndian
                    ? GenerateMethodCallExpression(bufferReader, "ReadUInt32BigEndian")
                    : GenerateMethodCallExpression(bufferReader, "ReadUInt32LittleEndian"),
                SpecialType.System_Int32 => context.IsBigEndian
                    ? GenerateMethodCallExpression(bufferReader, "ReadInt32BigEndian")
                    : GenerateMethodCallExpression(bufferReader, "ReadInt32LittleEndian"),
                SpecialType.System_UInt64 => context.IsBigEndian
                    ? GenerateMethodCallExpression(bufferReader, "ReadUInt64BigEndian")
                    : GenerateMethodCallExpression(bufferReader, "ReadUInt64LittleEndian"),
                SpecialType.System_Int64 => context.IsBigEndian
                    ? GenerateMethodCallExpression(bufferReader, "ReadInt64BigEndian")
                    : GenerateMethodCallExpression(bufferReader, "ReadInt64LittleEndian"),
                _ => null
            };

            return readExpression == null ? null : GenerateAssignmentStatement(value, CastExpression(ParseTypeName(typeSymbol.ToDisplayString()), readExpression));
        }

        static string GetAttributeFullName(SemanticModel semanticModel, AttributeSyntax attributeSyntax)
        {
            if (semanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
            {
                throw new InvalidOperationException("Could not get symbol info for attribute");
            }

            INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
            string fullName = attributeContainingTypeSymbol.ToDisplayString();
            return fullName;
        }

        static ExpressionStatementSyntax GenerateAssignmentStatement(ExpressionSyntax left, ExpressionSyntax right)
        {
            /*
             * left = right;
             */
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    left,
                    right));
        }

        static ExpressionStatementSyntax GenerateMethodCallStatement(ExpressionSyntax obj, string methodName, params ExpressionSyntax[] arguments)
        {
            /*
             * obj.methodName(...arguments);
             */
            ExpressionStatementSyntax methodCall =
                ExpressionStatement(GenerateMethodCallExpression(obj, methodName, arguments));

            return methodCall;
        }

        static InvocationExpressionSyntax GenerateMethodCallExpression(ExpressionSyntax obj, string methodName, params ExpressionSyntax[] arguments)
        {
            /*
             * obj.methodName(...arguments)
             */
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    obj,
                    IdentifierName(methodName)),
                ArgumentList(
                    SeparatedList(arguments.Select(Argument))));
        }

        static ForStatementSyntax GenerateRepeatBlock(string iteratorName, ExpressionSyntax count, BlockSyntax body)
        {
            /*
             * for (var iteratorName = 0; iteratorName < count; iteratorName++)
             * {
             *     ...body
             * }
             */

            return ForStatement(
                VariableDeclaration(
                    IdentifierName("var"),
                    SingletonSeparatedList(
                        VariableDeclarator(
                                Identifier(iteratorName))
                            .WithInitializer(
                                EqualsValueClause(
                                    Literal0)))),
                default,
                BinaryExpression(
                    SyntaxKind.LessThanExpression,
                    IdentifierName(iteratorName),
                    count),
                SingletonSeparatedList<ExpressionSyntax>(PostfixUnaryExpression(
                    SyntaxKind.PostIncrementExpression,
                    IdentifierName(iteratorName))),
                body);
        }

        static T? GetParentSyntax<T>(SyntaxNode? syntaxNode) where T : SyntaxNode
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

        class BinarySerializationGeneratorContext
        {
            public bool IsBigEndian { get; set; } = true;
            public ExpressionSyntax? MinimumLengthExpression { get; set; }
        }
    }
}