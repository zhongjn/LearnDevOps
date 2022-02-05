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

type WriteLineStream(cmd, onWriteLine) =
    inherit Stream()
    let buf = new MemoryStream()
    override this.CanWrite = true
    override this.Write(buffer) =
        buf.



type Shell with
    static member private CheckedExecInternal(cmd, f) =
        Trace.log $"command begin: {cmd}"
        let outStream = new MemoryStream()
        let errStream = new MemoryStream()
        let

        let res =
            f cmd
            |> CreateProcess.withStandardOutput (CreatePipe(false, outStream))
            |> CreateProcess.withStandardError (UseStream(false, errStream))
            |> Proc.run

        // TODO realtime output
        outStream.Seek(0L, SeekOrigin.Begin) |> ignore
        errStream.Seek(0L, SeekOrigin.Begin) |> ignore
        Trace.log $"command end with code {res.ExitCode}: {cmd}"
        let outReader = new StreamReader(outStream)
        Trace.log "stdout:"
        Trace.log <| outReader.ReadToEnd()
        let errReader = new StreamReader(errStream)
        Trace.traceError "stderr:"
        Trace.traceError <| errReader.ReadToEnd()

        if res.ExitCode <> 0 then
            failwith "exec failed"

    static member CheckedExec(cmd, args: seq<string>) =
        Shell.CheckedExecInternal(cmd, (fun c -> CreateProcess.fromRawCommand c args))

    static member CheckedExec(cmd, ?args) =
        Shell.CheckedExecInternal(cmd, (fun c -> CreateProcess.fromRawCommandLine c (args |> Option.defaultValue "")))

let kubectl (nameSuffix: string) (args: string) =
    Trace.log $"kubectl {args}"
    Shell.CheckedExec("kubectl", $"--context=kind-it-{nameSuffix} {args}")

let setupK8sCluster nameSuffix =
    Trace.log $"setting up k8s cluster it-{nameSuffix}"

    // create cluster
    Shell.CheckedExec("kind", $"create cluster --name it-{nameSuffix} --config scripts/kind-config.yaml")
    // setup local registry
    Shell.CheckedExec("bash", "scripts/local_registry.sh")
    // allow volume expansion
    Shell.CheckedExec(
        "kubectl",
        [ "--context=kind-it-putthenquery"
          "patch"
          "sc"
          "standard"
          "-p"
          "{\"allowVolumeExpansion\":true}" ]
    )

    // setup istio
    Shell.CheckedExec("istioctl", "install -y")
    kubectl nameSuffix "apply -f k8s/istio-app"

    // install app
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
    runSingleIntegrationTest "putthenquery" (fun _ -> ())

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
