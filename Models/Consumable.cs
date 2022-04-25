using System;
using System.Linq;
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
                int[] buffArr = Array.ConvertAll(_objectInfo[7].Split(' '), Convert.ToInt32);

                if (buffArr.Sum() != 0)
                {
                    HasBuff = true;

                    // parse the buffs
                    Buff = new Buff(
                        farming         : buffArr[0],
                        fishing         : buffArr[1],
                        mining          : buffArr[2],
                        digging         : buffArr[3],
                        luck            : buffArr[4],
                        foraging        : buffArr[5],
                        crafting        : buffArr[6],
                        maxStamina      : buffArr[7],
                        magneticRadius  : buffArr[8],
                        speed           : buffArr[9],
                        defense         : buffArr[10],
                        attack          : buffArr.Length > 11 ? Convert.ToInt32(buffArr[11]) : 0,
                        minutesDuration : _objectInfo.Count > 8 ? Convert.ToInt32(_objectInfo[8]) : -1,
                        source          : _objectInfo[0],
                        displaySource   : _objectInfo[4]);
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
