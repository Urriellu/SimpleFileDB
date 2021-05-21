# SimpleFileDB

SimpleFileDB is a free, fast, multiplatform, lightweight .NET Standard database library.

Databases are split into tables (stored as folders). Tables are split into rows (stored as JSON files). Rows are split into columns (stored as first-level properties in a JSON file).

## Usage

```C#
using System;
using System.IO;

namespace SimpleFileDB.Sample
{
    class MySampleRowClass
    {
        public int Aaa = 346;
        public string Bbb = "Sample text.";
        public float Ccc = 3.14159f;
    }

    class SampleProgram
    {
        static async void Main(string[] args)
        {
            string pathdb = Path.GetTempFileName();
            File.Delete(pathdb);
            Directory.CreateDirectory(pathdb);
            Console.WriteLine($"Sample database created in {pathdb}");

            string table1_name = "my table";
            string table1_row1_index = "first row";
            string table1_row2_index = "second row";

            SimpleFileDB db = new SimpleFileDB(pathdb);

            db.CreateTable(table1_name);
            db[table1_name][table1_row1_index] = "the content of the first row is simply a string"; // create/update a row
            string row_read_back = await db[table1_name].GetRow<string>(table1_row1_index); // read a row - note: support for asynchronous operations
            Console.WriteLine($"The value of the first row is: \"{row_read_back}\".");

            db[table1_name][table1_row2_index] = new MySampleRowClass(); // create/update a row containing an object
            MySampleRowClass readvalue = db[table1_name].GetRow<MySampleRowClass>(table1_row2_index).Result; // read a row as an object, synchronously
            Console.WriteLine($"The second row contains a {nameof(MySampleRowClass)} object: Aaa={readvalue.Aaa}, Bbb={readvalue.Bbb}, Ccc={readvalue.Ccc}.");
            Console.WriteLine($"The second row has been stored as a JSON file with contents:{Environment.NewLine}{File.ReadAllText(Path.Combine(pathdb, table1_name, table1_row2_index))}");

            db[table1_name].Delete(table1_row1_index);
            db[table1_name].Delete(table1_row2_index);
            db.DeleteTable(table1_name, deletecontents: false);
        }
    }
}
```

## Features

- Extremely easy to use.
- Multiplatform (platform agnostic).
- Each row is a simply JSON file. Therefore it can be easily stored on disk, network share, Google Drive, OneDrive...
- Row contents can be any serializable object. The value of a row can be an integer, a float, a string, or your own class.
- Read/write operations are asynchronous, so DB access does not necessarily block the rest of your application.
