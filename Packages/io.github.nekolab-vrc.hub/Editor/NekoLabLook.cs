using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NekoLab.Hub
{
    /// <summary>
    /// 窓の見た目：どこに何があるかがひと目で分かり、一度に出す文が少なく、狭い窓でも日本語の行がきれいに折り返す。
    /// 場所は地の色で分ける（上の帯と一覧は濃く、ホームの物はカードに乗せる）。折り返す文は、与えられた幅で並べる：
    /// Unity の並べ方は、折り返す文のいちばん狭い幅を「いちばん長い単語の幅」とするが、空白のない日本語の文は全体で1つの
    /// 長い単語になるので、ページを窓の外へ押し出し、文の中のただ1つの空白（「NekoLAB」の後など）で改行してしまうため。
    /// </summary>
    static class NekoLabLook
    {
        static bool built, builtDark, inPass;
        static readonly List<float> widths = new List<float>();
        static int next;
        static EditorWindow owner;
        static float guess = 300f;

        public static GUIStyle Title, Body, Muted, Small, SmallMuted, Warn, WarnBody, Bad, BadBody, Line, SmallLine, SmallMutedLine,
            GoodLine, MutedLine, WarnLine, RowName, RowNameOn, RowSmallOn, Mark, Number, NumberDark, CardPad, BandOnWords, BandOffWords,
            ChipWords, InlinePad;
        static int cardDepth; /* （今開いているカードの数。カードの中の注意は、自分の箱を付けずに描く） */

        static bool Dark => EditorGUIUtility.isProSkin;
        static Color Grey(float v) => new Color(v, v, v);
        public static Color Bar => Dark ? Grey(0.15f) : Grey(0.68f);
        public static Color Strip => Dark ? Grey(0.19f) : Grey(0.74f);
        public static Color Side => Dark ? Grey(0.17f) : Grey(0.72f);
        public static Color Card => Dark ? Grey(0.255f) : Grey(0.87f);
        public static Color Edge => Dark ? Grey(0.11f) : Grey(0.58f);
        public static Color Hover => Dark ? new Color(1f, 1f, 1f, 0.05f) : new Color(0f, 0f, 0f, 0.06f);
        public static Color Accent => Dark ? new Color(0.17f, 0.36f, 0.53f) : new Color(0.23f, 0.45f, 0.78f);
        public static Color Good => Dark ? new Color(0.37f, 0.82f, 0.54f) : new Color(0.08f, 0.5f, 0.24f);
        public static Color WarnColour => Dark ? new Color(0.96f, 0.76f, 0.32f) : new Color(0.5f, 0.28f, 0f);
        /* （まだの段階：明るいテーマでは、薄い丸に濃い数字。灰に白は読みにくいため） */
        public static Color StepOff => Dark ? Grey(0.4f) : Grey(0.8f);
        static Color ChipGround => Dark ? new Color(0.4f, 0.29f, 0.08f) : new Color(1f, 0.87f, 0.56f);
        public static Color BadColour => Dark ? new Color(1f, 0.46f, 0.46f) : new Color(0.74f, 0.1f, 0.1f);
        public static Color MutedColour => Dark ? Grey(0.6f) : Grey(0.3f);
        public static Color Off => Dark ? Grey(0.4f) : Grey(0.62f);

        /// <summary>窓を描く1回を始める。測った幅は、描く順番で覚えておく。</summary>
        public static void BeginPass(EditorWindow window, float firstGuess)
        {
            owner = window;
            next = 0;
            guess = Mathf.Max(80f, firstGuess);
            inPass = true;
            cardDepth = 0;
            Build();
        }

        /// <summary>描く1回を終える（商品が自分の窓に描くなど、窓の描く回の外で描いた文は、測らずにビューの幅で並べる）。</summary>
        public static void EndPass() => inPass = false;

        /// <summary>スタイルを作る（エディターのテーマが変わった時は作り直す）。ここの描く関数は、どれも先にこれを呼ぶ。</summary>
        public static void Build()
        {
            if (built && builtDark == Dark && Title != null) return;
            built = true;
            builtDark = Dark;
            MainButtonStyle = null; /* （初めて描く時に、テーマのボタンから作る） */
            Title = new GUIStyle(EditorStyles.boldLabel) { wordWrap = true };
            Body = new GUIStyle(EditorStyles.label) { wordWrap = true };
            Muted = Coloured(Body, MutedColour);
            Small = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            SmallMuted = Coloured(Small, MutedColour);
            Warn = Coloured(Small, WarnColour);
            WarnBody = Coloured(Body, WarnColour);
            Bad = Coloured(Small, BadColour);
            BadBody = Coloured(Body, BadColour);
            Line = new GUIStyle(EditorStyles.label) { wordWrap = false };
            MutedLine = Coloured(Line, MutedColour);
            WarnLine = Coloured(Line, WarnColour);
            SmallLine = new GUIStyle(EditorStyles.miniLabel) { wordWrap = false };
            SmallMutedLine = Coloured(SmallLine, MutedColour);
            GoodLine = Coloured(Line, Good);
            RowName = new GUIStyle(EditorStyles.boldLabel) { wordWrap = false, clipping = TextClipping.Clip };
            RowNameOn = Coloured(RowName, Color.white);
            RowSmallOn = Coloured(SmallLine, new Color(1f, 1f, 1f, 0.85f));
            Mark = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(), margin = new RectOffset(), fontSize = 9 };
            Mark.normal.textColor = MutedColour;
            Number = new GUIStyle(Mark) { fontSize = 10 };
            Number.normal.textColor = Color.white;
            CardPad = new GUIStyle { padding = new RectOffset(10, 10, 8, 9), margin = new RectOffset(0, 0, 0, 8) };
            NumberDark = new GUIStyle(Number);
            NumberDark.normal.textColor = Grey(0.25f);
            ChipWords = Coloured(new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(), fontSize = 11 },
                Dark ? new Color(1f, 0.87f, 0.56f) : new Color(0.36f, 0.19f, 0f));
            InlinePad = new GUIStyle { padding = new RectOffset(10, 0, 3, 3), margin = new RectOffset(0, 0, 4, 2) };
            BandOnWords = Coloured(new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter }, Dark ? Color.white : new Color(0.05f, 0.3f, 0.12f));
            BandOffWords = Coloured(new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter }, MutedColour);
        }

        static GUIStyle Coloured(GUIStyle from, Color colour)
        {
            var s = new GUIStyle(from);
            s.normal.textColor = s.hover.textColor = s.focused.textColor = s.active.textColor = colour;
            return s;
        }

        public static void Fill(Rect r, Color c)
        {
            if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(r, c);
        }

        public static void Disc(Rect r, Color c)
        {
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(r, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, r.width / 2f);
        }

        static void Ring(Rect r, Color c)
        {
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(r, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 1f, r.width / 2f);
        }

        /// <summary>カード：地の色と細い縁の箱。EndCard までに描いた物を乗せる。</summary>
        public static Rect BeginCard()
        {
            Build();
            cardDepth++;
            var r = EditorGUILayout.BeginVertical(CardPad);
            if (Event.current.type != EventType.Repaint) return r;
            GUI.DrawTexture(r, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, Card, 0f, 4f);
            GUI.DrawTexture(r, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, Edge, 1f, 4f);
            return r;
        }

        public static void EndCard()
        {
            cardDepth = Mathf.Max(0, cardDepth - 1);
            EditorGUILayout.EndVertical();
        }

        /// <summary>カードの見出し：太字の言葉と、長い説明がある時は「?」。</summary>
        public static void Heading(string words, string help)
        {
            Build();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(words, EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
            if (!string.IsNullOrEmpty(help)) Help(help);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>カードの上の注意。種類は左の帯と文字の色で見せる（Unity の HelpBox の代わり。HelpBox の文は、与えられた幅で並べられないため）。</summary>
        public static void Notice(string words, MessageType kind)
        {
            Build();
            var style = kind == MessageType.Warning ? WarnBody : kind == MessageType.Error ? BadBody : Body;
            var bar = kind == MessageType.Warning ? WarnColour : kind == MessageType.Error ? BadColour : Accent;
            /* カードの中では、自分の箱を付けず、帯と文だけにする（箱の中の箱は重く見える） */
            if (cardDepth > 0)
            {
                var inner = EditorGUILayout.BeginVertical(InlinePad);
                Text(words, style);
                EditorGUILayout.EndVertical();
                if (kind != MessageType.None) Fill(new Rect(inner.x, inner.y + 1f, 3f, inner.height - 2f), bar);
                return;
            }
            var r = BeginCard();
            Text(words, style);
            EndCard();
            if (kind == MessageType.None) return;
            Fill(new Rect(r.x + 1f, r.y + 1f, 3f, r.height - 2f), bar);
        }

        /// <summary>絵の入る所の空の枠（一覧の名前の位置をそろえる）。</summary>
        public static void Frame(Rect r)
        {
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(r, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, Edge, 1f, 3f);
        }

        /// <summary>地の付いた札の上の短い言葉（アバターを選んでいない時のアバターの行の知らせ。橙の文字だけだと、明るいテーマで読みにくい）。</summary>
        public static void Chip(string words, string help)
        {
            Build();
            var size = ChipWords.CalcSize(new GUIContent(words));
            var r = GUILayoutUtility.GetRect(size.x + 14f, 18f, GUILayout.Width(size.x + 14f), GUILayout.Height(18f));
            r.y += 1f;
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(r, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, ChipGround, 0f, 4f);
            GUI.Label(r, new GUIContent(words, help), ChipWords);
        }

        /// <summary>商品がアバターに導入済みかを、幅いっぱいの帯で出す（文字だけでは、ひと目で分からない）。
        /// 緑に「● 導入済み」、灰に「○ 未導入」。色は窓が決める。</summary>
        public static void InstallStatus(bool installed)
        {
            Build();
            var r = GUILayoutUtility.GetRect(0f, 26f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(r, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, installed ? BandOn : BandOff, 0f, 3f);
            GUI.Label(r, (installed ? "● " : "○ ") + NekoLabText.L(installed ? "hub.on" : "hub.off"), installed ? BandOnWords : BandOffWords);
            GUILayout.Space(4f);
        }

        static Color BandOn => Dark ? new Color(0.20f, 0.50f, 0.32f) : new Color(0.62f, 0.84f, 0.66f);
        static Color BandOff => Dark ? Grey(0.33f) : Grey(0.79f);

        /// <summary>16:9 の枠の中の商品の絵。枠いっぱいに描くので、Unity が取り込みの時に大きさを変えた絵
        /// （初めの設定では2の累乗にする）も、元の形で見える。</summary>
        public static void Picture(Rect r, Texture picture)
        {
            if (picture == null || Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(r, picture, ScaleMode.StretchToFill, true, 0f, Color.white, 0f, 3f);
        }

        /// <summary>ページの主な操作：幅いっぱいのボタン。どのページでも同じ高さで、太字。</summary>
        public static bool MainButton(string words)
        {
            Build();
            if (MainButtonStyle == null) MainButtonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            GUILayout.Space(2f);
            return GUILayout.Button(words, MainButtonStyle, GUILayout.Height(28f));
        }

        static GUIStyle MainButtonStyle;

        /// <summary>入らない時は、幅に合わせて切り、最後を「…」にした言葉。</summary>
        public static string Fit(string words, GUIStyle style, float width)
        {
            if (string.IsNullOrEmpty(words) || style.CalcSize(new GUIContent(words)).x <= width) return words;
            for (var n = words.Length - 1; n > 0; n--)
            {
                var cut = words.Substring(0, n).TrimEnd() + "…";
                if (style.CalcSize(new GUIContent(cut)).x <= width) return cut;
            }
            return "…";
        }

        /// <summary>小さな「?」。マウスを乗せると言葉が出る（長い説明は、ページに出さずにここへ）。</summary>
        public static void Help(string words)
        {
            Build();
            var r = GUILayoutUtility.GetRect(14f, 18f, GUILayout.Width(14f), GUILayout.Height(18f));
            var mark = new Rect(r.x, r.y + 3f, 13f, 13f);
            Ring(mark, MutedColour);
            GUI.Label(mark, new GUIContent("?", words), Mark);
        }

        /// <summary>丸の中の数字（ホームの段階）。</summary>
        public static void Step(int number, Color colour)
        {
            Build();
            var r = GUILayoutUtility.GetRect(16f, 18f, GUILayout.Width(16f), GUILayout.Height(18f));
            var disc = new Rect(r.x, r.y + 1f, 16f, 16f);
            Disc(disc, colour);
            GUI.Label(disc, number.ToString(), !Dark && colour == StepOff ? NumberDark : Number);
        }

        /// <summary>
        /// 置く場所と同じ幅で折り返す文（それより広げない）。高さは、前に描いた時の幅で決め（初めての時は見込みの幅）、
        /// 幅が変わったら窓を描き直す。
        /// </summary>
        public static void Text(GUIContent content, GUIStyle style)
        {
            var w = Measured(out var i);
            var r = GUILayoutUtility.GetRect(0f, Mathf.Ceil(style.CalcHeight(content, w)), style, GUILayout.ExpandWidth(true));
            Measure(i, r);
            GUI.Label(r, content, style);
        }

        public static void Text(string words, GUIStyle style) => Text(new GUIContent(Joined(words)), style);

        /// <summary>
        /// 日本語の隣の空白を、改行しない空白にした言葉。Unity は空白で先に改行するので、英単語の後に空白と日本語が続くと
        /// （「NekoLAB の…」）、英単語だけの行ができてしまうため（商品の言葉にも、そういう空白が残っていることがある）。英語の文はそのまま。
        /// </summary>
        public static string Joined(string words)
        {
            if (string.IsNullOrEmpty(words) || words.IndexOf(' ') < 0) return words;
            var c = words.ToCharArray();
            for (var k = 0; k < c.Length; k++)
                if (c[k] == ' ' && ((k > 0 && Japanese(c[k - 1])) || (k + 1 < c.Length && Japanese(c[k + 1])))) c[k] = ' ';
            return new string(c);
        }

        /* かな・漢字・全角の記号と文字 */
        static bool Japanese(char ch) => (ch >= '　' && ch <= 'ヿ') || (ch >= '㐀' && ch <= '鿿') || (ch >= '＀' && ch <= '￯');

        /// <summary>同じ物を、選んで手で写せる文として出す。</summary>
        public static void Selectable(string words, GUIStyle style)
        {
            var w = Measured(out var i);
            var r = GUILayoutUtility.GetRect(0f, Mathf.Ceil(style.CalcHeight(new GUIContent(words), w)) + 2f, GUILayout.ExpandWidth(true));
            Measure(i, r);
            EditorGUI.SelectableLabel(r, words, style);
        }

        /// <summary>今描いている場所の幅（前に測った値）。入るか分からない行を並べる時に使う。</summary>
        public static float Width()
        {
            var w = Measured(out var i);
            Measure(i, GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true)));
            return w;
        }

        static float Measured(out int i)
        {
            Build();
            if (!inPass) { i = -1; return Mathf.Max(80f, EditorGUIUtility.currentViewWidth - 40f); }
            i = next++;
            while (widths.Count <= i) widths.Add(0f);
            return widths[i] > 1f ? widths[i] : guess;
        }

        static void Measure(int i, Rect r)
        {
            if (i < 0 || Event.current.type != EventType.Repaint || Mathf.Abs(r.width - widths[i]) < 0.5f) return;
            widths[i] = r.width;
            if (owner != null) owner.Repaint();
        }
    }
}
