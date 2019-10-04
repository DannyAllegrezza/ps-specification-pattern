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
