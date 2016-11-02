# Overview

This tool generates a C# source code file containing the hash of the latest commit of your local git repository.

The name of the built binary is `dotnet-git-commit-hash`, this is for integration with .NET Core CLI tool.

# NuGet package

A NuGet package is available here: https://www.nuget.org/packages/Bitcraft.Tools.GitCommitHash

# How to use

## In command line

Hereafter is the list of supported arguments:

`--namespace`, `--ns`

Valid values | Default | Description
---|---|---
Valid C# namespace name | (nothing) | If namespace argument is not provided, the static class is generated without namespace.

---

`--class`

Valid values | Default | Description
---|---|---
Valid C# class name | GitCommitHash | The name of the static class that contains the git commit hash.

---

`--output`

Valid values | Default | Description
---|---|---
Valid filename | GitCommitHash.cs | The output source code file. If a relative filename is provided, it is relative to the current directory.<br>This uses `Directory.GetCurrentDirectory()`.

---

`--hash`

Valid values | Default | Description
---|---|---
**short** or **long** | short | The short form looks like this '9df9734', the long form is '9df97340b237a2af5988e1865cd874f134d2a660'.

---

`--access-modifier`

Valid values | Default | Description
---|---|---
**public** or **internal** | public | The access modifier of the generated static class.

---

`--indent`, `--indenting`

Valid values | Default | Description
---|---|---
**space**, **spaces**, **tab** or **tabs** | space | The characters used to indent the generated code.

---

`--indent-size`, `--indenting-size`

Valid values | Default | Description
---|---|---
Integer greater than or equal to zero | 4 | The number of indent character per level of indentation.

---

`--line-ending`

Valid values | Default | Description
---|---|---
**crlf** or **lf** | lf | The character(s) used for line ending of the generated code.

### Example

Run the command `dotnet dotnet-git-commit-hash.dll`, this will generate a file named `GitCommitHash.cs` in the current directory, containing the following code.

```CSharp
using System;

/// <summary>
/// Stores the git commit hash of the current HEAD of your local repository.
/// </summary>
public static class GitCommitHash
{
    /// <summary>
    /// Gets the git commit hash.
    /// </summary>
    public static string Value
    {
        get { return "9df9734"; }
    }
}
```

### Another example

```
dotnet dotnet-git-commit-hash.dll \
   --namespace Alice.Bob \
   --class Charly \
   --output Misc/MyFile.cs \
   --hash long \
   --access-modifier internal \
   --indent spaces \
   --indent-size 2 \
   --line-ending lf
```

```CSharp
using System;

namespace Alice.Bob
{
  /// <summary>
  /// Stores the git commit hash of the current HEAD of your local repository.
  /// </summary>
  internal static class Charly
  {
    /// <summary>
    /// Gets the git commit hash.
    /// </summary>
    public static string Value
    {
      get { return "9df97340b237a2af5988e1865cd874f134d2a660"; }
    }
  }
}
```

## With .NET Core CLI tool

To integrate this tool in your project, you have to modify your `project.json` file, as follow:

```
    ...
    "tools": {
        ...
        "Bitcraft.Tools.GitCommitHash": "<version>"
        ...
    }
    ...
    "scripts": {
        ...
        "precompile": "dotnet git-commit-hash --access-modifier internal"
        ...
    }
    ...
```

### Note

If you are already using the generated class in your project, and for some reasons the file to generate in not present on your local machine, you may get a compile error saying the class you generated does not exist.

This is because files to compile are evaluated before the precompile scripts run, and thus the generated file is not taken into account. To fix this, just run compilation again and it will work.

For more information about this issue, you can refer to the following GitHub links:
- https://github.com/dotnet/cli/issues/1475
- https://github.com/dotnet/cli/issues/3807
