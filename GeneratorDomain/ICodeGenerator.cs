using System;
using System.Collections.Generic;
using System.Text;

namespace GeneratorDomain
{
    public interface ICodeGenerator
    {
        int Generate(DomainModel model, String outputDirectory, String templateDirectory);
    }
}
