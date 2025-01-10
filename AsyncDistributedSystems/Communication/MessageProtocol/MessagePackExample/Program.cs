using MessagePack;
using System;
using System.IO;

// Define a class to be serialized/deserialized
[MessagePackObject]
public class MyData
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public string Name { get; set; }

    [Key(2)]
    public bool IsActive { get; set; }

    [Key(3)]
    public DateTime CreatedDate { get; set; }

    [Key(4)]
    public string[] Tags { get; set; }
}

public class Program
{
    public static void Main(string[] args)
    {
        // Create an instance of MyData
        var myData = new MyData
        {
            Id = 123,
            Name = "Example Data",
            IsActive = true,
            CreatedDate = DateTime.Now,
            Tags = new string[] { "tag1", "tag2", "tag3" }
        };

        // 1. Serialize to a byte array
        byte[] serializedBytes = MessagePackSerializer.Serialize(myData);

        // Display the serialized bytes (for demonstration purposes)
        Console.WriteLine("Serialized Bytes:");
        Console.WriteLine(BitConverter.ToString(serializedBytes));
        Console.WriteLine();

        // 2. Serialize to a file
        string filePath = "mydata.msgpack";
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            MessagePackSerializer.Serialize(fileStream, myData);
        }
        Console.WriteLine($"Data serialized to file: {filePath}");
        Console.WriteLine();

        // 3. Deserialize from the byte array
        MyData deserializedData = MessagePackSerializer.Deserialize<MyData>(serializedBytes);

        // Display the deserialized data
        Console.WriteLine("Deserialized Data (from byte array):");
        DisplayData(deserializedData);
        Console.WriteLine();

        // 4. Deserialize from the file
        using (var fileStream = new FileStream(filePath, FileMode.Open))
        {
            MyData deserializedFromFile = MessagePackSerializer.Deserialize<MyData>(fileStream);
            Console.WriteLine("Deserialized Data (from file):");
            DisplayData(deserializedFromFile);
        };
    }

    // Helper function to display the data
    static void DisplayData(MyData data)
    {
        Console.WriteLine($"  Id: {data.Id}");
        Console.WriteLine($"  Name: {data.Name}");
        Console.WriteLine($"  IsActive: {data.IsActive}");
        Console.WriteLine($"  CreatedDate: {data.CreatedDate}");
        if (data.Tags != null)
        {
            Console.WriteLine($"  Tags: {string.Join(", ", data.Tags)}");
        }
        else
        {
            Console.WriteLine($"  Tags: (not present in serialized data)");
        }
    }
}