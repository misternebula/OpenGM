using OpenGM.IO;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using OpenTK.Mathematics;
using UndertaleModLib.Models;

namespace OpenGM;

public class BBox
{
    /// <summary>
    /// X co-ordinate of the left-hand side of the bounding box.
    /// </summary>
    public double left;

    /// <summary>
    /// X co-ordinate of the right-hand side of the bounding box.
    /// </summary>
    public double right;

    /// <summary>
    /// Y co-ordinate of the top of the bounding box.
    /// </summary>
    public double top;

    /// <summary>
    /// Y co-ordinate of the top of the bounding box.
    /// </summary>
    public double bottom;
}

public static class CollisionManager
{
    public static bool CompatMode = false;
    
    public static BBox CalculateBoundingBox(GamemakerObject gm)
    {
        gm.bbox_dirty = false;
        // TODO : This is called a LOT. This needs to be as optimized as possible.

        var pos = new Vector2d(gm.x, gm.y);

        var index = gm.mask_index;
        if (index == -1)
        {
            index = gm.sprite_index;
        }

        if (index == -1)
        {
            // trying to generate a bounding box for an object with no sprites... uh oh!
            return new BBox() { left = gm.x, top = gm.y, right = gm.x, bottom = gm.y };
        }

        var addition = CompatMode ? 0 : 1;

        var origin = SpriteManager.GetSpriteOrigin(index);

        var left = pos.X + (gm.margins.X * gm.image_xscale) - (origin.X * gm.image_xscale);
        var top = pos.Y + (gm.margins.W * gm.image_yscale) - (origin.Y * gm.image_yscale);
        var right = pos.X + ((gm.margins.Y + addition) * gm.image_xscale) - (origin.X * gm.image_xscale);
        var bottom = pos.Y + ((gm.margins.Z + addition) * gm.image_yscale) - (origin.Y * gm.image_yscale);

        if (gm.image_xscale < 0)
        {
            (left, right) = (right, left);
        }

        if (gm.image_yscale < 0)
        {
            (top, bottom) = (bottom, top);
        }

        // Dont bother rotating if not needed
        if (CustomMath.ApproxEqual(gm.image_angle % 360, 0))
        {
            return new BBox()
            {
                left = left,
                top = top,
                right = right,
                bottom = bottom
            };
        }

        // co-ords of verts of unrotated bbox
        var topLeft = new Vector2d(left, top);
        var topRight = new Vector2d(right, top);
        var bottomRight = new Vector2d(right, bottom);
        var bottomLeft = new Vector2d(left, bottom);

        // rotate co-ords
        topLeft = topLeft.RotateAroundPoint(pos, gm.image_angle);
        topRight = topRight.RotateAroundPoint(pos, gm.image_angle);
        bottomRight = bottomRight.RotateAroundPoint(pos, gm.image_angle);
        bottomLeft = bottomLeft.RotateAroundPoint(pos, gm.image_angle);

        return new BBox
        {
            left = CustomMath.Min(topLeft.X, topRight.X, bottomRight.X, bottomLeft.X),
            right = CustomMath.Max(topLeft.X, topRight.X, bottomRight.X, bottomLeft.X),
            top = CustomMath.Min(topLeft.Y, topRight.Y, bottomRight.Y, bottomLeft.Y),
            bottom = CustomMath.Max(topLeft.Y, topRight.Y, bottomRight.Y, bottomLeft.Y)
        };
    }

    public static int Command_CollisionRectangle(GamemakerObject self, double x1, double y1, double x2, double y2, int obj, bool precise, bool notme)
    {
        bool IsValid(GamemakerObject? instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (notme && instance == self)
            {
                return false;
            }

            if (instance.Marked)
            {
                return false;
            }

            if (!instance.Active)
            {
                return false;
            }

            return Collision_Rectangle(instance, x1, y1, x2, y2, precise);
        }

        if (obj == GMConstants.all)
        {
            foreach (var instance in InstanceManager.instances.Values)
            {
                if (IsValid(instance))
                {
                    return instance.instanceId;
                }
            }
        }
        else if (obj < GMConstants.FIRST_INSTANCE_ID)
        {
            var instances = InstanceManager.FindByAssetId(obj);
            foreach (var instance in instances)
            {
                if (IsValid(instance))
                {
                    return instance.instanceId;
                }
            }
        }
        else
        {
            // instance id
            var instance = InstanceManager.FindByInstanceId(obj);

            if (IsValid(instance))
            {
                return instance!.instanceId;
            }
        }

        return GMConstants.noone;
    }

    public static int Command_CollisionPoint(GamemakerObject self, double x, double y, int obj, bool precise, bool notme)
    {
        bool IsValid(GamemakerObject? instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (notme && instance == self)
            {
                return false;
            }

            if (instance.Marked)
            {
                return false;
            }

            if (!instance.Active)
            {
                return false;
            }

            return Collision_Point(instance, x, y, precise);
        }

        if (obj == GMConstants.all)
        {
            foreach (var instance in InstanceManager.instances.Values)
            {
                if (IsValid(instance))
                {
                    return instance.instanceId;
                }
            }
        }
        else if (obj < GMConstants.FIRST_INSTANCE_ID)
        {
            var instances = InstanceManager.FindByAssetId(obj);
            foreach (var instance in instances)
            {
                if (IsValid(instance))
                {
                    return instance.instanceId;
                }
            }
        }
        else
        {
            // instance id
            var instance = InstanceManager.FindByInstanceId(obj);

            if (IsValid(instance))
            {
                return instance!.instanceId;
            }
        }

        return GMConstants.noone;
    }

    public static int Command_InstancePlace(GamemakerObject self, double x, double y, int obj)
    {
        var prevX = self.x;
        var prevY = self.y;
        self.x = x;
        self.y = y;

        bool IsValid(GamemakerObject? instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (instance.Marked)
            {
                return false;
            }

            if (!instance.Active)
            {
                return false;
            }

            return Collision_Instance(self, instance, true);
        }

        var returnValue = GMConstants.noone;

        if (obj == GMConstants.all)
        {
            foreach (var instance in InstanceManager.instances.Values)
            {
                if (IsValid(instance))
                {
                    returnValue = instance.instanceId;
                }
            }
        }
        else if (obj < GMConstants.FIRST_INSTANCE_ID)
        {
            var instances = InstanceManager.FindByAssetId(obj);
            foreach (var instance in instances)
            {
                if (IsValid(instance))
                {
                    returnValue = instance.instanceId;
                }
            }
        }
        else
        {
            // instance id
            var instance = InstanceManager.FindByInstanceId(obj);

            if (IsValid(instance))
            {
                returnValue = instance!.instanceId;
            }
        }

        self.x = prevX;
        self.y = prevY;
        
        return returnValue;
    }

    public static int Command_CollisionLine(GamemakerObject self, double x1, double y1, double x2, double y2, int obj, bool precise, bool notme)
    {
        bool IsValid(GamemakerObject? instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (notme && instance == self)
            {
                return false;
            }

            if (instance.Marked)
            {
                return false;
            }

            if (!instance.Active)
            {
                return false;
            }

            return Collision_Line(instance, x1, y1, x2, y2, precise);
        }

        if (obj == GMConstants.all)
        {
            foreach (var instance in InstanceManager.instances.Values)
            {
                if (IsValid(instance))
                {
                    return instance.instanceId;
                }
            }
        }
        else if (obj < GMConstants.FIRST_INSTANCE_ID)
        {
            var instances = InstanceManager.FindByAssetId(obj);
            foreach (var instance in instances)
            {
                if (IsValid(instance))
                {
                    return instance.instanceId;
                }
            }
        }
        else
        {
            // instance id
            var instance = InstanceManager.FindByInstanceId(obj);

            if (IsValid(instance))
            {
                return instance!.instanceId;
            }
        }

        return GMConstants.noone;
    }

    public static bool Collision_Rectangle(GamemakerObject self, double x1, double y1, double x2, double y2, bool precise)
    {
        if (self.bbox_dirty)
        {
            self.bbox = CalculateBoundingBox(self);
        }

        var addition = CompatMode ? 1d : 0d;

        var bl = CustomMath.Min(x1, x2);
        var br = CustomMath.Max(x1, x2);
        var bt = CustomMath.Min(y1, y2);
        var bb = CustomMath.Max(y1, y2);

        if (bl >= self.bbox_right + addition)
        {
            return false;
        }

        if (br < self.bbox_left)
        {
            return false;
        }

        if (bt >= self.bbox_bottom + addition)
        {
            return false;
        }

        if (bb < self.bbox_top)
        {
            return false;
        }

        var index = self.mask_index;
        if (index < 0)
        {
            index = self.sprite_index;
        }

        var spriteData = SpriteManager.GetSpriteAsset(index);

        if (spriteData == null || spriteData.Textures.Count == 0)
        {
            return false;
        }

        if (spriteData.SepMasks == UndertaleSprite.SepMaskType.RotatedRect)
        {
            var collided = SeparatingAxisCollisionBox(self, x1, y1, x2, y2);
            return collided;
        }

        if (precise && spriteData.SepMasks == UndertaleSprite.SepMaskType.Precise)
        {
            return PreciseCollisionRectangle(spriteData, CustomMath.FloorToInt(self.image_index), self.bbox, self.x, self.y, self.image_xscale, self.image_yscale, self.image_angle, new BBox()
            {
                left = CustomMath.Min(x1, x2),
                top = CustomMath.Min(y1, y2),
                right = CustomMath.Max(x1, x2),
                bottom = CustomMath.Max(y1, y2)
            });
        }

        if (!CompatMode)
        {
            var l = CustomMath.Max(bl, self.bbox_left);
            var t = CustomMath.Max(bt, self.bbox_top);
            var r = CustomMath.Min(br, self.bbox_right);
            var b = CustomMath.Min(bb, self.bbox_bottom);

            if (CustomMath.FloorToInt(l + 0.5) == CustomMath.FloorToInt(r + 0.5))
            {
                return false;
            }

            if (CustomMath.FloorToInt(t + 0.5) == CustomMath.FloorToInt(b + 0.5))
            {
                return false;
            }
        }

        return true;
    }

    public static bool Collision_Point(GamemakerObject self, double x, double y, bool precise)
    {
        if (self.bbox_dirty)
        {
            self.bbox = CalculateBoundingBox(self);
        }

        var addition = CompatMode ? 1d : -1e-05;

        if (x >= self.bbox_right + addition
            || x < self.bbox_left
            || y >= self.bbox_bottom + addition
            || y < self.bbox_top) // TODO : there's 2 instflags checks here too
        {
            return false; 
        }

        var index = self.mask_index;
        if (index < 0)
        {
            index = self.sprite_index;
        }

        var spriteData = SpriteManager.GetSpriteAsset(index);

        if (spriteData == null || spriteData.Textures.Count == 0)
        {
            return false;
        }

        if (spriteData.SepMasks == UndertaleSprite.SepMaskType.RotatedRect)
        {
            return SeparatingAxisCollisionPoint(self, x, y);
        }

        if (precise && spriteData.SepMasks == UndertaleSprite.SepMaskType.Precise)
        {
            // TODO : x/y values should be rounded. work out EXACTLY how they're rounded!
            return PreciseCollisionPoint(spriteData, CustomMath.FloorToInt(self.image_index), self.x, self.y, self.image_xscale, self.image_yscale, self.image_angle, x, y);
        }

        // AABB - We already know they intersect!
        return true;
    }

    public static bool Collision_Instance(GamemakerObject self, GamemakerObject other, bool precise)
    {
        if (self == other)
        {
            return false;
        }

        if (self.bbox_dirty)
        {
            self.bbox = CalculateBoundingBox(self);
        }

        if (other.bbox_dirty)
        {
            other.bbox = CalculateBoundingBox(self);
        }

        // TODO: comment said that this should be the other way around, but it
        // only works properly in PT like this
        var addition = CompatMode ? 1f : 0f;

        if (self.bbox_left >= (other.bbox_right + addition)
            || (self.bbox_right + addition) <= other.bbox_left
            || self.bbox_top >= (other.bbox_bottom + addition)
            || (self.bbox_bottom + addition) <= other.bbox_top)
        {
            // Bounding boxes don't even intersect.
            return false;
        }
         
        var index = self.mask_index;
        if (index < 0)
        {
            index = self.sprite_index;
        }

        var selfSprite = SpriteManager.GetSpriteAsset(index);

        if (selfSprite == null || selfSprite.Textures.Count == 0)
        {
            return false;
        }

        index = other.mask_index;
        if (index < 0)
        {
            index = other.sprite_index;
        }

        var otherSprite = SpriteManager.GetSpriteAsset(index);

        if (otherSprite == null || otherSprite.Textures.Count == 0)
        {
            return false;
        }

        if (selfSprite.SepMasks == UndertaleSprite.SepMaskType.RotatedRect || otherSprite.SepMasks == UndertaleSprite.SepMaskType.RotatedRect)
        {
            return SeparatingAxisCollision(self, other);
        }

        if (precise && (selfSprite.SepMasks == UndertaleSprite.SepMaskType.Precise || otherSprite.SepMasks == UndertaleSprite.SepMaskType.Precise))
        {
            return PreciseCollision(
                selfSprite, CustomMath.FloorToInt(self.image_index), self.bbox, self.x, self.y, self.image_xscale, self.image_yscale, self.image_angle,
                otherSprite, CustomMath.FloorToInt(other.image_index), other.bbox, other.x, other.y, other.image_xscale, other.image_yscale, other.image_angle);
        }

        if (!CompatMode)
        {
            var l = CustomMath.Max(self.bbox_left, other.bbox_left);
            var t = CustomMath.Max(self.bbox_top, other.bbox_top);
            var r = CustomMath.Min(self.bbox_right, other.bbox_right);
            var b = CustomMath.Min(self.bbox_bottom, other.bbox_bottom);

            if (CustomMath.FloorToInt(l + 0.5) == CustomMath.FloorToInt(r + 0.5))
            {
                return false;
            }

            if (CustomMath.FloorToInt(t + 0.5) == CustomMath.FloorToInt(b + 0.5))
            {
                return false;
            }
        }

        // AABB - We already know they intersect!
        return true;
    }

    public static bool Collision_Line(GamemakerObject self, double x1, double x2, double y1, double y2, bool precise)
    {
        if (self.Marked)
        {
            return false;
        }

        if (self.bbox_dirty)
        {
            self.bbox = CalculateBoundingBox(self);
        }

        var i_bbox = self.bbox;
        if (CustomMath.Min(x1, x2) >= i_bbox.right + 1)
        {
            return false;
        }

        if (CustomMath.Max(x1, x2) < i_bbox.left)
        {
            return false;
        }

        if (CustomMath.Min(y1, y2) >= i_bbox.bottom + 1)
        {
            return false;
        }

        if (CustomMath.Max(y1, y2) < i_bbox.top)
        {
            return false;
        }

        if (x2 < x1)
        {
            (x1, x2) = (x2, x1);
            (y2, y1) = (y1, y2);
        }

        if (x1 < i_bbox.left)
        {
            y1 += (i_bbox.left - x1) * (y2 - y1) / (x2 - x1);
            x1 = i_bbox.left;
        }

        if (x2 > (i_bbox.right + 1))
        {
            y2 += (i_bbox.right + 1 - x2) * (y2 - y1) / (x2 - x1);
            x2 = i_bbox.right + 1;
        }

        if ((y1 < i_bbox.top) && (y2 < i_bbox.top))
        {
            return false;
        }

        if ((y1 >= i_bbox.bottom + 1) && (y2 >= i_bbox.bottom + 1))
        {
            return false;
        }

        var index = self.mask_index;
        if (index < 0)
        {
            index = self.sprite_index;
        }

        var spriteData = SpriteManager.GetSpriteAsset(index);

        if (spriteData == null || spriteData.Textures.Count == 0)
        {
            return false;
        }

        if (spriteData.SepMasks == UndertaleSprite.SepMaskType.RotatedRect)
        {
            return SeparatingAxisCollisionLine(self, x1, y1, x2, y2);
        }

        if (precise && spriteData.SepMasks == UndertaleSprite.SepMaskType.Precise)
        {
            throw new NotImplementedException();
        }

        return true;
    }

    public static int Command_CollisionCircle(GamemakerObject self, double x, double y, double radius, int obj, bool precise, bool notme)
    {
        return Command_CollisionEllipse(self, x - radius, y - radius, x + radius, y + radius, obj, precise, notme);
    }

    public static int Command_CollisionEllipse(GamemakerObject self, double x1, double y1, double x2, double y2, int obj, bool precise, bool notme)
    {
        bool IsValid(GamemakerObject? instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (notme && instance == self)
            {
                return false;
            }

            if (instance.Marked)
            {
                return false;
            }

            if (!instance.Active)
            {
                return false;
            }

            return Collision_Ellipse(instance, x1, y1, x2, y2, precise);
        }

        if (obj == GMConstants.all)
        {
            foreach (var instance in InstanceManager.instances.Values)
            {
                if (IsValid(instance))
                {
                    return instance.instanceId;
                }
            }
        }
        else if (obj < GMConstants.FIRST_INSTANCE_ID)
        {
            var instances = InstanceManager.FindByAssetId(obj);
            foreach (var instance in instances)
            {
                if (IsValid(instance))
                {
                    return instance.instanceId;
                }
            }
        }
        else
        {
            // instance id
            var instance = InstanceManager.FindByInstanceId(obj);

            if (IsValid(instance))
            {
                return instance!.instanceId;
            }
        }

        return GMConstants.noone;
    }

    public static bool Collision_Ellipse(GamemakerObject self, double x1, double x2, double y1, double y2, bool precise)
    {
        // https://github.com/YoYoGames/GameMaker-HTML5/blob/7e96ef96d44629fc28618d81626f0cf1eaf61ede/scripts/yyInstance.js#L1940

        if (self.Marked)
        {
            return false;
        }

        if (self.bbox_dirty)
        {
            self.bbox = CalculateBoundingBox(self);
        }

        // TODO : round values - how are they rounded?
        var _x1 = (int)x1;
        var _y1 = (int)y1;
        var _x2 = (int)x2;
        var _y2 = (int)y2;

        int min_x1x2;
        int max_x1x2;
        int min_y1y2;
        int max_y1y2;

        if (_x1 < _x2)
        {
            min_x1x2 = _x1;
            max_x1x2 = _x2;
        }
        else
        {
            min_x1x2 = _x2;
            max_x1x2 = _x1;
        }
        if (_y1 < _y2)
        {
            min_y1y2 = _y1;
            max_y1y2 = _y2;
        }
        else
        {
            min_y1y2 = _y2;
            max_y1y2 = _y1;
        }

        // easy cases first
        var bbox = self.bbox;
        if (min_x1x2 >= bbox.right)
            return false;
        if (max_x1x2 < bbox.left)
            return false;
        if (min_y1y2 >= bbox.bottom)
            return false;
        if (max_y1y2 < bbox.top)
            return false;

        // check whether single line
        if ((_x1 == _x2) || (_y1 == _y2))
        {
            return Collision_Rectangle(self, x1, y1, x2, y2, precise);
        }

        // now see whether the ellipse intersect the bounding box
        var cx = (_x1 + _x2) * 0.5;
        var cy = (_y1 + _y2) * 0.5;
        if (!(bbox.left <= cx && bbox.right >= cx) && !(bbox.top <= cy && bbox.bottom >= cy))
        {
            var px = (bbox.right <= cx) ? bbox.right : bbox.left;
            var py = (bbox.bottom <= cy) ? bbox.bottom : bbox.top;
            if (!PtInEllipse(_x1, _y1, _x2, _y2, px, py))
                return false;
        }

        var index = self.mask_index;
        if (index < 0)
        {
            index = self.sprite_index;
        }

        var spriteData = SpriteManager.GetSpriteAsset(index);

        if (spriteData == null || spriteData.Textures.Count == 0)
        {
            return false;
        }

        if (spriteData.SepMasks == UndertaleSprite.SepMaskType.RotatedRect)
        {
            if (!SeparatingAxisCollisionEllipse(self, _x1, _y1, _x2, _y2))
                return false;
        }

        if ((!precise) || spriteData.SepMasks == UndertaleSprite.SepMaskType.AxisAlignedRect)
            return true;

        var g_rr = new BBox
        {
            left = min_x1x2,
            top = min_y1y2,
            right = max_x1x2,
            bottom = max_y1y2
        };

        // TODO : x and y need to be Rounded -- how?
        return PreciseCollisionEllipse(spriteData, (int)Math.Floor(self.image_index), bbox, (int)self.x, (int)self.y, self.image_xscale, self.image_yscale, self.image_angle, g_rr);
    }

    public static bool PtInEllipse(double _x1, double _y1, double _x2, double _y2, double _px, double _py)
    {
        //find ellipse centre, x&y radius
        var mx = (_x1 + _x2) * 0.5;
        var my = (_y1 + _y2) * 0.5;
        var ww = (_x2 - _x1) * 0.5;
        var hh = (_y2 - _y1) * 0.5;

        var a = (_px - mx) / ww;
        var b = (_py - my) / hh;
        return ((a * a) + (b * b) <= 1) ? true : false;
    }

    #region Precise Mask Checks

    public static bool PreciseCollision(
        SpriteData sprite1, int _img1, BBox _bb1, double _x1, double _y1, double _scale1x, double _scale1y, double _angle1,
        SpriteData sprite2, int _img2, BBox _bb2, double _x2, double _y2, double _scale2x, double _scale2y, double _angle2)
    {
        if (sprite1 == null || sprite2 == null)
        {
            return false;
        }

        if (sprite1.Textures.Count <= 0)
            return false;
        if (sprite2.Textures.Count <= 0)
            return false;

        if (sprite1.CollisionMasks.Count > 0)
            _img1 = _img1 % sprite1.CollisionMasks.Count;
        if (_img1 < 0)
        {
            _img1 = _img1 + sprite1.CollisionMasks.Count;
        }

        if (sprite2.CollisionMasks.Count > 0)
            _img2 = _img2 % sprite2.CollisionMasks.Count;

        if (_img2 < 0)
        {
            _img2 = _img2 + sprite2.CollisionMasks.Count;
        }

        _scale1x = 1.0 / _scale1x;
        _scale1y = 1.0 / _scale1y;
        _scale2x = 1.0 / _scale2x;
        _scale2y = 1.0 / _scale2y;

        var l = CustomMath.Max(_bb1.left, _bb2.left);

        l = Math.Floor(l) + 0.5;

        var r = CustomMath.Min(_bb1.right, _bb2.right);
        var t = CustomMath.Max(_bb1.top, _bb2.top);

        t = Math.Floor(t) + 0.5;
        var b = CustomMath.Min(_bb1.bottom, _bb2.bottom);


        var leftedge = sprite1.MarginLeft;
        var rightedge = sprite1.MarginRight + 1.0;
        var topedge = sprite1.MarginTop;
        var bottomedge = sprite1.MarginBottom + 1.0;

        if (sprite1.SepMasks == UndertaleSprite.SepMaskType.Precise)
        {
            if (leftedge < 0)
                leftedge = 0;
            if (rightedge > sprite1.Width)
                rightedge = sprite1.Width;

            if (topedge < 0)
                topedge = 0;
            if (bottomedge > sprite1.Height)
                bottomedge = sprite1.Height;
        }

        var sleftedge = sprite2.MarginLeft;
        var srightedge = sprite2.MarginRight + 1.0;
        var stopedge = sprite2.MarginTop;
        var sbottomedge = sprite2.MarginBottom + 1.0;

        if (sprite2.SepMasks == UndertaleSprite.SepMaskType.Precise)
        {
            if (sleftedge < 0)
                sleftedge = 0;
            if (srightedge > sprite2.Width)
                srightedge = sprite2.Width;

            if (stopedge < 0)
                stopedge = 0;
            if (sbottomedge > sprite2.Height)
                sbottomedge = sprite2.Height;
        }

        var hasrot1 = false;
        var hasrot2 = false;

        if (_angle1 > CustomMath.Epsilon || _angle1 < -CustomMath.Epsilon)
            hasrot1 = true;
        if (_angle2 > CustomMath.Epsilon || _angle2 < -CustomMath.Epsilon)
            hasrot2 = true;

        if (!hasrot1 && !hasrot2)
        {
            var du1 = _scale1x;
            var du2 = _scale2x;
            var u1 = ((l - _x1) * _scale1x + sprite1.OriginX);
            var u2 = ((l - _x2) * _scale2x + sprite2.OriginX);
            for (var i = l; i < r; i += 1.0, u1 += du1, u2 += du2)
            {
                if ((u1 < leftedge) || (u1 >= rightedge))
                    continue;

                if ((u2 < sleftedge) || (u2 >= srightedge))
                    continue;

                var u1i = CustomMath.FloorToInt(u1);
                var u2i = CustomMath.FloorToInt(u2);

                for (var j = t; j < b; j += 1.0)
                {
                    if (sprite1.SepMasks == UndertaleSprite.SepMaskType.Precise)
                    {
                        var v1 = ((j - _y1) * _scale1y + sprite1.OriginY);

                        if ((v1 < topedge) || (v1 >= bottomedge))
                            continue;

                        //if (sprite1.maskcreated)
                            if (!ColMaskSet(sprite1, u1i, CustomMath.FloorToInt(v1), sprite1.CollisionMasks[_img1]))
                                continue;
                    }

                    if (sprite2.SepMasks == UndertaleSprite.SepMaskType.Precise)
                    {
                        var v2 = ((j - _y2) * _scale2y + sprite2.OriginY);


                        if ((v2 < stopedge) || (v2 >= sbottomedge))
                            continue;
                        //if (sprite2.maskcreated)
                        //{
                            if (!ColMaskSet(sprite2, u2i, CustomMath.FloorToInt(v2), sprite2.CollisionMasks[_img2]))
                                continue;
                        //}
                    }

                    return true;
                }
            }
        }
        else
        {
            var ss1 = 0d;
            var cc1 = 0d;
            var ss2 = 0d;
            var cc2 = 0d;
            var u1 = 0d;
            var u2 = 0d;
            var v1 = 0d;
            var v2 = 0d;

            if (hasrot1)
            {
                ss1 = Math.Sin(-_angle1 * Math.PI / 180.0);
                cc1 = Math.Cos(-_angle1 * Math.PI / 180.0);
            }

            if (hasrot2)
            {
                ss2 = Math.Sin(-_angle2 * Math.PI / 180.0);
                cc2 = Math.Cos(-_angle2 * Math.PI / 180.0);
            }

            for (var i = l; i < r; i += 1.0)
            {
                if (!hasrot1)
                {
                    u1 = ((i - _x1) * _scale1x + sprite1.OriginX);
                    if ((u1 < leftedge) || (u1 >= rightedge))
                        continue;
                }

                if (!hasrot2)
                {
                    u2 = ((i - _x2) * _scale2x + sprite2.OriginX);
                    if ((u2 < sleftedge) || (u2 >= srightedge))
                        continue;
                }

                for (var j = t; j < b; j += 1.0)
                {
                    if (hasrot1)
                    {
                        u1 = ((cc1 * (i - _x1) + ss1 * (j - _y1)) * _scale1x + sprite1.OriginX);
                        if ((u1 < leftedge) || (u1 >= rightedge))
                            continue;
                        v1 = ((cc1 * (j - _y1) - ss1 * (i - _x1)) * _scale1y + sprite1.OriginY);
                    }
                    else
                    {
                        v1 = ((j - _y1) * _scale1y + sprite1.OriginY);
                    }

                    if ((v1 < topedge) || (v1 >= bottomedge))
                        continue;
                    if (sprite1.SepMasks == UndertaleSprite.SepMaskType.Precise)
                    {
                        //if (sprite1.maskcreated)
                        //{
                            if (!ColMaskSet(sprite1, CustomMath.FloorToInt(u1), CustomMath.FloorToInt(v1), sprite1.CollisionMasks[_img1]))
                                continue;
                        //}
                    }

                    if (hasrot2)
                    {
                        u2 = ((cc2 * (i - _x2) + ss2 * (j - _y2)) * _scale2x + sprite2.OriginX);
                        if ((u2 < sleftedge) || (u2 >= srightedge))
                            continue;
                        v2 = ((cc2 * (j - _y2) - ss2 * (i - _x2)) * _scale2y + sprite2.OriginY);
                    }
                    else
                    {
                        v2 = ((j - _y2) * _scale2y + sprite2.OriginY);
                    }

                    if ((v2 < stopedge) || (v2 >= sbottomedge))
                        continue;
                    if (sprite2.SepMasks == UndertaleSprite.SepMaskType.Precise)
                    {
                        //if (sprite2.maskcreated)
                        //{
                            if (!ColMaskSet(sprite2, CustomMath.FloorToInt(u2), CustomMath.FloorToInt(v2), sprite2.CollisionMasks[_img2]))
                                continue;
                        //}
                    }
                    return true;
                }
            }
        }

        return false;
    }

    public static bool PreciseCollisionPoint(SpriteData sprite, int imageIndex, double x, double y, double xscale, double yscale, double angle, double _x, double _y)
    {
        if (sprite.Textures.Count == 0)
        {
            return false;
        }

        imageIndex %= sprite.CollisionMasks.Count;
        if (imageIndex < 0)
        {
            imageIndex += sprite.CollisionMasks.Count;
        }

        x -= 0.5;
        y -= 0.5;

        int xx;
        int yy;

        if (Math.Abs(angle) < 0.0001)
        {
            xx = CustomMath.FloorToInt((_x - x) / xscale + sprite.OriginX);
            yy = CustomMath.FloorToInt((_y - y) / yscale + sprite.OriginY);
        }
        else
        {
            var ss = Math.Sin(-angle * Math.PI / 180.0);
            var cc = Math.Cos(-angle * Math.PI / 180.0);
            xx = CustomMath.FloorToInt((cc * (_x - x) + ss * (_y - y)) / xscale + sprite.OriginX);
            yy = CustomMath.FloorToInt((cc * (_y - y) - ss * (_x - x)) / yscale + sprite.OriginY);
        }

        return ColMaskSet(sprite, xx, yy, sprite.CollisionMasks[imageIndex]);
    }

    public static bool PreciseCollisionRectangle(SpriteData sprite, int imageIndex, BBox bbox, double x, double y, double xscale, double yscale, double angle, BBox rr)
    {
        imageIndex %= sprite.CollisionMasks.Count;
        if (imageIndex < 0)
        {
            imageIndex += sprite.CollisionMasks.Count;
        }

        var l = CustomMath.Max(bbox.left, rr.left);
        var r = CustomMath.Min(bbox.right, rr.right);
        var t = CustomMath.Max(bbox.top, rr.top);
        var b = CustomMath.Min(bbox.bottom, rr.bottom);

        x -= 0.5;
        y -= 0.5;

        if ((xscale == 1) && (yscale == 1) && (Math.Abs(angle) < 0.0001))
        {
            for (var i = l; i <= r; i++)
            {
                for (var j = t; j <= b; j++)
                {
                    var xx = CustomMath.FloorToInt(i - x + sprite.OriginX);
                    var yy = CustomMath.FloorToInt(j - y + sprite.OriginY);
                    if ((xx < 0) || (xx >= sprite.Width))
                        continue;
                    if ((yy < 0) || (yy >= sprite.Height))
                        continue;
                    if (ColMaskSet(sprite, xx, yy, sprite.CollisionMasks[imageIndex]))
                        return true;
                }
            }
        }
        else
        {
            var ss = Math.Sin(-angle * Math.PI / 180.0);
            var cc = Math.Cos(-angle * Math.PI / 180.0);
            var onescalex = 1.0 / xscale;
            var onescaley = 1.0 / yscale;
            for (var i = l; i <= r; i++)
            {
                for (var j = t; j <= b; j++)
                {
                    var xx = CustomMath.FloorToInt((cc * (i - x) + ss * (j - y)) * onescalex + sprite.OriginX);
                    var yy = CustomMath.FloorToInt((cc * (j - y) - ss * (i - x)) * onescaley + sprite.OriginY);
                    if ((xx < 0) || (xx >= sprite.Width))
                        continue;
                    if ((yy < 0) || (yy >= sprite.Height))
                        continue;
                    if (ColMaskSet(sprite, xx, yy, sprite.CollisionMasks[imageIndex]))
                        return true;
                }
            }
        }

        return false;
    }

    public static bool PreciseCollisionEllipse(SpriteData sprite, int _img1, BBox _bb1, int _x1, int _y1, double _scalex, double _scaley, double _angle, BBox _rr)
    {
        if (sprite.CollisionMasks.Count == 0)
        {
            return false;
        }

        _img1 %= sprite.CollisionMasks.Count;
        if (_img1 < 0)
        {
            _img1 += sprite.CollisionMasks.Count;
        }

        // Compute overlapping bounding box
        var l = CustomMath.Max(_bb1.left, _rr.left);
        var r = CustomMath.Min(_bb1.right, _rr.right);
        var t = CustomMath.Max(_bb1.top, _rr.top);
        var b = CustomMath.Min(_bb1.bottom, _rr.bottom);

        var mx = ((_rr.right + _rr.left) / 2);
        var my = ((_rr.bottom + _rr.top) / 2);
        var ww = 1.0 / ((_rr.right - _rr.left) / 2);
        var hh = 1.0 / ((_rr.bottom - _rr.top) / 2);

        if ((_scalex == 1) && (_scaley == 1) && (Math.Abs(_angle) < 0.0001))
        {
            // Case without scaling
            for (var i = (int)l; i <= r; i++)
            {
                var tmp = (i - mx) * ww;
                var sqrxx = tmp * tmp;//Sqr((i - mx) * ww);
                var xx = i - _x1 + sprite.OriginX;
                if ((xx < 0) || (xx >= sprite.Width))
                    continue;

                for (var j = (int)t; j <= b; j++)
                {
                    tmp = (j - my) * hh;
                    //if (sqrxx + Sqr((j - my) * hh) > 1) continue;   // outside ellipse
                    if (sqrxx + (tmp * tmp) > 1)
                        continue;   // outside ellipse

                    var yy = j - _y1 + sprite.OriginY;
                    if ((yy < 0) || (yy >= sprite.Height))
                        continue;

                    if (ColMaskSet(sprite, xx, yy, sprite.CollisionMasks[_img1]))
                        return true;

                }
            }
        }
        else
        {
            // Case with scaling
            var ss = Math.Sin(-_angle * Math.PI / 180.0);
            var cc = Math.Cos(-_angle * Math.PI / 180.0);
            var onescalex = 1.0 / _scalex;
            var onescaley = 1.0 / _scaley;

            for (var i = (int)l; i <= r; i++)
            {
                // common loop terms.
                var ix1 = (i - _x1);
                var cc_i_x1 = cc * ix1;
                var ss_i_x1 = ss * ix1;
                var tmp = (i - mx) * ww;
                var sq1 = tmp * tmp;//Sqr((i - mx) * ww);

                for (var j = t; j <= b; j++)    
                {
                    var jmy = (j - my) * hh;
                    if ((sq1 + (jmy * jmy)) > 1)
                        continue;   // outside ellipse

                    var j_y1 = j - _y1;
                    var xx = CustomMath.DoubleTilde(((cc_i_x1 + ss * j_y1) * onescalex) + sprite.OriginX);
                    if ((xx < 0) || (xx >= sprite.Width))
                        continue;

                    var yy = CustomMath.DoubleTilde(((cc * j_y1 - ss_i_x1) * onescaley) + sprite.OriginY);
                    if ((yy < 0) || (yy >= sprite.Height))
                        continue;


                    if (ColMaskSet(sprite, xx, yy, sprite.CollisionMasks[_img1]))
                        return true;
                }
            }
        }

        return false;
    }

    public static bool ColMaskSet(SpriteData sprite, int u, int v, byte[] pMaskBase)
    {
        // TODO : this changed in 2024.6. commenting out code to make it work how it used to do

        //if ((u < sprite.MarginLeft) || (u > sprite.MarginRight))
        //    return false;
        //if ((v < sprite.MarginTop) || (v > sprite.MarginBottom))
        //    return false;

        //u -= sprite.MarginLeft;
        //v -= sprite.MarginTop;

        //var bwidth = sprite.MarginRight - sprite.MarginLeft + 1;
        var bwidth = sprite.Width;
        var mwidth = (bwidth + 7) >> 3;
        var ouroff = u >> 3;

        if (v * mwidth + ouroff >= pMaskBase.Length)
        {
            DebugLog.LogWarning($"Index out of bounds - {sprite.Name} u:{u} v:{v} margins:{sprite.Margins} pMaskBase length:{pMaskBase.Length}");
            return false;
        }

        var mask = pMaskBase[v * mwidth + ouroff];

        var ourbit = 7 - (u & 7);

        if ((mask & (1 << ourbit)) != 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    #endregion

    #region Separating Axis Collision Tests

    public static bool SeparatingAxisCollision(GamemakerObject self, GamemakerObject other)
    {
        var p1 = getPoints(self);
        var p2 = getPoints(other);

        return sa_checkCollision(p1, p2);
    }

    public static bool SeparatingAxisCollisionPoint(GamemakerObject self, double x, double y)
    {
        var points = getPoints(self);
        var point = new Vector2d(x, y);
        return sa_checkCollisionPoint(points, point);
    }

    public static bool SeparatingAxisCollisionBox(GamemakerObject self, double x1, double y1, double x2, double y2)
    {
        var p1 = getPoints(self);
        var p2 = new Vector2d[4]
        {
            new(x1, y1),
            new(x2, y1),
            new(x1, y2),
            new(x2, y2)
        };

        return sa_checkCollision(p1, p2);
    }

    public static bool SeparatingAxisCollisionLine(GamemakerObject self, double x1, double y1, double x2, double y2)
    {
        var p1 = getPoints(self);
        var p2 = getPointsLine(x1, y1, x2, y2);

        return sa_checkCollisionLine(p1, p2);
    }

    public static bool SeparatingAxisCollisionEllipse(GamemakerObject self, double x1, double y1, double x2, double y2)
    {
        var p1 = getPoints(self);
        //var pcentre = { "x": (_x1 + _x2) * 0.5, "y": (_y1 + _y2) * 0.5 };
        var pcentre = new Vector2d((x1 + x2) * 0.5, (y1 + y2) * 0.5);
        var rx = Math.Abs(x1 - x2) * 0.5;
        var ry = Math.Abs(y1 - y2) * 0.5;
        return sa_checkCollisionEllipse(p1, pcentre, rx, ry);
    }

    public static Vector2d[] getPoints(GamemakerObject self)
    {
        var spriteIndex = (self.mask_index >= 0)
            ? self.mask_index 
            : self.sprite_index;
        var spriteData = SpriteManager.GetSpriteAsset(spriteIndex);

        if (spriteData == null)
        {
            throw new NotImplementedException();
        }

        // base on sprite bbox, and these can't change, so will always be in order, left<right and top<bottom
        var xmin = self.image_xscale * (spriteData.MarginLeft - spriteData.OriginX);
        var xmax = self.image_xscale * (spriteData.MarginRight - spriteData.OriginX + 1);

        var ymin = self.image_yscale * (spriteData.MarginTop - spriteData.OriginY);
        var ymax = self.image_yscale * (spriteData.MarginBottom - spriteData.OriginY + 1);

        var cc = Math.Cos(-self.image_angle * Math.PI / 180.0);
        var ss = Math.Sin(-self.image_angle * Math.PI / 180.0);

        // factor out "common" calculations...
        var cc_xmax = cc * xmax;
        var cc_xmin = cc * xmin;
        var ss_ymax = ss * ymax;
        var ss_ymin = ss * ymin;
        var cc_ymax = cc * ymax;
        var cc_ymin = cc * ymin;
        var ss_xmax = ss * xmax;
        var ss_xmin = ss * xmin;

        var rv = new Vector2d[4];
        var ix = self.x - 0.5;
        var iy = self.y - 0.5;

        rv[0] = new Vector2d(ix + cc_xmin - ss_ymin, iy + cc_ymin + ss_xmin);
        rv[1] = new Vector2d(ix + cc_xmax - ss_ymin, iy + cc_ymin + ss_xmax);
        rv[2] = new Vector2d(ix + cc_xmax - ss_ymax, iy + cc_ymax + ss_xmax);
        rv[3] = new Vector2d(ix + cc_xmin - ss_ymax, iy + cc_ymax + ss_xmin);

        return rv;
    }

    public static Vector2d[] getPointsLine(double x1, double y1, double x2, double y2)
    {
        var ret = new Vector2d[2];
        ret[0] = new Vector2d(x1, y1);
        ret[1] = new Vector2d(x2, y2);

        return ret;
    }

    public static bool sa_checkCollision(Vector2d[] p1, Vector2d[] p2)
    {
        var p1Axes = sa_getAxes(p1);
        var p2Axes = sa_getAxes(p2);

        for (var i = 0; i < 2; ++i)
        {
            var p1Proj = sa_getProjection(p1, p1Axes[i]);
            var p2Proj = sa_getProjection(p2, p1Axes[i]);

            var gap_present = ((p1Proj.max <= p2Proj.min) || (p2Proj.max <= p1Proj.min));

            if (gap_present)
                return false;
        }

        for (var i = 0; i < 2; ++i)
        {
            var p1Proj = sa_getProjection(p1, p2Axes[i]);
            var p2Proj = sa_getProjection(p2, p2Axes[i]);

            var gap_present = ((p1Proj.max <= p2Proj.min) || (p2Proj.max <= p1Proj.min));

            if (gap_present)
                return false;
        }

        return true;
    }

    public static bool sa_checkCollisionPoint(Vector2d[] p1, Vector2d p2)
    {
        var p1Axes = sa_getAxes(p1);

        for (var i = 0; i < 2; ++i)
        {
            var (min, max) = sa_getProjection(p1, p1Axes[i]);
            var p2Proj = p2.X * p1Axes[i].X + p2.Y * p1Axes[i].Y;

            var gap_present = ((max <= p2Proj) || (p2Proj <= min));

            if (gap_present)
                return false;
        }

        return true;
    }

    public static bool sa_checkCollisionLine(Vector2d[] p1, Vector2d[] p2)
    {
        var p1Axes = sa_getAxes(p1);
        var p2Axis = sa_getAxesLine(p2);

        for (var i = 0; i < 2; ++i)
        {
            var p1Proj = sa_getProjection(p1, p1Axes[i]);
            var p2Proj = sa_getProjectionLine(p2, p1Axes[i]);

            var gap_present = ((p1Proj.max <= p2Proj.min) || (p2Proj.max <= p1Proj.min));

            if (gap_present)
                return false;
        }

        {
            var p1Proj = sa_getProjection(p1, p2Axis);
            var p2Proj = sa_getProjectionLine(p2, p2Axis);

            var gap_present = ((p1Proj.max <= p2Proj.min) || (p2Proj.max <= p1Proj.min));

            if (gap_present)
                return false;
        }

        return true;
    }

    public static bool sa_checkCollisionEllipse(Vector2d[] p1, Vector2d pcentre, double rx, double ry)
    {
        //apply x scale transform to circle with radius ry
        var sx = Math.Abs(ry / rx);
        for (var i = 0; i < 4; ++i)
            p1[i].X *= sx;
        pcentre.X *= sx;
        var r = Math.Abs(ry);

        var p1Axes = sa_getAxes(p1);

        for (var i = 0; i < 2; ++i)
        {
            var p1Proj = sa_getProjection(p1, p1Axes[i]);
            var pCentreProj = pcentre.X * p1Axes[i].X + pcentre.Y * p1Axes[i].Y;

            var min = pCentreProj - r;
            var max = pCentreProj + r;

            var gap_present = ((p1Proj.max <= min) || (max <= p1Proj.min));

            if (gap_present)
                return false;
        }

        return true; //no separating axis found
    }

    public static Vector2d[] sa_getAxes(Vector2d[] points)
    {
        var ret = new Vector2d[2];

        for (var i = 0; i < 2; ++i)
        {
            var axis = points[i + 1] - points[i];
            ret[i] = axis.Normalized();
        }

        return ret;
    }

    public static Vector2d sa_getAxesLine(Vector2d[] points)
    {
        var axis = points[1] - points[0];
        axis = axis.Normalized();

        return new Vector2d(-axis.Y, axis.X);
    }

    public static (double min, double max) sa_getProjection(Vector2d[] points, Vector2d axis)
    {
        var newProj = points[0].X * axis.X + points[0].Y * axis.Y;
        var min = newProj;
        var max = newProj;

        for (var i = 1; i < 4; ++i)
        {
            newProj = points[i].X * axis.X + axis.Y * points[i].Y;

            if (newProj < min)
                min = newProj;
            else if (newProj > max)
                max = newProj;
        }

        return (min, max);
    }

    public static (double min, double max) sa_getProjectionLine(Vector2d[] points, Vector2d axis)
    {
        var newProj = points[0].X * axis.X + points[0].Y * axis.Y;

        var min = newProj;
        var max = newProj;

        for (var i = 1; i< 2; ++i)
        {
            newProj = points[i].X * axis.X + axis.Y * points[i].Y;

            if (newProj<min)
                min = newProj;
            else if (newProj > max)
                max = newProj;
        }

        return (min, max);
    }

    #endregion
}
