namespace EfTesting
{
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Linq;

    public class Program
    {
        /// <summary>
        /// EF performance tips
        /// </summary>
        public static void Main()
        {
            //TestGetData();
            //DeleteDataWithOneQuerry();
            //PrintComplexQuery();
            //CompiledQuery(5, "test");
            //NonTrackingQuery();
        }

        private static void NonTrackingQuery()
        {
            // warm up
            using (var db = new TestDbContext())
            {
                var cats = db.Cats
                    .Where(c => c.Id % 2 == 0)
                    .ToList();
            }

            var watch = Stopwatch.StartNew();

            // tracking
            using (var db = new TestDbContext())
            {
                var cats = db.Cats
                    .Where(c => c.Id % 2 == 0)
                    .ToList();

                Console.WriteLine(db.ChangeTracker.Entries().Count());
                Console.WriteLine(watch.Elapsed);
            }

            watch = Stopwatch.StartNew();

            // first way
            using (var db = new TestDbContext())
            {
                var cats = db.Cats
                    .AsNoTracking()
                    .Where(c => c.Id % 2 == 0)
                    .ToList();

                Console.WriteLine(db.ChangeTracker.Entries().Count());
                Console.WriteLine(watch.Elapsed);
            }

            watch = Stopwatch.StartNew();

            // second way. Change tracker here is to the scope of the db instance so all the queries inside are no tracking. To this EF core version seems to be the fastest way
            using (var db = new TestDbContext())
            {
                db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var cats = db.Cats
                    .Where(c => c.Id % 2 == 0)
                    .ToList();

                Console.WriteLine(db.ChangeTracker.Entries().Count());
                Console.WriteLine(watch.Elapsed);
            }

            watch = Stopwatch.StartNew();

            // third way is when we get data but we dont include a projection with the actual DB model in the query for example with the anonym object we select only specific data such as string, int and so on. So the query is no tracking here
            using (var db = new TestDbContext())
            {
                var cats = db.Cats
                    .Where(c => c.Id % 2 == 0)
                    .Select(c => new
                    {
                        c.Name,
                        c.Age,
                        OwnerName = c.Owner.Name
                    })
                    .ToList();

                Console.WriteLine(db.ChangeTracker.Entries().Count());
                Console.WriteLine(watch.Elapsed);
            }

            watch = Stopwatch.StartNew();

            // forth way is just an example if we include a projection with the actual DB model such as Owner then it will track the results
            using (var db = new TestDbContext())
            {
                var cats = db.Cats
                    .Where(c => c.Id % 2 == 0)
                    .Select(c => new
                    {
                        c.Name,
                        c.Age,
                        c.Owner
                    })
                    .ToList();

                Console.WriteLine(db.ChangeTracker.Entries().Count());
                Console.WriteLine(watch.Elapsed);
            }
            
            // if we have too many searches using the same property we should consider adding index on it (either with attribute or in the model builder)
        }

        private static void CompiledQuery(int age, string nameStart)
        {
            // again the first time will be a bit slower cuz its a cold query but after that it becomes warm and will be really fast
            using (var db = new TestDbContext())
            {
                var cat = CatQueries.CatQuery(db, age, nameStart);
            }

            // this is slow
            //using (var db = new TestDbContext())
            //{
            //    var cat = db.Cats
            //        .Where(c =>
            //            c.Age >= 15 &&
            //            c.Color.Contains("B") &&
            //            c.Owner.Cats.Any(oc => oc.Age < age) &&
            //            c.Owner.Cats.Count(oc => oc.Name.Length > 3) > 3)
            //        .Select(c => new CatFamilyResult
            //        {
            //            Name = c.Name,
            //            Cats = c.Owner
            //                .Cats
            //                .Count(oc =>
            //                    oc.Age < age &&
            //                    oc.Name.StartsWith(nameStart))
            //        })
            //        .ToList();
            //}
        }

        private static void PrintComplexQuery()
        {
            // some complex and heavy query
            // there are few ways to improve it: 
            // first is with https://docs.microsoft.com/en-us/ef/ef6/fundamentals/performance/ngen
            // second is to split the DbContext into multiple (general rule is to split it if it has more than 10 db sets)
            // third is to call the complex/heavy query on application start since EF uses expression trees to analyze, complie the code and then is being cached after the first use. So basically the first call of the heavy query is called cold query cuz it needs all the analyze, compile and execute and store in cache and then the second one is warm since its already cached and should be far more faster (just analyze and execute, the compile is expensive which we avoid).

            // example of complex
            using (var db = new TestDbContext())
            {
                var cat = db.Cats
                    .Where(c =>
                        c.Age >= 15 &&
                        c.Color.Contains("B") &&
                        c.Owner.Cats.Any(oc => oc.Age < 5) &&
                        c.Owner.Cats.Count(oc => oc.Name.Length > 3) > 3)
                    .Select(c => new
                    {
                        c.Name,
                        Cats = c.Owner
                            .Cats
                            .Count(oc =>
                                oc.Age < 5 &&
                                oc.Name.StartsWith("C"))
                    })
                    .ToList();
            }
        }

        private static void DeleteDataWithOneQuerry()
        {
            // this makes 2 queries which we dont want
            //using (var db = new TestDbContext())
            //{
            //    var cat = db.Cats.Find(1);

            //    db.Remove(cat);

            //    db.SaveChanges();
            //}

            // instead we want only 1 query
            // and because EF uses primary keys to track we can simulate object by creating empty one with the correct primary key
            using (var db = new TestDbContext())
            {
                var cat = new Cat { Id = 1 };

                db.Remove(cat);

                db.SaveChanges();
            }

            // for multiple entries the raw sql is better
            //using (var db = new TestDbContext())
            //{
            //    var searchWord = "or";

            //    var catsToDelete = db.Cats
            //        .Where(c => c.Name.Contains(searchWord))
            //        .Select(c => c.Id);

            //    db.RemoveRange(catsToDelete.Select(id => new Cat { Id = id }));

            //    db.SaveChanges();
            //}

            using (var db = new TestDbContext())
            {
                var searchWord = "search";

                db.Database
                    .ExecuteSqlCommand(
                        "DELETE FROM Cats WHERE Name LIKE @SearchWord", 
                        new SqlParameter("@SearchWord", searchWord));
            }
        }

        private static void TestGetData()
        {
            using (var db = new TestDbContext())
            {
                var results = db.Owners
                    .Where(o => o.Name.Contains("Gosho"))
                    .Select(o => new
                    {
                        Cats = o.Cats
                            .Where(c => c.Color.Contains("Yellow"))
                    })
                    .ToList();

                Console.WriteLine(string.Join(Environment.NewLine,
                    results
                        .FirstOrDefault()
                        .Cats
                        .Select(c => c.Name)));
            }
        }

        private static void SeedData()
        {
            using (var db = new TestDbContext())
            {
                var owner = new Owner
                {
                    Name = "Gosho",
                    Address = "Aleluq 5",
                };

                db.Owners.Add(owner);

                for (int i = 0; i < 40; i++)
                {
                    var cat = new Cat
                    {
                        Age = i,
                        Color = $"Yellow {i}",
                        Name = $"Rijko {i}",
                        Owner = owner
                    };

                    db.Cats.Add(cat);
                }

                db.SaveChanges();
            }
        }
    }
}
