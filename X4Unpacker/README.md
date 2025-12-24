# X4Unpacker

## What is this?

This is a command-line tool written in C# to extract game assets from X4 Foundations .cat and .dat archives. It allows you to access the raw files (xml, xsm, dds, etc.) used by the game for modding or inspection.

Its made to be a direct replacement for the X Compact Tool. Unpack function. I tried to use the compact tool on linux and it didnt work so this implementation should work better.

## Why use this tool?

- Smart Patching: X4 uses a "layer" system where files in 09.cat overwrite files in 01.cat. This tool automatically resolves these conflicts, ensuring you only get the final, correct version of every file.

- Internal De-duplication: Some catalog files contain duplicate entries (patches within patches). This tool identifies the correct winning line and ignores older versions to prevent infinite extraction loops.

- Incremental Updates: It checks file sizes and timestamps (UTC) before extracting. If you run the tool again after a game update, it only extracts the new or changed files.

- Parallel Processing: It uses multiple CPU threads to extract files rapidly.

## Requirements

.NET 9.0 SDK (or newer)

Windows, Linux, or macOS

## How to Compile

Open a terminal in the source folder.

Run the following command:
```shell
dotnet build
```
## How to Use

### Basic Usage

Run the tool without arguments to scan the current directory and extract to ./x4_unpacked. Just put it in the X4 Foundations folder and run it.

```shell
./X4Unpacker
```
#### Custom Paths

You can specify the input game directory and the output directory.

Syntax:
`X4Unpacker [Input Path] [Output Path] [Verify Hash (true/false)]`

Windows Example:
```shell
X4Unpacker.exe "C:\Program Files (x86)\Steam\steamapps\common\X4 Foundations" "C:\X4_Extracted"
```
Linux Example:
```shell
./X4Unpacker "/home/user/.local/share/Steam/steamapps/common/X4 Foundations" "./x4_unpacked"
```
## Notes

The tool mirrors the folder structure of DLCs (extensions) inside the output folder.

Signature files (*_sig.cat) are automatically ignored.

Hash validation (optional 3rd argument) is disabled by default for performance. Set to 'true' if you suspect data corruption.