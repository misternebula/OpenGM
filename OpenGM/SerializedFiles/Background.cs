using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.SerializedFiles;
[MemoryPackable]
public partial class Background
{
	public required int AssetIndex;

	public required string Name;
	public bool Transparent;
	public bool Smooth;
	public bool Preload;
	public required SpritePageItem? Texture;
}
