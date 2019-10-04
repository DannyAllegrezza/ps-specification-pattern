using System;
using System.Linq.Expressions;
using Logic.Utils;

namespace Logic.Movies
{
    public class Movie : Entity
    {
        public static readonly Expression<Func<Movie, bool>> IsSuitableForChildren = x => x.MpaaRating <= MpaaRating.PG;

        public static readonly Expression<Func<Movie, bool>> HasCDVersion = x => x.ReleaseDate <= DateTime.Now.AddMonths(-6);

        public virtual string Name { get; }
        public virtual DateTime ReleaseDate { get; }
        public virtual MpaaRating MpaaRating { get; }
        public virtual string Genre { get; }
        public virtual double Rating { get; }

        protected Movie()
        {
        }
    }


    public enum MpaaRating
    {
        G = 1,
        PG = 2,
        PG13 = 3,
        R = 4
    }
}
