﻿using System;
using System.IO;
using System.IO.NG;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace SimpleFileDB
{
    /// <summary>
    /// Simple File Database. Tables are stored as folders, rows as plain text files, and columns as first-level JSON properties.
    /// </summary>
    public class SimpleFileDB
    {
        /// <summary>Path to the directory that contains the entire database.</summary>
        public readonly string PathRoot;

        internal readonly SemaphoreSlim sm = new(1);

        /// <summary>Creates a new database object which allows accessing the file-based database stored at the given path.</summary>
        /// <param name="pathroot">Path to the directory that contains the database. This folder must exist.</param>
        /// <param name="iopriority">The Disk (I/O) priority to use when reading/writing to files and directories on disk.</param>
        public SimpleFileDB(string pathroot, IOPriorityClass iopriority = IOPriorityClass.L02_NormalEffort)
        {
            if (!DirectoryNG.Exists(pathroot, iopriority: iopriority)) throw new Exception($"Simple File DB directory does not exist: {pathroot}");
            this.PathRoot = pathroot;
            this.IOPriority = iopriority;
        }

        /// <summary>Retrieve a table.</summary>
        /// <param name="table">Table name/index/ID.</param>
        /// <returns>Object which represents a table and allows accessing its rows.</returns>
        public virtual SimpleFileDBTable this[string table] => GetTable<SimpleFileDBTable>(table);

        public const string ValidIndexChars = "abcdefghijklmnopqrstuvwxyz@.-,_!#$%^&()=+[]{};'~`ñ€´ç 0123456789";

        public IOPriorityClass IOPriority = IOPriorityClass.L02_NormalEffort;
        
        public static readonly JsonSerializerOptions Options = new ()
        {
            WriteIndented = true,
            IgnoreReadOnlyProperties = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null, // null is PascalCase
            DictionaryKeyPolicy = null, // null is PascalCase
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(namingPolicy: null) } // null is PascalCase
        };

        /// <summary>Create a new table.</summary>
        /// <param name="table">Table name/index/ID.</param>
        public virtual void CreateTable(string table)
        {
            DirectoryNG.CreateDirectory(GetPathTable(table), iopriority: IOPriority);
        }

        public virtual void ValidateTableID(string index) => ValidateIndex(index, ValidIndexChars);

        public virtual void ValidateRowID(string tableid, string index) => ValidateIndex(index, ValidIndexChars);

        protected void ValidateIndex(string index, string validchars)
        {
            foreach (char c in index)
            {
                if (char.IsUpper(c)) throw new Exception($"Invalid index '{index}' with character '{c}'. Uppercase characters are not allowed.");
                if (!validchars.Contains(c.ToString())) throw new Exception($"Invalid index '{index}' with character '{c}'. Make sure it's all lowercase and non-special characters.");
            }
        }

        /// <summary>Check if a table exists.</summary>
        /// <param name="tableid">Table name/index/ID.</param>
        /// <returns>True if the table exists. Otherwise false.</returns>
        public bool TableExists(string tableid)
        {
            sm.Wait(TimeSpan.FromSeconds(10));
            ValidateTableID(tableid);
            string pathFile = GetPathTable(tableid);
            bool exists = DirectoryNG.Exists(pathFile, iopriority: IOPriority);
            sm.Release(1);
            return exists;
        }

        /// <summary>Retrieve a table object when the table class has been customized/extended.</summary>
        /// <typeparam name="T">Class which represents the table. It must derived from <see cref="SimpleFileDBTable"/>.</typeparam>
        /// <param name="table">Table name/index/ID.</param>
        /// <returns>The table object.</returns>
        protected T GetTable<T>(string table) where T : SimpleFileDBTable, new() => new() { DB = this, TableID = table };

        internal string GetPathTable(string index) => Path.Combine(PathRoot, index);

        /// <summary>Delete a table and, optionally, its contents.</summary>
        /// <param name="tableindex">Table name/index/ID.</param>
        /// <param name="deletecontents">If true, the contents (rows) of the table are also deleted. If false and the table is not empty it throws an exception.</param>
        public virtual void DeleteTable(string tableindex, bool deletecontents) => DirectoryNG.Delete(GetPathTable(tableindex), deletecontents, iopriority: IOPriority);
    }
}
