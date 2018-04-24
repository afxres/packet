namespace FSharpTest

open System.Linq
open Mikodev.Network
open Microsoft.VisualStudio.TestTools.UnitTesting

module Extension =
    let AreaSequenceEqual<'t> (a : seq<'t>) (b : seq<'t>) = 
        if a.SequenceEqual b = false then 
            Assert.Fail()
        else ()

type Person = { id : int ; name : string }

[<TestClass>]
type FSharpTestClass () =

    [<TestMethod>]
    member __.BasicCollection () =
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