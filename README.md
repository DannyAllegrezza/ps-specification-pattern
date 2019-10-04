# ps-specification-pattern
My notes/code samples for the Pluralsight course "Specification Pattern in C#" by Vladimir Khorikov

# Getting started
The instructor has provided a SQL script which can be used to seed a new database in SQL Server.

1. Copy `Database.sql` into your `localdb` or wherever you're running an instance of SQL Server and execute the command.
2. Update `App.xaml.cs` `Init()` method to include the connection string to your database. In this demo I'm using SQL LocalDB.