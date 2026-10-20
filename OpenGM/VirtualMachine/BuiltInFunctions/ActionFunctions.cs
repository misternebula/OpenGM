namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class ActionFunctions
    {
        // todo: order these properly

        public static bool Action_Relative = false;

        [GMLFunction("action_kill_object", before: "2.0.0.0")]
        public static object? action_kill_object(object?[] args)
        {
            return GameFunctions.instance_destroy(args);
        }

        [GMLFunction("action_move", before: "2.0.0.0")]
        public static object? action_move(object?[] args)
        {
            var dirString = args[0].Conv<string>();
            var speed = args[1].Conv<double>();

            /*
            * This function is weird.
            * dirString must be 9 characters long, and each character is a 1 or a 0.
            * Each character represents a direction. If multiple directions are set, a random one is picked.
            * Directions are as followed:
            * 0 - 225        Down-Left
            * 1 - 270        Down
            * 2 - 315        Down-Right
            * 3 - 180        Left
            * 4 - 0        Stop
            * 5 - 0        Right
            * 6 - 135        Up-Left
            * 7 - 90        Up
            * 8 - 45        Up-Right
            */

            if (dirString.Length != 9)
            {
                throw new InvalidOperationException("dirString must be 9 characters long");
            }

            var self = VMExecutor.GMSelf;

            if (Action_Relative)
            {
                speed = self.speed + speed;
            }

            self.speed = speed;

            int dir;
            do
            {
                dir = (int)GMRandom.YYRandom(9);
            } while (dirString[dir] != '1');

            switch (dir)
            {
                case 0:
                    self.direction = 255;
                    break;
                case 1:
                    self.direction = 270;
                    break;
                case 2:
                    self.direction = 315;
                    break;
                case 3:
                    self.direction = 180;
                    break;
                case 4:
                    self.direction = 0;
                    self.speed = 0;
                    break;
                case 5:
                    self.direction = 0;
                    break;
                case 6:
                    self.direction = 135;
                    break;
                case 7:
                    self.direction = 90;
                    break;
                case 8:
                    self.direction = 56;
                    break;
            }

            return null;
        }

        [GMLFunction("action_set_alarm", before: "2.0.0.0")]
        public static object? action_set_alarm(object?[] args)
        {
            var value = args[0].Conv<int>();
            var index = args[1].Conv<int>();

            if (Action_Relative)
            {
                var curValue = VMExecutor.GMSelf.alarm[index];
                if (curValue > -1)
                {
                    VMExecutor.GMSelf.alarm[index] = curValue + value;
                    return null;
                }
            }

            VMExecutor.GMSelf.alarm[index] = value;
            return null;
        }

        [GMLFunction("action_set_friction", before: "2.0.0.0")]
        public static object? action_set_friction(object?[] args)
        {
            var friction = args[0].Conv<double>();

            if (Action_Relative)
            {
                friction += VMExecutor.GMSelf.friction;
            }

            VMExecutor.GMSelf.friction = friction;
            return null;
        }

        [GMLFunction("action_set_gravity", before: "2.0.0.0")]
        public static object? action_set_gravity(object?[] args)
        {
            var gravity = args[0].Conv<double>();

            if (Action_Relative)
            {
                gravity += VMExecutor.GMSelf.gravity;
            }

            VMExecutor.GMSelf.gravity = gravity;
            return null;
        }

        [GMLFunction("action_set_hspeed", before: "2.0.0.0")]
        public static object? action_set_hspeed(object?[] args)
        {
            var hspeed = args[0].Conv<double>();

            if (Action_Relative)
            {
                hspeed += VMExecutor.GMSelf.hspeed;
            }

            VMExecutor.GMSelf.hspeed = hspeed;
            return null;
        }

        [GMLFunction("action_set_vspeed", before: "2.0.0.0")]
        public static object? action_set_vspeed(object?[] args)
        {
            var vspeed = args[0].Conv<double>();

            if (Action_Relative)
            {
                vspeed += VMExecutor.GMSelf.vspeed;
            }

            VMExecutor.GMSelf.vspeed = vspeed;
            return null;
        }

        [GMLFunction("action_move_to", before: "2.0.0.0")]
        public static object? action_move_to(object?[] args)
        {
            var x = args[0].Conv<double>();
            var y = args[1].Conv<double>();

            var self = VMExecutor.GMSelf;

            if (Action_Relative)
            {
                x += self.x;
                y += self.y;
            }

            self.x = x;
            self.y = y;
            return null;
        }

        [GMLFunction("action_create_object", before: "2.0.0.0")]
        public static object? action_create_object(object?[] args)
        {
            var obj = args[0].Conv<int>();
            var x = args[1].Conv<double>();
            var y = args[2].Conv<double>();

            var self = VMExecutor.GMSelf;

            if (Action_Relative)
            {
                x += self.x;
                y += self.y;
            }

            return ScriptResolver.instance_create([x, y, obj]);
        }
    }
}
