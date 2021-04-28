﻿using System;
using System.IO;

namespace FileDB.Sample
{
    class MySampleRowClass
    {
        public int Aaa = 346;
        public string Bbb = "Sample text.";
        public float Ccc = 3.14159f;
    }

    class SampleProgram
    {
        static void Main(string[] args)
        {
            string pathdb = Path.GetTempFileName();
            File.Delete(pathdb);
            Directory.CreateDirectory(pathdb);
            Console.WriteLine($"Sample database created in {pathdb}");

            string table1_name = "my table";
            string table1_row1_index = "first row";
            string table1_row2_index = "second row";

            FileDB db = new FileDB(pathdb);

            db.CreateTable(table1_name);
            db[table1_name][table1_row1_index] = "the content of the first row is simply a string";
            Console.WriteLine($"The value of the first row is: \"{db[table1_name].GetRow<string>(table1_row1_index).Result}\".");

            db[table1_name][table1_row2_index] = new MySampleRowClass();
            MySampleRowClass readvalue = db[table1_name].GetRow<MySampleRowClass>(table1_row2_index).Result;
            Console.WriteLine($"The second row contains a {nameof(MySampleRowClass)} object: Aaa={readvalue.Aaa}, Bbb={readvalue.Bbb}, Ccc={readvalue.Ccc}.");
            Console.WriteLine($"The second row has been stored as a JSON file with contents:{Environment.NewLine}{File.ReadAllText(Path.Combine(pathdb, table1_name, table1_row2_index))}");

            db[table1_name].Delete(table1_row1_index);
            db[table1_name].Delete(table1_row2_index);
            db.DeleteTable(table1_name, deletecontents: false);
        }
    }
}
