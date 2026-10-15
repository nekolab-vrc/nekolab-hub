using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace NekoLab.Hub
{
    /// <summary>
    /// NekoLAB の窓：全部の NekoLAB の商品で1つの窓。メニューの NekoLAB の下に出す。プロジェクトの中の商品を見つけて、
    /// それだけを一覧に出し、それぞれがアバターに導入済みかを見せ、押すと導入のページへ進む。窓はどの商品も知らない：
    /// 名札を読み、各商品が出すページ（NekoLabProductPage）を見せるだけ。ネットにつながらず、プロジェクトの外を読み書きしない。
    /// </summary>
    public class NekoLabHub : EditorWindow
    {
        public const string ToolVersion = "1.0.0";

        List<NekoLabNameplate> plates = new List<NekoLabNameplate>();
        readonly Dictionary<string, NekoLabProductPage> pages = new Dictionary<string, NekoLabProductPage>();
        string selected; /* （null：ホーム） */
        int tab;
        Vector2 listScroll, pageScroll;
        VRCAvatarDescriptor avatar;
        NekoLabBits.Tally tally;
        double tallyAt;
        string supportText;
        double supportAt;
        bool supportOpen;

        [MenuItem("NekoLAB/はじめに (Start)", false, 0)]
        static void OpenMenu() => Open(null);

        /// <summary>窓を開く。商品の id を渡すと、その商品のページで開く。</summary>
        public static NekoLabHub Open(string productId)
        {
            var w = GetWindow<NekoLabHub>();
            w.titleContent = new GUIContent("NekoLAB");
            w.minSize = new Vector2(640f, 560f);
            w.wantsMouseMove = true; /* （一覧の行は、マウスが乗ると色が付く） */
            w.Refresh();
            if (productId != null) w.Select(productId);
            w.Show();
            return w;
        }

        public VRCAvatarDescriptor Avatar
        {
            get => avatar;
            set { if (value != avatar) { avatar = value; tally = null; } }
        }

        /// <summary>今見せているページのタブ（商品のページが変えてもよい。例：「導入へ進む」）。</summary>
        public int Tab { get => tab; set => tab = value; }
        public string Selected => selected;
        public IReadOnlyList<NekoLabNameplate> Plates => plates;
        public NekoLabProductPage Page(string id) => id != null && pages.TryGetValue(id, out var p) ? p : null;

        public void Select(string id)
        {
            selected = plates.Any(p => p.id == id) ? id : null;
            var page = Page(selected);
            tab = 0;
            /* （商品のコードが例外を出しても、窓は開く） */
            if (page != null)
                try { tab = Mathf.Clamp(page.FirstTab(this), 0, Mathf.Max(0, (page.TabNames?.Length ?? 1) - 1)); }
                catch (System.Exception e) { Debug.LogWarning("NekoLAB: " + page.GetType().FullName + ": " + e.GetType().Name); }
            pageScroll = Vector2.zero;
            SceneView.RepaintAll();
        }

        void OnEnable()
        {
            Refresh();
            PickAvatar();
            SceneView.duringSceneGui += OnScene;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnScene;
            foreach (var p in pages.Values) p.Close();
        }

        void OnProjectChange() { Refresh(); Repaint(); }
        void OnHierarchyChange() { ForgetGoneAvatar(); if (avatar == null) PickAvatar(); tally = null; Repaint(); }

        /* シーンから消えたアバターは、Unity では null でも、欄には「Missing」と出たまま残る。
           ここで忘れて、欄を None に、アバターの行を「選んでください」に戻す */
        void ForgetGoneAvatar()
        {
            if (!ReferenceEquals(avatar, null) && avatar == null) { avatar = null; tally = null; }
        }
        void OnSelectionChange()
        {
            var picked = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>() : null;
            if (picked != null && picked != avatar) { Avatar = picked; Repaint(); }
        }

        /* 商品の名札と、商品のコードが出すページ（NekoLabProductPage を継いだ型の全部） */
        void Refresh()
        {
            plates = NekoLabNameplate.FindAll();
            foreach (var type in TypeCache.GetTypesDerivedFrom<NekoLabProductPage>())
            {
                if (type.IsAbstract) continue;
                NekoLabProductPage page = null;
                string id = null;
                /* （作れないページや Id のないページは外す。ほかの商品は出る） */
                try { page = (NekoLabProductPage)System.Activator.CreateInstance(type); id = page.Id; } catch { }
                if (!string.IsNullOrEmpty(id) && !pages.ContainsKey(id)) pages[id] = page;
            }
            if (selected != null && plates.All(p => p.id != selected)) selected = null;
            tally = null;
            pictures.Clear();
        }

        /* 開いているシーンのアバター（1体だけの時。または選んでいる物） */
        void PickAvatar()
        {
            var picked = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>() : null;
            if (picked != null) { Avatar = picked; return; }
            var all = FindObjectsOfType<VRCAvatarDescriptor>();
            if (all.Length == 1) Avatar = all[0];
        }

        /// <summary>どのページも使うアバターの欄（窓は自分の行にも出している）。</summary>
        public void AvatarField()
        {
            Avatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(NekoLabText.L("hub.avatar"), avatar, typeof(VRCAvatarDescriptor), true);
            if (avatar == null) NekoLabLook.Text(NekoLabText.L("hub.noavatar"), NekoLabLook.Warn);
        }

        /// <summary>アバター全体の同期パラメーターの使用量のゲージ（数え直すのは、多くて1秒に2回）。</summary>
        public void BitsGauge()
        {
            if (tally == null || EditorApplication.timeSinceStartup - tallyAt > 0.5)
            {
                var products = plates.Select(p => (p.name, p.InstanceOn(avatar))).Where(x => x.Item2 != null).ToList();
                tally = NekoLabBits.Count(avatar, products);
                tallyAt = EditorApplication.timeSinceStartup;
            }
            NekoLabBits.DrawGauge(tally, avatar);
        }

        /// <summary>開発用の試しのため：今の数え。</summary>
        public NekoLabBits.Tally CountNow()
        {
            var products = plates.Select(p => (p.name, p.InstanceOn(avatar))).Where(x => x.Item2 != null).ToList();
            return tally = NekoLabBits.Count(avatar, products);
        }

        /// <summary>サポート情報の文（NekoLabSupport）：バージョンと商品だけ。個人の情報は入らない。</summary>
        public string SupportInfo() => NekoLabSupport.Info(plates, Page, avatar, this);

        /// <summary>どのページにも出せる、サポートのカード：何のための物かの1行（中身の説明は「?」の中）・コピーのボタン・
        /// コピーされる内容（たたんである）。</summary>
        public void SupportBlock()
        {
            NekoLabLook.BeginCard();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(NekoLabText.L("support.head"), EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
            NekoLabLook.Help(NekoLabText.L("support.about"));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            NekoLabLook.Text(NekoLabText.L("support.line"), NekoLabLook.SmallMuted);
            GUILayout.Space(4f);
            if (GUILayout.Button(NekoLabText.L("support.copy"), GUILayout.Height(24f)))
            {
                EditorGUIUtility.systemCopyBuffer = supportText = SupportInfo();
                supportAt = EditorApplication.timeSinceStartup;
                ShowNotification(new GUIContent(NekoLabText.L("support.copied")));
            }
            supportOpen = EditorGUILayout.Foldout(supportOpen, NekoLabText.L("support.show"), true);
            if (supportOpen)
            {
                /* （見せている間、作り直すのは多くて1秒に1回） */
                if (supportText == null || EditorApplication.timeSinceStartup - supportAt > 1.0)
                {
                    supportText = SupportInfo();
                    supportAt = EditorApplication.timeSinceStartup;
                }
                NekoLabLook.Selectable(supportText, new GUIStyle(EditorStyles.textArea) { wordWrap = true });
            }
            NekoLabLook.EndCard();
        }

        /* 窓の場所ごとに地の色を変えて、どこに何があるかをひと目で分かるようにする：上の帯（名前・バージョン・言語）・
           アバターの行・左の一覧・右のページ。それぞれを自分の領域で並べるので、ある場所の物がほかの場所を窓の外へ押し出さない */
        const float BarHeight = 26f, AvatarHeight = 28f, ListWidth = 196f; /* （一覧は、商品の小さな絵が入る幅） */

        void OnGUI()
        {
            var w = position.width;
            var h = position.height;
            NekoLabLook.BeginPass(this, w - ListWidth - 70f);
            if (Event.current.type == EventType.Layout) ForgetGoneAvatar();
            if (Event.current.type == EventType.MouseMove) Repaint();

            NekoLabLook.Fill(new Rect(0f, 0f, w, BarHeight), NekoLabLook.Bar);
            GUILayout.BeginArea(new Rect(10f, 4f, w - 20f, BarHeight - 6f));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("NekoLAB", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
            GUILayout.Label("v" + ToolVersion, NekoLabLook.SmallMutedLine, GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            var lang = GUILayout.Toolbar(NekoLabText.Lang, new[] { "日本語", "English" }, EditorStyles.miniButton, GUILayout.Width(120f));
            if (lang != NekoLabText.Lang) { NekoLabText.Lang = lang; tally = null; supportText = null; }
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();

            NekoLabLook.Fill(new Rect(0f, BarHeight, w, AvatarHeight), NekoLabLook.Strip);
            NekoLabLook.Fill(new Rect(0f, BarHeight + AvatarHeight - 1f, w, 1f), NekoLabLook.Edge);
            GUILayout.BeginArea(new Rect(10f, BarHeight + 5f, w - 20f, AvatarHeight - 8f));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(NekoLabText.L("hub.avatar"), GUILayout.Width(64f));
            /* （窓の幅いっぱいには広げない。選んでいない時の知らせが、欄のすぐ横に来るように） */
            Avatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(avatar, typeof(VRCAvatarDescriptor), true, GUILayout.MinWidth(120f), GUILayout.MaxWidth(320f));
            if (avatar == null)
            {
                GUILayout.Space(6f);
                /* （地の付いた札に。説明はマウスを乗せると出す。橙の文字だけだと、明るいテーマで読みにくい） */
                NekoLabLook.Chip(NekoLabText.L("hub.noavatar.short"), NekoLabText.L("hub.noavatar"));
                NekoLabLook.Help(NekoLabText.L("hub.noavatar"));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();

            var top = BarHeight + AvatarHeight;
            NekoLabLook.Fill(new Rect(0f, top, ListWidth, h - top), NekoLabLook.Side);
            NekoLabLook.Fill(new Rect(ListWidth, top, 1f, h - top), NekoLabLook.Edge);
            GUILayout.BeginArea(new Rect(6f, top + 6f, ListWidth - 12f, h - top - 8f));
            List();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(ListWidth + 11f, top + 8f, w - ListWidth - 19f, h - top - 10f));
            /* （Unity のピッカーのように描くのを途中で止めるページがあっても、並べ方は Unity に任せ、描く回を終えるだけにする） */
            try
            {
                if (selected == null) Home();
                else ProductPage();
            }
            finally { NekoLabLook.EndPass(); }
            GUILayout.EndArea();
        }

        /* 一覧：ホーム、その下に見つけた商品（それぞれアバターに導入済みか） */
        void List()
        {
            listScroll = GUILayout.BeginScrollView(listScroll, GUIStyle.none, GUI.skin.verticalScrollbar);
            if (Row(selected == null, NekoLabText.L("hub.home"), null, null, NekoLabLook.Off, 26f)) Select(null);
            GUILayout.Space(10f);
            GUILayout.Label(NekoLabText.L("hub.products"), NekoLabLook.SmallMutedLine);
            if (plates.Count == 0) NekoLabLook.Text(NekoLabText.L("hub.none"), NekoLabLook.SmallMuted);
            /* （どれかの商品に絵があれば、絵のない商品にも同じ大きさの空の枠を置き、名前の始まりをそろえる） */
            var framed = plates.Any(p => Image(p) != null);
            foreach (var p in plates)
            {
                /* 名前の下にバージョン。導入済みかは、アバターを選んでいる時だけ出す（選んでいない時は、アバターの行がそう知らせる） */
                var on = avatar != null && p.InstanceOn(avatar) != null;
                var status = avatar == null ? null : NekoLabText.L(on ? "hub.on" : "hub.off");
                if (Row(selected == p.id, p.name, p.version, status, on ? NekoLabLook.Good : NekoLabLook.Off, 40f, Image(p), p.summary?.Current, framed)) Select(p.id);
                GUILayout.Space(2f);
            }
            GUILayout.EndScrollView();
        }

        /* 一覧の1行：選んでいる時とマウスが乗った時は色を付ける。商品の小さな絵（ほかの商品に絵があれば、ない商品は空の枠）。
           名前は残りの幅いっぱいに（入らない時は「…」で切り、マウスを乗せると全部と、何の商品かを出す）。その下にバージョンと、点の付いた状態 */
        bool Row(bool chosen, string name, string version, string status, Color dot, float height, Texture picture = null, string about = null, bool framed = false)
        {
            var r = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            var under = r.Contains(Event.current.mousePosition);
            if (chosen) NekoLabLook.Fill(r, NekoLabLook.Accent);
            else if (under) NekoLabLook.Fill(r, NekoLabLook.Hover);
            EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
            var left = r.x + 8f;
            if (picture != null || framed)
            {
                var frame = new Rect(r.x + 5f, r.y + (height - 25f) / 2f, 44f, 25f);
                if (picture != null) NekoLabLook.Picture(frame, picture);
                else NekoLabLook.Frame(frame);
                left = r.x + 55f;
            }
            var nameStyle = chosen ? NekoLabLook.RowNameOn : NekoLabLook.RowName;
            var smallStyle = chosen ? NekoLabLook.RowSmallOn : NekoLabLook.SmallMutedLine;
            var second = version != null;
            var nameRect = new Rect(left, r.y + (second ? 3f : (height - 18f) / 2f), r.xMax - left - 4f, 18f);
            var shown = NekoLabLook.Fit(name, nameStyle, nameRect.width);
            var tip = (shown != name ? name : "") + (!string.IsNullOrEmpty(about) ? (shown != name ? "\n" : "") + about : "");
            GUI.Label(nameRect, new GUIContent(shown, tip.Length > 0 ? tip : null), nameStyle);
            if (second)
            {
                var x = left;
                var line = new GUIContent(version);
                var versionWidth = smallStyle.CalcSize(line).x;
                GUI.Label(new Rect(x, r.y + 20f, versionWidth, 16f), line, smallStyle);
                if (status != null)
                {
                    x += versionWidth + 6f;
                    NekoLabLook.Disc(new Rect(x, r.y + 25f, 7f, 7f), dot);
                    x += 11f;
                    GUI.Label(new Rect(x, r.y + 20f, Mathf.Max(0f, r.xMax - x - 4f), 16f), status, smallStyle);
                }
            }
            return GUI.Button(r, GUIContent.none, GUIStyle.none) && !chosen;
        }

        /* ホーム：使い方（段階。済んだ物は緑）・商品がない時の知らせ・同期パラメーター・サポート */
        void Home()
        {
            pageScroll = GUILayout.BeginScrollView(pageScroll, GUIStyle.none, GUI.skin.verticalScrollbar);
            Steps();
            if (plates.Count == 0)
            {
                NekoLabLook.BeginCard();
                GUILayout.Label(NekoLabText.L("hub.empty.head"), EditorStyles.boldLabel);
                NekoLabLook.Text(NekoLabText.L("hub.empty"), NekoLabLook.SmallMuted);
                NekoLabLook.EndCard();
            }
            BitsGauge();
            SupportBlock();
            EditorGUILayout.BeginHorizontal();
            var autoWords = NekoLabText.L("hub.autoopen");
            var auto = EditorGUILayout.ToggleLeft(autoWords, NekoLabAutoOpen.Enabled, EditorStyles.miniLabel,
                GUILayout.Width(EditorStyles.miniLabel.CalcSize(new GUIContent(autoWords)).x + 20f));
            if (auto != NekoLabAutoOpen.Enabled) NekoLabAutoOpen.Enabled = auto;
            GUILayout.FlexibleSpace();
            GUILayout.Label(new GUIContent(NekoLabText.L("hub.safe.short"), NekoLabText.L("hub.safe")), NekoLabLook.SmallMutedLine, GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        /* 1 アバターを選ぶ、2 商品を選ぶ、3 導入する：選んだアバターに商品が導入されている所まで「済み」（緑）。
           入る時は横に並べ、入らない時は縦に並べる */
        void Steps()
        {
            NekoLabLook.BeginCard();
            GUILayout.Label(NekoLabText.L("steps.head"), EditorStyles.boldLabel);
            var picked = avatar != null;
            var installed = picked && plates.Any(p => p.InstanceOn(avatar) != null);
            var done = new[] { picked, installed, installed };
            var now = !picked ? 0 : !installed ? 1 : -1;
            var words = new[] { NekoLabText.L("steps.1"), NekoLabText.L("steps.2"), NekoLabText.L("steps.3") };
            var need = words.Sum(x => NekoLabLook.Line.CalcSize(new GUIContent(x)).x + 22f) + 2f * 18f;
            var row = NekoLabLook.Width() >= need;
            if (row) EditorGUILayout.BeginHorizontal();
            for (var k = 0; k < 3; k++)
            {
                if (!row) EditorGUILayout.BeginHorizontal();
                else if (k > 0) GUILayout.Label("›", NekoLabLook.MutedLine, GUILayout.Width(12f));
                NekoLabLook.Step(k + 1, done[k] ? NekoLabLook.Good : k == now ? NekoLabLook.Accent : NekoLabLook.StepOff);
                GUILayout.Label(words[k], done[k] ? NekoLabLook.GoodLine : k == now ? NekoLabLook.Line : NekoLabLook.MutedLine, GUILayout.ExpandWidth(false));
                if (!row) { GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal(); }
            }
            if (row) { GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal(); }
            NekoLabLook.EndCard();
        }

        /* 商品のページ：名前、その下にタブと中身（中身は商品が描く） */
        void ProductPage()
        {
            var page = Page(selected);
            var plate = plates.FirstOrDefault(x => x.id == selected);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(plate != null ? plate.name : selected, EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
            if (plate != null) GUILayout.Label(plate.version, NekoLabLook.SmallMutedLine, GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            /* 新しい窓が要る商品を先に知らせる：その時はページが組み立てに入らない（ページのアセンブリは Hub のバージョンで守られている）ので、
               「商品をもう一度インポートする」では直らない */
            var old = plate != null && !string.IsNullOrEmpty(plate.toolVersion) && NekoLabVersion.Compare(plate.toolVersion, ToolVersion) > 0;
            if (old) Notice(string.Format(NekoLabText.L("hub.oldtool"), plate.toolVersion));
            string[] names = null;
            if (page != null)
                try { names = page.TabNames; } catch { }
            if (page == null || names == null || names.Length == 0) { if (!old) Notice(NekoLabText.L("hub.nopage")); return; }
            tab = Mathf.Clamp(tab, 0, names.Length - 1);
            /* （タブが1つなら、タブの帯は出さない。上の名前で、どの商品のページか分かる） */
            if (names.Length > 1)
            {
                var was = tab;
                tab = GUILayout.Toolbar(tab, names);
                if (tab != was) SceneView.RepaintAll();
            }
            EditorGUILayout.Space(4f);
            pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
            Picture(plate);
            page.DrawTab(tab, this);
            EditorGUILayout.EndScrollView();
        }

        /* 商品の絵と、何の商品かの1行（名札の image と summary）。どのタブでも上に、どの商品でも同じ形で窓が描く
           （いくつかの商品が入っている時に、見分けられるように）。横は 480 px まで・16:9。ページと一緒にスクロールする */
        void Picture(NekoLabNameplate plate)
        {
            var picture = Image(plate);
            var line = plate?.summary?.Current;
            if (picture != null)
            {
                var w = Mathf.Min(480f, NekoLabLook.Width());
                var r = GUILayoutUtility.GetRect(w, w * 9f / 16f, GUILayout.Width(w), GUILayout.Height(w * 9f / 16f));
                NekoLabLook.Picture(r, picture);
            }
            if (!string.IsNullOrEmpty(line)) NekoLabLook.Text(line, NekoLabLook.SmallMuted);
            if (picture != null || !string.IsNullOrEmpty(line)) GUILayout.Space(6f);
        }

        readonly Dictionary<string, Texture2D> pictures = new Dictionary<string, Texture2D>();

        /* 名札の絵。1回だけ読み込む（プロジェクトが変わったら読み直す）。プロジェクトの Assets の中の場所だけ読む
           （窓はプロジェクトの外を読まない） */
        Texture2D Image(NekoLabNameplate plate)
        {
            var path = plate?.image;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", System.StringComparison.Ordinal)) return null;
            if (!pictures.TryGetValue(path, out var picture)) pictures[path] = picture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return picture;
        }

        static void Notice(string words) => NekoLabLook.Notice(words, MessageType.Warning);

        void OnScene(SceneView view)
        {
            var page = Page(selected);
            if (page != null) page.OnSceneGUI(view, tab, this);
        }

    }
}
