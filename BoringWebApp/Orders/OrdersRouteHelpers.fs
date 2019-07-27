namespace BoringWebApp.Orders
open Microsoft.AspNetCore.Routing

type OrdersRouteHelpers(linkGenerator: LinkGenerator) =
    let byActionValues (a: string) (parameters: 'a) =
        linkGenerator.GetPathByAction(a, "Orders", parameters)

    let byAction (a: string) =
        linkGenerator.GetPathByAction(a, "Orders")

    member this.Index =
        byAction "Index"

    member this.Create =
        byAction "Create"

    member this.Show (orderId: int) =
        byActionValues "Show" {|OrderId=orderId|}

    member this.AddItem (orderId: int) =
        byActionValues "AddItem" {|OrderId=orderId|}

    member this.RemoveItem (orderId: int, orderItemId: int) =
        byActionValues "RemoveItem" {|OrderId=orderId; OrderItemId=orderItemId|}
