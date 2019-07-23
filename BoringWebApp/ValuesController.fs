namespace BoringWebApp.Controllers

open System.Threading.Tasks
open BoringWebApp
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Routing
open FSharp.Control.Tasks.V2

[<CLIMutable>]
type ValueRequest =
    {
        Value: string
    }

[<CLIMutable>]
type IntId =
    {
        Id: int
    }

type ValuesRouteHelpers(linkGenerator: LinkGenerator) =
    let byActionValues (a: string) (parameters: 'a) = linkGenerator.GetPathByAction(a, "Values", parameters)
    let byAction (a: string) = byActionValues a null

    member this.Index            = byAction "Index"
    member this.Create           = byAction "Create"
    member this.Show (id: int)   = byActionValues "Show" {Id=id}
    member this.Delete (id: int) = byActionValues "Delete" {Id=id}
    member this.Update (id: int) = byActionValues "Update" {Id=id}

[<ApiController>]
type ValuesController (repo: ValueRepository) as this =
    inherit ControllerBase()

    let ok (body: 'body) = ActionResult<'body>(body)
    let created (id: int) (body: 'body) =
        let actionParams = {|Id=id|}
        this.CreatedAtAction("Show", actionParams, body) |> ActionResult<'body>


    [<HttpGet("api/values/", Name="Values.Index")>]
    [<ProducesResponseType(200)>]
    member __.Index() : Task<ActionResult<BoringValue[]>> =
        task {
            let! values = repo.All()
            return values |> Seq.toArray |> ok
        }

    [<HttpGet("api/values/{id}", Name="Values.Show")>]
    [<ProducesResponseType(200)>]
    member __.Show(id: int) : Task<ActionResult<BoringValue>> =
        task {
            let! result = repo.FindById id
            return result |> ok
        }

    [<HttpPost("api/values/", Name="Values.Create")>]
    [<ProducesResponseType(201)>]
    member this.Create([<FromBody>] request: ValueRequest) : Task<ActionResult<IntId>> =
        task {
            let! value = repo.Insert {Id=0; Value=request.Value}
            let responseBody = { Id = value.Id }
            return responseBody |> created value.Id
        }

    [<HttpPost("api/values/{id}", Name="Values.Update")>]
    [<ProducesResponseType(200)>]
    member __.Update(id:int, [<FromBody>] request: ValueRequest) : Task<ActionResult<BoringValue>> =
        task {
            use! txn = repo.BeginTransaction()
            let! original = repo.FindByIdForUpdate id
            let updated = {original with Value=request.Value}
            let! n = repo.Update(updated)
            do! txn.CommitAsync()
            match n with
            | 1 -> return updated |> ok
            | _ -> return failwith "Failed to delete"
        }

    [<HttpDelete("api/values/{id}", Name="Values.Delete")>]
    [<ProducesResponseType(204)>]
    member __.Delete(id:int) =
        task {
            match! repo.Delete(id) with
            | 1 -> return NoContentResult() :> ActionResult
            | _ -> return NotFoundResult() :> ActionResult
        }
