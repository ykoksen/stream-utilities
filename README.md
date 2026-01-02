# stream-utilities
Contains simple utilities for helping when working with the Stream class in .Net

It currently contains the following helper classes:

## DelegatingStream
This is usefull for overwriting a single method in a stream, or simply embedding small code.

## TestReadStream
Usefull in scenarios where you want to test code that handles real world `Stream`. The `TestReadStream` behaves much like a real world `Stream` does - for example it does not allow you to set position. This enables you to identify possible bugs that would not have been found by testing with the `MemoryStream`.

## StreamCounter
Can be usefull to track the number of bytes read or written. Rather usefull if you receive a `Stream` where you cannot determine length (ex. a file received via network), but you want to calculate it while you process it (ex. while saving it to a database).

## BufferBackgroundStream
A double buffer stream that will allow reading from one buffer while filling the other in the background using a background thread. Usefull for paralizing multiple time consuming stream operations. Example if you both want to calculate hash for a Stream and then compress it. Or receive it via network, compress and store in database (here you might want to use this class twice).

## TestLargeInputStream
For testing. This stream simulates a large input stream without using large amounts of memory. It simply returns random bytes when read. You can specify the length of the stream.

## TestEndlessStream
For testing. This stream simulates an endless input stream without using large amounts of memory. It simply returns random bytes when read. You can call a method to simulate end of the stream (on subsequent reads).

## StreamInverter (Obsolete and removed)
Can reverse a Stream wrapper direction. Usefull if you have a Stream that takes an output Stream in the constructor, but you wanted it to take an input Stream instead. Ex. when using the Stream compression methods in .net (ex. [GZipStream](https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.gzipstream?view=net-8.0)). This is obsolete, use [Pipe](https://learn.microsoft.com/en-us/dotnet/api/system.io.pipelines.pipe?view=net-10.0) in .Net instead.