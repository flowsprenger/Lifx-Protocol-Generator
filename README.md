Simple extensible code generator for lifx protocol messages used by https://github.com/flowsprenger/RxLifx-Swift

Contains model for the protocol messages documented here: https://lan.developer.lifx.com

See Model/Model.xml for the definition of the message
The format of the Model can be validated against Model/schema.xsd

At the moment two moduels are available for generating C# and Swift code.

Prerequisite

Install dot net (Windows, OSX, Linux):
https://www.microsoft.com/net/learn/get-started/

You should be able to build and from an ide (Visual Studio (for Mac), Rider) by creating the appropriate run config.

Building on the the commandline with .net core

build from the root directory:

`dotnet build`

this will build:
```
Codegen/bin/Debug/netcoreapp1.1/Codegen.dll - the main entry point for the codegenerator
GeneratorDomain/bin/Debug/netstandard1.6/GeneratorDomain.dll - data models used by the generator, see also Codegen/Model/schema.xsd
GeneratorSharp/bin/Debug/netstandard1.6/GeneratorSharp.dll - code generator utilities for swift
GeneratorSwift/bin/Debug/netstandard1.6/GeneratorSwift.dll - code generator untilities for c# code
```



generate the code:

```
dotnet Codegen/bin/Debug/netcoreapp1.1/Codegen.dll -t Templates/Swift -o generated/Swift -m Model -g GeneratorSwift/bin/Debug/netstandard1.6/GeneratorSwift.dll
```

or

```
dotnet Codegen/bin/Debug/netcoreapp1.1/Codegen.dll -t Templates/Sharp -o generated/Sharp -m Model -g GeneratorSharp/bin/Debug/netstandard1.6/GeneratorSharp.dll
```
