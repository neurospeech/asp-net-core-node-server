# NodeServer for ASP.NET Core

An extension of NodeServices to execute side by side versions of node packages.

## Installation

`Install-Package NeuroSpeech.NodeServer`

Install and setup ProGet from https://inedo.com/proget and publish your private packages onto npm registry inside ProGet. Support for authenticated npm registry will come in future.

## Features

1. Side by side execution of different version of same package
2. Download and extract npm packages along with dependencies from private NPM repository such as ProGet
3. TempFolder can be configured on `D:` drive, recommended for Azure VMs.

## Security

1. NodeServer does not execute `npm install` or `npm start` script.
2. TempFolder drive needs write access
3. Only packages listed in `PrivatePackages` will be downloaded and extracted, however dependencies will not be restricted to package whitelist. Your developers must be careful for not include them in package dependencies.

## Setup

```c#
services.AddSingleton<NodeServer.NodeServer>(
    sp => new NodeServer.NodeServer(sp,
        new NodeServer.NodeServerOptions
        {
            // This must be unique to avoid multiple process accessing same
            // folder conflicts
            TempFolder = "D:\\temp\\" + Guid.NewGuid(),
            // ProGet private registry url
            NPMRegistry = "https://private-proget.company.com/npm/PRIVATE",
            PrivatePackages = new string[] {
                "@company/package@1.0.1",
                "@company/core@1.0.1"
            }
        }));
```

## Execution

```c#

    [HttpGet("template/{version}/{name}")]
    public async Task<string> ProcessTemplate(
        [FromRoute] string name,
        [FromRoute] string version,
        [FromBody] EmailModel model,
        [FromService] NodeServer.NodeServer server
    ) {

        // get node services from the installed package version
        // if version does not exist, it will download package
        // along with all its dependencies
        var package = await 
            server.GetInstalledPackageASync($"@company/template@{version}");

        var nodeServices = package.NodeServices;

        return await nodeServices.InvokeExportAsync(
            "index.js",
            "default",
            new {
                fromName: "Sender",
                fromAddress: "senderEmail",
                body: model
            });

    }

```

`GetInstalledPackageAsync` method will create a folder `{TempFolder}\npm\{package}\v{version}` and it will extract package from given npm registry.

> It will not install npm package, as IIS Website may not have sufficient rights to execute `npm` command. So in order to make things simpler, NodeServer inspects package.json file and downloads all dependencies in `node_modules` folder. It does not execute any scripts.