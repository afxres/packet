# Binary packet
基于键值对形式的二进制数据包生成解析工具, 方便在网络上传输数据.

## 基本信息
* 示例代码: [`Program.cs`](https://github.com/afxres/packet/blob/master/Exchange/Sample/Program.cs)
* 单元测试: [`Entrance.cs`](https://github.com/afxres/packet/blob/master/Exchange/Testing/Entrance.cs)
* NuGet Package: [`Mikodev.Exchange`](https://www.nuget.org/packages/Mikodev.Exchange/)

### 支持的类型 (可自定义类型转换器)
```
Byte, SByte, Int16, UInt16, Int32, Uint32, Int64, UInt64
Single, Double
String (UTF-8), DateTime (As Int64), TimeSpan (As Int64), Guid
IPAddress, IPEndPoint
```

### 支持的集合
```
一维数组, List, Dictionary
实现 IEnumerable 接口, 且含有构造函数参数为 IEnumerable<T> 的类型
实现 IEnumerable 接口, 且含有 Add(T item) 方法
实现 IDictionary<T, K> 接口 (仅序列化)
F# List, F# Map, F# Set
```

### 其他信息
* 字节序: 默认为小端, 在大端系统中自动翻转预设类型的字节序 (可通过修改源码中的 Extension.UseLittleEndian 字段来控制字节序)
* 自定义转换器: 继承 ``` PacketConverter<T> ``` 并实现抽象方法; 若类型字节长度不固定, 将 ``` Length ``` 属性设为 ``` 0 ```

## 代码示例

引用命名空间
``` csharp
using Mikodev.Network;
```

### 读写示例

基本格式读写
```csharp
var packet = new PacketWriter()
    .SetValue("id", Guid.NewGuid())
    .SetValue("name", "Alice")
    .SetItem("data", new PacketWriter() // 嵌套
        .SetValue("timestamp", DateTime.Now)
        .SetEnumerable("tags", new[] { "girl", "doctor" }) // 写入集合
    );

var buffer = packet.GetBytes(); // 生成二进制数据包
var reader = new PacketReader(buffer); // 读取数据包

var id = reader["id"].GetValue<Guid>();
var name = (string)reader["name"].GetValue(typeof(string)); // 指定类型读取
var time = reader["data/timestamp"].GetValue<DateTime>(); // 读取子节点
var tags = reader["data/tags"].GetArray<string>();
```

动态读写
```csharp
var packet = new PacketWriter();
var d = (dynamic)packet;
d.id = 1024;
d.name = "Bob";
d.data.ipaddr = IPAddress.Loopback;
d.data.tags = new[] { "boy", "tall" };

var buffer = packet.GetBytes();
var reader = new PacketReader(buffer);
var r = (dynamic)reader;

var id = (int)r.id;
var name = (string)r.name;
var address = (IPAddress)r.data.ipaddr;
var tags = (string[])r.data.tags;
```

### 序列化写入

序列化匿名对象
```csharp
var packet = PacketWriter.Serialize(new
{
    id = Guid.NewGuid(),
    name = "Candy",
    details = new
    {
        age = 18,
    },
});
```

### 自定义类型, 转换器

自定义数据类
```csharp
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public IEnumerable<string> Tags { get; set; }
}
```

自定义泛型转换器
```csharp
public class PersonConverter : IPacketConverter<Person>
{
    public int Length => 0; // 长度非固定, 返回零

    public byte[] GetBytes(Person value)
    {
        if (value == null)
            return new byte[0];
        // 借助 PacketRawWriter, 生成固定格式数据包
        var raw = new PacketRawWriter();
        raw.SetValue(value.Id);
        raw.SetValue(value.Name);

        var tags = value.Tags;
        if (tags != null)
            foreach (var i in tags)
                raw.SetValue(i);
        return raw.GetBytes();
    }

    public Person GetValue(byte[] buffer, int offset, int length)
    {
        var p = new Person();
        // 使用 PacketRawReader, 解析固定格式数据包
        var raw = new PacketRawReader(buffer, offset, length);
        p.Id = raw.GetValue<int>();
        p.Name = raw.GetValue<string>();

        var tags = new List<string>();
        while (raw.Any)
            tags.Add(raw.GetValue<string>());
        p.Tags = tags;
        return p;
    }

    byte[] IPacketConverter.GetBytes(object value)
    {
        return GetBytes((Person)value);
    }

    object IPacketConverter.GetValue(byte[] buffer, int offset, int length)
    {
        return GetValue(buffer, offset, length);
    }
}
```

使用自定义转换器词典读写数据
```csharp
var p = new Person
{
    Id = 2048,
    Name = "Emma",
    Tags = new[] { "cute" },
};

var customConverters = new Dictionary<Type, IPacketConverter>()
{
    [typeof(Person)] = new PersonConverter(),
};

var packet = PacketWriter.Serialize(p, customConverters);
var buffer = packet.GetBytes();
var reader = new PacketReader(buffer, customConverters);
var person = reader.GetValue<Person>();
```

### 对 F# 的支持

F# 集合 List Map Set
``` fsharp
open Mikodev.Network
open System

[<EntryPoint>]
let main argv = 
    // list
    let list = [ for i in 0..9 do yield i * i ]
    let ta = PacketConvert.Serialize(list)
    let ra = PacketConvert.Deserialize<int list>(ta)

    // map
    let map = seq { for i in 0..9 do yield (i, sprintf "%x" (i * i)) } |> Map
    let tb = PacketConvert.Serialize(map)
    let rb = PacketConvert.Deserialize<Map<int, string>>(tb)

    // set
    let random = new Random()
    let set = seq { for i in 0..9 do yield random.Next() } |> Set
    let tc = PacketConvert.Serialize(set)
    let rc = PacketConvert.Deserialize<Set<int>>(tc)

    0
```

记录
``` fsharp
open Mikodev.Network
open System

type Book = { id : Guid; title : string; tags : string list }

[<EntryPoint>]
let main argv = 
    let blank = { id = Guid.NewGuid(); title = "Way to bug"; tags = [ "exciting"; "marvelous" ] }
    let buffer = PacketConvert.Serialize(blank)
    let book = PacketConvert.Deserialize<Book>(buffer)
    0
```
