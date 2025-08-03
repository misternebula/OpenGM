﻿using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine.BuiltInFunctions;

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

        public static int ParticleSystemCreate()
        {
            var element = new CLayerParticleElement
            {
                Type = ElementType.ParticleSystem,
                Id = GameLoader.CurrentElementID++,
            };
            
            // TODO : add element to room

            var id = ParticleSystemId++;
            var sys = new ParticleSystem();

            element.SystemID = id;
            sys.ElementID = element.Id;
            PartSystems.Add(id, sys);

            return id;
        }

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
            DebugLog.LogWarning("Method HandleLife not implemented.");
        }

        public static void HandleMotion(ParticleSystem sys)
        {
            DebugLog.LogWarning("Method HandleMotion not implemented.");
        }

        public static void HandleShape(ParticleSystem sys)
        {
            DebugLog.LogWarning("Method HandleShape not implemented.");
        }

        public static void Draw(int ind)
        {
            var sys = PartSystems[ind];

            if (sys.oldtonew)
            {
                DrawParticles(sys, 0, sys.Particles.Count, 1);
            }
            else
            {
                DrawParticles(sys, sys.Particles.Count - 1, -1, -1);
            }
        }

        public static void DrawParticles(ParticleSystem sys, int start, int end, int increment)
        {
            for (var i = start; i != end; i += increment)
            {
                var p = sys.Particles[i];
                if (p.Lifetime < 1)
                {
                    return;
                }

                var type = PartTypes[p.PartType];
                var spriteExists = SpriteManager.SpriteExists(type.Sprite);
                SpriteData data;
                int spriteStart;

                if (spriteExists)
                {
                    data = SpriteManager.GetSpriteAsset(type.Sprite)!;

                    if (data.Textures.Count < 1)
                    {
                        return;
                    }

                    if (!type.SpriteAnim)
                    {
                        spriteStart = p.SpriteStart;
                    }
                    else if (!type.SpriteStretch)
                    {
                        spriteStart = p.SpriteStart + p.Age;
                    }
                    else
                    {
                        spriteStart = p.SpriteStart + ((p.Age * data.Textures.Count) / p.Lifetime);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        // ParticleSystem_Emitter_Burst
        public static void EmitterBurst(ParticleSystem system, ParticleEmitter emitter, int parttype, int number)
        {
            // what the actual fuck is this??? i am so confused
            if (number < 0)
            {
                if (GMRandom.YYRandom(-number) != 0)
                {
                    return;
                }

                number = 1;
            }

            DebugLog.LogWarning("Method EmitterBurst not implemented.");
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
            ComputeColor(p);
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

            return p;
        }

        // https://github.com/YoYoGames/GameMaker-HTML5/blob/74b6f0783881bb08be6aab3a73aedb81ed338ca5/scripts/yyParticle.js#L626
        private static void ComputeColor(Particle p)
        {
            var type = PartTypes[p.PartType];

            if (p.Age <= 0 || p.Lifetime <= 0)
            {
                switch (type.ColMode)
                {
                    case 0: // COLMODE_ONE
                    case 1: // COLMODE_TWO
                    case 2: // COLMODE_THREE
                        p.Color = type.ColPar[0];
                        break;
                    case 3: // COLMODE_RGB
                        var r = (int)MyRandom(type.ColPar[0], type.ColPar[1], 0);
                        var g = (int)MyRandom(type.ColPar[2], type.ColPar[3], 0);
                        var b = (int)MyRandom(type.ColPar[4], type.ColPar[5], 0);
                        p.Color = (int)GraphicFunctions.make_color_rgb(r, g, b);
                        break;
                    case 4: // COLMODE_HSV
                        var h = (int)MyRandom(type.ColPar[0], type.ColPar[1], 0);
                        var s = (int)MyRandom(type.ColPar[2], type.ColPar[3], 0);
                        var v = (int)MyRandom(type.ColPar[4], type.ColPar[5], 0);
                        p.Color = (int)GraphicFunctions.make_color_hsv(h, s, v);
                        break;
                    case 5: // COLMODE_MIX
                        p.Color = (int)GraphicFunctions.merge_colour(type.ColPar[0], type.ColPar[1], GMRandom.fYYRandom());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                switch (type.ColMode)
                {
                    case 0: // COLMODE_ONE
                        p.Color = type.ColPar[0];
                        break;
                    case 1: // COLMODE_TWO
                    {
                        var val = p.Age / (double)p.Lifetime;
                        if (val > 1)
                        {
                            val = 1;
                        }

                        p.Color = (int)GraphicFunctions.merge_colour(type.ColPar[0], type.ColPar[1], val);
                        break;
                    }
                    case 2: // COLMODE_THREE
                    {
                        var val = 2 * p.Age / (double)p.Lifetime;
                        if (val > 2)
                        {
                            val = 2;
                        }

                        if (val < 1)
                        {
                            p.Color = (int)GraphicFunctions.merge_colour(type.ColPar[0], type.ColPar[1], val);
                        }
                        else
                        {
                            p.Color = (int)GraphicFunctions.merge_colour(type.ColPar[1], type.ColPar[2], val - 1);
                        }

                        break;
                    }
                }
            }
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
