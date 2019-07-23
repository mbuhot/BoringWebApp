/// This module contains some helpers that make working with XUnit.Assert and F# type inference a bit smoother.
module BoringWebApp.Test.Should

open System
open Xunit

let equal (expected: 'a) (actual: 'a) =
//    Assert.True((expected = actual), (sprintf "Expected %A, got %A" expected actual))
    Assert.Equal<'a>(expected, actual)

let beGreaterThan<'a when 'a :> IComparable<'a>> (expected: 'a) (actual: 'a) =
    Assert.True(actual.CompareTo(expected) > 0, sprintf "Expected %A to be greater than %A" actual expected)
