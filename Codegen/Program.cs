using System.Linq;
using System.Xml.Linq;
using GeneratorDomain;
using System.Collections.Generic;
using CommandLine;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Codegen
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var options = CommandLine.Parser.Default.ParseArguments<Options>(args);
            options.MapResult((result) =>
            {
                return GenerateCode(result);
            }, (errors) =>
            {
                return 1;
            });
        }

        private static int GenerateCode(Options result)
        {
            XDocument xdoc = XDocument.Parse("<def></def>");
            System.IO.DirectoryInfo directory = new DirectoryInfo(result.ModelDirectory);
            if (directory.Exists)
            {
                directory.GetFiles().Where((file) => file.Extension == ".xml").Aggregate(xdoc, (doc, file) => {
                    doc.Root.Add(XDocument.Load(file.FullName).Root.Elements());
                    return doc;
                });
            }
            else
            {

                return 1;
            }

            var model = BuildModel(xdoc);

            var dllPath = Path.IsPathRooted(result.Generator) ? result.Generator : Path.Combine(Directory.GetCurrentDirectory(), result.Generator);

            var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
            var type = asm.ExportedTypes.First(t => t.GetInterfaces().Contains(typeof(ICodeGenerator)));

            ICodeGenerator generator = Activator.CreateInstance(type) as ICodeGenerator;

            if (generator != null) return generator.Generate(model, result.OutputDirectory, result.TemplateDirectory);

            return 1;
        }

        private static DomainModel BuildModel(XDocument xdoc)
        {
            var model = new DomainModel
            {
                Enumerations = xdoc.Root.Elements("enum").Select(element => new DomainEnumeration
                {

                    Name = element.Attribute("name").Value,
                    BaseType = element.Attribute("baseType").Value,
                    Values = element.Elements("value").Select(e => new DomainValue
                    {
                        Name = e.Value,
                        Value = IntOrDefault(e.Attribute("value"), 0)
                    }).ToList()
                }).ToList(),

                Types = xdoc.Root.Elements("type").Select(element => new DomainType
                {
                    Name = element.Attribute("name").Value,
                    MessageId = IntOrDefault(element.Attribute("messageId"), 0),
                    Properties = element.Elements("property").Select(e => new DomainProperty
                    {
                        Name = e.Attribute("name").Value,
                        TypeName = e.Attribute("type").Value,
                        Length = IntOrDefault(e.Attribute("length"), 0),
                        Size = IntOrDefault(e.Attribute("size"), 0)
                    }).ToList()
                }).ToList()
            };

            model.Types.ForEach(t =>
            {
                t.Domain = model;
                t.Properties.ForEach(p => p.Domain = model);
            });

            return model;
        }

        private static int IntOrDefault(XAttribute attribute, int def)
        {
            int value;
            if (attribute != null && int.TryParse(attribute.Value, out value))
            {
                return value;
            }
            return def;
        }
    }


    class Options
    {
        [Option('g', "generator", Required = true, HelpText = "Dll conatining the generator implementation")]
        public string Generator { get; set; }

        [Option('t', "templates", Required = true, HelpText = "Directory containing the template files")]
        public string TemplateDirectory { get; set; }

        [Option('o', "output", Required = true, HelpText = "Directory containing the template files")]
        public string OutputDirectory { get; set; }

        [Option('m', "model", Required = true, HelpText = "Directory containing the model definitions")]
        public string ModelDirectory { get; set; }

        [Option('v', "verbose",  HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }
    }

}




