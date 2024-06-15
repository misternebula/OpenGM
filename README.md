# OpenGM

## What is this?
This program aims to run Gamemaker Studio games, emulating the original games as closely as possible. The assembly code of the game is parsed and executed in a stack-based system, and the built-in Gamemaker functions are replicated too.

## How do I get this running with a game?
> [!IMPORTANT]  
> A lot of this is temporary, since the project is (relatively) unfinished. A better way of loading games is planned.
1. Build the project.
2. Navigate to `\GMRunner\bin\Debug\net8.0\`
3. Place all files and folders from the game folder into this folder.
4. (Optional) Install OpenAL by following [these instructions](https://github.com/misternebula/OpenGM/blob/main/AudioManager.cs#L15-L18) (Windows only)
5. Run the project.
6. OpenGM should now extract all the game data and assets and export them into custom formats. If any errors occur during this stage, OpenGM currently does not support low-level features of the GameMaker version the game uses.

## Legal Stuff
This project is covered by the MIT license. See [the license](LICENSE) for more information.

No closed-source code has been used in this project. Any code taken from public open-source GameMaker repositories has had its source marked.
