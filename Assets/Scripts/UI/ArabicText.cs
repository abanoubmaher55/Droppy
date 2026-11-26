using System.Text;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UI;

namespace SaveWaterEG
{
    // Arabic support for legacy UI Text (which does not shape or reorder Arabic).
    // Fix() reshapes letters into presentation forms and reverses Arabic runs to
    // visual RTL order, keeping Latin/number runs LTR. Reverses by text element
    // so emoji surrogate pairs stay intact. Multi-line strings are fixed per line.
    public static class ArabicText
    {
        static readonly Dictionary<char, int> Forms = new Dictionary<char, int>
        {
            // isolated, final, initial, medial packed as 4 ints per letter
            ['ء'] = 0xFE80, ['آ'] = 0xFE81, ['أ'] = 0xFE83, ['ؤ'] = 0xFE85,
            ['إ'] = 0xFE87, ['ئ'] = 0xFE89, ['ا'] = 0xFE8D, ['ب'] = 0xFE8F,
            ['ة'] = 0xFE93, ['ت'] = 0xFE95, ['ث'] = 0xFE99, ['ج'] = 0xFE9D,
            ['ح'] = 0xFEA1, ['خ'] = 0xFEA5, ['د'] = 0xFEA9, ['ذ'] = 0xFEAB,
            ['ر'] = 0xFEAD, ['ز'] = 0xFEAF, ['س'] = 0xFEB1, ['ش'] = 0xFEB5,
            ['ص'] = 0xFEB9, ['ض'] = 0xFEBD, ['ط'] = 0xFEC1, ['ظ'] = 0xFEC5,
            ['ع'] = 0xFEC9, ['غ'] = 0xFECD, ['ف'] = 0xFED1, ['ق'] = 0xFED5,
            ['ك'] = 0xFED9, ['ل'] = 0xFEDD, ['م'] = 0xFEE1, ['ن'] = 0xFEE5,
            ['ه'] = 0xFEE9, ['و'] = 0xFEED, ['ى'] = 0xFEEF, ['ي'] = 0xFEF1,
        };

        // letters that never join to the NEXT letter (right-joining only)
        static readonly HashSet<char> NoJoinNext = new HashSet<char>
        { 'ء', 'ا', 'أ', 'إ', 'آ', 'د', 'ذ', 'ر', 'ز', 'و', 'ؤ', 'ة', 'ى' };

        // Assigns shaped RTL text to a UI Text (use for dynamic strings).
        public static void Set(Text t, string s) { t.text = Fix(s); }

        public static bool HasArabic(string s)
        {
            if (s == null) return false;
            foreach (var c in s) if (c >= 0x0621 && c <= 0x064A) return true;
            return false;
        }

        public static string Fix(string s)
        {
            if (!HasArabic(s)) return s;
            var lines = s.Split('\n');
            for (int i = 0; i < lines.Length; i++) lines[i] = FixLine(lines[i]);
            return string.Join("\n", lines);
        }

        static string FixLine(string line)
        {
            // 1) reshape to presentation forms (logical order)
            var shaped = new StringBuilder(line.Length + 8);
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (!Forms.TryGetValue(c, out int iso))
                {
                    if (c >= 0x064B && c <= 0x0652) continue; // drop tashkeel (unused here)
                    shaped.Append(c);
                    continue;
                }
                bool prevJoin = i > 0 && Forms.ContainsKey(line[i - 1]) && !NoJoinNext.Contains(line[i - 1]);
                bool nextJoin = i + 1 < line.Length && Forms.ContainsKey(line[i + 1]) && !NoJoinNext.Contains(c);
                int form;
                if (prevJoin && nextJoin) form = iso + 3;      // medial
                else if (prevJoin) form = iso + 1;             // final
                else if (nextJoin) form = iso + 2;             // initial
                else form = iso;                               // isolated
                // right-joining-only letters have no initial/medial: fall back
                if (NoJoinNext.Contains(c) && (form == iso + 2 || form == iso + 3)) form = iso;
                shaped.Append((char)form);
            }

            // 2) reverse by text element (keeps surrogate pairs intact)
            var elems = new List<string>();
            var e = StringInfo.GetTextElementEnumerator(shaped.ToString());
            while (e.MoveNext()) elems.Add(e.GetTextElement());
            elems.Reverse();

            // 3) re-reverse Latin/digit runs so they read LTR again
            var sb = new StringBuilder(elems.Count + 8);
            int k = 0;
            while (k < elems.Count)
            {
                if (IsLatinRun(elems[k]))
                {
                    int j = k;
                    while (j < elems.Count && IsLatinRun(elems[j])) j++;
                    for (int m = j - 1; m >= k; m--) sb.Append(elems[m]);
                    k = j;
                }
                else { sb.Append(elems[k]); k++; }
            }
            return sb.ToString();
        }

        static bool IsLatinRun(string el)
        {
            char c = el[0];
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                || c == '.' || c == ',' || c == '%' || c == '/' || c == '-' || c == '+';
        }
    }
}
