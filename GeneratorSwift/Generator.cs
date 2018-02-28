using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using DotLiquid;
using GeneratorDomain;
using System.Text;

namespace GeneratorSwift
{
    public class GenerateSwift: ICodeGenerator
    {
        public DomainModel Model;
        private Template classTemplate;
        private Template enumTemplate;
        private Template mappingTemplate;
        private string outputDirectory;
        private string templateDirectory;

        public int Generate(DomainModel model, String outputDirectory, String templateDirectory)
        {
            this.outputDirectory = outputDirectory;
            this.templateDirectory = templateDirectory;
            Model = model;

            classTemplate = Template.Parse(File.ReadAllText(Path.Combine(templateDirectory,"Class.template")));
            enumTemplate = Template.Parse(File.ReadAllText(Path.Combine(templateDirectory, "Enum.template")));
            mappingTemplate = Template.Parse(File.ReadAllText(Path.Combine(templateDirectory, "Mapping.template")));

            Generate();

            return 0;
        }

        public void Generate()
        {
            Directory.CreateDirectory(outputDirectory);

            StringBuilder output = new StringBuilder();

            foreach (var domainType in Model.Types)
            {
                output.Append(
                    classTemplate.Render(new RenderParameters(CultureInfo.InvariantCulture)
                    {
                        LocalVariables = Hash.FromAnonymousObject(domainType),
                        Filters = new[] { typeof(SwiftFilters) }
                    })
                );
            }

            foreach (var domainType in Model.Enumerations)
            {
                output.Append(
                    enumTemplate.Render(new RenderParameters(CultureInfo.InvariantCulture)
                    {
                        LocalVariables = Hash.FromAnonymousObject(domainType),
                        Filters = new[] {typeof(SwiftFilters)}
                    })
                );
            }

            output.Append(
                mappingTemplate.Render(new RenderParameters(CultureInfo.InvariantCulture)
                {
                    LocalVariables = Hash.FromAnonymousObject(new {MessageTypesById = Model.MessageTypesByIdList}),
                    Filters = new[] {typeof(SwiftFilters)}
                })
                );


            File.WriteAllText(Path.Combine(outputDirectory, "DomainModel.swift"), output.ToString());
        }
    }

    public static class SwiftFilters
    {
        public static string PropertyTypeDecl(DomainProperty property)
        {
            if (property.Domain.IsEnumeration(property.TypeName))
            {
                return $"{TypeLookup(property.TypeName)}?";
            }
            if (property.Length == 0)
            {
                return TypeLookup(property.TypeName);
            }
            return $"[{TypeLookup(property.TypeName)}]";
        }

        public static string TypeLookup(string input)
        {
            switch (input)
            {
                case PrimitiveTypes.BYTE:
                    return "UInt8";
                case PrimitiveTypes.SHORT:
                    return "UInt16";
                case PrimitiveTypes.SIGNED_SHORT:
                    return "Int16";
                case PrimitiveTypes.WORD:
                    return "UInt32";
                case PrimitiveTypes.LONG:
                    return "UInt64";
                case PrimitiveTypes.FLOAT:
                    return "Float32";
                case PrimitiveTypes.STRING:
                    return "String";
                default:
                    return input;
            }
        }

        public static string WriterMethod(DomainProperty property, string streamName)
        {
            if (property.Domain.IsEnumeration(property.TypeName))
            {
                var enumeration = property.Domain.Enumeration(property.TypeName);
                return TypeWriterMethod(enumeration.BaseType, $"({property.Name}?.rawValue ?? 0)", 0, streamName);
            }
            if (property.Length == 0)
            {
                return TypeWriterMethod(property.TypeName, property.Name, property.Size, streamName);
            }

            return
                $"{streamName}.writeArray(value: {property.Name}, writer:{{{property.Name}, stream in return {TypeWriterMethod(property.TypeName, property.Name, property.Size, streamName)}}})";
        }

        public static string ReaderMethod(DomainProperty property, string streamName)
        {
            if (property.Domain.IsEnumeration(property.TypeName))
            {
                var enumeration = property.Domain.Enumeration(property.TypeName);
                return $"{property.TypeName}(rawValue: {TypeReaderMethod(enumeration.BaseType, 0, streamName)})";
            }
            if (property.Length == 0)
            {
                return TypeReaderMethod(property.TypeName, property.Size, streamName);
            }
            return
                $"try {streamName}.readArray(size: {property.Length}, generator:{{stream in return {TypeReaderMethod(property.TypeName, property.Size, streamName)}}})";
        }

        private static string TypeReaderMethod(string propertyTypeName, int propertySize, string streamName)
        {
            switch (propertyTypeName)
            {
                case PrimitiveTypes.BYTE:
                    return $"try {streamName}.readByte()";
                case PrimitiveTypes.SHORT:
                    return $"try {streamName}.readShort()";
                case PrimitiveTypes.SIGNED_SHORT:
                    return $"try {streamName}.readSignedShort()";    
                case PrimitiveTypes.WORD:
                    return $"try {streamName}.readWord()";
                case PrimitiveTypes.LONG:
                    return $"try {streamName}.readLong()";
                case PrimitiveTypes.FLOAT:
                    return $"try {streamName}.readFloat()";
                case PrimitiveTypes.STRING:
                    return $"try {streamName}.readString(size: {propertySize})";
                default:
                    return $"try throwIfNil(guarded: {propertyTypeName}(stream: {streamName}))";
            }
        }
         

        private static string TypeWriterMethod(string propertyTypeName, string propertyName, int propertySize, string streamName = "stream")
        {
            switch (propertyTypeName)
            {
                case PrimitiveTypes.BYTE:
                    return $"{streamName}.writeByte(value:{propertyName})";
                case PrimitiveTypes.SHORT:
                    return $"{streamName}.writeShort(value:{propertyName})";
                case PrimitiveTypes.SIGNED_SHORT:
                    return $"{streamName}.writeSignedShort(value:{propertyName})";
                case PrimitiveTypes.WORD:
                    return $"{streamName}.writeWord(value:{propertyName})";
                case PrimitiveTypes.LONG:
                    return $"{streamName}.writeLong(value:{propertyName})";
                case PrimitiveTypes.FLOAT:
                    return $"{streamName}.writeFloat(value:{propertyName})";
                case PrimitiveTypes.STRING:
                    return $"{streamName}.writeString(value: {propertyName}, size:{propertySize})";
                default:

                    return $"{propertyName}.emit(stream: {streamName})";
            }
        }
    }
}