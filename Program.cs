using CommunityToolkit.Diagnostics;
using Ipfs.Http;
using OwlCore;
using OwlCore.Kubo;
using OwlCore.Services;
using OwlCore.Storage;
using OwlCore.Storage.SystemIO;

Logger.MessageReceived += (s, e) =>
{
    var message = $"{DateTime.UtcNow:O} [{e.Level}] [Thread {Thread.CurrentThread.ManagedThreadId}] L{e.CallerLineNumber} {Path.GetFileNameWithoutExtension(e.CallerFilePath)} {e.CallerMemberName}{(e.Exception is not null ? $" Exception: {e.Exception} |" : string.Empty)}: {e.Message}";
    Console.WriteLine(message);
};

var apiAddress = GetNamedCommandLineArg("api") ?? "http://127.0.0.1:5001";
var checkIntervalSecondsStr = GetNamedCommandLineArg("interval-seconds") ?? "3600";
var checkIntervalSeconds = int.Parse(checkIntervalSecondsStr);

var outputPath = GetNamedCommandLineArg("output-path");
if (string.IsNullOrWhiteSpace(outputPath))
{
    Logger.LogCritical("Argument --output-path not supplied. Exiting.");
    Environment.Exit(-1);
    return;
}

var ipnsAddress = GetNamedCommandLineArg("ipns");
if (string.IsNullOrWhiteSpace(ipnsAddress))
{
    Logger.LogCritical("Argument --ipns not supplied. Exiting.");
    Environment.Exit(-1);
    return;
}

var ipfsClient = new IpfsClient(apiAddress);

var timer = new System.Timers.Timer();
timer.Interval = TimeSpan.FromSeconds(checkIntervalSeconds).TotalMilliseconds;
timer.Elapsed += (sender, eventArgs) => _ = RunAsync();

async Task RunAsync()
{
    Logger.LogInformation($"Starting run");

    Guard.IsNotNull(ipfsClient);
    Guard.IsNotNullOrWhiteSpace(ipnsAddress);

    Logger.LogInformation($"Resolving {ipnsAddress}.");

    var cid = await ipfsClient.Name.ResolveAsync(ipnsAddress, true);
    if (cid is null)
    {
        Logger.LogCritical($"Could not resolve IPNS {ipnsAddress}. Exiting.");
        Environment.Exit(-1);
        return;
    }

    Logger.LogInformation($"Resolved to {cid}");

    var destinationFolder = new SystemFolder(outputPath);
    var sourceFolder = new IpfsFolder(cid.Split("/ipfs/", StringSplitOptions.RemoveEmptyEntries)[0], ipfsClient);

    await CopyFolderContentsRecursive(sourceFolder, destinationFolder);
}

await RunAsync();
timer.Start();

// Block process from exiting automatically.
new ManualResetEvent(false).WaitOne();

async Task CopyFolderContentsRecursive(IFolder source, IModifiableFolder destination)
{
    Logger.LogInformation($"Starting recursive copy of {source.Id} to {destination.Id}.");

    await foreach (var item in source.GetItemsAsync())
    {
        if (item is IFile file)
        {
            Logger.LogInformation($"Copying file {file.Name} (id: {file.Id}) to {destination.Id}.");
            await destination.CreateCopyOfAsync(file);
        }

        if (item is IFolder subFolder)
        {
            Logger.LogInformation($"Creating folder {subFolder.Name} in destination {destination.Id}.");
            var newFolder = await destination.CreateFolderAsync(subFolder.Name);

            if (newFolder is not IModifiableFolder newModifiableFolder)
            {
                Logger.LogCritical($"New folder created in {subFolder.Name} (id {subFolder.Id}) is not a modifiable folder. Exiting.");
                Environment.Exit(-1);
                return;
            }

            await CopyFolderContentsRecursive(source: subFolder, destination: newModifiableFolder);
        }
    }
    
    Logger.LogInformation($"Finished recursive copy of {source.Id} to {destination.Id}.");
}

bool GetFlagCommandLineArg(string name) => Environment.GetCommandLineArgs().FirstOrDefault(x => x.Contains($"--{name}")) is not null;
string? GetNamedCommandLineArg(string name) => Environment.GetCommandLineArgs().FirstOrDefault(x => x.Contains($"--{name}=")) is string str ? str.Replace($"--{name}=", "") : null;