namespace OpenGM.Particles
{
	public static class ParticleManager
	{
		public static int ParticleTypeId;
		public static Dictionary<int, ParticleType> PartTypes = new();

		public static int ParticleSystemId;
		public static Dictionary<int, ParticleSystem> PartSystems = new();

		public static int ParticleEmitterId;
		public static Dictionary<int, ParticleEmitter> PartEmitters = new();

		/*public static int ParticleSystemCreate()
		{

		}*/

		public static int ParticleTypeCreate()
		{
			var id = ParticleTypeId++;
			PartTypes.Add(id, new ParticleType());
			return id;
		}

		public static int ParticleEmitterCreate(int particleSystem)
		{
			var id = ParticleEmitterId++;
			var sys = PartSystems[particleSystem];

			var emitter = new ParticleEmitter();

			sys.Emitters.Add(emitter);
			PartEmitters.Add(id, emitter);

			return id;
		}

		public static void UpdateSystem(int ind)
		{
			var sys = PartSystems[ind];
			HandleLife(sys);
			HandleMotion(sys);
			HandleShape(sys);

			foreach (var emitter in sys.Emitters)
			{
				if (emitter.Created && emitter.Number != 0)
				{
					// burst
				}
			}
		}

		public static void HandleLife(ParticleSystem sys)
		{

		}

		public static void HandleMotion(ParticleSystem sys)
		{

		}

		public static void HandleShape(ParticleSystem sys)
		{

		}
	}
}
