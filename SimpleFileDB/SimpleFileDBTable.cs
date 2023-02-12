using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.NG;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SimpleFileDB
{
    /// <summary>Represents a table in the <see cref="SimpleFileDB"/>. It allows reading and writing rows.</summary>
    public class SimpleFileDBTable
    {
        /// <summary>The database this table belongs to.</summary>
        public SimpleFileDB DB { get; internal set; }

        /// <summary>Table Name/Index/ID.</summary>
        public string TableID { get; internal set; }

        /// <summary>Path to the directory which represents this table and therefore contains all its rows.</summary>
        public string PathTable => Path.Combine(DB.PathRoot, TableID);

        public SimpleFileDBTable() { }

        public bool IsCacheEnabled = true;

        /// <summary>Create a new object that represents a Simple File Database table.</summary>
        /// <param name="db">Database this table belongs to.</param>
        /// <param name="tableid">Table name/index/ID.</param>
        public SimpleFileDBTable(SimpleFileDB db, string tableid)
        {
            this.DB = db;
            DB.ValidateTableID(tableid);
            this.TableID = tableid;
        }

        /// <summary>Check if a row exists.</summary>
        /// <param name="rowindex">Row index.</param>
        /// <returns>True if the row exists. Otherwise false.</returns>
        public bool RowExists(string rowindex)
        {
            DB.ValidateRowID(TableID, rowindex);
            if (IsCacheEnabled && cache.ContainsKey(rowindex)) return true; // if it's in the cache we don't need to read the file
            string pathFile = GetPathRow(rowindex);
            return FileNG.Exists(pathFile, iopriority: DB.IOPriority);
        }

        /// <summary>List of all row IDs.</summary>
        public virtual string[] AllKeys => DirectoryNG.GetFiles(PathTable, iopriority: DB.IOPriority).Where(f => !f.StartsWith('.')).Select(f => Path.GetFileName(f)).ToArray();

        private readonly ConcurrentDictionary<string, string> cache = new();

        /// <summary>Retrieves a row from the database. Throws an exception if it doesn't exist.</summary>
        /// <typeparam name="T">Parse it as the given type.</typeparam>
        /// <param name="v">Row ID (index).</param>
        public virtual async Task<T> GetRow<T>(string rowindex)
        {
            if (IsCacheEnabled)
            {
                try
                {
                    string cachedValue = cache[rowindex];
                    T value = JsonSerializer.Deserialize<T>(cachedValue, JsonSerializerOptions);
                    return value;
                }
                catch { }
            }

            await DB.sm.WaitAsync(TimeSpan.FromSeconds(10));
            try
            {
                DB.ValidateRowID(TableID, rowindex);
                string pathFile = GetPathRow(rowindex);
                FileInfo fi = new FileInfo(pathFile);
                if (!fi.Exists) throw new Exception($"Row with index '{rowindex}' does not exist in table '{TableID}'.");

                string json = "";
                T v;
                try
                {
                    json = await FileNG.ReadAllTextAsync(pathFile, iopriority: DB.IOPriority);
                    if (string.IsNullOrEmpty(json)) throw new Exception($"File read as empty");
                    v = JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    try
                    {
                        json = await FileNG.ReadAllTextAsync(pathFile, iopriority: DB.IOPriority);
                        if (string.IsNullOrEmpty(json)) throw new Exception($"File read as empty");
                        v = JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
                    }
                    catch
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));

                        try
                        {
                            json = await FileNG.ReadAllTextAsync(pathFile, iopriority: DB.IOPriority);
                            if (string.IsNullOrEmpty(json)) throw new Exception($"File read as empty");
                            v = JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Unable to read and parse file '{pathFile}': {ex.Message}. Content: {json}.");
                        }
                    }
                }

                if (v == null) throw new Exception($"Unable to parse file: {pathFile}");
                if (IsCacheEnabled) cache[rowindex] = json;
                return v;
            }
            finally
            {
                DB.sm.Release();
            }
        }

        /// <summary>Store an object as a row.</summary>
        /// <param name="rowindex">Row ID (index).</param>
        /// <param name="value">Value of the row.</param>
        public virtual async Task WriteRow(string rowindex, object value)
        {
            await DB.sm.WaitAsync(TimeSpan.FromSeconds(10));
            try
            {
                DB.ValidateRowID(TableID, rowindex);
                string pathFile = GetPathRow(rowindex);
                string json = JsonSerializer.Serialize(value, JsonSerializerOptions);
                try { await FileNG.WriteAllTextAsync(pathFile, json, iopriority: DB.IOPriority); }
                catch (IOException)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    try { await FileNG.WriteAllTextAsync(pathFile, json, iopriority: DB.IOPriority); } // retry after one second
                    catch (IOException)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        await FileNG.WriteAllTextAsync(pathFile, json, iopriority: DB.IOPriority); // retry again after 5 seconds... or let it crash
                    }
                }
                if (IsCacheEnabled) cache[rowindex] = json;
            }
            finally
            {
                DB.sm.Release();
            }
        }

        public readonly JsonSerializerOptions JsonSerializerOptions = new ()
        {
            WriteIndented = true,
            IgnoreReadOnlyProperties = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null, // null is PascalCase
            DictionaryKeyPolicy = null, // null is PascalCase
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(namingPolicy: null) } // null is PascalCase
        };

        /// <summary>Reads or writes a row.</summary>
        /// <param name="rowindex">Row ID (index).</param>
        /// <returns>The row, as a raw <see cref="JObject"/>.</returns>
        public object this[string rowindex]
        {
            get => GetRow<JsonNode>(rowindex).Result;
            set => WriteRow(rowindex, value).Wait();
        }

        public virtual string GetPathRow(string rowindex) => Path.Combine(PathTable, rowindex);

        /// <summary>Delete a row.</summary>
        /// <param name="rowindex">Row ID (index).</param>
        public virtual void Delete(string rowindex)
        {
            FileNG.Delete(GetPathRow(rowindex), iopriority: DB.IOPriority);
            cache.TryRemove(rowindex, out _);
        }

        public void ValidateRowID(string id) => DB.ValidateRowID(TableID, id);
    }
}
