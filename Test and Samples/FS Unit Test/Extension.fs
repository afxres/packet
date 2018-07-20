module FSharpTest.Extension

open System.Linq
open Microsoft.VisualStudio.TestTools.UnitTesting

let AreaSequenceEqual<'t> (a : seq<'t>) (b : seq<'t>) = 
    if a.SequenceEqual b = false then 
        Assert.Fail()
    else ()

let AreMapEqual<'k , 'v when 'k : comparison and 'v : equality> (a : Map<'k, 'v>) (b : Map<'k, 'v>) =
    if (a |> Map.count) <> (b |> Map.count) then
        Assert.Fail()
    for p in a do
        if p.Value <> b.[p.Key] then
            Assert.Fail()
    ()

let AreSetEqual<'t when 't : comparison and 't : equality> (a : Set<'t>) (b : Set<'t>) =
    if (a |> Set.count) <> (b |> Set.count) then
        Assert.Fail()
    for i in a do
        if not (b |> Seq.contains i) then
            Assert.Fail()
    ()