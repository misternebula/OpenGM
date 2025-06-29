namespace OpenGM.Particles
{
	public class ParticleSystem
	{
		public List<Particle> Particles = new();
		public List<ParticleEmitter> Emitters = new();
		public bool oldtonew;
		public float Depth;
		public float XDraw;
		public float YDraw;
		public bool AutomaticUpdate;
		public bool AutomaticDraw;
		public int ElementID;
		public bool Volatile;
	}
}
