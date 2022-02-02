namespace LearnDevOps.Frontend

#nowarn "20"

open System
open Nest

type Doc = { Id: int; Content: string }

type ESAgent(uri: string) =
    let settings = new ConnectionSettings(Uri(uri))
    let client = ElasticClient(settings)

    member this.Query() =
        client.Search<Doc> (fun (s: SearchDescriptor<Doc>) ->
            s
                .From(0)
                .Size(10)
                .Query(fun q -> q.Match(fun m -> m.Field(fun f -> f.Content).Query("Martijn"))))


    member this.Put() =
        let doc =
            { Doc.Id = 1
              Content = "hello_elastic" }

        client.Index(doc, (fun idx -> idx.Index("test_index")))
