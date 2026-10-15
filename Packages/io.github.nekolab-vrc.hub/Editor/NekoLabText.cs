using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NekoLab.Hub
{
    /// <summary>
    /// NekoLAB の窓の言葉（日本語と英語）を1つの表に持つ（商品の言葉は、商品の側の表に持つ）。
    /// 言語の設定はプロジェクトの中（UserSettings：EditorUserSettings の "NekoLAB.Lang"。"0" 日本語、"1" 英語）に置き、
    /// 窓と全部の NekoLAB の商品が同じ設定を使う（商品はこの道具がなくても同じ設定を読める）。初めての時は PC の言語で決める。
    /// 日本語の文では、英単語と日本語の間に空白を入れず、いくつかの語でできた名前の中の空白は、改行しない空白にしてある
    /// （Unity は空白で先に改行するので、おかしな所で行が切れるため）。
    /// </summary>
    public static class NekoLabText
    {
        /// <summary>言語の設定の EditorUserSettings のキー。"0" 日本語、"1" 英語（まだない時は PC の言語）。</summary>
        public const string LangKey = "NekoLAB.Lang";

        public static int Lang
        {
            get => int.TryParse(EditorUserSettings.GetConfigValue(LangKey), out var v) ? Mathf.Clamp(v, 0, 1) : Application.systemLanguage == SystemLanguage.Japanese ? 0 : 1;
            set => EditorUserSettings.SetConfigValue(LangKey, Mathf.Clamp(value, 0, 1).ToString());
        }

        public static string L(string key) => Words.TryGetValue(key, out var w) ? w[Mathf.Clamp(Lang, 0, 1)] : key;

        static readonly Dictionary<string, string[]> Words = new Dictionary<string, string[]>
        {
            ["hub.home"] = new[] { "ホーム", "Home" },
            ["hub.products"] = new[] { "このプロジェクトの商品", "In this project" },
            ["hub.none"] = new[] { "まだありません", "None yet" },
            ["hub.empty.head"] = new[] { "商品が見つかりません", "No product found" },
            ["hub.empty"] = new[] { "NekoLABの商品のunitypackageをインポートすると、左の一覧に表示されます。", "Import a NekoLAB product's unitypackage and it shows in the list on the left." },
            ["hub.on"] = new[] { "導入済み", "Installed" },
            ["hub.off"] = new[] { "未導入", "Not installed" },
            ["hub.avatar"] = new[] { "アバター", "Avatar" },
            ["hub.noavatar.short"] = new[] { "アバターを選んでください", "Pick your avatar" },
            ["hub.noavatar"] = new[] { "シーンに置いたアバター（VRC Avatar Descriptor）を、ここで選びます。", "Pick the avatar in your scene (the one with a VRC Avatar Descriptor) here." },
            ["steps.head"] = new[] { "使い方", "How to use" },
            ["steps.1"] = new[] { "アバターを選ぶ", "Pick an avatar" },
            ["steps.2"] = new[] { "左の一覧から商品を選ぶ", "Pick a product on the left" },
            ["steps.3"] = new[] { "導入する", "Install it" },
            ["hub.oldtool"] = new[] { "この商品には、新しいNekoLAB Hub（{0}以上）が必要です。ALCOMで「NekoLAB Hub」を更新してください。", "This product needs a newer NekoLAB Hub ({0} or later). Update \"NekoLAB Hub\" in ALCOM." },
            ["hub.nopage"] = new[] { "この商品の設定画面が見つかりません。商品のunitypackageを、もう一度インポートしてください。", "This product's pages were not found. Import the product's unitypackage again." },
            ["hub.safe.short"] = new[] { "ネットにはつながりません", "Works offline" },
            ["hub.safe"] = new[] { "この画面はネットにつながらず、プロジェクトの外のファイルも読み書きしません。", "This window never goes online and reads or writes nothing outside the project." },
            ["hub.autoopen"] = new[] { "商品をインポート・更新した時に、この画面を開く", "Open this window when a product is imported or updated" },

            ["support.head"] = new[] { "サポート", "Support" },
            ["support.line"] = new[] { "BOOTHで質問する時は、この情報も一緒に送ってください。", "When you ask a question at the BOOTH shop, send this with it." },
            ["support.about"] = new[] {
                "入るのは、ソフトと商品のバージョン・アバターへの導入状況・同期パラメーターの使用量だけです。PCの場所・ユーザー名・アバター名などは入りません。",
                "It holds only the versions of the software and the products, which of them are on the avatar, and the synced parameters in use. No paths, user names or avatar names." },
            ["support.copy"] = new[] { "サポート情報をコピー", "Copy support info" },
            ["support.copied"] = new[] { "コピーしました", "Copied" },
            ["support.show"] = new[] { "コピーされる内容を見る（個人情報は入りません）", "See what is copied (nothing personal)" },

            ["bits.head"] = new[] { "同期パラメーターの使用量", "Synced parameters in use" },
            ["bits.what"] = new[] {
                "VRChatで、ほかの人に送るアバターの状態の量です（単位はbit）。上限は256bitで、超えるとアップロードできません。残りが多いほど、ほかのギミックを追加できます。",
                "How much of your avatar's state VRChat sends to everyone else (in bits). The limit is 256: over it, the avatar cannot be uploaded. The more is left, the more gimmicks you can add." },
            ["bits.own"] = new[] { "アバター本体", "The avatar itself" },
            ["bits.ma"] = new[] { "ほかのギミック（Modular Avatar）", "Other gimmicks (Modular Avatar)" },
            ["bits.vrcf"] = new[] { "VRCFuryのギミック（目安）", "VRCFury gimmicks (estimate)" },
            ["bits.left"] = new[] { "残り {0}", "{0} left" },
            ["bits.over"] = new[] { "256bitを超えています。このままではアップロードできません。", "Over 256 bits: the avatar cannot be uploaded like this." },
            ["bits.noma"] = new[] { "Modular Avatarが入っていないため、NekoLABの商品とModular Avatarのギミックの分は数えていません。", "Modular Avatar is not in the project, so the NekoLAB products and Modular Avatar gimmicks are not counted." },
            ["bits.maread"] = new[] { "このバージョンのModular Avatarは読み取れなかったため、NekoLABの商品とModular Avatarのギミックの分は数えていません。ALCOMで「NekoLAB Hub」を更新してください。", "This version of Modular Avatar could not be read, so the NekoLAB products and Modular Avatar gimmicks are not counted. Update \"NekoLAB Hub\" in ALCOM." },
            ["bits.vrcf.note"] = new[] { "VRCFuryの分は目安です（設定によっては、実際はもっと少なくなります）。", "The VRCFury part is an estimate (with some VRCFury settings it is less)." },
            ["bits.vrcf.compress"] = new[] { "VRCFuryのパラメーター圧縮が使われているため、実際は表示より少なくなります。", "VRCFury's parameter compression is in use: the real total is lower than shown." },
            ["bits.noavatar"] = new[] { "アバターを選ぶと表示されます", "Shown once an avatar is picked" },
        };
    }
}
