using System;
using System.Globalization;
using System.IO;
using System.Text;
using DotLiquid;
using GeneratorDomain;
using System.Text;

namespace GeneratorSharp
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

            output.Append(prependTemplate.Render(new RenderParameters(CultureInfo.InvariantCulture)));
            foreach (var domainType in Model.Types)
            {
                output.Append(
                    classTemplate.Render(new RenderParameters(CultureInfo.InvariantCulture)
                    {
                        LocalVariables = Hash.FromAnonymousObject(domainType),
                        Filters = new[] { typeof(SharpFilters) }
                    })
                );
            }
            

            foreach (var domainType in Model.Enumerations)
            {
                output.Append(
                    enumTemplate.Render(new RenderParameters(CultureInfo.InvariantCulture)
                    {
                        LocalVariables = Hash.FromAnonymousObject(domainType),
                        Filters = new[] { typeof(SharpFilters) }
                    })
                );
            }

            output.Append(
                mappingTemplate.Render(new RenderParameters(CultureInfo.InvariantCulture)
                {
                    LocalVariables = Hash.FromAnonymousObject(new { MessageTypesById = Model.MessageTypesByIdList }),
                    Filters = new[] { typeof(SharpFilters) }
                })
                );


            output.Append(appendTemplate.Render(new RenderParameters(CultureInfo.InvariantCulture)));

            File.WriteAllText(Path.Combine(outputDirectory, "DomainModel.cs"), output.ToString());
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
            return $"{TypeLookup(property.TypeName)}[]";
        }

        public static string TypeLookup(string input)
        {
            switch (input)
            {
                case PrimitiveTypes.BYTE:
                    return "byte";
                case PrimitiveTypes.SHORT:
                    return "ushort";
                case PrimitiveTypes.SIGNED_SHORT:
                    return "short";   
                case PrimitiveTypes.WORD:
                    return "uint";
                case PrimitiveTypes.LONG:
                    return "ulong";
                case PrimitiveTypes.FLOAT:
                    return "float";
                case PrimitiveTypes.STRING:
                    return "byte[]";
                default:
                    return input;
            }
        }
    }
}
