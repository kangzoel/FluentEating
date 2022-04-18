using System.Collections.Generic;
using StardewValley;
using SDVObject = StardewValley.Object;

namespace FluentEating.Models
{
    internal class Consumable : SDVObject
    {
        private readonly List<string> _objectInfo;

        public enum ItemType
        {
            Food,
            Drink
        }

        public readonly bool HasBuff;

        public readonly new ItemType Type;

        public Consumable(SDVObject obj) : base(obj.ParentSheetIndex, obj.Stack)
        {
            _objectInfo = new List<string>();

            var rawObjectInfo = Game1.objectInformation[obj.ParentSheetIndex];
            var rawObjectInfoStr = rawObjectInfo.Split('/');

            foreach (var value in rawObjectInfoStr)
                _objectInfo.Add(value);

            // parse whether the object is food or drink
            if (_objectInfo.Count > 6)
            {
                Type = _objectInfo[6] switch
                {
                    "food" => ItemType.Food,
                    "drink" => ItemType.Drink,
                    _ => ItemType.Food
                };
            }

            // parse whether object has buff or not
            if (_objectInfo.Count > 7)
                if (!_objectInfo[7].Equals("0 0 0 0 0 0 0 0 0 0 0"))
                    HasBuff = true;
        }
    }
}
