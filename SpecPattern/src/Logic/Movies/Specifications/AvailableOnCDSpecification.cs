using System;
using System.Linq.Expressions;

namespace Logic.Movies.Specifications
{
    public sealed class AvailableOnCDSpecification : Specification<Movie>
    {
        private const int MonthsBeforeDVDIsOut = 6;
        public override Expression<Func<Movie, bool>> ToExpression()
        {

            return x => x.ReleaseDate <= DateTime.Now.AddMonths(-MonthsBeforeDVDIsOut);
        }
    }
}
