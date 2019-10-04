using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using Logic.Utils;
using NHibernate;
using NHibernate.Linq;

namespace Logic.Movies
{
    public class MovieRepository
    {
        public Maybe<Movie> GetOne(long id)
        {
            using (ISession session = SessionFactory.OpenSession())
            {
                return session.Get<Movie>(id);
            }
        }

        public IReadOnlyList<Movie> GetList(
            bool forKidsOnly,
            double minimumRating,
            bool availableOnCD)
        {
            using (ISession session = SessionFactory.OpenSession())
            {
                return session.Query<Movie>()
                    .Where(x => 
                        (x.MpaaRating <= MpaaRating.PG || !forKidsOnly) &&
                        x.Rating >= minimumRating &&
                        (x.ReleaseDate <= DateTime.Now.AddMonths(-6) || !availableOnCD))
                    .ToList();
            }
        }
    }
}
