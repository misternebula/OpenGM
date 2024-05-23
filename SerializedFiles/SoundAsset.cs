using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.SerializedFiles;

[Serializable]
public class SoundAsset
{
	public int AssetID;
	public string Name;
	public string File;
	public uint Effects;
	public float Volume;
	public float Pitch;
}
