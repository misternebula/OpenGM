using System;

namespace MotionTK
{
	public abstract class Packet : IDisposable
	{
		~Packet() => Dispose();
		public virtual void Dispose() => GC.SuppressFinalize(this);
	}
}