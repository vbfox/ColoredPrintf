### New in 2.0.0-alpha.1

* Depend on MasterOfFoo 2.0 for interpolated strings support

### New in 1.0.5

* Bump MasterOfFoo minimal version so that NuGet algorithm of resolving lowest version is happy

### New in 1.0.4

* Make dependency ranges less strict (For `FSharp.Core` 6.x)
* Build with 5.0.201 SDK
* Specify the package license using SPDX
* Embed the package icon

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
