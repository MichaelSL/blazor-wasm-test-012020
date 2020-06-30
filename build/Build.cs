using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Private docker registry URL (with protocol)")]
    readonly string DockerPrivateRegistry;
    [Parameter("Private Docker registry login")]
    readonly string DockerLogin;
    [Parameter("Private Docker registry password")]
    readonly string DockerPassword;
    [Parameter("Build version - Default is '0.1.0'")]
    readonly string BuildVersion = "0.1.0";

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / ".artifacts";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution.GetProject("BlazorWasmRegex.Server"))
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Logger.Info("No tests yet :(");
        });

    Target Publish => _ => _
        .DependsOn(Compile, SetVersion)
        .Executes(() =>
        {
            DotNetPublish(_ => _
                .SetProject(Solution.GetProject("BlazorWasmRegex.Server"))
                .SetOutput(ArtifactsDirectory / "publish"));
        });

    Target SetVersion => _ => _
        .Requires(() => BuildVersion)
        .Triggers(CleanUpSwVersion)
        .Executes(() =>
        {
            Logger.Info("Setting application version");
            var fileText = File.ReadAllText("Directory.build.props.template");
            fileText = fileText.Replace("$ver", BuildVersion);
            File.WriteAllText("Directory.Build.props", fileText);

            Logger.Info("Updating 'service-worker.published.js'");
            AbsolutePath swFilePath = Solution.GetProject("BlazorWasmRegex.Client").Directory / "wwwroot" / "service-worker.published.js";
            if (!File.Exists(swFilePath))
            {
                Logger.Warn("No service worker file found");
                return;
            }
            var swFileText = File.ReadAllLines(swFilePath).ToList();
            DateTime utcNow = DateTime.UtcNow;
            swFileText.Add($"// UPDATED: {utcNow:MM/dd/yy H:mm:ss.ffff}");
            File.WriteAllLines(swFilePath, swFileText);
        });

    Target CleanUpSwVersion => _ => _
        .After(Publish, BuildArmDockerContainer, BuildDockerContainer)
        .Executes(() =>
        {
            Logger.Info("Cleaning up 'service-worker.published.js'");
            AbsolutePath swFilePath = Solution.GetProject("BlazorWasmRegex.Client").Directory / "wwwroot" / "service-worker.published.js";
            if (!File.Exists(swFilePath))
            {
                Logger.Warn("No service worker file found");
                return;
            }
            var swFileText = File.ReadAllLines(swFilePath).ToList();
            swFileText.RemoveAt(swFileText.Count - 1);
            File.WriteAllLines(swFilePath, swFileText);
        });

    const string ArmTag = "arm";
    const string ImageName = "regex-tester";
    string ArmFullImageName
    {
        get
        {
            var name = $"{Regex.Replace(DockerPrivateRegistry, @"^https?\:\/\/", string.Empty)}/{ImageName}:{ArmTag}";
            Logger.Info($"{nameof(ArmFullImageName)} = {name}");
            return name;
        }
    }

    string FullImageName
    {
        get
        {
            var name = $"{Regex.Replace(DockerPrivateRegistry, @"^https?\:\/\/", string.Empty)}/{ImageName}:x64";
            Logger.Info($"{nameof(FullImageName)} = {name}");
            return name;
        }
    }

    Target BuildArmDockerContainer => _ => _
        .NotNull(DockerPrivateRegistry)
        .DependsOn(Compile, SetVersion)
        .Executes(() =>
        {
            var path = Solution.GetProject("BlazorWasmRegex.Server").Directory;
            Logger.Info($"Building Docker image in {path}");
            DockerTasks.DockerBuild(c => c
                .SetPath(Solution.Directory)
                .SetFile(path / "Dockerfile-Arm")
                .SetTag(ArmFullImageName)
                .EnableNoCache());
        });

    Target BuildDockerContainer => _ => _
        .NotNull(DockerPrivateRegistry)
        .DependsOn(Compile, SetVersion)
        .Executes(() =>
        {
            var path = Solution.GetProject("BlazorWasmRegex.Server").Directory;
            Logger.Info($"Building Docker image in {path}");
            DockerTasks.DockerBuild(c => c
                .SetPath(Solution.Directory)
                .SetFile(path / "Dockerfile")
                .SetTag(FullImageName)
                .EnableNoCache());
        });

    Target LoginToDockerRegistry => _ => _
        .NotNull(DockerPrivateRegistry)
        .NotNull(DockerPassword)
        .NotNull(DockerLogin)
        .Executes(() =>
        {
            DockerTasks.DockerLogin(c => c
                .SetServer(DockerPrivateRegistry)
                .SetUsername(DockerLogin)
                .SetPassword(DockerPassword));
        });

    Target PushArmDockerContainer => _ => _
        .DependsOn(BuildArmDockerContainer, LoginToDockerRegistry)
        .Executes(() =>
        {
            DockerTasks.DockerPush(c => c
                .SetName(ArmFullImageName));
        });

    Target PushDockerContainer => _ => _
        .DependsOn(BuildDockerContainer, LoginToDockerRegistry)
        .Executes(() =>
        {
            DockerTasks.DockerPush(c => c
                .SetName(FullImageName));
        });
}
