using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.VisualBasic;

namespace SimpleFileDB
{
    [TestClass]
    public class SimpleFileDBTests
    {
        static Random rnd = new();
        public static string RandomString(int minlength, int maxlength) => RandomString(rnd.Next(minlength, maxlength));
        public static string RandomString(int minlength, int maxlength, string chars) => RandomString(rnd.Next(minlength, maxlength), chars);
        public static string RandomString(int length, string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789") => new(Enumerable.Repeat(chars, length).Select(s => s[rnd.Next(s.Length)]).ToArray());

        
        enum MySampleEnum { Default, SecondValue }
        
        class MySampleRowClass
        {
            public int Aaa { get; set; } = 346;
            public string Bbb { get; set; } = "Sample text.";
            public float Ccc { get; set; } = 3.14159f;
            public MySampleEnum TheEnum { get; set; } = MySampleEnum.Default;
            [JsonIgnore] public string ShouldBeIgnored = "to be ignored";
            public string ToBeRemoved { get; set; } = "remove me manually";
        }

        [TestMethod]
        public void SimpleFileDB_T01()
        {
            string pathdb = Path.GetTempFileName();
            File.Delete(pathdb);
            Directory.CreateDirectory(pathdb);

            string table1_name = RandomString(3, 15, SimpleFileDB.ValidIndexChars);
            string table1_row1_index = RandomString(3, 15, SimpleFileDB.ValidIndexChars);

            SimpleFileDB db = new SimpleFileDB(pathdb, IOPriorityClass.L03_HighEffort);

            // Create table
            Assert.IsFalse(db.TableExists(table1_name));
            db.CreateTable(table1_name);
            db[table1_name].IsCacheEnabled = false;
            Assert.IsTrue(db.TableExists(table1_name));

            // Write, then read a row as string
            Assert.IsFalse(db[table1_name].RowExists(table1_row1_index));
            string table1_row1_expectedvalue = "Hello World!";
            db[table1_name][table1_row1_index] = table1_row1_expectedvalue;
            Assert.IsTrue(db.TableExists(table1_name));
            Assert.IsTrue(db[table1_name].RowExists(table1_row1_index));
            string table1_row1_readvalue = db[table1_name].GetRow<string>(table1_row1_index).Result;
            Assert.IsTrue(table1_row1_expectedvalue == table1_row1_readvalue);

            // Check raw access to files
            string pathtable = db.GetPathTable(table1_name);
            Assert.IsTrue(Directory.Exists(pathtable));
            string pathrow1 = db[table1_name].GetPathRow(table1_row1_index);
            Assert.IsTrue(File.Exists(pathrow1));
            string table1_row1_readvalue_rawjson = File.ReadAllText(pathrow1).Trim('"');
            Assert.IsTrue(table1_row1_readvalue_rawjson == table1_row1_expectedvalue);

            // Write, then read a row as an object
            
            MySampleRowClass row1_obj_expected = new MySampleRowClass() {
                Aaa = 8523644,
                Bbb = "Sample Row Test",
                Ccc = 2.7182818284590452f,
                TheEnum = MySampleEnum.SecondValue
            };
            db[table1_name][table1_row1_index] = row1_obj_expected;
            
            // remove one property from the JSON
            string json = File.ReadAllText(db[table1_name].GetPathRow(table1_row1_index));
            Assert.IsTrue(json.Contains(nameof(MySampleRowClass.ToBeRemoved)));
            File.WriteAllText(db[table1_name].GetPathRow(table1_row1_index), json.Replace($",\n  \"{nameof(MySampleRowClass.ToBeRemoved)}\": \"{row1_obj_expected.ToBeRemoved}\"", ""));
            json = File.ReadAllText(db[table1_name].GetPathRow(table1_row1_index));
            Assert.IsFalse(json.Contains(nameof(MySampleRowClass.ToBeRemoved)));
            
            MySampleRowClass row1_obj_readvalue = db[table1_name].GetRow<MySampleRowClass>(table1_row1_index).Result;
            Assert.IsTrue(json.Contains("Aaa"), "Missing property, or not properly capitalized");
            Assert.IsFalse(json.ToLower().Contains("ignore"), "Property not ignored");
            Assert.IsTrue(json.Contains(nameof(MySampleEnum.SecondValue)));
            Assert.IsTrue(row1_obj_expected.Aaa == row1_obj_readvalue.Aaa);
            Assert.IsTrue(row1_obj_expected.Bbb == row1_obj_readvalue.Bbb);
            Assert.IsTrue(row1_obj_expected.Ccc == row1_obj_readvalue.Ccc);

            // Delete row
            db[table1_name].Delete(table1_row1_index);
            Assert.IsFalse(File.Exists(pathrow1));

            // Delete table
            db.DeleteTable(table1_name, deletecontents: false);
            Assert.IsFalse(Directory.Exists(pathtable));

            // Delete DB
            Directory.Delete(pathdb, false);
        }
    }
}
