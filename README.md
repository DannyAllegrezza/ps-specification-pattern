# ps-specification-pattern
My notes/code samples for the Pluralsight course "Specification Pattern in C#" by Vladimir Khorikov.

Additional Resources:
1. https://deviq.com/specification-pattern/

# Getting started
The instructor has provided a SQL script which can be used to seed a new database in SQL Server.

1. Copy `Database.sql` into your `localdb` or wherever you're running an instance of SQL Server and execute the command.
2. Update `App.xaml.cs` `Init()` method to include the connection string to your database. In this demo I'm using SQL LocalDB.

## LINQ Refresh

There are primary interfaces when using LINQ.

1. `IEnumerable` - in an ORM this is used to generate the actual SQL
2. `IQueryable` - in an ORM this 

`IEnumerable` has a `Func<TSource,bool>` predicate where as `IQueryable` has a predicate of `Expression<Func<TSource,bool>>`.

#### IQueryable inherits from IEnumerable
This is important to remember, because let's say we have a type which implements both IEnumerable and IQueryable and we want to use the `.Where()` method, the compiler is going to pick the most "specific/concrete" implementation of `.Where()` which would be from `IQueryable`.

## Word of caution

A reminder -- you can write code that is safe at compile time but will **blow up** at execution time. Imagine the following scenario:

```
// method on the Movie type
public bool IsSuitableForChildren(){
	return MpaaRating <= PG;
}
```

and then we tried to use this in a LINQ query using an ORM

```
// ef
dbContext.Movies.Where(x => x.IsSuitableForChildren()).ToList(); // kaboom!
```

Although 

* Expressions can be compiled into delegates, but this is a one way operation. You cannot take a delegate and "decompile" it into an Expression!

## The Specification Pattern

### Problems it tries to solve
The problems listed here are specific to issues I've personally encountered while working with ORM tools such as entity framework.

Imagine you have a database that stores Movie data. Something simple, like the table used in this demo:

| MovieId     	| (PK) int     	|
| Name        	| nvarchar(50) 	|
| ReleaseDate 	| datetime     	|
| MpaaRating  	| int          	|
| Genre       	| nvarchar(50) 	|
| Rating      	| float        	|


* Duplicating domain knowledge

### Attempt 1: Implementing the Specification Pattern the Naive Way
Our problems at hand are that our Repository `GetList()` method continues to grow as we tack on filters. 

```
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
```

This is really common and something I've seen in many production applications. To solve this, our first naive attempt is to apply a basic version of the Specification Pattern through plain ol' C# Expressions. Let's remove those filters and just take in a single `Expression`

```
public IReadOnlyList<Movie> GetList(Expression<Func<Movie,bool>> expression)
{
    using (ISession session = SessionFactory.OpenSession())
    {
        return session.Query<Movie>()
            .Where(expression)
            .ToList();
    }
}
```

On our `Movie` domain object, let's add these two filters as `Expression`s. 

```
    public class Movie : Entity
    {
        public static readonly Expression<Func<Movie, bool>> IsSuitableForChildren = x => x.MpaaRating <= MpaaRating.PG;

        public static readonly Expression<Func<Movie, bool>> HasCDVersion = x => x.ReleaseDate <= DateTime.Now.AddMonths(-6);
		
		/ ** We can get rid of the old Methods
		public virtual bool IsSuitableForChildren()
        {
            return MpaaRating <= MpaaRating.PG;
        }

        public virtual bool HasCDVersion()
        {
            return ReleaseDate <= DateTime.Now.AddMonths(-6);
        }
		** /
	}
```

Now our client `MovieListViewModel` class must be updated. Previously, it had duplicated domain knowledge which we tried to encapsulate into the 2 methods above which are commented out.

```
private void BuyChildTicket(long movieId)
{
    Maybe<Movie> movieOrNothing = _repository.GetOne(movieId);
    if (movieOrNothing.HasNoValue)
        return;

    Movie movie = movieOrNothing.Value;

	// if (!movie.IsSuitableForChildren()) ... old version - calls the Method on the Movie class

	// new version with Expression. We have to manually compile the Expression to a Delegate... 
    Func<Movie,bool> isSuitableForChildren = Movie.IsSuitableForChildren.Compile();

    if (!isSuitableForChildren(movie))
    {
        // ...
    }
}
```

As the comments show, the whole idea here is that we now have an `Expression` which **can** be used to generate SQL at our base Repo method layer. The old Methods couldn't be used in the Repository's `Query<Movie>()` method, which returns an `IQueryable`.

Finally, the `Search()` method gets updated 

```
private void Search()
{
	// previous implementation of calling the Repository
    // Movies = _repository.GetList(ForKidsOnly, MinimumRating, OnCD);

	// new implementation using our Expressions
    // here is where our refactor falls apart..
    Expression<Func<Movie, bool>> expression = ForKidsOnly ? Movie.IsSuitableForChildren : x => true;

    Movies = _repository.GetList(expression);

    Notify(nameof(Movies));
}
```
The 2 major drawbacks are that:

1. We're only passing in 1 Expression, so we have no easy way to combine the Expressions together for dynamic filtering. 
2. We have to compile the Expression into a Delegate in the client code. Gnarly.

## Attempt 2: Using a Generic Specification class
This attempt is only slightly better, but sets us up for where we are going. 
> Keep in mind that a Specification is a container for **one piece** of domain knowledge that can be reused in different scenarios.

```
public class GenericSpecification<T>
{
    public Expression<Func<T, bool>> Expression { get; }

    public GenericSpecification(Expression<Func<T, bool>> expression)
    {
        Expression = expression;
    }

    public bool IsSatisfiedBy(T entity)
    {
        return Expression.Compile().Invoke(entity);
    }
}
```

The repository gets updated as follows:

```
public IReadOnlyList<Movie> GetList(GenericSpecification<Movie> specification)
{
    using (ISession session = SessionFactory.OpenSession())
    {
        return session.Query<Movie>()
            .Where(specification.Expression)
            .ToList();
    }
}
```

The client code will then `new` up instances of this class..

```
var specification = new GenericSpecification<Movie>(x => x.ReleaseDate <= DateTime.Now.AddMonths(-6));

if (!specification.IsSatisfiedBy(movie)) { ... }

// old code
// Func<Movie,bool> isSuitableForChildren = Movie.IsSuitableForChildren.Compile();
// if (!isSuitableForChildren(movie)) { ... }

```

* This is just a thin wrapper on top of the Expressions we've used in our first attempt. 
* Has the same problems as our first attempt.
* Should be avoided, doesn't solve our problem.

## The IQueryable debate
This is a hot topic which I've read countless articles, watched videos, and had many watercooler chats about. 

> Why not just return `IQueryable` from your repository? 

At first glance, the pros seem to outweigh the cons. 

* You're being very generic, and putting the earnest in the client callers hands
* No need for complex Expressions, Specifications, etc
* Instead of returning IEnumerable, which will pull down the entire table into memory, you can let your client specify their exact condition, including the columns they want to pull down, to generate the best possible SQL

The author of this course is in the "never return IQueryable from a public interface" camp. Steve Smith has a great episode of Weekly Dev Tips discussing this topic.

https://www.weeklydevtips.com/episodes/024

## Attempt 3: The Specification class

In this attempt, we implement the Specification pattern in a way that is encapsulated and type safe.

```
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

```

#### Guidelines
* Be as "specific" as possible
* Avoid making an ISpecification interface - YAGNI
* Make specifications immutable