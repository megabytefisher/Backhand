using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Backhand.Common.BinarySerialization.Generation.Syntax;

namespace Backhand.Common.BinarySerialization.Generation
{
    [Generator]
    public class BinarySerializationGenerator : IIncrementalGenerator
    {
        static BinarySerializationGenerator()
        {
            //Debugger.Launch();
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(m => m != null)!;

            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses
                = context.CompilationProvider.Combine(classDeclarations.Collect());

            context.RegisterSourceOutput(
                compilationAndClasses,
                (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        {
            return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } c &&
                   c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
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

                    if (fullName == Names.Types.GenerateBinarySerializationAttribute)
                    {
                        return classDeclarationSyntax;
                    }
                }
            }

            return null;
        }

        static void Execute(
            Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes,
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
                    (string name, CompilationUnitSyntax result) = GenerateSerializationPartialClass(
                        compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree), classDeclarationSyntax);
                    context.AddSource(
                        $"{name}.BinarySerialization.g.cs",
                        result.NormalizeWhitespace().ToFullString());
                }
                catch (Exception ex)
                {
                    //Debugger.Launch();
                }
            }
        }

        static (string FullClassName, CompilationUnitSyntax Result) GenerateSerializationPartialClass(
            SemanticModel semanticModel,
            ClassDeclarationSyntax sourceClassDeclaration)
        {
            BaseNamespaceDeclarationSyntax? sourceNamespace =
                GetParentSyntax<BaseNamespaceDeclarationSyntax>(sourceClassDeclaration);

            if (sourceNamespace == null)
            {
                throw new InvalidOperationException("Source class must be in a namespace");
            }

            BinarySerializationGeneratorContext context = new();
            GenerateBinarySerializationAttribute sourceClassAttribute = ParseAttribute<GenerateBinarySerializationAttribute>(semanticModel, sourceClassDeclaration) ??
                                                                        throw new InvalidOperationException("Source class did not have GenerateBinarySerializationAttribute");

            context.Endian = sourceClassAttribute.Endian;
            if (!string.IsNullOrEmpty(sourceClassAttribute.MinimumLengthProperty)) context.MinimumLengthExpression = IdentifierName(sourceClassAttribute.MinimumLengthProperty);

            // Get properties of source class that are marked with BinarySerializedAttribute
            IEnumerable<PropertyDeclarationSyntax> binarySerializedProperties = sourceClassDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(p => p.AttributeLists.Count > 0)
                .Where(p =>
                    p.AttributeLists.Any(a =>
                        a.Attributes.Any(at => GetAttributeFullName(semanticModel, at) == Names.Types.BinarySerializeAttribute)));

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

            string fullName = $"{sourceNamespace.Name}.{sourceClassDeclaration.Identifier}";
            return (fullName, compilationUnit);
        }

        static MethodDeclarationSyntax GenerateGetSizeMethod(
            BinarySerializationGeneratorContext context,
            SemanticModel semanticModel,
            IEnumerable<PropertyDeclarationSyntax> binarySerializedProperties)
        {
            // Generate a method which returns the size of the serialized object

            // Local variables
            IdentifierNameSyntax size = IdentifierName("size");

            // Method statements
            List<StatementSyntax> statements = new()
            {
                // int size = 0
                Statements.DeclareVariable(Types.Int32, size, Expressions.Zero)
            };

            foreach (PropertyDeclarationSyntax propertyDeclarationSyntax in binarySerializedProperties)
            {
                ExpressionSyntax property = IdentifierName(propertyDeclarationSyntax.Identifier);

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
                                size,
                                primitiveSizeExpression
                            )
                        )
                    );
                    continue;
                }

                // Check if property type implements IBinarySerializable
                if (propertyTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == Names.Types.IBinarySerializable))
                {
                    // size += property.GetSize()
                    statements.Add(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.AddAssignmentExpression,
                                size,
                                Expressions.IBinarySerializable.GetSize(property))));

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
                        // size += sizeof(type) * property.Length
                        statements.Add(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.AddAssignmentExpression,
                                    size,
                                    BinaryExpression(
                                        SyntaxKind.MultiplyExpression,
                                        primitiveSizeExpression,
                                        Expressions.Array.Length(property)))));

                        continue;
                    }

                    // Check if array element type is IBinarySerializable
                    if (arrayElementTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == Names.Types.IBinarySerializable))
                    {
                        // size += array.Sum(e => e.GetSize())
                        IdentifierNameSyntax e = IdentifierName("e");

                        statements.Add(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.AddAssignmentExpression,
                                    size,
                                    Expressions.IEnumerable.Sum(property, e, Expressions.IBinarySerializable.GetSize(e)))));
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
                            size,
                            context.MinimumLengthExpression),
                        ReturnStatement(context.MinimumLengthExpression)));
            }

            // return size
            statements.Add(ReturnStatement(IdentifierName("size")));

            MethodDeclarationSyntax getSizeMethod = MethodDeclaration(ParseTypeName("int"), Names.Methods.IBinarySerializable.GetSize)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithBody(Block(statements));

            return getSizeMethod;
        }

        static MethodDeclarationSyntax GenerateWriteMethod(
            BinarySerializationGeneratorContext context,
            SemanticModel semanticModel,
            IEnumerable<PropertyDeclarationSyntax> binarySerializedProperties)
        {
            // Arguments
            IdentifierNameSyntax bufferWriter = IdentifierName("bufferWriter");

            // Local variables
            IdentifierNameSyntax startIndex = IdentifierName("startIndex");

            List<StatementSyntax> statements = new()
            {
                // int startIndex = bufferWriter.Index
                Statements.DeclareVariable(Types.Int32, startIndex, Expressions.SpanWriter.Index(bufferWriter))
            };

            // Generate statements to write each property to the buffer
            foreach (PropertyDeclarationSyntax propertyDeclarationSyntax in binarySerializedProperties)
            {
                ExpressionSyntax property = IdentifierName(propertyDeclarationSyntax.Identifier);

                // Resolve property type symbol
                ITypeSymbol? propertyTypeSymbol = semanticModel.GetTypeInfo(propertyDeclarationSyntax.Type).Type;
                if (propertyTypeSymbol == null)
                {
                    throw new InvalidOperationException("Could not resolve property type symbol");
                }

                // Check if we can write as a primitive
                ExpressionStatementSyntax? primitiveWriteStatement =
                    GeneratePrimitiveWriteStatement(
                        context,
                        propertyTypeSymbol,
                        property,
                        bufferWriter);

                if (primitiveWriteStatement != null)
                {
                    statements.Add(primitiveWriteStatement);
                    continue;
                }

                // Check if property type implements IBinarySerializable
                if (propertyTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == Names.Types.IBinarySerializable))
                {
                    // property.Write(ref bufferWriter)
                    statements.Add(ExpressionStatement(Expressions.IBinarySerializable.Write(property, bufferWriter)));
                    continue;
                }

                // Check if property type is an array
                if (propertyTypeSymbol is IArrayTypeSymbol arrayTypeSymbol)
                {
                    IdentifierNameSyntax i = IdentifierName("i");
                    ExpressionSyntax propertyElement = Expressions.Index(property, i);
                    ExpressionSyntax propertyLength = Expressions.Array.Length(property);

                    // Get array element type
                    ITypeSymbol arrayElementTypeSymbol = arrayTypeSymbol.ElementType;

                    // Check if array element type is a primitive
                    // 
                    primitiveWriteStatement =
                        GeneratePrimitiveWriteStatement(
                            context,
                            arrayElementTypeSymbol,
                            propertyElement,
                            bufferWriter);

                    if (primitiveWriteStatement != null)
                    {
                        // for (int i = 0; i < property.Length; i++)
                        // {
                        //     bufferWriter.Write(property[i]);
                        // }
                        statements.Add(
                            Statements.For(
                                i,
                                Expressions.Zero,
                                Expressions.LessThan(i, propertyLength),
                                PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, i),
                                primitiveWriteStatement));
                        continue;
                    }

                    // Check if array element type is IBinarySerializable
                    if (arrayElementTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == Names.Types.IBinarySerializable))
                    {
                        // foreach (int i = 0; i < property.Length; i++)
                        // {
                        //     property[i].Write(ref bufferWriter);
                        // }
                        statements.Add(
                            Statements.For(
                                i,
                                Expressions.Zero,
                                Expressions.LessThan(i, propertyLength),
                                PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, i),
                                ExpressionStatement(Expressions.IBinarySerializable.Write(propertyElement, bufferWriter))));
                    }

                    continue;
                }
            }

            if (context.MinimumLengthExpression != null)
            {
                // writtenLength = bufferWriter.Index - startIndex
                // remainingLength = MinimumLength - writtenLength
                // if (remainingLength > 0) bufferWriter.Advance(remainingLength)
                ExpressionSyntax writtenLength =
                    ParenthesizedExpression(
                        BinaryExpression(
                            SyntaxKind.SubtractExpression,
                            Expressions.SpanWriter.Index(bufferWriter),
                            startIndex));
                ExpressionSyntax remainingLength =
                    ParenthesizedExpression(
                        BinaryExpression(
                            SyntaxKind.SubtractExpression,
                            context.MinimumLengthExpression,
                            writtenLength));

                statements.Add(
                    IfStatement(
                        BinaryExpression(
                            SyntaxKind.GreaterThanExpression,
                            remainingLength,
                            Expressions.Zero),
                        ExpressionStatement(Expressions.SpanWriter.Advance(bufferWriter, remainingLength))));
            }

            // Build ref SpanWriter<byte> bufferWriter parameter
            ParameterSyntax bufferWriterParameter =
                Parameter(bufferWriter.Identifier)
                    .WithType(Types.ByteSpanWriter)
                    .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)));

            MethodDeclarationSyntax writeMethod =
                MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Names.Methods.IBinarySerializable.Write)
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
            // Arguments
            IdentifierNameSyntax bufferReader = IdentifierName("bufferReader");

            // Local variables
            IdentifierNameSyntax startOffset = IdentifierName("startOffset");

            List<StatementSyntax> statements = new()
            {
                // long startOffset = bufferReader.Consumed
                Statements.DeclareVariable(Types.Int64, startOffset, Expressions.SequenceReader.Consumed(bufferReader))
            };

            // Generate statements to read each property from the buffer
            foreach (PropertyDeclarationSyntax propertyDeclarationSyntax in binarySerializedProperties)
            {
                ExpressionSyntax property = IdentifierName(propertyDeclarationSyntax.Identifier);

                // Resolve property type symbol
                ITypeSymbol? propertyTypeSymbol = semanticModel.GetTypeInfo(propertyDeclarationSyntax.Type).Type;
                if (propertyTypeSymbol == null)
                {
                    throw new InvalidOperationException("Could not resolve property type symbol");
                }

                // Check if we can read as a primitive
                ExpressionStatementSyntax? primitiveReadStatement =
                    GeneratePrimitiveReadStatement(
                        context,
                        propertyTypeSymbol,
                        property,
                        bufferReader);

                if (primitiveReadStatement != null)
                {
                    statements.Add(primitiveReadStatement);
                    continue;
                }

                // Check if property type implements IBinarySerializable
                if (propertyTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == Names.Types.IBinarySerializable))
                {
                    // property.Read(ref bufferReader)
                    statements.Add(ExpressionStatement(Expressions.IBinarySerializable.Read(property, bufferReader)));
                    continue;
                }

                // Check if property type is an array
                if (propertyTypeSymbol is IArrayTypeSymbol arrayTypeSymbol)
                {
                    IdentifierNameSyntax i = IdentifierName("i");
                    ExpressionSyntax propertyElement = Expressions.Index(property, i);
                    ExpressionSyntax propertyLength = Expressions.Array.Length(property);

                    // Get array element type
                    ITypeSymbol arrayElementTypeSymbol = arrayTypeSymbol.ElementType;

                    // Check if array element type is a primitive
                    primitiveReadStatement =
                        GeneratePrimitiveReadStatement(
                            context,
                            arrayElementTypeSymbol,
                            propertyElement,
                            bufferReader);


                    if (primitiveReadStatement != null)
                    {
                        // for (int i = 0; i < array.Length; i++)
                        // {
                        //     property[i] = bufferReader.Read();
                        // }
                        statements.Add(
                            Statements.For(
                                i,
                                Expressions.Zero,
                                Expressions.LessThan(i, propertyLength),
                                PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, i),
                                primitiveReadStatement));
                        continue;
                    }

                    // Check if array element type is IBinarySerializable
                    if (arrayElementTypeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == Names.Types.IBinarySerializable))
                    {
                        // foreach (var e in array)
                        // {
                        //     e.Read(ref bufferWriter);
                        // }
                        statements.Add(
                            Statements.For(
                                i,
                                Expressions.Zero,
                                Expressions.LessThan(i, propertyLength),
                                PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, i),
                                ExpressionStatement(Expressions.IBinarySerializable.Read(propertyElement, bufferReader))));
                    }

                    continue;
                }
            }

            if (context.MinimumLengthExpression != null)
            {
                // readLength = bufferReader.Consumed - startOffset
                // remainingLength = context.MinimumLength - readLength
                // if (remainingLength > 0) bufferReader.Advance(remainingLength)
                ExpressionSyntax readLength =
                    ParenthesizedExpression(
                        BinaryExpression(
                            SyntaxKind.SubtractExpression,
                            Expressions.SequenceReader.Consumed(bufferReader),
                            startOffset));
                ExpressionSyntax remainingLength =
                    ParenthesizedExpression(
                        BinaryExpression(
                            SyntaxKind.SubtractExpression,
                            context.MinimumLengthExpression,
                            readLength));

                statements.Add(
                    IfStatement(
                        BinaryExpression(
                            SyntaxKind.GreaterThanExpression,
                            remainingLength,
                            Expressions.Zero),
                        ExpressionStatement(Expressions.SequenceReader.Advance(bufferReader, remainingLength))));
            }

            // Build ref SequenceReader<byte> bufferReader parameter
            ParameterSyntax bufferReaderParameter =
                Parameter(Identifier("bufferReader"))
                    .WithType(Types.ByteSequenceReader)
                    .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)));

            MethodDeclarationSyntax readMethod =
                MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Names.Methods.IBinarySerializable.Read)
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
                    return Expressions.SizeOf(typeSymbol);
                default:
                    return null;
            }
        }

        // Generate: e.g. bufferWriter.Write(value);
        static ExpressionStatementSyntax? GeneratePrimitiveWriteStatement(
            BinarySerializationGeneratorContext context,
            ITypeSymbol typeSymbol,
            ExpressionSyntax value,
            ExpressionSyntax bufferWriter)
        {
            // If typeSymbol is an enum, get the underlying type
            ITypeSymbol writeType = typeSymbol;
            if (typeSymbol is INamedTypeSymbol { EnumUnderlyingType: not null } namedTypeSymbol)
            {
                writeType = namedTypeSymbol.EnumUnderlyingType;
            }

            ExpressionSyntax? writeExpression = writeType.SpecialType switch
            {
                SpecialType.System_Boolean =>
                    Expressions.SpanWriter.Write(bufferWriter, ConditionalExpression(value, Expressions.LiteralNumeric((byte)1), Expressions.LiteralNumeric((byte)0))),
                SpecialType.System_Byte => Expressions.SpanWriter.Write(bufferWriter, Expressions.Cast(writeType, value)),
                SpecialType.System_UInt16 => context.Endian == Endian.Big
                    ? Expressions.SpanWriter.WriteUInt16BigEndian(bufferWriter, Expressions.Cast(writeType, value))
                    : Expressions.SpanWriter.WriteUInt16LittleEndian(bufferWriter, Expressions.Cast(writeType, value)),
                SpecialType.System_Int16 => context.Endian == Endian.Big
                    ? Expressions.SpanWriter.WriteInt16BigEndian(bufferWriter, Expressions.Cast(writeType, value))
                    : Expressions.SpanWriter.WriteInt16LittleEndian(bufferWriter, Expressions.Cast(writeType, value)),
                SpecialType.System_UInt32 => context.Endian == Endian.Big
                    ? Expressions.SpanWriter.WriteUInt32BigEndian(bufferWriter, Expressions.Cast(writeType, value))
                    : Expressions.SpanWriter.WriteUInt32LittleEndian(bufferWriter, Expressions.Cast(writeType, value)),
                SpecialType.System_Int32 => context.Endian == Endian.Big
                    ? Expressions.SpanWriter.WriteInt32BigEndian(bufferWriter, Expressions.Cast(writeType, value))
                    : Expressions.SpanWriter.WriteInt32LittleEndian(bufferWriter, Expressions.Cast(writeType, value)),
                SpecialType.System_UInt64 => context.Endian == Endian.Big
                    ? Expressions.SpanWriter.WriteUInt64BigEndian(bufferWriter, Expressions.Cast(writeType, value))
                    : Expressions.SpanWriter.WriteUInt64LittleEndian(bufferWriter, Expressions.Cast(writeType, value)),
                SpecialType.System_Int64 => context.Endian == Endian.Big
                    ? Expressions.SpanWriter.WriteInt64BigEndian(bufferWriter, Expressions.Cast(writeType, value))
                    : Expressions.SpanWriter.WriteInt64LittleEndian(bufferWriter, Expressions.Cast(writeType, value)),
                _ => null
            };

            if (writeExpression == null) return null;
            return ExpressionStatement(writeExpression);
        }

        // Generate: e.g. bufferReader.Read();
        static ExpressionStatementSyntax? GeneratePrimitiveReadStatement(
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
                                Expressions.SequenceReader.Read(bufferReader),
                                Expressions.LiteralNumeric((byte)0))),
                        Expressions.False,
                        Expressions.True),
                SpecialType.System_Byte => Expressions.SequenceReader.Read(bufferReader),
                SpecialType.System_UInt16 => context.Endian == Endian.Big
                    ? Expressions.SequenceReader.ReadUInt16BigEndian(bufferReader)
                    : Expressions.SequenceReader.ReadUInt16LittleEndian(bufferReader),
                SpecialType.System_Int16 => context.Endian == Endian.Big
                    ? Expressions.SequenceReader.ReadInt16BigEndian(bufferReader)
                    : Expressions.SequenceReader.ReadInt16LittleEndian(bufferReader),
                SpecialType.System_UInt32 => context.Endian == Endian.Big
                    ? Expressions.SequenceReader.ReadUInt32BigEndian(bufferReader)
                    : Expressions.SequenceReader.ReadUInt32LittleEndian(bufferReader),
                SpecialType.System_Int32 => context.Endian == Endian.Big
                    ? Expressions.SequenceReader.ReadInt32BigEndian(bufferReader)
                    : Expressions.SequenceReader.ReadInt32LittleEndian(bufferReader),
                SpecialType.System_UInt64 => context.Endian == Endian.Big
                    ? Expressions.SequenceReader.ReadUInt64BigEndian(bufferReader)
                    : Expressions.SequenceReader.ReadUInt64LittleEndian(bufferReader),
                SpecialType.System_Int64 => context.Endian == Endian.Big
                    ? Expressions.SequenceReader.ReadInt64BigEndian(bufferReader)
                    : Expressions.SequenceReader.ReadInt64LittleEndian(bufferReader),
                _ => null
            };

            if (readExpression == null) return null;
            return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, value, Expressions.Cast(typeSymbol, readExpression)));
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

        class BinarySerializationGeneratorContext
        {
            public Endian Endian { get; set; } = Endian.Big;
            public ExpressionSyntax? MinimumLengthExpression { get; set; }
        }
    }
}