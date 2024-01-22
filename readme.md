# Table of contents:
- Getting started
- Code style
- Dependency injection
- Configuration
- Utilites
- Commands
- Handlers
- Background tasks
- Database
- Exceptions

# Getting started
1. Fork and clone this repo
2. Join this server: https://discord.gg/CuvKvEAQkN For additional information

# Code style
- Interface names start with a capital `I`.
- Attribute types end with the word `Attribute`.
- Use meaningful and descriptive names for variables, methods, and classes.
- Prefer clarity over brevity.
- Use PascalCase for class names and method names.
- Use PascalCase for constant names, both fields and local constants.
- Use camelCase for method arguments, local variables, and private fields.
- Private instance fields start with an underscore (`_`).
- Avoid using abbreviations or acronyms in names, except for widely known and accepted abbreviations.

- Use specific exception types to provide meaningful error messages.
- Use LINQ methods for collection manipulation to improve code readability.
- Use `var` only when a reader can infer the type from the expression.
- Use `var` instead of `new()` for creating implicit objects.
- Basic types like `int` always use explicit types.
- Use string interpolation to concatenate strings.
- Use `StringBuilder` for appending to strings in loops.
- Initialize collections with the collection expression `int[] array = [];`.
- use the simple `using` construct for `IDisposable` objects.
- Use Allman style.
- Line breaks should occur before binary operators, if necessary.
- Don't use braces if a single statement will do. If one brach of the if statement requires braces then use braces in all branches

<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@24,400,0,0" />

# Dependency injection
Dependency injection is how we will provide dependencies  to different areas.

The dependencies are located in the `Services` folder. They are classes that are injected into other classes to perform various actions like receiving requests from discord and storing data.

Static classes are only used for constants and utility functions, not for storing data. For storing data singletons will be used instead. These are regular classes that we register in the app host for example:

```csharp
public class Storage
{
	public int Number { get; set; }
}
```
and then to register the service in `Program.cs`: 
```csharp
.ConfigureServices(services =>
{
    services
        // singletons
        .AddSingleton<Storage>();
})
```
Now, to access this class there are 2 ways of injecting it.
For other services we will use constructor injection:
```csharp
public class MyService
{
    private readonly Storage _storage;

    public MyService(Storage storage)
    {
        _storage = storage; // injecting storage
    }
}
```
> [!IMPORTANT]
> *It is best practice to make injected properties **readonly**, to ensure that you don't have separate states of services in your classes*.

For commands, we will use property injection instead:
```csharp
public class CommandsClass : InteractionModuleBase<SocketInteractionContext>
{
    public required Storage Storage { get; set; }
}
```

# Configuration
Use the BotOptions.cs file to define settings.
Remember to add your settings file into the host builder.

Example
```csharp
var configuration = new ConfigurationBuilder()
    .AddCommandLine(args)
    .AddEnvironmentVariables(prefix: "DOTNET_")
    .SetBasePath(Path.Combine(AppContext.BaseDirectory, "Files"))
    .AddJsonFile("YourOptionsFile.json", optional: false, reloadOnChange: true)
    .Build();
```
and
```csharp
services
    // config
    .Configure<YourOptionsClass>(configuration)
```

To read from the configuration files Inject it into your class 
```csharp
Constructor(IOptionsMonitor<YourOptionsClass> options)
{
   var option = options.CurrentValue.YourOption;
}
```

> [!TIP]
> IOptionsMonitor is used here to ensure that any changes that you make in your configuration files, is automatically applied to the current state. And because of this, you should not store the state of configuration files in a field or property anywhere in your class. to ensure that you are getting the latest state in the configuration file.
 

# Utilities
The utility folder contains files with useful functions that aren't injected into classes like extension methods and static methods. Most code that is "stateless" / does not store any runtime data usually goes here.

Constants are also put in utility classes like Emotes and Urls. 

Remember to add a `summary` to the methods to document their use

# Commands
Command classes are transient services which means they are created and destroyed each time a command is run.

> [!IMPORTANT]
> Because transient services are created and destroyed every time a command is run, it means that you cannot store data inside of a command. You must store data using a Singleton

There are 2 types of commands: Message commands `>>command` and slash commands `/command`.

Message commands will only be used for internal functions and to give commands to the bot directly like shutting down, Message command should only be used in the case where the commands are used to administer the bot.

Everything else will be handled using slash commands

Message commands are classes that inherit from `ModuleBase`, all of them will go into the `Modules` folder. Example:
```csharp
public class Testing : ModuleBase
{
    [ModCommand]
    [Command("test")]
    public async Task Test()
    {
        await ReplyAsync("Message Received");
    }
}
```
The `[ModCommand]` attribute is a custom attribute for ensuring that commands can only be run by Moderators.
There is also the `[RequireOwner]` attribute, Which indicates that the slash command or message command requires you to be the Bot Owner to execute this. 

Slash commands will inherit from `InteractionModuleBase<SocketInteractionContext>` Example:
```csharp
public class Basic : InteractionModuleBase<SocketInteractionContext>
{
    [ModCommand]
    [SlashCommand("test", "responds to a commands")]
    public async Task Test(bool boolean)
    {
        await RespondAsync("Response");
    }
}
```

> [!IMPORTANT]
> A Bot Owner is distinct from a Moderator, A Moderator should not be able to use Bot Owner commands. and a Bot Owner should not be able to use Moderator commands.

For more information see:

https://discordnet.dev/guides/text_commands/intro.html
https://discordnet.dev/guides/int_basics/application-commands/intro.html

# Handlers
Handlers are services that will receive events from discord and execute the logic associated. They are of type `IHostedService` 

Hosted services are classes that will perform tasks continuously like receiving events from discord or background tasks. Each one needs the following method overriden:
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
```

The stoppingToken is used to signal to running services that the application is about to close, an example of using it is: 

```csharp
while (!stoppingToken.IsCancellationRequested)
{
    // asynchronous code here...
}
```

If you have to wait for the bot to fully connect to discord to start performing actions you can use `await Client.WaitForReadyAsync(stoppingToken);`

If you need to perform logic when the service is stopped (e.g. the program is stopped) you can override the method:

```csharp
public Task StopAsync(CancellationToken cancellationToken)
```

For more information see: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-8.0&tabs=visual-studio

> [!NOTE]
> Unlike singletons you can't inject hosted services. Keep that in mind.


Register the events into methods and then handle their logic inside a `Task.Run()`

> [!IMPORTANT]
> Because events are handled in the same thread as the gateway thread, use Task.Run() to offload the logic into another thread

> [!WARNING]
> Please keep in mind thread safety when you are handling events, Be sure to use mutexes or threads safe C# classes such as concurrent collection, ConcurrentDictionary or ConcurrentBag.

# Background tasks
Background tasks is code that executes periodically, normally to check for unbans or clear temporary data like user cooldowns, for convenience 3 methods have been provided. One that runs only at the start to set up data, one that runs often, and another that runs sparserly

# Database
EF Core is used to handle all the database operations. To create a connection to the database first inject a `IDbContextFactory<SpiritContext>` object and create it with `db.CreateDbContext()`

> [!TIP]
> This object implements IDisposable, make sure to dispose of it properly, such as with the using keyword, or with the IDisposable.Dispose() method

Database objects follow their own style conventions, unlike other types, the names have the "db" prefix to differentiate them from other similar objects for example:
`dbUser`, `dbBadge` etc.

To learn more about EF core: https://learn.microsoft.com/en-us/ef/core/

# Exceptions
exceptions raised during events wrapped in a `Task.Run()` are handled by the `.ContinueWith()` method, you can copy the template from another event and just edit the message describing where the exception happened.

For exceptions that happen inside commands are automatically handled by their respective command handler so no extra action is required.

