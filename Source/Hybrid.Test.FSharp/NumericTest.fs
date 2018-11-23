namespace FSharpTest

open Mikodev.Network
open Microsoft.VisualStudio.TestTools.UnitTesting
open System

type Integers = {
    int16Array : int16 array;
    int32Array : int32 array;
    int64Array : int64 array;
    uint16Array : uint16 array;
    uint32Array : uint32 array;
    uint64Array : uint64 array 
}

[<TestClass>]
type NumericTest () =

    member __.ValidateInteger value = 
        let wa = new PacketWriter()
        wa.SetEnumerable("i16", value.int16Array) |> ignore
        wa.SetEnumerable("i32", value.int32Array) |> ignore
        wa.SetEnumerable("i64", value.int64Array) |> ignore
        wa.SetEnumerable("u16", value.uint16Array) |> ignore
        wa.SetEnumerable("u32", value.uint32Array) |> ignore
        wa.SetEnumerable("u64", value.uint64Array) |> ignore

        let ta = wa.GetBytes()
        let ra = new PacketReader(ta)
        let rai16 = ra.["i16"].GetArray<int16>()
        let rai32 = ra.["i32"].GetArray<int32>()
        let rai64 = ra.["i64"].GetArray<int64>()
        let rau16 = ra.["u16"].GetArray<uint16>()
        let rau32 = ra.["u32"].GetArray<uint32>()
        let rau64 = ra.["u64"].GetArray<uint64>()

        Extension.AreaSequenceEqual rai16 value.int16Array
        Extension.AreaSequenceEqual rai32 value.int32Array
        Extension.AreaSequenceEqual rai64 value.int64Array
        Extension.AreaSequenceEqual rau16 value.uint16Array
        Extension.AreaSequenceEqual rau32 value.uint32Array
        Extension.AreaSequenceEqual rau64 value.uint64Array

        let wb = PacketWriter.Serialize(value)
        let tb = wb.GetBytes()
        let rb = new PacketReader(tb)
        let vb = rb.Deserialize<Integers>()
        
        Extension.AreaSequenceEqual vb.int16Array value.int16Array
        Extension.AreaSequenceEqual vb.int32Array value.int32Array
        Extension.AreaSequenceEqual vb.int64Array value.int64Array
        Extension.AreaSequenceEqual vb.uint16Array value.uint16Array
        Extension.AreaSequenceEqual vb.uint32Array value.uint32Array
        Extension.AreaSequenceEqual vb.uint64Array value.uint64Array
        ()

    [<TestMethod>]
    member this.EmptyIntegers () =
        let value = {
            int16Array = Array.zeroCreate<int16>(0);
            int32Array = Array.zeroCreate<int32>(0);
            int64Array = Array.zeroCreate<int64>(0);
            uint16Array = Array.zeroCreate<uint16>(0);
            uint32Array = Array.zeroCreate<uint32>(0);
            uint64Array = Array.zeroCreate<uint64>(0); }

        this.ValidateInteger value
        ()
        
    [<TestMethod>]
    member this.RandomIntegers () =
        let random = new Random()
        let buffer = Array.zeroCreate<byte>(sizeof<int64>)
        let number () =
            random.NextBytes(buffer)
            BitConverter.ToInt64(buffer, 0)

        let max () = random.Next(4, 16)

        let value = {
            int16Array = [| for i in 0..max() do yield int16(number()) |];
            int32Array = [| for i in 0..max() do yield int32(number()) |];
            int64Array = [| for i in 0..max() do yield int64(number()) |];
            uint16Array = [| for i in 0..max() do yield uint16(number()) |];
            uint32Array = [| for i in 0..max() do yield uint32(number()) |];
            uint64Array = [| for i in 0..max() do yield uint64(number()) |] }

        this.ValidateInteger value
        ()

    member __.ValidateChars (buffer : char array) =
        let wa = new PacketWriter()
        wa.SetEnumerable("chars", buffer) |> ignore
        let ta = wa.GetBytes()
        let ra = new PacketReader(ta)
        let rac = ra.["chars"].GetArray<char>()

        let wb = PacketWriter.Serialize(buffer)
        let tb = wb.GetBytes()
        let rb = new PacketReader(tb)
        let rbc = rb.Deserialize<char array>()

        Extension.AreaSequenceEqual buffer rac
        Extension.AreaSequenceEqual buffer rbc
        ()

    [<TestMethod>]
    member this.EmptyChars () =
        this.ValidateChars (Array.zeroCreate<char>(0))
        ()

    [<TestMethod>]
    member this.RandomChars () =
        let random = new Random()
        let buffer = [| for i in 0..random.Next(0, 16) do yield char(random.Next(0x4e00, 0x9fa6)) |]
        
        this.ValidateChars buffer
        ()
