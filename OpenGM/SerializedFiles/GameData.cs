﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemoryPack;
using OpenTK.Mathematics;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class GameData
{
	public required string Filename;
	public required int LastObjectID;
	public required int LastTileID;
	public required string Name;
	public required BranchType BranchType;
	public required uint Major;
	public required uint Minor;
	public required uint Release;
	public required uint Build;
	public required Vector2i DefaultWindowSize;
	public required float FPS;
}

public enum BranchType
{
	Pre2022,
	LTS2022,
	Post2022
}
