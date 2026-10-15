using UnityEditor;

namespace NekoLab.Hub
{
    /// <summary>
    /// 商品のページで使う、窓と同じ部品。どのページも窓のほかの所と同じ見た目になる：カード・「?」付きの見出し・
    /// 幅に合わせて折り返す文（日本語も）・Unity の HelpBox の代わりの注意・導入済みかどうかの帯・ページの主なボタン。
    /// わざと少なくしてある。ここの名前は 1.0.0 から変えない。ページの DrawTab の中で使う
    /// （窓が描きながら文の幅を測る。ほかの所で使うと、ビューの幅で並べる）。
    /// </summary>
    public static class NekoLabUI
    {
        /// <summary>カードを始める（地の色の付いた箱）。EndCard までに描いた物が、カードに乗る。</summary>
        public static void BeginCard() => NekoLabLook.BeginCard();

        /// <summary>BeginCard で始めたカードを終える。</summary>
        public static void EndCard() => NekoLabLook.EndCard();

        /// <summary>カードの見出し（太字）。<paramref name="help"/> があれば「?」を付け、マウスを乗せると出す（null なら付けない）。</summary>
        public static void Heading(string words, string help = null) => NekoLabLook.Heading(words, help);

        /// <summary>文。幅に合わせて折り返す。</summary>
        public static void Text(string words) => NekoLabLook.Text(words, NekoLabLook.Body);

        /// <summary>小さく控えめな文（見出しの下の1行や補足）。幅に合わせて折り返す。</summary>
        public static void SmallText(string words) => NekoLabLook.Text(words, NekoLabLook.SmallMuted);

        /// <summary>カードの上の注意。Info（青い帯）・Warning（橙）・Error（赤）・None（帯なし）。</summary>
        public static void Notice(string words, MessageType kind = MessageType.Info) => NekoLabLook.Notice(words, kind);

        /// <summary>商品がアバターに導入済みかを、窓の言葉（"導入済み" / "未導入"）の帯で出す。</summary>
        public static void InstallStatus(bool installed) => NekoLabLook.InstallStatus(installed);

        /// <summary>ページの主な操作（導入するなど）のボタン。どのページでも同じ大きさ。押された時に true。</summary>
        public static bool MainButton(string words) => NekoLabLook.MainButton(words);
    }
}
