using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace NekoLab.Hub
{
    /// <summary>
    /// サポート情報：買った人が写して、BOOTH のショップへの質問に添える物。Unity・VRChat SDK・Modular Avatar と、
    /// 商品を作った時のほかのパッケージのバージョン、この窓と、プロジェクトの中の NekoLAB の商品のバージョン。
    /// アバターを選んでいれば、どの商品が導入済みかと同期パラメーターの使用量。各商品のページが足す行（NekoLabProductPage.SupportLines）。
    /// PC と、それを使う人のことは入れない：PC の場所も、人・アバター・シーンの名前も入れない。個人の情報に見える行は、
    /// 誰が書いた行でも外す（点検の K-10 と同じ見方）。作者が読むので、言語の設定にかかわらず英語で書く。
    /// </summary>
    public static class NekoLabSupport
    {
        /* いつも出す物。商品の名札が、ほかのパッケージを足すことがある */
        static readonly (string id, string label)[] Known =
        {
            ("com.vrchat.avatars", "VRChat SDK (Avatars)"),
            ("nadena.dev.modular-avatar", "Modular Avatar"),
            ("nadena.dev.ndmf", "NDMF"),
            ("com.vrcfury.vrcfury", "VRCFury"),
            ("jp.lilxyzw.liltoon", "lilToon"),
        };

        /// <summary>個人の情報に見えて外した行の代わりに出す文。</summary>
        public const string LeftOut = "(a line left out: it looked personal)";

        /// <summary>このプロジェクトに入っているパッケージのバージョン（"-"：入っていない）。プロジェクトのパッケージの一覧から読む（ネットにはつながらない）。</summary>
        public static string PackageVersion(string id) => UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/" + id)?.version ?? "-";

        /// <summary>
        /// これらの商品のサポート情報（ページは id で引く。アバターは null でもよい）。窓は自分の分を NekoLabHub.SupportInfo で出す。
        /// hub は各ページの SupportLines に渡す。null でもよい。
        /// </summary>
        public static string Info(IReadOnlyList<NekoLabNameplate> plates, Func<string, NekoLabProductPage> pageOf, VRCAvatarDescriptor avatar, NekoLabHub hub)
        {
            plates = plates ?? new List<NekoLabNameplate>();
            var lines = new List<string> { "NekoLAB Hub " + NekoLabHub.ToolVersion, "Unity " + Application.unityVersion };
            var ids = Known.Select(k => k.id).ToList();
            foreach (var p in plates)
                foreach (var package in p.packages ?? new string[0])
                {
                    var id = (package ?? "").Split(' ')[0];
                    if (id != "" && !ids.Contains(id)) ids.Add(id);
                }
            foreach (var id in ids) lines.Add((Known.FirstOrDefault(k => k.id == id).label ?? id) + " " + PackageVersion(id));

            lines.Add("NekoLAB products: " + plates.Count);
            var products = new List<(string, GameObject)>();
            foreach (var p in plates)
            {
                var on = avatar != null ? p.InstanceOn(avatar) : null;
                if (on != null) products.Add((p.name, on));
                lines.Add("- " + p.name + " " + p.version + " (" + p.id + ", needs Hub " + (string.IsNullOrEmpty(p.toolVersion) ? "any" : p.toolVersion) + ")"
                    + (avatar == null ? "" : on != null ? " [on the avatar]" : " [not on the avatar]"));
                if (p.packages != null && p.packages.Length > 0) lines.Add("  made with: " + string.Join(", ", p.packages));
                var page = pageOf?.Invoke(p.id);
                if (page == null) { lines.Add("  no page (a compile error, or this Hub is older than it needs)"); continue; }
                string[] own;
                try { own = page.SupportLines(hub); }
                catch (Exception e) { own = new[] { "(its own lines failed: " + e.GetType().Name + ")" }; }
                foreach (var line in own ?? new string[0])
                    if (!string.IsNullOrEmpty(line))
                        foreach (var part in line.Split('\n')) lines.Add("  " + part.TrimEnd('\r'));
            }

            if (avatar == null) lines.Add("Avatar: none picked");
            else
            {
                var t = NekoLabBits.Count(avatar, products);
                lines.Add("Synced bits on the avatar: " + t.Total + " / " + NekoLabBits.Limit
                    + (!t.modularAvatar ? " (Modular Avatar missing: its parts not counted)" : !t.modularAvatarRead ? " (Modular Avatar not read: its parts not counted)" : "")
                    + (t.vrcFury ? " (VRCFury: an estimate" + (t.vrcFuryCompress ? ", compression in use" : "") + ")" : ""));
            }
            return string.Join("\n", lines.Select(l => NekoLabCheck.LooksPersonal(l) ? l.Substring(0, l.Length - l.TrimStart().Length) + LeftOut : l));
        }
    }
}
