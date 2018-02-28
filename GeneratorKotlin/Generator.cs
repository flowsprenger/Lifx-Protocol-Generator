using System;
using System.IO;
using System.Text;
using DotLiquid;
using GeneratorDomain;
using System.Text;

namespace GeneratorKotlin
{
    public class Generator : ICodeGenerator
    {
        public DomainModel Model;
        private Template classTemplate;
        private Template enumTemplate;
        private Template mappingTemplate;
        private Template prependTemplate;
        private Template appendTemplate;
        private string outputDirectory;
        private string templateDirectory;

        public int Generate(DomainModel model, String outputDirectory, String templateDirectory)
        {
            this.outputDirectory = outputDirectory;
            this.templateDirectory = templateDirectory;
            Model = model;

            prependTemplate = Template.Parse(File.ReadAllText(Path.Combine(templateDirectory, "Prepend.template")));
            appendTemplate = Template.Parse(File.ReadAllText(Path.Combine(templateDirectory, "Append.template")));
            classTemplate = Template.Parse(File.ReadAllText(Path.Combine(templateDirectory, "Class.template")));
            enumTemplate = Template.Parse(File.ReadAllText(Path.Combine(templateDirectory, "Enum.template")));
            mappingTemplate = Template.Parse(File.ReadAllText(Path.Combine(templateDirectory, "Mapping.template")));

            Generate();

            return 0;
        }

        public void Generate()
        {
            Directory.CreateDirectory(outputDirectory);

            StringBuilder output = new StringBuilder();

            output.Append(prependTemplate.Render(new RenderParameters()));
            foreach (var domainType in Model.Types)
            {
                output.Append(
                    classTemplate.Render(new RenderParameters
                    {
                        LocalVariables = Hash.FromAnonymousObject(domainType),
                        Filters = new[] { typeof(SharpFilters) }
                    })
                );
            }
            

            foreach (var domainType in Model.Enumerations)
            {
                output.Append(
                    enumTemplate.Render(new RenderParameters
                    {
                        LocalVariables = Hash.FromAnonymousObject(domainType),
                        Filters = new[] { typeof(SharpFilters) }
                    })
                );
            }

            output.Append(
                mappingTemplate.Render(new RenderParameters
                {
                    LocalVariables = Hash.FromAnonymousObject(new { MessageTypesById = Model.MessageTypesByIdList }),
                    Filters = new[] { typeof(SharpFilters) }
                })
                );


            output.Append(appendTemplate.Render(new RenderParameters()));

            File.WriteAllText(Path.Combine(outputDirectory, "DomainModel.kt"), output.ToString());
        }
    }

    public static class SharpFilters
    {
        public static string PropertyTypeDecl(DomainProperty property)
        {
            if (property.Length == 0)
            {
                return TypeLookup(property.TypeName);
            }
            return $"Array<{TypeLookup(property.TypeName)}>";
        }

        public static string TypeLookup(string input)
        {
            switch (input)
            {
                case PrimitiveTypes.BYTE:
                    return "Byte";
                case PrimitiveTypes.SHORT:
                    return "Short";
                case PrimitiveTypes.SIGNED_SHORT:
                    return "Short";   
                case PrimitiveTypes.WORD:
                    return "Int";
                case PrimitiveTypes.LONG:
                    return "Long";
                case PrimitiveTypes.FLOAT:
                    return "Float";
                case PrimitiveTypes.STRING:
                    return "ByteArray";
                default:
                    return input;
            }
        }
        
        public static string ReaderMethod(DomainProperty property, string streamName)
        {
            if (property.Domain.IsEnumeration(property.TypeName))
            {
                var enumeration = property.Domain.Enumeration(property.TypeName);
                return $"{property.TypeName}.fromValue({TypeReaderMethod(enumeration.BaseType, 0, streamName)})";
            }
            if (property.Length == 0)
            {
                return TypeReaderMethod(property.TypeName, property.Size, streamName);
            }
            return
                $"(0 until {property.Length}).map {{ {TypeReaderMethod(property.TypeName, property.Size, streamName)} }}.toTypedArray()";
        }

        private static string TypeReaderMethod(string propertyTypeName, int propertySize, string streamName)
        {
            switch (propertyTypeName)
            {
                case PrimitiveTypes.BYTE:
                    return $"{streamName}.get()";
                case PrimitiveTypes.SHORT:
                    return $"{streamName}.getShort()";
                case PrimitiveTypes.SIGNED_SHORT:
                    return $"{streamName}.getShort()";    
                case PrimitiveTypes.WORD:
                    return $"{streamName}.getInt()";
                case PrimitiveTypes.LONG:
                    return $"{streamName}.getLong()";
                case PrimitiveTypes.FLOAT:
                    return $"{streamName}.getFloat()";
                case PrimitiveTypes.STRING:
                    return $"{streamName}.getString({propertySize})";
                default:
                    return $"{propertyTypeName}({streamName})";
            }
        }
        
        public static string WriterMethod(DomainProperty property, string streamName)
        {
            if (property.Domain.IsEnumeration(property.TypeName))
            {
                var enumeration = property.Domain.Enumeration(property.TypeName);
                return TypeWriterMethod(enumeration.BaseType, $"({property.Name}.value)", 0, streamName);
            }
            if (property.Length == 0)
            {
                return TypeWriterMethod(property.TypeName, property.Name, property.Size, streamName);
            }

            return
                $"assert({property.Name}.size == {property.Size}); {property.Name}.forEach {{ {TypeWriterMethod(property.TypeName, "it", property.Size, streamName)} }}";
        }
        
        private static string TypeWriterMethod(string propertyTypeName, string propertyName, int propertySize, string streamName = "stream")
        {
            switch (propertyTypeName)
            {
                case PrimitiveTypes.BYTE:
                    return $"{streamName}.put({propertyName})";
                case PrimitiveTypes.SHORT:
                    return $"{streamName}.putShort({propertyName})";
                case PrimitiveTypes.SIGNED_SHORT:
                    return $"{streamName}.putShort({propertyName})";
                case PrimitiveTypes.WORD:
                    return $"{streamName}.putInt({propertyName})";
                case PrimitiveTypes.LONG:
                    return $"{streamName}.putLong({propertyName})";
                case PrimitiveTypes.FLOAT:
                    return $"{streamName}.putFloat({propertyName})";
                case PrimitiveTypes.STRING:
                    return $"(0 until {propertySize}).forEach {{ {streamName}.put({propertyName}[it]) }}";
                default:

                    return $"{propertyName}.addToByteBuffer({streamName})";
            }
        }

        public static string EnforcePrimitive(string propertyTypeName)
        {
            switch (propertyTypeName)
            {
                case PrimitiveTypes.BYTE:
                    return $".toByte()";
                case PrimitiveTypes.SHORT:
                    return $".toShort()";
                case PrimitiveTypes.SIGNED_SHORT:
                    return $".toShort()";
                case PrimitiveTypes.WORD:
                    return $".toInt()";
                case PrimitiveTypes.LONG:
                    return $".toLong()";
                case PrimitiveTypes.FLOAT:
                    return $".toFloat()";
                case PrimitiveTypes.STRING:
                    return $".toString()";
                default:

                    return $"";
            }
        }
    }
}
