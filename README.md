**English | [简体中文](./README.zh-CN.md)**

# EmmyLua Doc Generator

This tool loads C# assemblies and converts their types into [EmmyLua](https://emmylua.github.io/index.html) documentation following the standards of [MoonSharp](https://github.com/moonsharp-devs/moonsharp/) and the [LuaLS](https://github.com/LuaLS/lua-language-server).

## Features

- Loads DLL assemblies based on .NET reflection
- Includes type annotations
  - `@enum name`: Enums, mapped as `table`
  - `@class name : base, interface, ...`: Classes, supports base classes and interfaces
- Includes members
  - `field`: Fields
  - `property`: Properties
  - `indexer`: Indexers
  - `method`: Methods, supports overloads, async, generics automatically interpreted by **xLua** (for reference only, as this branch follows the MoonSharp by evil-factory standard), optional parameters, and variable parameters
  - `constructor`: Constructors, with the same level of support as above
  - `@operator`: Operator overloads, supports `add`, `sub`, `mul`, `div`, `unm`
- Member access modifiers
  - `@private`: Private members
  - `@protected`: Protected members
  - `@package`: **Internal** members within the assembly
- Composite Type Mapping
  - `TValue this[TKey idx]`: `{ [TKey]: TValue }`
  - `IEnumerable<T>`, `IEnumerator<T>`: `{ [nil]: T }`
  - `Nullable<T>`: `T|nil`
- Delegate types are automatically converted to function types
- Unrecognized `userdata` is mapped as composite types to provide more precise code hints

## Usage

### 1. Create a configuration file

Create a JSON configuration file (refer to `config.json`):

```json
{
  "assemblies": [
    {
      "path": "path/to/assembly.dll",
      "types": [
        "Namespace.Type1",
        "Namespace.Type2",
        "*"
      ]
    }
  ],
  "outputDir": "output"
}
```

#### Configuration Explanation

- `assemblies` - List of assemblies to load
  - `path` - Path to the DLL file (supports relative or absolute paths)
  - `types` - List of types to generate documentation for (generics are not supported)
    - `"NS1.NS2.ClassName"` - Specify the full type name
    - `"NS1.NS2.ClassName+NestedClass"` - Nested types
- `outputDir` - Output directory (defaults to "output")

### 2. Run the generator

```bash
dotnet run -- config.json
```

### 3. Generated Example
- [Barotrauma Lua Annotations](https://github.com/zhu-rengong/Barotrauma-Lua-Annotations)

## Dependencies

- .NET 8.0 or higher

## Notes
- Generated documentation uses the `CS.` prefix for namespaces, consistent with xLua
- Lua keywords are automatically escaped (e.g., `and` becomes `__and__`)
- Except for the key generics, other types are mapped as `userdata`
- Compiler-generated members are automatically filtered out
- The first parameter type for operator overloads must be the same as the declaring class
- Multidimensional indexers are not supported