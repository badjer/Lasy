using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lasy.MySql
{
    public static class MySqlConnectTo
    {

        public static SqlDB Db(string connString, bool strictTables = true)
        {
            return new MySqlDB(connString, new MySqlAnalyzer(connString), strictTables);
        }

        public static ModifiableSqlDB ModifiableDb(string connString, ITypedDBAnalyzer taxonomy = null)
        {
            var analyzer = new MySqlAnalyzer(connString);
            var modifier = new MySqlModifier(connString, analyzer, taxonomy);
            var db = new MySqlDB(connString, analyzer, false);
            return new ModifiableSqlDB(db, modifier);
        }
    }
}
