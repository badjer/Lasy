﻿using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Nvelope.Reflection;
using Lasy;
using NUnit.Framework;
using System;
using Nvelope;

namespace LasyTests.Sql
{
    [TestFixture]
    public class SqlDBTests
    {
        [SetUp]
        public void Setup()
        {
            using (var conn = new SqlConnection(Config.TestDBConnectionString))
            {
                conn.Execute("delete Person");
                conn.Execute("delete Organization");
                conn.Execute("delete SchemaA.Foo");
                conn.Execute("delete SchemaB.Foo");
            }

        }

        [Test]
        public void ReadAll()
        {
            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);

            var results = db.ReadAll<Person>();

            int actualCount = int.MinValue;
            using (var conn = new SqlConnection(Config.TestDBConnectionString))
            {
                actualCount = conn.ExecuteSingleValue<int>("select count(*) from Person");
            }

            Assert.AreEqual(results.Count(), actualCount);
        }

        [Test (Description = "If we want to select only certain columns from an entire table, rather than entire rows")]
        public void ReadAllCustomFields()
        {
            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);
            db.Insert("Person", new Person { FirstName = "Foo" });

            var desiredColumns = new List<string>(){ "PersonId", "FirstName" };

            var results = db.ReadAll("Person", desiredColumns);

            int actualCount = int.MinValue;
            using (var conn = new SqlConnection(Config.TestDBConnectionString))
            {
                actualCount = conn.ExecuteSingleValue<int>("select count(*) from Person");
            }

            //Make sure we get all of the rows in the table
            Assert.AreEqual(results.Count(), actualCount);

            //Make sure we only got the columns we requested rather than entire rows
            Assert.AreEqual(results.First().Keys.ToList(), desiredColumns);
        }

        [Test]
        public void ReadFilterByNullField()
        {
            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);
            var data = new Person() { FirstName = "test", LastName = "person", Age = null };
            var dataKeys = db.Insert("Person", data);

            // Read where Age is null
            var fromDb = db.Read("Person", new Dictionary<string, object>() { { "Age", DBNull.Value } });
            Assert.True(fromDb.Any());
            fromDb = db.Read("Person", new Dictionary<string, object>() { { "Age", null } });
            Assert.True(fromDb.Any());
        }

        [Test]
        public void ReadFilterByMultipleNullField()
        {
            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);
            var data = new Person() { FirstName = "test", LastName = "person", Age = null };
            var dataKeys = db.Insert("Person", data);

            // Read where Age is null and PersonId = X
            var fromDb = db.Read("Person", new Dictionary<string, object>() { { "Age", DBNull.Value } }.Union(dataKeys)); ;
            Assert.True(fromDb.Any());
            fromDb = db.Read("Person", new Dictionary<string, object>() { { "Age", null } }.Union(dataKeys));
            Assert.True(fromDb.Any());
        }

        [Test]
        public void ReadsFromView()
        {
            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);
            db.Insert("Person", new Person());
            Assert.AreNotEqual(0, db.ReadAll("ID_NUMView"));
        }

        [Test]
        public void ReadFromNonExistantTable()
        {
            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);
            
            var table = "foobartable";
            Assert.False(db.Analyzer.TableExists(table), "Ooops, our test table actually existed - we need to test against a table that doesn't exist");

            db.StrictTables = false;
            Assert.AreEqual("()", db.Read(table, null).Print(),
                "We had StrictTables set to false, so we should have just got back an empty list");

            db.StrictTables = true;
            Assert.Throws<NotATableException>(() => db.Read(table, null),
                "We had StrictTables set to true, so we should have throw an exception");
        }

        [Test]
        public void CRUDOnTwoTablesSameNameDifferentSchema()
        {
            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);
            var data = new { FooId = 4 }._AsDictionary();
            db.Insert("SchemaA.Foo", data);
            var fromDb = db.Read("SchemaA.Foo", data).Single();
            Assert.True(fromDb.IsSameAs(data));
            var newData = new { FooId = 7 }._AsDictionary();
            db.Update("SchemaA.Foo", newData, data);
            fromDb = db.Read("SchemaA.Foo", newData).Single();
            Assert.True(fromDb.IsSameAs(newData));
            db.Delete("SchemaA.Foo", newData);
            Assert.IsEmpty(db.Read("SchemaA.Foo", new { }));
        }

        /// <summary>
        /// If someone else is using the database at the same time, this could fail
        /// </summary>
        [Test]
        public void Insert()
        {
            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);

            var contents = db.ReadAll<Person>();

            var person = new Person();
            person.FirstName = "test";
            person.LastName = "person";

            db.Insert("Person", person._AsDictionary());

            Assert.AreEqual(contents.Count() + 1, db.ReadAll<Person>().Count());
        }

        [Test]
        public void Update()
        {
            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);

            var person = new Person();
            person.FirstName = "test";
            person.LastName = "person";

            var keys = db.Insert("Person", person._AsDictionary());

            //Retrieve the saved person so we can verify it saved with our intended data
            var retrieved = db.RawRead("Person", keys).First();
            Assert.AreEqual(person.FirstName, retrieved["FirstName"]);
            Assert.AreEqual(person.LastName, retrieved["LastName"]);

            person.FirstName = "newFirst";
            person.LastName = "newLastName";

            //Update and retrieve the person again so we can verify that the Update worked
            db.Update("Person", person._AsDictionary(), keys);
            retrieved = db.RawRead("Person", keys).First();
            Assert.AreEqual(person.FirstName, retrieved["FirstName"]);
            Assert.AreEqual(person.LastName, retrieved["LastName"]);
        }

        [Test(Description = "Verify that a record has been deleted")]
        public void Delete()
        {
            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);

            var person = new Person();
            person.FirstName = "bob";
            person.LastName = "morris";

            var key = db.Insert("Person", person._AsDictionary());
            db.Delete("Person", key);

            Assert.AreEqual(0, db.RawRead("Person", key).Count());
        }

        [Test(Description = "We need to make sure we can pass in null values to inserts")]
        public void AllowNullInsert()
        {
            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);

            var org = new Organization();
            org.Name = "My Organization";

            var key = db.Insert("Organization", org._AsDictionary());

            //Verify that only 1 row exists with the key of this inserted organization
            Assert.AreEqual(1, db.RawRead("Organization", key).Count());
        }

        [Test(Description = "If a transaction is rolled back we should not see any results in our connection or any other connections")]
        [Ignore]
        public void TransactionRollback()
        {
            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);
            var transaction = db.BeginTransaction();

            var person = new Person();
            person.FirstName = "test";
            person.LastName = "person";

            var keys = transaction.Insert("Person", person._AsDictionary());

            transaction.Rollback();

            Assert.AreEqual(0, db.RawRead("Person", keys).Count());
        }

        [Test(Description = "If a transaction is abandoned we want to make sure no results were obtained from the commands inside it")]
        [Ignore]
        public void TransactionAbandoned()
        {
            var keys = new Dictionary<string, object>();

            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);

            using(var transaction = db.BeginTransaction())
            {
                var person = new Person();
                person.FirstName = "test";
                person.LastName = "person";

                keys = transaction.Insert("Person", person._AsDictionary());
            }

            var conn = ConnectTo.Sql2005(Config.TestDBConnectionString);
            Assert.AreEqual(0, conn.RawRead("Person", keys).Count());
        }

        [Test(Description = "Results should be present on a successful, committed transaction")]
        [Ignore]
        public void TransactionSuccessful()
        {
            var db = ConnectTo.Sql2005(Config.TestDBConnectionString);
            var transaction = db.BeginTransaction();

            var person = new Person();
            person.FirstName = "test";
            person.LastName = "person";

            var keys = transaction.Insert("Person", person._AsDictionary());

            transaction.Commit();

            Assert.AreEqual(1, db.RawRead("Person", keys).Count());
        }
    }
}
