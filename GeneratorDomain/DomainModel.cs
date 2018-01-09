using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using DotLiquid;

namespace GeneratorDomain
{
    public class DomainModel : Drop
    {
        public List<DomainEnumeration> Enumerations;
        public List<DomainType> Types;

        public Dictionary<int, string> MessageTypesById
        {
            get { return Types.Where(t => t.MessageId != 0).ToDictionary(type => type.MessageId, type => type.Name); }
        }

        public List<KeyValueDrop<int, string>> MessageTypesByIdList
        {
            get
            {
                return
                    MessageTypesById.Select(x => new KeyValueDrop<int, string> {Key = x.Key, Value = x.Value}).ToList();
            }
        }

        public bool IsEnumeration(string name)
        {
            return Enumerations.Exists(e => e.Name == name);
        }

        public DomainEnumeration Enumeration(string propertyName)
        {
            return Enumerations.First(e => e.Name == propertyName);
        }

        public DomainType ClassType(string propertyName)
        {
            return Types.First(e => e.Name == propertyName);
        }

        public int StructuralSize(string typeName, DomainProperty property)
        {
            var length = property.Length == 0 ? 1 : property.Length;

            switch (typeName)
            {
                case PrimitiveTypes.BYTE:
                    return 1 * length;
                case PrimitiveTypes.SHORT:
                    return 2 * length;
                case PrimitiveTypes.SIGNED_SHORT:
                    return 2 * length;
                case PrimitiveTypes.WORD:
                    return 4 * length;
                case PrimitiveTypes.FLOAT:
                    return 4 * length;
                case PrimitiveTypes.LONG:
                    return 8 * length;
                case PrimitiveTypes.STRING:
                    return property.Size * length;
                default:
                    if (IsEnumeration(typeName))
                    {
                        return StructuralSize(Enumeration(typeName).BaseType, property) * length;
                    }
                    return ClassType(typeName).Size * length;
            }
        }
    }

    public class DomainEnumeration : Drop
    {
        public List<DomainValue> Values { get; set; }
        public string Name { get; set; }
        public string BaseType { get; set; }
    }

    public class DomainType : Drop
    {
        public List<DomainProperty> Properties { get; set; }
        public string Name { get; set; }
        public int MessageId { get; set; }
        public DomainModel Domain { get; set; }

        public int Size
        {
            get
            {
                return Properties.Aggregate(0,
                    (size, property) => { return size + Domain.StructuralSize(property.TypeName, property); });
            }
        }
    }

    public class DomainProperty : Drop
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public int Length { get; set; }
        public int Size { get; set; }
        public DomainModel Domain { get; set; }
    }

    public class DomainValue : Drop
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class KeyValueDrop<TK, TV> : Drop
    {
        public TK Key { get; set; }
        public TV Value { get; set; }
    }

    public class PrimitiveTypes
    {
        public const string FLOAT = "float";
        public const string STRING = "string";
        public const string BYTE = "byte";
        public const string SHORT = "short";
        public const string SIGNED_SHORT = "signedshort";
        public const string WORD = "word";
        public const string LONG = "long";
    }
}