using Code.Scripts.Core;
using TMPro;
using UnityEngine;

namespace Code.Scripts.UI
{
    public class WeekDayBanner : MonoBehaviour
    {
        [SerializeField] private TMP_Text _weekText;
        [SerializeField] private TMP_Text _dayText;

        public static void Refresh(GameProgress progress)
        {
            if (progress == null)
            {
                Debug.LogError("Cannot refresh the week/day banner with null progress.");
                return;
            }

            WeekDayBanner banner = Object.FindFirstObjectByType<WeekDayBanner>();
            if (banner == null)
            {
                Debug.LogError("WeekDayBanner is missing from the active scene.");
                return;
            }

            banner.Show(progress);
        }

        private void Show(GameProgress progress)
        {
            if (_weekText == null || _dayText == null)
            {
                Debug.LogError("WeekDayBanner needs Week and Day TMP references.", this);
                return;
            }

            _weekText.text = $"WEEK {progress.Week}";
            _dayText.text = $"Day {progress.Day}";
        }
    }
}
