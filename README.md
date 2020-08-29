# DirtyDatabaseGenerator
A quick-and-dirty C# program to create arbitrarily-sized databases with a configurable number of tables, indexes, columns, and rows of data - all randomized

At the time of writing this program outputs SQL that can be ran against the following MSSQL version:

SQL Server Management Studio						15.0.18206.0  
Microsoft Analysis Services Client Tools						15.0.1567.0  
Microsoft Data Access Components (MDAC)						10.0.18362.1  
Microsoft MSXML						3.0 6.0   
Microsoft Internet Explorer						9.11.18362.0  
Microsoft .NET Framework						4.0.30319.42000  
Operating System						10.0.18362  

## Purpose
This program is an extremely quick and minimal program that generates a database of arbitrary size for any kind of testing.
It is nowhere near what I would call a sexy public API to be used regularly, but in a pinch it can certainly
service its purpose.

## How to use
Clone the repo, modify the arguments in `Main` to suit your needs, and run. The program will open two instances of notepad.  
The first will drop-and-recreate the database, and all tables therein.  
The second will create all of the rows to be inserted per-table.
