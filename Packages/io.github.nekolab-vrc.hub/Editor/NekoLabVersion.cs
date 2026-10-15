using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NekoLab.Hub
{
    /// <summary>
    /// バージョンの番号（"1.2.3"・"1.0.0-rc.1"）を、セマンティック バージョニングの決まりで比べる。数字の部分ごとに数として比べ、
    /// プレリリースは正式な版より前（"1.0.0-rc.1" は "1.0.0" より前）。Unity の Version Defines と同じ数え方。
    /// </summary>
    public static class NekoLabVersion
    {
        /// <summary>0 より小さい：a が前。0：同じ。0 より大きい：a が後。</summary>
        public static int Compare(string a, string b)
        {
            Split(a, out var coreA, out var preA);
            Split(b, out var coreB, out var preB);
            for (var k = 0; k < Math.Max(coreA.Length, coreB.Length); k++)
            {
                var c = (k < coreA.Length ? coreA[k] : 0).CompareTo(k < coreB.Length ? coreB[k] : 0);
                if (c != 0) return c;
            }
            /* 数字が同じ時：正式な版はプレリリースより後。プレリリースどうしは区切りごとに比べる（数字は数として、文字より前） */
            if (preA.Length == 0 || preB.Length == 0) return preB.Length.CompareTo(preA.Length);
            for (var k = 0; k < Math.Min(preA.Length, preB.Length); k++)
            {
                var x = int.TryParse(preA[k], out var xn);
                var y = int.TryParse(preB[k], out var yn);
                var c = x && y ? xn.CompareTo(yn) : x ? -1 : y ? 1 : Math.Sign(string.CompareOrdinal(preA[k], preB[k]));
                if (c != 0) return c;
            }
            return preA.Length.CompareTo(preB.Length);
        }

        /// <summary>"1.2.3" の形、または "1.2.3-" の後にプレリリース（"rc.1" など）が付いた形か。</summary>
        public static bool IsValid(string v) => v != null && Regex.IsMatch(v, @"^\d+\.\d+\.\d+(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$");

        /* 数字の部分と、プレリリースの区切り（正式な版は無し）に分ける。"+" の後ろは比べない */
        static void Split(string v, out int[] core, out string[] pre)
        {
            v = (v ?? "").Trim();
            var plus = v.IndexOf('+');
            if (plus >= 0) v = v.Substring(0, plus);
            var dash = v.IndexOf('-');
            pre = dash < 0 ? new string[0] : v.Substring(dash + 1).Split('.');
            core = (dash < 0 ? v : v.Substring(0, dash)).Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();
        }
    }
}
