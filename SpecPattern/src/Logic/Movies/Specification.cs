using System;
using System.Linq.Expressions;

namespace Logic.Movies
{
    public abstract class Specification<T>
    {
        public bool IsSatisfiedBy(T entity)
        {
            Func<T, bool> predicate = ToExpression().Compile();
            return predicate(entity);
        }

        /// <summary>
        /// Making this abstract requires inheriting classes to provide the implementation of this method.
        /// </summary>
        /// <returns></returns>
        public abstract Expression<Func<T, bool>> ToExpression();
    }
}
