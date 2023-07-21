# stream-utilities
Contains simple utilities for helping when working with the Stream class in .Net

It currently contains the following helper classes:

## DelegatingStream
This is usefull for overwriting a single method in a stream, or simply embedding small code.

## TestReadStream
Usefull in scenarios where you want to test code that handles real world `Stream`. The `TestReadStream` behaves much like a real world `Stream` does - for example it does not allow you to set position. This enables you to identify possible bugs that would not have been found by testing with the `MemoryStream`.

## StreamCounter
Can be usefull to track the number of bytes read or written. Rather usefull if you receive a `Stream` where you cannot determine length (ex. a file received via network), but you want to calculate it while you process it (ex. while saving it to a database).