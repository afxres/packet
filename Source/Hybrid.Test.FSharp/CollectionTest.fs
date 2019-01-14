namespace FSharpTest

open Mikodev.Network
open Mikodev.Binary
open Microsoft.VisualStudio.TestTools.UnitTesting
open System

type Person = { id : int ; name : string }

type Book(id : int, name : string, price : decimal) =
    member __.id = id

    member __.name = name

    member __.price = price

    override __.ToString() = sprintf "id : %d, name : %s, price : %f" id name price

    override __.Equals obj =
        match obj with
        | :? Book as other -> id = other.id && name = other.name && price = other.price
        | _ -> false

    override __.GetHashCode() = 1313131313 + id + name.GetHashCode() + price.GetHashCode()

[<TestClass>]
type CollectionTest () =
    
    let generator = new Generator()
    
    [<TestMethod>]
    member __.BasicCollections () =
        let source = [ for i in 0..9 do yield i * i ]
        let b1 = generator.ToBytes(source)
        let b2 = PacketConvert.Serialize(source)

        let reader = new PacketReader(b1)
        let l1 = reader.Deserialize<int list>()
        let a1 = reader.Deserialize<int array>()
        let s1 = reader.Deserialize<int seq>()
        
        let token = generator.AsToken(new ReadOnlyMemory<byte>(b2))
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
        let b1 = generator.ToBytes(source)
        let b2 = PacketConvert.Serialize(source)
        let reader = new PacketReader(b1)
        let l1 = reader.Deserialize<Person list>()
        let a1 = reader.Deserialize<Person array>()
        let s1 = reader.Deserialize<Person seq>()

        let token = generator.AsToken(new ReadOnlyMemory<byte>(b2))
        let l2 = token.As<Person list>()
        let a2 = token.As<Person array>()
        let s2 = token.As<Person seq>()

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
        let ta1 = generator.ToBytes(a)
        let tb1 = generator.ToBytes(b)
        let ta2 = PacketConvert.Serialize(a)
        let tb2 = PacketConvert.Serialize(b)

        let ra1 = PacketConvert.Deserialize<Map<string, Person>>(ta1)
        let rb1 = PacketConvert.Deserialize<Map<int, string>>(tb1)
        let ra2 = generator.ToValue<Map<string, Person>>(new ReadOnlySpan<byte>(ta2))
        let rb2 = generator.ToValue<Map<int, string>>(new ReadOnlySpan<byte>(tb2))

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

        let ta1 = generator.ToBytes(a)
        let tb1 = generator.ToBytes(b)
        let tc1 = generator.ToBytes(c)
        let ta2 = PacketConvert.Serialize(a)
        let tb2 = PacketConvert.Serialize(b)
        let tc2 = PacketConvert.Serialize(c)

        let ra1 = PacketConvert.Deserialize<Set<int>>(ta1)
        let rb1 = PacketConvert.Deserialize<Set<string>>(tb1)
        let rc1 = PacketConvert.Deserialize<Set<Person>>(tc1)
        let ra2 = generator.ToValue<Set<int>>(new ReadOnlySpan<byte>(ta2))
        let rb2 = generator.ToValue<Set<string>>(new ReadOnlySpan<byte>(tb2))
        let rc2 = generator.ToValue<Set<Person>>(new ReadOnlySpan<byte>(tc2))

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
        let t1 = generator.ToBytes(a)
        let r = new PacketReader(t1)
        let r1 = r.Deserialize<Book>()
        let r2 = generator.ToValue<Book>(new ReadOnlySpan<byte>(t2))
        Assert.AreEqual(a, r1)
        Assert.AreEqual(a, r2)
        ()
        