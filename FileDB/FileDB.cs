using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FileDB
{
    public class FileDB
    {
        public readonly string PathRoot;

        internal readonly SemaphoreSlim sm = new SemaphoreSlim(1);

        public FileDB(string pathroot)
        {
            if (!Directory.Exists(pathroot)) throw new Exception($"File DB directory does not exist: {pathroot}");
            this.PathRoot = pathroot;
        }

        public virtual FileDBTable this[string table] => GetTable<FileDBTable>(table);

        internal const string ValidIndexChars = "abcdefghijklmnopqrstuvwxyz@.-,_!#$%^&()=+[]{};'~`ñ€´ç 0123456789";

        public void CreateTable(string table)
        {
            Directory.CreateDirectory(GetPathTable(table));
        }

        public virtual void ValidateIndex(string index)
        {
            foreach (char c in index)
            {
                if (char.IsUpper(c)) throw new Exception($"Invalid index '{index}' with character '{c}'. Uppercase characters are not allowed.");
                if (!ValidIndexChars.Contains(c.ToString())) throw new Exception($"Invalid index '{index}' with character '{c}'. Make sure it's all lowercase and non-special characters.");
            }
        }

        public bool TableExists(string index)
        {
            sm.Wait(TimeSpan.FromSeconds(10));
            ValidateIndex(index);
            string pathFile = GetPathTable(index);
            bool exists = Directory.Exists(pathFile);
            sm.Release(1);
            return exists;
        }

        protected T GetTable<T>(string table) where T : FileDBTable, new() => new T() { DB = this, TableID = table };

        internal string GetPathTable(string index) => Path.Combine(PathRoot, index);

        public void DeleteTable(string tableindex, bool deletecontents) => Directory.Delete(GetPathTable(tableindex), deletecontents);
    }
}
