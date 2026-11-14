using System.Collections.Generic;
using System.Linq;
using Code.Scripts.Dice;
using UnityEngine;

namespace Code.Scripts.Shop
{
    public class ShopOffer
    {
        public readonly struct Entry
        {
            public int Number { get; }
            public DiceSO Dice { get; }

            public Entry(int number, DiceSO dice)
            {
                Number = number;
                Dice = dice;
            }
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly DiceSO[] _pool;
        private readonly int _size;
        private readonly int _maxNumber;
        private int _offerWeek;

        public ShopOffer(DiceSO[] pool, int size, int maxNumber)
        {
            _pool = pool;
            _size = size;
            _maxNumber = maxNumber;
        }

        public IReadOnlyList<Entry> Entries => _entries;
        public int Count => _entries.Count;

        public void RefreshShopOffer(int week)
        {
            if (_offerWeek == week && _entries.Count > 0) return;
            _offerWeek = week;
            _entries.Clear();

            List<DiceSO> dice = _pool == null ? new List<DiceSO>() : _pool.Where(d => d != null).Distinct().ToList();
            if (dice.Count == 0 || _maxNumber < 1) return;

            List<int> numbers = Enumerable.Range(1, _maxNumber).ToList();
            Shuffle(numbers);
            numbers = numbers.Take(Mathf.Clamp(_size, 0, _maxNumber)).OrderBy(n => n).ToList();

            Shuffle(dice);
            for (int i = 0; i < numbers.Count; i++)
            {
                DiceSO offered = i < dice.Count ? dice[i] : dice[Random.Range(0, dice.Count)];
                _entries.Add(new Entry(numbers[i], offered));
            }
        }

        public DiceSO Get(int number)
        {
            foreach (Entry entry in _entries)
                if (entry.Number == number) return entry.Dice;
            return null;
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}