using System;
using System.Linq.Expressions;

namespace Logic.Movies.Specifications
{
    public class MoviesForKidsSpecification : Specification<Movie>
    {
        public override Expression<Func<Movie, bool>> ToExpression()
        {
            return movie => movie.MpaaRating <= MpaaRating.PG;
        }
    }
}
