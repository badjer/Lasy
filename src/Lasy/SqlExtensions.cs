using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using Nvelope;
using Nvelope.Data;
using Nvelope.Reflection;

namespace Lasy
{
    public static class SqlExtensions
    {
        /// <summary>
        /// Automatically construct a SQLParameter from the name and value and add it to the collection
        /// </summary>
        /// <remarks>You can have lazy-loaded values by passing a Func{object} in value - this will
        /// call Realize on the value, which will run the function and return the value</remarks>
        /// <param name="comm"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void AddParameter(this IDbCommand comm, string name, object value)
        {
            var realizedValue = value.Realize();
            var sqlType = SqlTypeConversion.GetSqlType(realizedValue);
            var para = comm.CreateParameter();
            para.ParameterName = name;
            para.DbType = sqlType.DbType;
            para.Value = SqlTypeConversion.ConvertToSqlValue(realizedValue);
            comm.Parameters.Add(para);
        }

        /// <summary>
        /// Create a bunch of SQLParameters from the dictionary and add them to the collection
        /// </summary>
        /// <remarks>You can have lazy-loaded values by having the values of the dictionary be
        /// Func{object} - this will call Realize on the values</remarks>
        /// <param name="comm"></param>
        /// <param name="paras"></param>
        public static void AddParameters(this IDbCommand comm, object paras)
        {
            if (paras == null)
                return;

            var dict = paras._AsDictionary();
            foreach (var kv in dict)
                comm.AddParameter(kv.Key, kv.Value);
        }

        /// <summary>
        /// Execute the query and run the callback with the results
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="paras"></param>
        public static ICollection<Dictionary<string,object>> Execute(this IDbConnection conn, string sql,
            object paras = null)
        {
            if (conn.State == ConnectionState.Closed)
                conn.Open();

            using (var comm = conn.CreateCommand())
                return comm.Execute(sql, paras);
            
        }

        public static ICollection<Dictionary<string,object>> Execute(this IDbCommand comm, string sql,
            object paras = null)
        {            
            comm.CommandType = CommandType.Text;
            comm.CommandText = sql;
            comm.AddParameters(paras);
            using (var reader = comm.ExecuteReader())
                return reader.AllRows();
        }

        public static int ExecuteNonQuery(this IDbConnection conn, string sql, object paras = null)
        {
            if (conn.State == ConnectionState.Closed)
                conn.Open();

            using (var comm = conn.CreateCommand())
                return comm.ExecuteNonQuery(sql, paras);
        }

        public static int ExecuteNonQuery(this IDbCommand comm, string sql, object paras = null)
        {
            comm.CommandType = CommandType.Text;
            comm.CommandText = sql;
            comm.AddParameters(paras);
            return comm.ExecuteNonQuery();
        }

        public static object ExecuteScalar(this IDbConnection conn, string sql, object paras = null)
        {
            if (conn.State == ConnectionState.Closed)
                conn.Open();

            using (var comm = conn.CreateCommand())
                return comm.ExecuteScalar(sql, paras);
        }

        public static object ExecuteScalar(this IDbCommand comm, string sql, object paras = null)
        {
            comm.CommandType = CommandType.Text;
            comm.CommandText = sql;
            comm.AddParameters(paras);
            return comm.ExecuteScalar();
        }

        public static ICollection<object> ExecuteSingleColumn(this IDbConnection conn, string sql, object paras = null)
        {
            if (conn.State == ConnectionState.Closed)
                conn.Open();

            using (var comm = conn.CreateCommand())
                return comm.ExecuteSingleColumn(sql, paras);
        }

        public static ICollection<object> ExecuteSingleColumn(this IDbCommand comm, string sql, object paras = null)
        {
            comm.CommandType = CommandType.Text;
            comm.CommandText = sql;
            comm.AddParameters(paras);
            using (var reader = comm.ExecuteReader())
                return reader.SingleColumn<object>();
        }

    }
}
