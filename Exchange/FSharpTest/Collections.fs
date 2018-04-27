namespace FSharpTest

open System.Linq
open Mikodev.Network
open Microsoft.VisualStudio.TestTools.UnitTesting

module Extension =
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

type Person = { id : int ; name : string }

type Book(id : int, name : string, price : decimal) =
    member this.id = id
    member this.name = name
    member this.price = price

[<TestClass>]
type Collections () =

    [<TestMethod>]
    member __.BasicCollections () =
        let source = [ for i in 0..9 do yield i * i ]
        let buffer = PacketConvert.Serialize(source)
        let reader = new PacketReader(buffer)
        let list = reader.Deserialize<int list>()
        let array = reader.Deserialize<int array>()
        let sequence = reader.Deserialize<int seq>()

        Extension.AreaSequenceEqual source list
        Extension.AreaSequenceEqual source array
        Extension.AreaSequenceEqual source sequence
        ()

    [<TestMethod>]
    member __.RecordCollections () =
        let source = 
            [for i in 0..9 do 
                yield { id = i; name = sprintf "%02x" i }]
        let buffer = PacketConvert.Serialize(source)
        let reader = new PacketReader(buffer)
        let list = reader.Deserialize<Person list>()
        let array = reader.Deserialize<Person array>()
        let sequence = reader.Deserialize<Person seq>()

        let objectList = reader.Deserialize<obj list>()
        let objectArray = reader.Deserialize<obj array>()

        Extension.AreaSequenceEqual source list
        Extension.AreaSequenceEqual source array
        Extension.AreaSequenceEqual source sequence
        ()

    [<TestMethod>]
    member __.Map () =
        let a = [0..15] |> List.map (fun r -> (sprintf "%d" r, { id = -r; name = sprintf "%02x" r })) |> Map
        let b = [0..15] |> List.map (fun r -> (r, sprintf "%d" r)) |> Map
        let ta = PacketConvert.Serialize(a)
        let tb = PacketConvert.Serialize(b)

        let ra = PacketConvert.Deserialize<Map<string, Person>>(ta)
        let rb = PacketConvert.Deserialize<Map<int, string>>(tb)

        Extension.AreMapEqual a ra
        Extension.AreMapEqual b rb
        ()

    [<TestMethod>]
    member __.Set () =
        let a = [0..15] |> List.map ((*) 2) |> Set
        let b = [0..15] |> List.map (sprintf "%d") |> Set
        let c = [0..15] |> List.map (fun r -> { id = r; name = sprintf "%d" r }) |> Set

        let ta = PacketConvert.Serialize(a)
        let tb = PacketConvert.Serialize(b)
        let tc = PacketConvert.Serialize(c)

        let ra = PacketConvert.Deserialize<Set<int>>(ta)
        let rb = PacketConvert.Deserialize<Set<string>>(tb)
        let rc = PacketConvert.Deserialize<Set<Person>>(tc)

        Extension.AreSetEqual a ra
        Extension.AreSetEqual b rb
        Extension.AreSetEqual c rc
        ()

    [<TestMethod>]
    member __.Class () =
        let a = new Book(1, "F# Pro", decimal(50))
        let w = PacketWriter.Serialize(a)
        let t = w.GetBytes()
        let r = new PacketReader(t)
        let v = r.Deserialize<Book>()
        ()
        