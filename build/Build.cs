using System;
using System.Linq;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
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
            DotNetRestore(_ => _
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(_ => _
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Publish => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPublish(_ => _
                .SetProject(Solution.GetProject("BlazorWasmRegexTest.Server"))
                .SetOutput(ArtifactsDirectory / "publish"));
        });

    const string ArmTag = "arm";
    const string ImageName = "regex-tester";
    string ArmFullImageName
    {
        get
        {
            var name = $"{Regex.Replace(DockerPrivateRegistry, @"^https?\:\/\/", string.Empty)}/{ImageName}:{ArmTag}";
            Console.WriteLine($"{nameof(ArmFullImageName)} = {name}");
            return name;
        }
    }

    Target BuildArmDockerContainer => _ => _
        .NotNull(DockerPrivateRegistry)
        .DependsOn(Compile)
        .Executes(() =>
        {
            var path = Solution.GetProject("BlazorWasmRegexTest.Server").Directory;
            Console.WriteLine($"Building Docker image in {path}");
            DockerTasks.DockerBuild(c => c
                .SetPath(Solution.Directory)
                .SetFile(path / "Dockerfile-Arm")
                .SetTag(ArmFullImageName));
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
}
