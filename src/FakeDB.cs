﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lasy;
using Nvelope;

namespace Lasy
{
    public class FakeDB : IReadable, IWriteable, IReadWrite
    {
        public FakeDB()
        {
        }

        public FakeDB(IDBAnalyzer analyzer) : this()
        {
            Analyzer = analyzer;
        }

        public Dictionary<string, FakeDBTable> DataStore = new Dictionary<string, FakeDBTable>();

        public FakeDBTable Table(string tableName)
        {
            if (DataStore.ContainsKey(tableName))
                return DataStore[tableName];
            else
                return new FakeDBTable();
        }

        public virtual void Wipe()
        {
            DataStore = new Dictionary<string, FakeDBTable>();
        }

        public virtual IEnumerable<Dictionary<string, object>> RawRead(string tableName, Dictionary<string, object> id, ITransaction transaction = null)
        {
            if (transaction != null)
                return (transaction as FakeDBTransaction).RawRead(tableName, id);

            if (!DataStore.ContainsKey(tableName))
                return new List<Dictionary<string, object>>();

            return DataStore[tableName].Read(id);
        }

        public virtual IEnumerable<Dictionary<string, object>> RawReadCustomFields(string tableName, IEnumerable<string> fields, Dictionary<string, object> id, ITransaction transaction = null)
        {
            if (transaction != null)
                return (transaction as FakeDBTransaction).RawReadCustomFields(tableName, fields, id);

            if (!DataStore.ContainsKey(tableName))
                return new List<Dictionary<string, object>>();

            return DataStore[tableName].Read(id, fields);
        }

        public virtual IEnumerable<Dictionary<string, object>> RawReadAll(string tableName, ITransaction transaction = null)
        {
            if (transaction != null)
                return (transaction as FakeDBTransaction).RawReadAll(tableName);

            if (!DataStore.ContainsKey(tableName))
                return new List<Dictionary<string, object>>();

            return DataStore[tableName].Read(new Dictionary<string, object>());
        }

        public virtual IEnumerable<Dictionary<string, object>> RawReadAllCustomFields(string tableName, IEnumerable<string> fields, ITransaction transaction = null)
        {
            if (transaction != null)
                return (transaction as FakeDBTransaction).RawReadAllCustomFields(tableName, fields);

            if (!DataStore.ContainsKey(tableName))
                return new List<Dictionary<string, object>>();

            return DataStore[tableName].Read(new Dictionary<string, object>(), fields);
        }

        private IDBAnalyzer _analyzer = new FakeDBAnalyzer();

        public IDBAnalyzer Analyzer
        {
            get { return _analyzer; }
            set { _analyzer = value; }
        }

        public Dictionary<string, object> NewAutokey(string tableName)
        {
            if (!DataStore.ContainsKey(tableName))
                DataStore.Add(tableName, new FakeDBTable());

            var table = DataStore[tableName];

            var autoKey = Analyzer.GetAutoNumberKey(tableName);
            if (autoKey == null)
                return new Dictionary<string, object>();
            else
                return new Dictionary<string, object>() { { autoKey, table.NextAutoKey++ } };
        }

        public bool CheckKeys(string tableName, Dictionary<string, object> row)
        {
            try
            {
                var res = this.ExtractKeys(tableName, row);
                return true;
            }
            catch (KeyNotSetException)
            {
                return false;
            }
        }

        public virtual Dictionary<string, object> Insert(string tableName, Dictionary<string, object> row, ITransaction transaction = null)
        {
            if (transaction != null)
                return (transaction as FakeDBTransaction).Insert(tableName, row);

            if (!DataStore.ContainsKey(tableName))
                DataStore.Add(tableName, new FakeDBTable());
            
            var table = DataStore[tableName];

            row = row.ScrubNulls();

            var autoKeys = NewAutokey(tableName);
            var dictToUse = row.Union(autoKeys);
            CheckKeys(tableName, dictToUse);
            table.Add(dictToUse);

            return this.ExtractKeys(tableName, dictToUse);
        }

        public virtual void Delete(string tableName, Dictionary<string, object> fieldValues, ITransaction transaction = null)
        {
            if (transaction != null)
            {
                (transaction as FakeDBTransaction).Delete(tableName, fieldValues);
                return;
            }

            if (DataStore.ContainsKey(tableName))
            {
                fieldValues = fieldValues.ScrubNulls();
                var victims = DataStore[tableName].FindByFieldValues(fieldValues).ToList();
                victims.ForEach(x => DataStore[tableName].Remove(x));
            }
        }

        public virtual void Update(string tableName, Dictionary<string, object> dataFields, Dictionary<string, object> keyFields, ITransaction transaction = null)
        {
            if (transaction != null)
            {
                (transaction as FakeDBTransaction).Update(tableName, dataFields, keyFields);
                return;
            }

            if(!DataStore.ContainsKey(tableName))
                return;

            dataFields = dataFields.ScrubNulls();
            keyFields = keyFields.ScrubNulls();

            var victims = DataStore[tableName].Where(r => r.IsSameAs(keyFields, keyFields.Keys));
            foreach (var vic in victims)
                foreach (var key in dataFields.Keys)
                    vic[key] = dataFields[key];                
        }

        public virtual  ITransaction BeginTransaction()
        {
            return new FakeDBTransaction(this);
        }
    }
}
