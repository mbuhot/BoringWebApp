module BoringWebApp.Values.ValuesService


let capitalizeValue (v: BoringValue) =
    { v with
        Value = v.Value.ToUpper()
    }
