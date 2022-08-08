# stream-utilities
Contains simple utilities for helping when working with the Stream class in .Net

It currently contains two helper classes:

## DelegatingStream
This is usefull for overwriting a single method in a stream, or simply embedding small code.

## TestReadStream
Usefull in scenarios where you want to test code that handles real world `Stream`. The `TestReadStream` behaves much like a real world `Stream` does - for example it does not allow you to set position. This enables you to identify possible bugs that would not have been found by testing with the `MemoryStream`.
