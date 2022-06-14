# osu! Beatmap Packs importer

Imports archives of osu! beatmaps as Collections in osu!stable.

## Installation

Download the executable from the [releases tab](https://github.com/ItsShamed/osu-packs-importer/releases). You may want to put it somewhere easily accessible and add it to your PATH.

## Usage

```
USAGE:
Convert and import a beatmap pack into a collection in the game:
  OsuPackImporter "<path to archive>"
Convert without importing a beatmap pack into a collection in the game:
  OsuPackImporter --no-import "<path to archive>"
Convert a beatmap pack into an .osdb file:
  OsuPackImporter --osdb "<path to output .osdb file>" "<path to archive>"

  -v, --verbose          (Default: false) Log more stuff to the console. Useful to debug the program and catch errors.

  --no-import            (Default: false) Prevent automatic import of beatmaps in the game when dumping into collection.db.

  --no-rename            (Default: false) Don't rename collections after import.

  --osdb                 Whether to export or not the parsed archive as an OSDB file.

  --osudir               The location of your osu!stable installation.

  --help                 Display help screen.

  --version              Display version information.

  input path (pos. 0)    Required. Location of the beatmap pack archive.
```

To convert a beatmap pack to a collection in-game, you can use the following command:

```
OsuPackImporter "<path to archive>"
```

To convert a beatmap pack into an .osdb file, you need to specify the path to the output .osdb file:

```
OsuPackImporter --osdb "<path to output .osdb file>" "<path to archive>"
```

By default, the program will ask you if you want to rename the imported
collections. You can disable this behaviour by passing the `--no-rename` 
flag:

```
OsuPackImporter --no-rename "<path to archive>"
```

If you don't provide arguments, it will go in a kind of interactive mode to set
those arguments for you.

__Note__: You will need to close the game before running the program, especially if you're not using conversion to a `.osdb` file.
The program will pause by itself anyway.

## License

This program is licensed under the [MIT license](LICENSE).