namespace OpenGM
{
    public class PersistentRoom
    {
	    public required int RoomAssetId;

	    public required RoomContainer Container;
	    public required List<GamemakerObject> Instances;
    }
}
