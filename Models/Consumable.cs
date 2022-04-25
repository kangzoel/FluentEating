using System;
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

        public readonly Buff Buff;

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
            {
                if (!_objectInfo[7].Equals("0 0 0 0 0 0 0 0 0 0 0"))
                {
                    HasBuff = true;

                    // parse the buff
                    string[] buffArr = _objectInfo[7].Split(' ');
                    int minutesDuration = _objectInfo.Count > 8 ? Convert.ToInt32(_objectInfo[8]) : -1;

                    Buff = new Buff(
                        Convert.ToInt32(buffArr[0]),
                        Convert.ToInt32(buffArr[1]),
                        Convert.ToInt32(buffArr[2]),
                        Convert.ToInt32(buffArr[3]),
                        Convert.ToInt32(buffArr[4]),
                        Convert.ToInt32(buffArr[5]),
                        Convert.ToInt32(buffArr[6]),
                        Convert.ToInt32(buffArr[7]),
                        Convert.ToInt32(buffArr[8]),
                        Convert.ToInt32(buffArr[9]),
                        Convert.ToInt32(buffArr[10]),
                        buffArr.Length > 11 ? Convert.ToInt32(buffArr[11]) : 0,
                        minutesDuration,
                        _objectInfo[0],
                        _objectInfo[4]);
                }
            }
        }

        internal void ApplyBuff()
        {
            if (_objectInfo.Count > 6 && Type.Equals(ItemType.Drink))
                Game1.buffsDisplay.tryToAddDrinkBuff(Buff);
            else if (Convert.ToInt32(_objectInfo[2]) > 0)
                Game1.buffsDisplay.tryToAddFoodBuff(Buff, Math.Min(120000, (int)((double)Convert.ToInt32(_objectInfo[2]) / 20.0 * 30000.0)));
        }
    }
}
