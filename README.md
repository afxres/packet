# Binary packet
基于键值对形式的二进制数据包生成解析工具, 方便在网络上传输数据.

## 基本信息
* 示例代码: [`Program.cs`](https://github.com/afxres/packet/blob/master/Exchange/Sample/Program.cs)
* 单元测试: [`Entrance.cs`](https://github.com/afxres/packet/blob/master/Exchange/Testing/Entrance.cs)
* NuGet Package: [`Mikodev.Exchange`](https://www.nuget.org/packages/Mikodev.Exchange/)

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
var tags = reader["data/tags"].GetArray();
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

序列化词典 (也可用于 ExpandoObject)
```csharp
var packet = PacketWriter.Serialize(new Dictionary<string, object>()
{
    ["integer"] = 1024,
    ["directory"] = new Dictionary<string, object>()
    {
        ["string"] = "Dave",
    },
    ["anonymous"] = new
    {
        number = 20,
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
