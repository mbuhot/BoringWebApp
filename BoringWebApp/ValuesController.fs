namespace BoringWebApp.Controllers

open BoringWebApp
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Routing

[<CLIMutable>]
type CreateRequest = {
    Value: string
}

type IntId = { Id: int }

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
        this.CreatedAtAction("Show", {|Id=id|}, body) |> ActionResult<'body>


    [<HttpGet("api/values/", Name="Values.Index")>]
    [<ProducesResponseType(200)>]
    member __.Index() : ActionResult<BoringValue[]> =
        repo.All() |> Seq.toArray |> ok

    [<HttpGet("api/values/{id}", Name="Values.Show")>]
    [<ProducesResponseType(200)>]
    member __.Show(id: int) : ActionResult<BoringValue> =
        repo.FindById id |> ok

    [<HttpPost("api/values/", Name="Values.Create")>]
    [<ProducesResponseType(201)>]
    member this.Create([<FromBody>] request: CreateRequest) : ActionResult<IntId> =
        let value = repo.Insert {Id=0; Value=request.Value}
        let responseBody = { Id = value.Id }
        responseBody |> created value.Id

    [<HttpPost("api/values/{id}", Name="Values.Update")>]
    [<ProducesResponseType(200)>]
    member __.Update(id:int, [<FromBody>] request: CreateRequest) : ActionResult<BoringValue> =
        use txn = repo.BeginTransaction()
        let original = repo.FindByIdForUpdate id
        let updated = {original with Value=request.Value}
        let n = repo.Update(updated)
        txn.Commit()
        if n = 1 then
            updated |> ok
        else
            failwith "Failed to update"

    [<HttpDelete("api/values/{id}", Name="Values.Delete")>]
    [<ProducesResponseType(204)>]
    member __.Delete(id:int) =
        if repo.Delete(id) = 1 then
            NoContentResult()
        else
            failwith "Failed to delete"
