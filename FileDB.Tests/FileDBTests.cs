using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace FileDB
{
    [TestClass]
    public class FileDBTests
    {
        static Random rnd = new Random();
        public static string RandomString(int minlength, int maxlength) => RandomString(rnd.Next(minlength, maxlength));
        public static string RandomString(int minlength, int maxlength, string chars) => RandomString(rnd.Next(minlength, maxlength), chars);
        public static string RandomString(int length, string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789") => new string(Enumerable.Repeat(chars, length).Select(s => s[rnd.Next(s.Length)]).ToArray());

        class MySampleRowClass
        {
            public int Aaa = 346;
            public string Bbb = "Sample text.";
            public float Ccc = 3.14159f;
        }

        [TestMethod]
        public void FileDB_T01()
        {
            string pathdb = Path.GetTempFileName();
            File.Delete(pathdb);
            Directory.CreateDirectory(pathdb);

            string table1_name = RandomString(3, 15, FileDB.ValidIndexChars);
            string table1_row1_index = RandomString(3, 15, FileDB.ValidIndexChars);

            FileDB db = new FileDB(pathdb);

            // Create table
            Assert.IsFalse(db.TableExists(table1_name));
            db.CreateTable(table1_name);
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
            MySampleRowClass row1_obj_expected = new MySampleRowClass() { Aaa = 8523644, Bbb = "Sample Row Test", Ccc = 2.7182818284590452f };
            db[table1_name][table1_row1_index] = row1_obj_expected;
            MySampleRowClass row1_obj_readvalue = db[table1_name].GetRow<MySampleRowClass>(table1_row1_index).Result;
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
