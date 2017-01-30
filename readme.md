## RavenDB indexes bug demo

This demo reproduces critical problem with RavenDB auto-indexes.

### Intro

Apparently, there's an issue which causes the valid index on some document field to be broken,
once a new "auto" index is created, referencing some non-existent field.

### Running

In order to run the demo, please ensure you have an instance of ravenDB running on your local machine on port 8080, or adjust the RavenDbUrl constant in Program.cs

Once ran, please do the follwing:

1. run import (i)
2. ensure you've got 5 items found in both cases (if not, press enter to re-run query - the eventual consistency...)
3. create broken index (c)
4. the ravendb-side query should return no results, yet the in-memory filtering should return 5 items
5. remove broken index (r)
6. the query should behave properly again


### Sidenotes

The demo creates the broken index using ``session.Advanced.DocumentQuery``. Please note however that this can be done via Raven Studio as well. 
Go to "Query", select Entities/dynamic index, type any field name you want (ex. ``Foo: 99``) and run the query. The invalid index has been created for you.

For some reason, when the typed-index (t) is created before an invalid dynamic (auto) index, the querying works properly.

The demo has been tested against the builds 35187 (most recent) and 3800 of Raven Server.