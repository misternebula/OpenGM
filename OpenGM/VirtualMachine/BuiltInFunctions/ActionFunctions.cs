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
            var speed = args[1].Conv<float>();

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

            if (Action_Relative)
            {
                speed = VMExecutor.Self.GMSelf.speed + speed;
            }

            VMExecutor.Self.GMSelf.speed = speed;

            int dir;
            do
            {
                dir = (int)GMRandom.YYRandom(9);
            } while (dirString[dir] != '1');

            switch (dir)
            {
                case 0:
                    VMExecutor.Self.GMSelf.direction = 255;
                    break;
                case 1:
                    VMExecutor.Self.GMSelf.direction = 270;
                    break;
                case 2:
                    VMExecutor.Self.GMSelf.direction = 315;
                    break;
                case 3:
                    VMExecutor.Self.GMSelf.direction = 180;
                    break;
                case 4:
                    VMExecutor.Self.GMSelf.direction = 0;
                    VMExecutor.Self.GMSelf.speed = 0;
                    break;
                case 5:
                    VMExecutor.Self.GMSelf.direction = 0;
                    break;
                case 6:
                    VMExecutor.Self.GMSelf.direction = 135;
                    break;
                case 7:
                    VMExecutor.Self.GMSelf.direction = 90;
                    break;
                case 8:
                    VMExecutor.Self.GMSelf.direction = 56;
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
                var curValue = VMExecutor.Self.GMSelf.alarm[index].Conv<int>();
                if (curValue > -1)
                {
                    VMExecutor.Self.GMSelf.alarm[index] = curValue + value;
                    return null;
                }
            }

            VMExecutor.Self.GMSelf.alarm[index] = value;
            return null;
        }

        [GMLFunction("action_set_friction", before: "2.0.0.0")]
        public static object? action_set_friction(object?[] args)
        {
            var friction = args[0].Conv<float>();

            if (Action_Relative)
            {
                friction += VMExecutor.Self.GMSelf.friction;
            }

            VMExecutor.Self.GMSelf.friction = friction;
            return null;
        }

        [GMLFunction("action_set_gravity", before: "2.0.0.0")]
        public static object? action_set_gravity(object?[] args)
        {
            var gravity = args[0].Conv<float>();

            if (Action_Relative)
            {
                gravity += VMExecutor.Self.GMSelf.gravity;
            }

            VMExecutor.Self.GMSelf.gravity = gravity;
            return null;
        }

        [GMLFunction("action_set_hspeed", before: "2.0.0.0")]
        public static object? action_set_hspeed(object?[] args)
        {
            var hspeed = args[0].Conv<float>();

            if (Action_Relative)
            {
                hspeed += VMExecutor.Self.GMSelf.hspeed;
            }

            VMExecutor.Self.GMSelf.hspeed = hspeed;
            return null;
        }

        [GMLFunction("action_set_vspeed", before: "2.0.0.0")]
        public static object? action_set_vspeed(object?[] args)
        {
            var vspeed = args[0].Conv<float>();

            if (Action_Relative)
            {
                vspeed += VMExecutor.Self.GMSelf.vspeed;
            }

            VMExecutor.Self.GMSelf.vspeed = vspeed;
            return null;
        }

        [GMLFunction("action_move_to", before: "2.0.0.0")]
        public static object? action_move_to(object?[] args)
        {
            var x = args[0].Conv<float>();
            var y = args[1].Conv<float>();

            if (Action_Relative)
            {
                x += VMExecutor.Self.GMSelf.x;
                y += VMExecutor.Self.GMSelf.y;
            }

            VMExecutor.Self.GMSelf.x = x;
            VMExecutor.Self.GMSelf.y = y;
            return null;
        }
    }
}
