# OpenGM

## What is this?
This program aims to run Gamemaker Studio games, emulating the original games as closely as possible. The assembly code of the game is parsed and executed in a stack-based system, and the built-in Gamemaker functions are replicated too.

## How do I get this running with a game?
> [!IMPORTANT]  
> A lot of this is temporary, since the project is (relatively) unfinished. A better way of loading games is planned.
1. Build the project.
2. Navigate to (or create) `\OpenGM\OpenGM\bin\game\`
3. Place all files and folders from the game folder into this folder.
5. Run the project.
6. OpenGM should now convert the games `data.win` into a file named `data_OpenGM.win`. *This is not the same file format as the original file.* This file is a compressed file used to store intermediary data that OpenGM use.

If any errors occur, OpenGM probably does not support features of the GameMaker version the game uses.

## Supported Games
Ideally, any non-YYC Gamemaker game should work. However, every engine version has its own bugs and changes. To maintain compatibility, we're trying to cover as many quirks as possible, but it would be impossible to find everything. Here's what we've got so far :

- Prior to GMS2, depth rendering was forced to 0 - the same as using `layer_force_draw_depth`.
- Prior to GMS2, `#` characters were treated as newlines.
- Prior to 2.3, `script_execute` returned `0` instead of `null`.
- Prior to 2.3, script ids started at 0 instead of 10000.
- Prior to 2.3, instances of the same depth were rendered by descending instance id instead of ascending.
- Prior to 2022.1, collision was calculated with integer values instead of floating-point values.

The following games have been tested with OpenGM, and are used to improve functionality and test parity. If you test a different game, please let us know how it goes!

| Game | Engine Version | Notes |
| -------------------------------------------- | -------------------- | --------------------------------------------------------------------------------------------------------------- |
| MINDWAVE Demo								   | 2024.11.0.226		  | Loads, but missing core engine features.																		|
| DELTARUNE                                    | 2022.0.3.104 (LTS-I) |																													|
| - Chapter 1                                  | "					  | Only very minor visual glitches/flashes.																		|
| - Chapter 2                                  | "					  | 																												|
| - Chapter 3                                  | "					  |																													|
| - Chapter 4                                  | "					  |																													|
| DELTARUNE Chapter 1 & 2 DEMO (LTS Branch)    | 2022.0.3.99 (LTS)	  | Works well.																										|
| FAITH: The Unholy Trinity v1.5               | 2022.0.2.49 (LTS)    |	Loads, camera stuck in one place?																				|
| Pizza Tower Eggplant Build                   | 2022.3.0.497		  |																													|
| DELTARUNE Chapter 1 & 2 DEMO                 | 2022.1.0.482		  |	Works well.																										|
| Pizza Tower SAGE 2019 Demo                   | 2.2.3.344			  |																													|
| DELTARUNE Chapter 1 (SURVEY_PROGRAM)         | 2.2.0.258			  |	Works well.																										|
| FAITH Demon Seige							   | 1.4.9999			  | Screen shader effect not working, some text invisible, camera stuck in top left, enemies don't spawn properly.	|
| Undertale                                    | 1.0.0.1539			  | Kind of works, but breaks in a lot of places.																	|

## Dependencies
- [MemoryPack](https://github.com/Cysharp/MemoryPack)
- [NAudio](https://github.com/naudio/NAudio)
- [OpenTK](https://github.com/opentk/opentk)
- [StbImageSharp](https://github.com/StbSharp/StbImageSharp)
- [StbVorbisSharp](https://github.com/StbSharp/StbVorbisSharp)
- [MotionTK](https://github.com/AtomCrafty/MotionTK)
- [UndertaleModLib](https://github.com/UnderminersTeam/UndertaleModTool)

## Legal Stuff
This project is covered by the MIT license. See [the license](LICENSE) for more information.

Parts of the [HTML runner](https://github.com/YoYoGames/GameMaker-HTML5) have been used as reference for how to implement certain functionality.
