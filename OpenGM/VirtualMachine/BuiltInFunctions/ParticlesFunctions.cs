using OpenGM.Particles;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class ParticlesFunctions
    {
        [GMLFunction("part_type_create")]
        public static object? part_type_create(object?[] args)
        {
            return ParticleManager.ParticleTypeCreate();
        }

        // part_type_destroy
        // part_type_exists
        // part_type_clear
        // part_type_shape

        [GMLFunction("part_type_sprite")]
        public static object? part_type_sprite(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var sprite = args[1].Conv<int>();
            var animate = args[2].Conv<bool>();
            var stretch = args[3].Conv<bool>();
            var random = args[4].Conv<bool>();

            var type = ParticleManager.PartTypes[ind];
            type.Sprite = sprite;
            type.SpriteAnim = animate;
            type.SpriteStretch = stretch;
            type.SpriteRandom = random;

            return null;
        }

        [GMLFunction("part_type_size")]
        public static object? part_type_size(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var size_min = args[1].Conv<double>();
            var size_max = args[2].Conv<double>();
            var size_incr = args[3].Conv<double>();
            var size_wiggle = args[4].Conv<double>();

            var type = ParticleManager.PartTypes[ind];
            type.SizeMin = size_min;
            type.SizeMax = size_max;
            type.SizeIncr = size_incr;
            type.SizeRandom = size_wiggle;

            return null;
        }

        [GMLFunction("part_type_scale", GMLFunctionFlags.Stub)]
        public static object? part_type_scale(object?[] args)
        {
            return null;
        }

        [GMLFunction("part_type_life")]
        public static object? part_type_life(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var life_min = args[1].Conv<int>();
            var life_max = args[2].Conv<int>();

            var type = ParticleManager.PartTypes[ind];
            type.LifeMin = life_min;
            type.LifeMax = life_max;

            return null;
        }

        // part_type_step

        [GMLFunction("part_type_death")]
        public static object? part_type_death(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var death_number = args[1].Conv<int>();
            var death_type = args[2].Conv<int>();

            var type = ParticleManager.PartTypes[ind];
            type.DeathNumber = death_number;
            type.DeathType = death_type;

            return null;
        }

        [GMLFunction("part_type_speed")]
        public static object? part_type_speed(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var speed_min = args[1].Conv<double>();
            var speed_max = args[2].Conv<double>();
            var speed_incr = args[3].Conv<double>();
            var speed_wiggle = args[4].Conv<double>();

            var type = ParticleManager.PartTypes[ind];
            type.SpeedMin = speed_min;
            type.SpeedMax = speed_max;
            type.SpeedIncr = speed_incr;
            type.SpeedRandom = speed_wiggle;

            return null;
        }

        [GMLFunction("part_type_direction")]
        public static object? part_type_direction(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var dir_min = args[1].Conv<double>();
            var dir_max = args[2].Conv<double>();
            var dir_incr = args[3].Conv<double>();
            var dir_wiggle = args[4].Conv<double>();

            var type = ParticleManager.PartTypes[ind];
            type.DirMin = dir_min;
            type.DirMax = dir_max;
            type.DirIncr = dir_incr;
            type.DirRandom = dir_wiggle;
            return null;
        }

        // part_type_orientation

        [GMLFunction("part_type_gravity")]
        public static object? part_type_gravity(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var grav_amount = args[1].Conv<double>();
            var grav_direction = args[2].Conv<double>();

            var type = ParticleManager.PartTypes[ind];
            type.Gravity = grav_amount;
            type.GravityDirection = grav_direction;
            return null;
        }

        // part_type_color_mix
        // part_type_color_rgb
        // part_type_color_hsv
        // part_type_color1
        // part_type_color2
        // part_type_color3
        // part_type_colour_mix
        // part_type_colour_rgb
        // part_type_colour_hsv
        // part_type_colour1
        // part_type_colour2
        // part_type_colour3
        // part_type_alpha1
        // part_type_alpha2

        [GMLFunction("part_type_alpha3")]
        public static object? part_type_alpha3(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var alpha1 = args[1].Conv<double>();
            var alpha2 = args[2].Conv<double>();
            var alpha3 = args[3].Conv<double>();

            var type = ParticleManager.PartTypes[ind];
            type.AlphaStart = alpha1;
            type.AlphaMiddle = alpha2;
            type.AlphaEnd = alpha3;

            return null;
        }

        [GMLFunction("part_type_blend")]
        public static object? part_type_blend(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var additive = args[1].Conv<bool>();

            var type = ParticleManager.PartTypes[ind];
            type.AdditiveBlend = additive;

            return null;
        }

        [GMLFunction("part_system_create")]
        public static object? part_system_create(object?[] args)
        {
            return ParticleManager.ParticleSystemCreate();
        }

        [GMLFunction("part_system_destroy", GMLFunctionFlags.Stub)]
        public static object? part_system_destroy(object?[] args)
        {
            //var ind = args[0].Conv<int>();

            return null;
        }

        // part_system_exists
        // part_system_clear
        // part_system_draw_order

        [GMLFunction("part_system_depth", GMLFunctionFlags.Stub)]
        public static object? part_system_depth(object?[] args)
        {
            return null;
        }

        // part_system_position
        // part_system_automatic_update

        [GMLFunction("part_system_automatic_draw")]
        public static object? part_system_automatic_draw(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var automatic = args[1].Conv<bool>();

            var sys = ParticleManager.PartSystems[ind];
            sys.AutomaticDraw = automatic;

            return null;
        }

        [GMLFunction("part_system_update")]
        public static object? part_system_update(object?[] args)
        {
            var ind = args[0].Conv<int>();

            ParticleManager.UpdateSystem(ind);

            return null;
        }

        [GMLFunction("part_system_drawit", GMLFunctionFlags.Stub)]
        public static object? part_system_drawit(object?[] args)
        {
            //var ind = args[0].Conv<int>();

            return null;
        }

        // part_system_create_layer
        // part_system_get_layer
        // part_system_layer
        // part_particles_create
        // part_particles_create_color
        // part_particles_create_colour
        // part_particles_clear
        // part_particles_count

        [GMLFunction("part_emitter_create")]
        public static object? part_emitter_create(object?[] args)
        {
            var ps = args[0].Conv<int>();
            return ParticleManager.ParticleEmitterCreate(ps);
        }

        // part_emitter_destroy

        [GMLFunction("part_emitter_destroy_all", GMLFunctionFlags.Stub)]
        public static object? part_emitter_destroy_all(object?[] args)
        {
            //var ps = args[0].Conv<int>();

            return null;
        }

        // part_emitter_exists
        // part_emitter_clear

        [GMLFunction("part_emitter_region")]
        public static object? part_emitter_region(object?[] args)
        {
            var ps = args[0].Conv<int>();
            var ind = args[1].Conv<int>();
            var xmin = args[2].Conv<double>();
            var xmax = args[3].Conv<double>();
            var ymin = args[4].Conv<double>();
            var ymax = args[5].Conv<double>();
            var shape = args[6].Conv<int>();
            var distribution = args[7].Conv<int>();

            // currently not used as we store emitters with global ids, instead of per system
            //var sys = ParticleManager.PartSystems[ps]; 
            var emitter = ParticleManager.PartEmitters[ind];

            emitter.XMin = xmin;
            emitter.XMax = xmax;
            emitter.YMin = ymin;
            emitter.YMax = ymax;
            emitter.Shape = shape;
            emitter.Distribution = distribution;

            return null;
        }

        [GMLFunction("part_emitter_burst", GMLFunctionFlags.Stub)]
        public static object? part_emitter_burst(object?[] args)
        {
            return null;
        }

        [GMLFunction("part_emitter_stream")]
        public static object? part_emitter_stream(object?[] args)
        {
            var ps = args[0].Conv<int>();
            var ind = args[1].Conv<int>();
            var parttype = args[2].Conv<int>();
            var number = args[3].Conv<int>();

            // currently not used as we store emitters with global ids, instead of per system
            //var sys = ParticleManager.PartSystems[ps]; 
            var emitter = ParticleManager.PartEmitters[ind];

            emitter.Number = number;
            emitter.PartType = parttype;

            return null;
        }

        // effect_create_blow
        // effect_create_above
        // effect_clear
    }
}
