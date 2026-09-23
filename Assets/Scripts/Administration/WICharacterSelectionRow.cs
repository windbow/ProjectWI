using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WICharacterSelectionRow : MonoBehaviour
    {
        // 편집 시점에 완성한 행의 입력·초상·이름·설명 참조입니다.
        [SerializeField] private Button button;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text detail;
        private string lastDetail;
        public Button Button => button;
        public Image Portrait => portrait;
        public TMP_Text Label => detail;
        public string SearchText => title.text + " " + detail.text;
        public string SortName => title.text.Replace("[●] ", "").Replace("[ ] ", "").Replace("● ", "");

        // 기존 스냅샷의 첫 줄을 이름 칸으로 나누고 나머지를 가로 요약으로 표시합니다.
        public void Format()
        {
            if (detail.text == lastDetail)
            {
                return;
            }
            string source = detail.text;
            int split = source.IndexOf('\n');
            title.text = split < 0 ? source : source.Substring(0, split);
            detail.text = split < 0 ? string.Empty : source.Substring(split + 1).Replace("\n", " · ");
            lastDetail = detail.text;
            bool selected = title.text.StartsWith("[●]") || title.text.StartsWith("●");
            button.image.color = selected ? new Color32(40, 78, 109, 255) : new Color32(24, 36, 51, 255);
        }
    }
}
