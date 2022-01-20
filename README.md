# Nuget Package Updater
This project is intended to keep private nuget feeds updated on a 
more frequent schedule, pulling from a configured external source.

## Get Started
First, we need to populate the appsettings.json with relevant information

```json
"External": {
    "UserName": "Lois.Einhorn@miami.com",
    "Password": "FinkleIsEinhorn",
    "SourceUri": "https://nuget.telerik.com/v3/index.json",
    "AllVersions": true, // If true, will pull all > versions
    "PackageList": [
      "" // Specify only certain packages to keep up-to-date
    ]
  },
  "InternalFeed": {
    "DevOpsPAT": "i7izeohyhs3al6uybpk5tpkp6xwqow2nilao7pfk77wuno6utfha", 
    "Collection": "JAGC-DefaultCollection", 
    "ServerName": "jagc-az-devops1",
    "FeedUrl": "https://miamipd.com/DefaultCollection/_packaging/Super-SecretFeed/nuget/v3/index.json",
    "FeedSourceId": "de9039a3-f0ef-4fb4-8679-5efc8705e138",
  }
```

Ensure you have a `c:\temp` folder for the output

You should now be ready to run!

## Install on Windows


### Publishing 
Publish the app is pretty straight forward. Setup a new Folder Publish profile.
> An alternative publishing approach is to build the *.dll (instead of an *.exe), and when you install 
> the published app using the Windows Service Control Manager you delegate to the .NET CLI and pass the DLL. 
> For more information, see [.NET CLI: dotnet command](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet).

![Publishing image](/.media/publish-settings.png "Local Folder Publish")

Ensure that the following settings are specified:

- Deployment mode: Self-contained
- Produce single file: checked
- Enable ReadyToRun compilation: checked
- Trim unused assemblies (in preview): unchecked

Finally, select Publish. The app is compiled, and the resulting .exe file is published to the /publish output directory.

Alternatively, you could use the .NET CLI to publish the app:
```cmd
dotnet publish --output "C:\custom\publish\directory"
```

### Create the Windows Service

To create the Windows Service, use the native Windows Service Control Manager's (sc.exe) create command. Run PowerShell as an Administrator.
```cmd
sc.exe create ".NET Nuget Updater Service" binpath="C:\Path\To\App.WindowsService.exe"
```

>If you need to change the content root of the host configuration, 
> you can pass it as a command-line argument when specifying the binpath:
> `sc.exe create "Svc Name" binpath="C:\Path\To\App.exe --contentRoot C:\Other\Path"`

You should see 
```cmd
[SC] CreateService SUCCESS
```

Now run it!
```cmd
sc.exe start ".NET Nuget Updater Service"
```