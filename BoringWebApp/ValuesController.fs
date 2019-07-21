namespace BoringWebApp.Controllers

open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Routing

type ValuesRouteHelpers(linkGenerator: LinkGenerator) =

    member this.IndexPath =
        linkGenerator.GetPathByName("Values.Index", values=null)

    member this.ShowPath(id: int) =
        let values = {|Id = id|}
        linkGenerator.GetPathByName("Values.Show", values=values)

    member this.CreatePath =
        linkGenerator.GetPathByName("Values.Create", values=null)

    member this.UpdatePath(id: int) =
        linkGenerator.GetPathByName("Values.Update", values=id)

    member this.DeletePath(id: int) =
        linkGenerator.GetPathByName("Values.Delete", values=id)


[<CLIMutable>]
type CreateRequest = {
    Value: string
}

[<ApiController>]
type ValuesController (routes: ValuesRouteHelpers) =
    inherit ControllerBase()

    [<HttpGet("api/values/", Name="Values.Index")>]
    member __.Index() =
        let values = [|"value1"; "value2"|]
        ActionResult<string[]>(values)

    [<HttpGet("api/values/{id}", Name="Values.Show")>]
    member __.Show(id:int) =
        let value = "value"
        ActionResult<string>(value)

    [<HttpPost("api/values/", Name="Values.Create")>]
    member this.Create([<FromBody>] request: CreateRequest) =
        let link = routes.ShowPath(id=123)
        let responseBody = {| Value = "12345" |}
        CreatedResult(location=link, value=responseBody)

    [<HttpPut("api/values/{id}", Name="Values.Update")>]
    member __.Update(id:int, [<FromBody>] value:string) =
        OkResult()

    [<HttpDelete("api/values/{id}", Name="Values.Delete")>]
    member __.Delete(id:int) =
        NoContentResult()
