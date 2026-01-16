# EmmyLua Documentation Generator

通过读取 C# 程序集，将其中的类型转换为遵循 [MoonSharp](https://github.com/moonsharp-devs/moonsharp/) 和 [Lua Language Server](https://github.com/LuaLS/lua-language-server) 标准的 [EmmyLua](https://emmylua.github.io/index.html) 文档。

## 功能特性

- 基于 .NET 反射读取 DLL 程序集
- 包括类型注解
  - `@enum name`: 枚举，映射为`table`
  - `@class name : base, interface, ...`: 类，支持基类、接口
- 包括成员
  - `field`: 字段
  - `property`: 属性
  - `indexer`: 索引器
  - `method`: 方法，支持重载、异步、由 **xLua** 自动解释的泛型、可选参数、可变参数
  - `constructor`: 构造器，支持度同上
  - `@operator`: 运算符重载，支持 `add` `sub` `mul` `div` `unm`
- 成员访问修饰符
  - `@private`: 私有成员
  - `@protected`: 受保护成员
  - `@package`: 程序集内**internal**成员
- 关键泛型映射
  - `IList<T>`: `T[]`
  - `IDictionary<K, V>`: `{ [K]: V }`
  - `IEnumerable<T>` `IEnumerator<T>`: `{ [nil]: T }`
  - `Nullable<T>`: `T|nil`
- 完整的 C# 到 Lua 类型映射
- 委托类型自动转换为函数类型

## 使用方法

### 1. 创建配置文件

创建 JSON 配置文件（参考 `config.json`）：

```json
{
  "assemblies": [
    {
      "path": "path/to/assembly.dll",
      "types": [
        "Namespace.Type1",
        "Namespace.Type2",
        "*"
      ],
    }
  ],
  "outputDir": "output"
}
```

#### 配置说明

- `assemblies` - 要加载的程序集列表
  - `path` - DLL 文件路径（支持相对或绝对路径）
  - `types` - 要生成文档的类型列表（不支持泛型）
    - `"NS1.NS2.ClassName"` - 指定完整类型名
    - `"NS1.NS2.ClassName+NestedClass"` - 嵌套类型
- `outputDir` - 输出目录（默认为 "output"）

### 2. 运行生成器

```bash
dotnet run -- config.json
```

### 3. 生成示例
- [息风谷战略Lua注解](https://github.com/zhu-rengong/ZhanGuoWuxiaLuaAnnotations)

## 依赖项

- .NET 8.0 或更高版本

## 注意事项
- 生成的文档使用 `CS.` 前缀命名空间，与 xLua 保持一致
- Lua 关键字会自动转义（如 `and` 转为 `__and__`）
- 除了关键泛型外，均会被映射为 `userdata`
- 编译器生成的成员会被自动过滤
- 运算符重载的第一个参数类型必须与声明类相同
- 不支持多维索引器


