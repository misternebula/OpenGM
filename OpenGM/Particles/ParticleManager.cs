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
					EmitterBurst(sys, emitter, emitter.PartType, emitter.Number);
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

		public static void EmitterBurst(ParticleSystem system, ParticleEmitter emitter, int parttype, int number)
		{

		}

		public static Particle CreateParticle(double x, double y, int particleTypeId)
		{
			var p = new Particle();
			var type = PartTypes[particleTypeId];

			p.Alive = true;
			p.PartType = particleTypeId;
			p.X = x;
			p.Y = y;
			p.XOld = x;
			p.YOld = y;
			p.Speed = MyRandom(type.SpeedMin, type.SpeedMax, 0);
			p.Dir = MyRandom(type.DirMin, type.DirMax, 0);
			p.Angle = MyRandom(type.AngleMin, type.AngleMax, 0);
			p.Lifetime = (int)MyRandom(type.LifeMin, type.LifeMax, 0); // TODO : exactly how is this rounded?
			p.Age = 0;
			// compute color
			p.Alpha = type.AlphaStart;
			p.Size = MyRandom(type.SizeMin, type.SizeMax, 0);
			if (!type.SpriteRandom)
			{
				p.SpriteStart = 0;
			}
			else
			{
				p.SpriteStart = (int)GMRandom.YYRandom(10000);
			}
			p.Ran = (int)GMRandom.YYRandom(10000);
		}

		private static double MyRandom(double min, double max, double dist)
		{
			var range = max - min;

			if (range <= 0)
			{
				return min;
			}

			double xx;

			switch (dist)
			{
				case 0: // PART_EDISTR_LINEAR
					return (GMRandom.fYYRandom() * range) + min;
				case 1: // PART_EDISTR_GAUSSIAN
					do
					{
						xx = (GMRandom.fYYRandom() - 0.5) * 6;
					} while (Math.Exp(-(xx * xx) * 0.5) <= GMRandom.fYYRandom());

					return min + ((xx + 3) * (1.0 / 6.0)) * range;
				case 2: // PART_EDISTR_INVGAUSSIAN
					do
					{
						xx = (GMRandom.fYYRandom() - 0.5) * 6;
					} while (!(Math.Exp(-(xx * xx) * 0.5) > GMRandom.fYYRandom()));

					if (xx < 0)
					{
						xx += 6;
					}

					return min + (xx * (1.0 / 6.0)) * range;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
