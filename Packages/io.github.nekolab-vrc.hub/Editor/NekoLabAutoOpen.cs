using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NekoLab.Hub
{
    /// <summary>
    /// 商品のバージョンごとに1回だけ、NekoLAB の窓を自動で開く（初めてインポートした時と、更新した時）。
    /// 全部の商品の分をこの窓がまとめて受け持つので、商品の側では開かない。
    /// 開いた記録は PC ではなくプロジェクトの中（UserSettings）に残す。窓のホームか、商品の開発用のプロジェクト（EnabledKey）で止められる。
    /// </summary>
    public static class NekoLabAutoOpen
    {
        /// <summary>EditorUserSettings のキー。"0" の時は自動で開かない（商品の開発用のプロジェクトが入れる）。</summary>
        public const string EnabledKey = "NekoLAB.Hub.AutoOpen";
        /* ＋商品の id：最後に開いた時のバージョン */
        const string ShownKey = "NekoLAB.Hub.Shown.";

        public static bool Enabled
        {
            get => EditorUserSettings.GetConfigValue(EnabledKey) != "0";
            set => EditorUserSettings.SetConfigValue(EnabledKey, value ? "1" : "0");
        }

        /* スクリプトの組み立ての後（コードのある商品を入れた時・この窓を入れた時や更新した時） */
        [InitializeOnLoadMethod]
        static void OnLoad() => Later();

        /* 名札がインポートされた時（コードのない商品や、ページだけでは組み立て直しが起きない時も拾う） */
        class Imported : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                if (imported.Any(p => Path.GetFileNameWithoutExtension(p) == NekoLabNameplate.FileName)) Later();
            }
        }

        static void Later()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall -= Check;
            EditorApplication.delayCall += Check;
        }

        /* まだ開いていないバージョンの商品：1つならその商品のページ、いくつかならホームを開く */
        static void Check()
        {
            if (!Enabled || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var fresh = NekoLabNameplate.FindAll().Where(p => EditorUserSettings.GetConfigValue(ShownKey + p.id) != p.version).ToList();
            if (fresh.Count == 0) return;
            foreach (var p in fresh) EditorUserSettings.SetConfigValue(ShownKey + p.id, p.version);
            NekoLabHub.Open(fresh.Count == 1 ? fresh[0].id : null);
        }
    }
}
