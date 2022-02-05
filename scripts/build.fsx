#r "paket:
nuget FSharp.Control.AsyncSeq
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"

open System.IO
open FSharp.Control
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

type Shell with
    static member private CheckedExecInternal(cmd, f) =
        Trace.log $"command begin: {cmd}"
        let outStream = StreamRef.Empty
        let errStream = StreamRef.Empty

        let res =
            f cmd
            |> CreateProcess.withStandardOutput (CreatePipe(outStream))
            |> CreateProcess.withStandardError (CreatePipe(errStream))
            |> CreateProcess.ensureExitCode
            |> Proc.start

        let outReader = new StreamReader(outStream.Value)
        let errReader = new StreamReader(errStream.Value)

        let readerToSeq (reader: StreamReader) err =
            AsyncSeq.replicateUntilNoneAsync (
                async {
                    let! line = reader.ReadLineAsync() |> Async.AwaitTask
                    return Option.ofObj line
                }
            )
            |> AsyncSeq.map (fun line -> (line, err))

        AsyncSeq.merge (readerToSeq outReader false) (readerToSeq errReader true)
        |> AsyncSeq.iter (fun (line, err) ->
            let printer =
                if err then
                    Trace.traceError
                else
                    Trace.log
            printer $"[{cmd}] {line}")
        |> Async.RunSynchronously

        res.Wait()
        Trace.log $"command end: {cmd}"

    static member CheckedExec(cmd, args: seq<string>) =
        Shell.CheckedExecInternal(cmd, (fun c -> CreateProcess.fromRawCommand c args))

    static member CheckedExec(cmd, ?args) =
        Shell.CheckedExecInternal(cmd, (fun c -> CreateProcess.fromRawCommandLine c (args |> Option.defaultValue "")))

let kubectl (nameSuffix: string) (args: string) =
    Trace.log $"kubectl {args}"
    Shell.CheckedExec("kubectl", $"--context=kind-it-{nameSuffix} {args}")

let setupK8sPortForward idx nameSuffix =
    let mutable proc = null
    CreateProcess.fromRawCommandLine
        "kubectl"
        ($"port-forward -n istio-system"
         + $" service/istio-ingressgateway {8080 + idx}:80")
    |> CreateProcess.redirectOutput
    |> CreateProcess.addOnStartedEx (fun info -> proc <- info.Process)
    |> Proc.start
    |> ignore
    proc

type ClusterContext =
    { Name: string
      Idx: int
      PortForwardProcess: System.Diagnostics.Process }

let setupK8sCluster idx nameSuffix =
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
    kubectl nameSuffix "label namespace default istio-injection=enabled"

    // todo
    kubectl
        nameSuffix
        ("create secret generic frontend-es-cred"
         + " --from-literal=ES_URI=http://quickstart-es-http:9200"
         + " --from-literal=ES_PASSWORD=todo"
         + " --from-literal=ES_USERNAME=todo")
    // install app
    Shell.CheckedExec(
        "helm",
        "upgrade --install frontend k8s/charts/frontend"
        + " --set imageVersion=2022-02-02-19-53"
        + " --set registry=localhost:5000"
    )

    let proc = setupK8sPortForward idx nameSuffix
    { Name = nameSuffix
      Idx = idx
      PortForwardProcess = proc }

let teardownK8sCluster (ctx: ClusterContext) =
    ctx.PortForwardProcess.Kill()
    Shell.CheckedExec("kind", $"delete cluster --name it-{ctx.Name}")

let mutable nextTestIndex = 0

let runSingleIntegrationTest name body =
    Trace.log $"Integration test: {name}"
    let idx = nextTestIndex
    nextTestIndex <- nextTestIndex + 1
    Shell.Exec("kind", $"delete cluster --name it-{name}")
    |> ignore
    let ctx = setupK8sCluster idx name
    body idx name
    System.Threading.Thread.Sleep(10000 * 100)
    teardownK8sCluster ctx
// teardownK8sCluster name

let testPutThenQuery () =
    runSingleIntegrationTest "putthenquery" (fun _ _ -> ())

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
