using System.Collections.Generic;
using Code.Scripts.Core;
using Code.Scripts.Dice;

namespace Code.Scripts.Shop
{
    public class ShopOffer
    {
        private readonly List<DiceSO> _dice = new List<DiceSO>();
        private readonly DiceSO[] _pool;
        private readonly int _min;
        private readonly int _max;
        private int _offerWeek;
        
        public ShopOffer(DiceSO[] pool, int min, int max)
        {
            _pool = pool;
            _min = min;
            _max = max;
        }
        
        public IReadOnlyList<DiceSO> Dice => _dice;
        public int Count => _dice.Count;

        public void RefreshShopOffer(int week)
        {
            if(_offerWeek == week && _dice.Count > 0) return;
            _offerWeek = week;
            _dice.Clear();
            
            if(_pool == null || _pool.Length == 0) return;
            
            int count = UnityEngine.Random.Range(_min, _max + 1);
            for (int i = 0; i < count; i++)
            {
                DiceSO dice = _pool[UnityEngine.Random.Range(0, _pool.Length)];
                if(dice != null && !_dice.Contains(dice))
                    _dice.Add(dice);
            }
        }
        
        public DiceSO Get(int number) => number >= 1 && number < _dice.Count ? _dice[number - 1] : null;
    }
}