# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Upgrade BlazorWasmRegex/Shared/BlazorWasmRegex.Shared.csproj
3. Upgrade BlazorWasmRegex/Client/BlazorWasmRegex.Client.csproj
4. Upgrade BlazorWasmRegex/Server/BlazorWasmRegex.Server.csproj
5. Upgrade build/_build.csproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                              | Current Version | New Version | Description                                   |
|:----------------------------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| BlazorStrap                                               |   1.5.1         |  5.2.0      | Recommended for .NET 10.0                     |
| Blazored.LocalStorage                                     |                 |  4.5.0      | Replacement for Cloudcrate.AspNetCore.Blazor.Browser.Storage |
| Cloudcrate.AspNetCore.Blazor.Browser.Storage              |   3.0.0         |             | Deprecated - replace with Blazored.LocalStorage |
| Microsoft.AspNetCore.Components.WebAssembly               |   7.0.0         |  10.0.0     | Recommended for .NET 10.0                     |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer     |   7.0.0         |  10.0.0     | Recommended for .NET 10.0                     |
| Microsoft.AspNetCore.Components.WebAssembly.Server        |   7.0.0         |  10.0.0     | Recommended for .NET 10.0                     |
| Microsoft.VisualStudio.Azure.Containers.Tools.Targets     |   1.17.0        |  1.21.0     | Recommended for .NET 10.0                     |
| Nuke.Common                                               |   6.2.1         |  8.1.2      | Recommended for .NET 10.0                     |
| System.Net.Http.Json                                      |   7.0.0         |  10.0.0     | Recommended for .NET 10.0                     |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### BlazorWasmRegex.Shared.csproj modifications

Project properties changes:
  - Target framework should be changed from `net7.0` to `net10.0`

#### BlazorWasmRegex.Client.csproj modifications

Project properties changes:
  - Target framework should be changed from `net7.0` to `net10.0`

NuGet packages changes:
  - BlazorStrap should be updated from `1.5.1` to `5.2.0` (*recommended for .NET 10.0*)
  - Cloudcrate.AspNetCore.Blazor.Browser.Storage should be removed (*deprecated - replace with Blazored.LocalStorage*)
  - Blazored.LocalStorage should be added with version `4.5.0` (*replacement for deprecated package*)
  - Microsoft.AspNetCore.Components.WebAssembly should be updated from `7.0.0` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Components.WebAssembly.DevServer should be updated from `7.0.0` to `10.0.0` (*recommended for .NET 10.0*)
  - System.Net.Http.Json should be updated from `7.0.0` to `10.0.0` (*recommended for .NET 10.0*)

Other changes:
  - Update Program.cs to use new Blazored.LocalStorage API instead of Cloudcrate storage
  - Update root component registration syntax for .NET 10.0

#### BlazorWasmRegex.Server.csproj modifications

Project properties changes:
  - Target framework should be changed from `net7.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Components.WebAssembly.Server should be updated from `7.0.0` to `10.0.0` (*recommended for .NET 10.0*)
  - Microsoft.VisualStudio.Azure.Containers.Tools.Targets should be updated from `1.17.0` to `1.21.0` (*recommended for .NET 10.0*)

Other changes:
  - Migrate from Startup.cs pattern to minimal hosting model (Program.cs only)
  - Update to .NET 10.0 hosting patterns

#### _build.csproj modifications

Project properties changes:
  - Target framework should be changed from `net7.0` to `net10.0`

NuGet packages changes:
  - Nuke.Common should be updated from `6.2.1` to `8.1.2` (*recommended for .NET 10.0*)
