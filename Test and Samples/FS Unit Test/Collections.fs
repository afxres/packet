namespace FSharpTest

open System.Linq
open Mikodev.Network
open Mikodev.Binary
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

    override this.ToString() = sprintf "id : %d, name : %s, price : %f" id name price

    override this.Equals obj =
        match obj with
        | :? Book as other -> id = other.id && name = other.name && price = other.price
        | _ -> false

    override this.GetHashCode() = 1313131313 + id + name.GetHashCode() + price.GetHashCode()

[<TestClass>]
type Collections () =
    
    let cache = new Cache()
    
    [<TestMethod>]
    member __.BasicCollections () =
        let source = [ for i in 0..9 do yield i * i ]
        let b1 = cache.Serialize(source)
        let b2 = PacketConvert.Serialize(source)

        let reader = new PacketReader(b1)
        let l1 = reader.Deserialize<int list>()
        let a1 = reader.Deserialize<int array>()
        let s1 = reader.Deserialize<int seq>()
        
        let token = cache.NewToken((Block)b2)
        let l2 = token.As<int list>()
        let a2 = token.As<int array>()
        let s2 = token.As<int seq>()

        Extension.AreaSequenceEqual source l1
        Extension.AreaSequenceEqual source a1
        Extension.AreaSequenceEqual source s1
        
        Extension.AreaSequenceEqual source l2
        Extension.AreaSequenceEqual source a2
        Extension.AreaSequenceEqual source s2
        ()

    [<TestMethod>]
    member __.RecordCollections () =
        let source = 
            [for i in 0..9 do 
                yield { id = i; name = sprintf "%02x" i }]
        let b1 = cache.Serialize(source)
        let b2 = PacketConvert.Serialize(source)
        let reader = new PacketReader(b1)
        let l1 = reader.Deserialize<Person list>()
        let a1 = reader.Deserialize<Person array>()
        let s1 = reader.Deserialize<Person seq>()

        let token = cache.NewToken((Block)b2)
        let l2 = token.As<Person list>()
        let a2 = token.As<Person array>()
        let s2 = token.As<Person seq>()

        let objectList = reader.Deserialize<obj list>()
        let objectArray = reader.Deserialize<obj array>()

        Extension.AreaSequenceEqual source l1
        Extension.AreaSequenceEqual source a1
        Extension.AreaSequenceEqual source s1
        
        Extension.AreaSequenceEqual source l2
        Extension.AreaSequenceEqual source a2
        Extension.AreaSequenceEqual source s2
        ()

    [<TestMethod>]
    member __.Map () =
        let a = [0..15] |> List.map (fun r -> (sprintf "%d" r, { id = -r; name = sprintf "%02x" r })) |> Map
        let b = [0..15] |> List.map (fun r -> (r, sprintf "%d" r)) |> Map
        let ta1 = cache.Serialize(a)
        let tb1 = cache.Serialize(b)
        let ta2 = PacketConvert.Serialize(a)
        let tb2 = PacketConvert.Serialize(b)

        let ra1 = PacketConvert.Deserialize<Map<string, Person>>(ta1)
        let rb1 = PacketConvert.Deserialize<Map<int, string>>(tb1)
        let ra2 = cache.Deserialize<Map<string, Person>>((Block)ta2)
        let rb2 = cache.Deserialize<Map<int, string>>((Block)tb2)

        Extension.AreMapEqual a ra1
        Extension.AreMapEqual b rb1
        Extension.AreMapEqual a ra2
        Extension.AreMapEqual b rb2
        ()

    [<TestMethod>]
    member __.Set () =
        let a = [0..15] |> List.map ((*) 2) |> Set
        let b = [0..15] |> List.map (sprintf "%d") |> Set
        let c = [0..15] |> List.map (fun r -> { id = r; name = sprintf "%d" r }) |> Set

        let ta1 = cache.Serialize(a)
        let tb1 = cache.Serialize(b)
        let tc1 = cache.Serialize(c)
        let ta2 = PacketConvert.Serialize(a)
        let tb2 = PacketConvert.Serialize(b)
        let tc2 = PacketConvert.Serialize(c)

        let ra1 = PacketConvert.Deserialize<Set<int>>(ta1)
        let rb1 = PacketConvert.Deserialize<Set<string>>(tb1)
        let rc1 = PacketConvert.Deserialize<Set<Person>>(tc1)
        let ra2 = cache.Deserialize<Set<int>>((Block)ta2)
        let rb2 = cache.Deserialize<Set<string>>((Block)tb2)
        let rc2 = cache.Deserialize<Set<Person>>((Block)tc2)

        Extension.AreSetEqual a ra1
        Extension.AreSetEqual b rb1
        Extension.AreSetEqual c rc1
        Extension.AreSetEqual a ra2
        Extension.AreSetEqual b rb2
        Extension.AreSetEqual c rc2
        ()

    [<TestMethod>]
    member __.Class () =
        let a = new Book(1, "F# Pro", decimal(50))
        let w = PacketWriter.Serialize(a)
        let t2 = w.GetBytes()
        let t1 = cache.Serialize(a)
        let r = new PacketReader(t1)
        let r1 = r.Deserialize<Book>()
        let r2 = cache.Deserialize<Book>((Block)t2)
        Assert.AreEqual(a, r1)
        Assert.AreEqual(a, r2)
        ()
        