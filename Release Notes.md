## New in 1.0.3

* Include pdb files in package (With SourceLink)
* Include XmlDoc in package

## New in 1.0.2

* Remove ValueTuple reference

### New in 1.0.1

* Add an icon to the NuGet package

### New in 1.0.0

* .Net 4.5 & .Net Core 2.0 compatible

### New in 0.3.0

* Fix printing escaped parts when text is accumulated. `colorprintfn "1 %d %d" 2 3` was generating `231` ([Issue #1](https://github.com/vbfox/ColoredPrintf/issues/1))

### New in 0.2.0

* Colors can now be specified as parameters like: `colorprintfn "Hello $%A[world] !" ConsoleColor.Red`.
* Pull a version of MasterOfFoo with correct `%A` output.

### New in 0.1.0

* First version
