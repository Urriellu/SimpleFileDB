using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleFileDB
{
    public class SimpleFileDBTable
    {
        public SimpleFileDB DB { get; internal set; }
        public string TableID { get; internal set; }

        public string PathTable => Path.Combine(DB.PathRoot, TableID);

        public SimpleFileDBTable() { }

        public SimpleFileDBTable(SimpleFileDB db, string tableid)
        {
            this.DB = db;
            DB.ValidateIndex(tableid);
            this.TableID = tableid;
        }

        public bool RowExists(string index)
        {
            DB.ValidateIndex(index);
            string pathFile = GetPathRow(index);
            return File.Exists(pathFile);
        }

        public virtual string[] AllKeys => Directory.GetFiles(PathTable).Where(f => !f.StartsWith('.')).Select(f => Path.GetFileName(f)).ToArray();

        /// <summary>Retrieves a row from the database. Throws an exception if it doesn't exist.</summary>
        /// <typeparam name="T">Parse it as the given type.</typeparam>
        /// <param name="v">Row ID (index)</param>
        public virtual async Task<T> GetRow<T>(string rowindex)
        {
            await DB.sm.WaitAsync(TimeSpan.FromSeconds(10));
            try
            {
                DB.ValidateIndex(rowindex);
                string pathFile = GetPathRow(rowindex);
                if (!File.Exists(pathFile)) throw new Exception($"Row with index '{rowindex}' does not exist in table '{TableID}'.");
                string json = await File.ReadAllTextAsync(pathFile);
                T v = JsonConvert.DeserializeObject<T>(json);
                return v;
            }
            finally
            {
                DB.sm.Release();
            }
        }

        public virtual async Task WriteRow(string rowindex, object value)
        {
            await DB.sm.WaitAsync(TimeSpan.FromSeconds(10));
            try
            {
                DB.ValidateIndex(rowindex);
                string pathFile = GetPathRow(rowindex);
                string json = JsonConvert.SerializeObject(value, Formatting.Indented);
                await File.WriteAllTextAsync(pathFile, json);
            }
            finally
            {
                DB.sm.Release();
            }
        }

        public object this[string rowindex]
        {
            get => GetRow<object>(rowindex);
            set => WriteRow(rowindex, value).Wait();
        }

        public virtual string GetPathRow(string rowindex) => Path.Combine(PathTable, rowindex);

        public DateTime GetRowCreationTime(string rowindex) => File.GetCreationTime(GetPathRow(rowindex));

        public virtual void Delete(string rowindex) => File.Delete(GetPathRow(rowindex));
    }
}
