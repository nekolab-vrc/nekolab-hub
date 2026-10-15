using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace NekoLab.Hub
{
    /// <summary>
    /// NekoLAB の商品が、窓に見つけてもらうために自分のフォルダに置く物：商品のプレハブの横の NekoLAB_Nameplate.json（名札）。
    /// 窓は、プロジェクトの中に見つけた名札の商品だけを一覧に出す。商品のほかのファイルは読まない。
    /// </summary>
    [System.Serializable]
    public class NekoLabNameplate
    {
        public string id = "", name = "", maker = "", version = "", page = "";
        public string[] prefabs = new string[0];
        public string[] packages = new string[0];
        public int syncedBits;
        public string toolVersion = ""; /* （この商品が動く、いちばん古い NekoLAB の窓のバージョン。空：どれでも） */
        /// <summary>商品の絵。商品のフォルダの中の場所（16:9・横 960 px くらい）。窓が商品のタブの上に出し、一覧の名前の横にも小さく出す
        /// （どの商品か、ひと目で分かるように）。空：絵なし。</summary>
        public string image = "";
        /// <summary>何の商品かを1行で（日本語と英語）。絵の下に出す。</summary>
        public Words summary = new Words();
        [System.NonSerialized] public string path;

        /// <summary>窓の2つの言語で書いた1行。</summary>
        [System.Serializable]
        public class Words
        {
            public string ja = "", en = "";

            /// <summary>今の言語の1行（空なら、もう一方の言語の1行）。</summary>
            public string Current => NekoLabText.Lang == 0 ? (string.IsNullOrEmpty(ja) ? en : ja) : (string.IsNullOrEmpty(en) ? ja : en);
        }

        public const string FileName = "NekoLAB_Nameplate";

        /* プロジェクトの中の全部の名札：Assets/NekoLAB の下の商品のフォルダと、パッケージの中 */
        public static List<NekoLabNameplate> FindAll()
        {
            var found = new List<NekoLabNameplate>();
            foreach (var guid in AssetDatabase.FindAssets(FileName + " t:TextAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != FileName) continue;
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (text == null) continue;
                NekoLabNameplate plate = null;
                try { plate = JsonUtility.FromJson<NekoLabNameplate>(text.text); } catch { }
                if (plate == null || string.IsNullOrEmpty(plate.id)) continue;
                plate.path = path;
                if (found.All(p => p.id != plate.id)) found.Add(plate);
            }
            return found.OrderBy(p => p.name).ToList();
        }

        /* アバターに置かれた、この商品：商品のプレハブのどれかのインスタンス（いちばん外側の物） */
        public GameObject InstanceOn(VRCAvatarDescriptor avatar)
        {
            if (avatar == null || prefabs == null) return null;
            foreach (var t in avatar.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;
                var source = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject));
                if (prefabs.Contains(source)) return t.gameObject;
            }
            return null;
        }
    }

    /// <summary>
    /// 商品が NekoLAB の窓に出す、自分のページ。窓が持つのは枠だけ（一覧・アバター・言語・同期パラメーター）で、
    /// タブ（導入・色など）は各商品のコードが出す。窓がプロジェクトの型の中から見つけ、名札と id で組にする。
    /// </summary>
    public abstract class NekoLabProductPage
    {
        /// <summary>名札の id。</summary>
        public abstract string Id { get; }
        /// <summary>タブの名前（今の言語で。NekoLabText.Lang）。</summary>
        public abstract string[] TabNames { get; }
        /// <summary>一覧で商品を押した時に開くタブ。アバターに導入していない間は、導入のタブにする。</summary>
        public virtual int FirstTab(NekoLabHub hub) => 0;
        public abstract void DrawTab(int tab, NekoLabHub hub);
        /// <summary>タブを開いている間、Scene ビューの上に描く（アバターのどこに付くかなど）。</summary>
        public virtual void OnSceneGUI(SceneView view, int tab, NekoLabHub hub) { }
        public virtual void Close() { }
        /// <summary>
        /// サポート情報（NekoLabSupport）に足す、この商品の行。作者が質問の時に知りたい事を書く（例：どの付け方にしたか）。
        /// PC の場所や名前は書かない（個人の情報に見える行は外される）。hub は null のことがある。
        /// </summary>
        public virtual string[] SupportLines(NekoLabHub hub) => null;
    }
}
