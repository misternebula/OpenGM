namespace OpenGM.Particles
{
    public class ParticleType
    {
        public int Sprite = -1;
        public bool SpriteAnim = true;
        public bool SpriteStretch = false;
        public bool SpriteRandom = false;
        public int Shape = 0;
        public double SizeMin = 1;
        public double SizeMax = 1;
        public double SizeIncr = 0;
        public double SizeRandom = 0;
        public double XScale = 1;
        public double YScale = 1;
        public int LifeMin = 100;
        public int LifeMax = 100;
        public int DeathType = 0;
        public int DeathNumber = 0;
        public int StepType = 0;
        public int StepNumber = 0;
        public double SpeedMin = 0;
        public double SpeedMax = 0;
        public double SpeedIncr = 0;
        public double SpeedRandom = 0;
        public double DirMin = 0;
        public double DirMax = 0;
        public double DirIncr = 0;
        public double DirRandom = 0;
        public double Gravity = 0;
        public double GravityDirection = 270;
        public double AngleMin = 0;
        public double AngleMax = 0;
        public double AngleIncr = 0;
        public double AngleRandom = 0;
        public bool AngleDir = false;
        public int ColMode = 0;
        public int[] ColPar = new int[6] { 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0, 0, 0 };
        public double AlphaStart = 1;
        public double AlphaMiddle = 1;
        public double AlphaEnd = 1;
        public bool AdditiveBlend;
    }
}
