# DirtyDatabaseGenerator
A quick-and-dirty C# program to create arbitrarily-sized databases with a configurable number of tables, indexes, columns, and rows of data - all randomized

## Purpose
This program is an extremely quick and minimal program that generates a database of arbitrary size for any kind of testing.
It is nowhere near what I would call a sexy public API to be used regularly, but in a pinch it can certainl
service its purpose.

## How to use
Clone the repo, modify the arguments in `Main` to suit your needs, and run. The program will open two instances of notepad.  
The first will drop-and-recreate the database, and all tables therein.  
The second will create all of the rows to be inserted per-table.
