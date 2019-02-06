# StructLib
A .Net library to define structures at runtime using formats like Python struct

### The accepted format are
A struct formula is a string composed of the following chars

| Char | Type | Size in bytes   |
------|------|-------|
| i | int   | 4                 |
| I | uint  | 4                 |
| l | int   | 4                 |
| L | uint  | 4                 |
| h | short | 2                 |
| H | ushort| 2                 |
| b | byte  | 1                 |
| c | char  | 1                 |
| ? | bool  | 1                 |
| q | Int64 | 8                 |
| Q | UInt64| 8                 |
| f | float | 4                 |
| d | double| 8                 |
| s | string| null terminated   |

You notice that 'l' and 'L' are translated to **int** and **uint** resp., it's to keep the same [definitions as python][format-characters] because C# defines **long** and **ulong** as 8 bytes

[format-characters]:https://docs.python.org/2/library/struct.html#format-characters

### Json
you can combine the chars above with field names to into a json format as follow : 
{ "NameOfField0" : "TypeFormatOfField0",...,"NameOfField0N" :"TypeFormatOfFieldN"}

### How it works?
* An instance of Generator class uses reflection to define a struct based on a given formula (the string described above)
* Use that Generator to create an of Structure class, this one encapsulated an instance of the struct defined before
* You can get/set values from/to fields by accessin to StructureInstance["FieldName"].Value
* You can set pack/unpack binary values (byte[]) same as python's strcut

### Example
```C#
string jsonFormula = "{'A' : 'I','B' :'b', 'C' : 'c'}";
var gen = Generator.CreateGeneratorJson(jsonFormula);

var inst = gen.CreateInstance(); // create a new instance of the struct

inst["A"].Value = 1U;
inst["B"].Value = (byte)2;
inst["C"].Value = 'C';
foreach (byte b in inst.Pack())
    Console.Write($"{b:x02} ");// 01 00 00 00 02 43
    
Console.WriteLine("");

var inst2 = Generator.UnpackJSon(jsonFormula, inst.Pack()); // create another instance of the struct using data packed from the previous one
foreach (byte b in inst2.Pack()) // it will show the same output
    Console.Write($"{b:x02} ");// 01 00 00 00 02 43 
```

### Case of use
In my case I've written this library to use it with program that communicates with arduino throught serial port
The program offers and advanced serial reader/writer with the possiblity to pack/unpack sent/received data


