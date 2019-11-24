namespace EfTesting
{
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class CatQueries
    {
        public static Func<TestDbContext, int, string, IEnumerable<CatFamilyResult>> CatQuery
            => EF.CompileQuery((TestDbContext db, int age, string nameStart) =>
                db.Cats
                    .Where(c =>
                        c.Age >= 15 &&
                        c.Color.Contains("B") &&
                        c.Owner.Cats.Any(oc => oc.Age < age) &&
                        c.Owner.Cats.Count(oc => oc.Name.Length > 3) > 3)
                    .Select(c => new CatFamilyResult
                    {
                        Name = c.Name,
                        Cats = c.Owner
                            .Cats
                            .Count(oc =>
                                oc.Age < age &&
                                oc.Name.StartsWith(nameStart))
                    }));
    }
}
