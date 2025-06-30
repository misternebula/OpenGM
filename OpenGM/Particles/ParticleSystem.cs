namespace OpenGM.Particles
{
	public class ParticleSystem
	{
		public List<Particle> Particles = new();
		public List<ParticleEmitter> Emitters = new();
		public bool oldtonew = true;
		public float Depth;
		public float XDraw;
		public float YDraw;
		public bool AutomaticUpdate = true;
		public bool AutomaticDraw = true;
		public int ElementID;
		public bool Volatile;
	}
}
