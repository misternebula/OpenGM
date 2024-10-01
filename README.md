# OpenGM

## What is this?
This program aims to run Gamemaker Studio games, emulating the original games as closely as possible. The assembly code of the game is parsed and executed in a stack-based system, and the built-in Gamemaker functions are replicated too.

## How do I get this running with a game?
> [!IMPORTANT]  
> A lot of this is temporary, since the project is (relatively) unfinished. A better way of loading games is planned.
1. Build the project.
2. Navigate to (or create) `\GMRunner\OpenGM\bin\game\`
3. Place all files and folders from the game folder into this folder.
4. (Optional) Install OpenAL by following [these instructions](https://github.com/misternebula/OpenGM/blob/main/AudioManager.cs#L15-L18) (Windows only)
5. Run the project.
6. OpenGM should now convert the games `data.win` into a file named `data_OpenGM.win`. *This is not the same file format as the original file.* This file is a compressed file used to store intermediary data that OpenGM use.

If any errors occur, OpenGM probably does not support features of the GameMaker version the game uses.

## Supported Games
Ideally, any Gamemaker game should work. However, it's impossible to support every single quirk of every single engine version.

The following games are used in development to test feature parity, and as such OpenGM should support any game running on the same engine version as these games :
- DELTARUNE (Chapter 1 & 2 DEMO) - Engine Version 2022.1
- DELTARUNE Chapter 1 (SURVEY_PROGRAM) - Engine Version 2.0.6

The following games are either being worked on, or planned to be supported :
- Undertale - Engine Version 1.0.0.1539

Not every feature from these games may work, but the game will at least load and try to execute.

## Legal Stuff
This project is covered by the MIT license. See [the license](LICENSE) for more information.

Parts of the [HTML runner](https://github.com/YoYoGames/GameMaker-HTML5) have been used as reference for how to implement certain functionality.
