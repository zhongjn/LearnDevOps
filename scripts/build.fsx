#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"

open System
open System.Runtime.CompilerServices
open System.IO
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

let checkedExec cmd args =
    Trace.log $"command begin: {cmd} {args}"
    let outStream = new MemoryStream()
    let errStream = new MemoryStream()
    let res = CreateProcess.fromRawCommandLine cmd args
            |> CreateProcess.withStandardOutput (UseStream (false, outStream))
            |> CreateProcess.withStandardError (UseStream (false, errStream))
            |> Proc.run
    Trace.log $"command end ({res.ExitCode}): {cmd} {args}"
    let outReader = new StreamReader(outStream)
    Trace.log <| outReader.ReadToEnd()
    let errReader= new StreamReader(errStream)
    Trace.traceError <| errReader.ReadToEnd()
    if res.ExitCode <> 0 then
        failwith "exec failed"


type Shell with
    static member CheckedExec(cmd, ?args, ?dir) =
        let u = CreateProcess.fromRawCommand "./folder/mytool.exe" ["arg1"; "arg2"]
                        |> CreateProcess.redirectOutput
                        |> Proc.run // start with the above configuration
        let ret =
            Shell.Exec(cmd, ?args = args, ?dir = dir)

        if ret <> 0 then
            failwith $"non-zero return code: {ret}, cmd: {cmd}, args: {args}"

let kubectl (nameSuffix: string) (args: string) =
    Trace.log $"kubectl {args}"
    Shell.CheckedExec("kubectl", $"--context={nameSuffix} {args}")

let setupK8sCluster nameSuffix =
    Console.Out.Flush()
    Console.Error.Flush()
    Shell.CheckedExec("bash", "scripts/err.sh")
    Console.Out.Flush()
    Console.Error.Flush()
    Shell.CheckedExec("bash", "scripts/err.sh")
    Console.Out.Flush()
    Console.Error.Flush()
    Shell.CheckedExec("bash", "scripts/err.sh")
    Console.Out.Flush()
    Console.Error.Flush()
    Trace.log $"setting up k8s cluster it-{nameSuffix}"

    // create cluster
    Shell.CheckedExec("kind", $"create cluster --name it-{nameSuffix} --config scripts/kind-config.yaml")
    // setup local registry
    Shell.CheckedExec("bash", "scripts/local_registry.sh")
    // allow volume expansion
    kubectl nameSuffix """patch sc standard -p '{"allowVolumeExpansion":"true"}')"""
    // setup istio
    Shell.CheckedExec("istioctl", "install -y")
    kubectl nameSuffix "apply -f k8s/istio-app"

    Shell.CheckedExec(
        "helm",
        "upgrade --install frontend k8s/charts/frontend"
        + " --set imageVersion=2022-02-02-19-53"
        + " --set registry=http://localhost:5000"
    )

let teardownK8sCluster nameSuffix =
    let err =
        Shell.Exec("kind", $"delete cluster --name it-{nameSuffix}")

    Trace.log $"teardown cluster err {err}"

let runSingleIntegrationTest name body =
    Trace.log $"Integration test: {name}"
    teardownK8sCluster name
    setupK8sCluster name
    body name
// teardownK8sCluster name

let testPutThenQuery () =
    runSingleIntegrationTest "putThenQuery" (fun _ -> ())

Target.initEnvironment ()

Target.create "build" (fun _ -> !! "src/**/bin" ++ "src/**/obj" |> Shell.cleanDirs)

Target.create "unit_test" (fun _ -> !! "src/**/*.*proj" |> Seq.iter (DotNet.test id))
"build" ==> "unit_test"

Target.create "integration_test" (fun _ -> testPutThenQuery ())

// Target.create "Clean" (fun _ ->
//     !! "src/**/bin"
//     ++ "src/**/obj"
//     |> Shell.cleanDirs
// )

// Target.create "Build" (fun _ ->
//     !! "src/**/*.*proj"
//     |> Seq.iter (DotNet.build id)
// )

Target.create "all" ignore

Target.runOrDefault "all"
"unit_test" ==> "all"
"integration_test" ==> "all"
