using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;

namespace NekoLab.Hub
{
    /// <summary>
    /// NekoLAB の商品が守る形の点検：プロジェクトの中の全部の名札・商品のページ・この道具を使うアセンブリ。
    /// 商品の開発用のプロジェクト向け：組み立ての自動チェックから Report() を名前で呼び（このアセンブリは参照しない）、
    /// 「NG」で始まる行があれば止める。メニューはない（買った人は使わない）。
    /// </summary>
    public static class NekoLabCheck
    {
        /// <summary>この道具のパッケージの名前（商品の Version Defines に書く名前）。</summary>
        public const string PackageName = "io.github.nekolab-vrc.hub";
        /// <summary>この道具がある時だけ、商品のページのアセンブリが組み立てられるようにするシンボル。</summary>
        public const string Define = "NEKOLAB_HUB";
        const string AssemblyName = "NekoLab.Hub.Editor";

        /// <summary>
        /// 点検の全部を文で返す：1行目は "NG 数" か "OK"。続けて問題ごとに1行（"NG [K-..] 場所: 中身"）、
        /// 目で見る物（"LOOK [K-..] 場所: 中身"。問題としては数えない）、点検した商品（"- id 名前 バージョン ..."）。
        /// 名前で呼ぶ：Type.GetType("NekoLab.Hub.NekoLabCheck, NekoLab.Hub.Editor").GetMethod("Report").Invoke(null, null)
        /// </summary>
        public static string Report()
        {
            var checkedOnes = new List<string>();
            var looks = new List<string>();
            var ng = Problems(checkedOnes, looks);
            return (ng.Count > 0 ? "NG " + ng.Count : "OK") + " (NekoLAB Hub " + NekoLabHub.ToolVersion + ")"
                + (ng.Count > 0 ? "\n" + string.Join("\n", ng) : "") + (looks.Count > 0 ? "\n" + string.Join("\n", looks) : "")
                + (checkedOnes.Count > 0 ? "\n" + string.Join("\n", checkedOnes) : "");
        }

        /// <summary>見つけた問題（"NG [K-..] 場所: 中身"。空なら問題なし）。点検した商品は checkedOnes に、
        /// 目で見る物は looks に入れる。</summary>
        public static List<string> Problems(List<string> checkedOnes = null, List<string> looks = null)
        {
            var ng = new List<string>();
            void No(string code, string where, string what) => ng.Add("NG [" + code + "] " + where + ": " + what);
            void Look(string code, string where, string what) => looks?.Add("LOOK [" + code + "] " + where + ": " + what);

            /* 名札：その名前のファイルを全部ここで読む（FindAll は読めない物を外すため） */
            var plates = new List<NekoLabNameplate>();
            foreach (var guid in AssetDatabase.FindAssets(NekoLabNameplate.FileName + " t:TextAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != NekoLabNameplate.FileName) continue;
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path)?.text ?? "";
                NekoLabNameplate plate = null;
                try { plate = JsonUtility.FromJson<NekoLabNameplate>(text); } catch { }
                if (plate == null) { No("K-01", path, "名札を JSON として読めない / not readable as JSON"); continue; }
                plate.path = path;
                CheckPlate(plate, text, No, Look);
                if (!string.IsNullOrEmpty(plate.id)) plates.Add(plate);
            }
            foreach (var same in plates.GroupBy(p => p.id).Where(g => g.Count() > 1))
                No("K-03", same.Key, "同じ id の名札が " + same.Count() + " つある / several nameplates share this id: " + string.Join(", ", same.Select(p => p.path)));

            /* ページ：それぞれの名札とアセンブリと一緒に見る */
            var pages = TypeCache.GetTypesDerivedFrom<NekoLabProductPage>().Where(t => !t.IsAbstract).ToList();
            var pageIds = new Dictionary<string, Type>();
            foreach (var type in pages)
            {
                string id = null;
                try { id = ((NekoLabProductPage)Activator.CreateInstance(type)).Id; }
                catch (Exception e) { No("K-07", type.FullName, "ページを作れない / the page cannot be made (" + e.GetType().Name + ")"); continue; }
                if (pageIds.TryGetValue(id ?? "", out var other)) { No("K-07", id, "同じ Id のページが2つある / two pages share this Id: " + other.FullName + ", " + type.FullName); continue; }
                pageIds[id ?? ""] = type;
                var plate = plates.FirstOrDefault(p => p.id == id);
                if (plate == null) { No("K-07", type.FullName, "Id \"" + id + "\" の名札がない / no nameplate has this page's Id"); continue; }
                var asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(type.Assembly.GetName().Name);
                if (string.IsNullOrEmpty(asmdef)) { No("K-08", type.FullName, "ページが自分の asmdef に入っていない / the page is not in an assembly definition of its own"); continue; }
                CheckGuard(asmdef, plate.toolVersion, No);
                CheckSource(asmdef, plate.toolVersion, No, Look);
            }
            foreach (var plate in plates.Where(p => !pageIds.ContainsKey(p.id)))
                No("K-07", plate.id, "この商品のページが見つからない（組み立てのエラーか、toolVersion がこの Hub より新しい） / no page (a compile error, or toolVersion newer than this Hub)");

            /* この道具を使う、ほかの全部のアセンブリ：ページと同じ守りが要る（ないと、この道具のない買った人がアップロードできない） */
            var hubAsmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(AssemblyName);
            var hubGuid = string.IsNullOrEmpty(hubAsmdef) ? null : AssetDatabase.AssetPathToGUID(hubAsmdef);
            var pageAsmdefs = new HashSet<string>(pageIds.Values.Select(t => CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(t.Assembly.GetName().Name)).Where(p => p != null));
            foreach (var guid in AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (pageAsmdefs.Contains(path)) continue;
                var def = ReadAsmdef(path);
                if (def != null && def.references.Any(r => r == AssemblyName || hubGuid != null && r == "GUID:" + hubGuid)) CheckGuard(path, null, No);
            }

            /* この道具のファイルが、公開した時のままか */
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(NekoLabCheck).Assembly);
            if (info != null && Inside(info.resolvedPath))
                foreach (var (path, what) in FileProblems(info.resolvedPath)) No("K-13", "Packages/" + PackageName + "/" + path, what);

            if (checkedOnes != null)
                foreach (var plate in plates)
                    checkedOnes.Add("- " + plate.id + " " + plate.name + " " + plate.version + " (toolVersion " + plate.toolVersion + ", page "
                        + (pageIds.TryGetValue(plate.id, out var t) ? t.FullName : "-") + ", " + plate.path + ")");
            return ng;
        }

        /* K-02〜K-06・K-10・K-15：名札1つの中身 */
        static void CheckPlate(NekoLabNameplate plate, string text, Action<string, string, string> No, Action<string, string, string> Look)
        {
            CheckPicture(plate, No, Look);
            var at = plate.path;
            if (string.IsNullOrEmpty(plate.id)) { No("K-02", at, "id が空 / id is empty"); return; }
            if (!Regex.IsMatch(plate.id, "^[a-z0-9]+(-[a-z0-9]+)*$")) No("K-02", at, "id は小文字の英字・数字・「-」だけ / id: lowercase letters, digits and \"-\" only");
            if (string.IsNullOrEmpty(plate.name)) No("K-04", at, "name が空 / name is empty");
            if (string.IsNullOrEmpty(plate.maker)) No("K-04", at, "maker が空 / maker is empty");
            if (!NekoLabVersion.IsValid(plate.version)) No("K-04", at, "version が「1.2.3」の形でない / version is not like 1.2.3: \"" + plate.version + "\"");
            if (!NekoLabVersion.IsValid(plate.toolVersion)) No("K-05", at, "toolVersion が「1.2.3」の形でない / toolVersion is not like 1.2.3: \"" + plate.toolVersion + "\"");
            else if (NekoLabVersion.Compare(plate.toolVersion, NekoLabHub.ToolVersion) > 0)
                No("K-05", at, "toolVersion " + plate.toolVersion + " がこの Hub " + NekoLabHub.ToolVersion + " より新しい / newer than this Hub");
            if (!at.StartsWith("Assets/NekoLAB/", StringComparison.Ordinal)) No("K-11", at, "名札は Assets/NekoLAB/<商品名>/ に置く / keep the nameplate in Assets/NekoLAB/<product>/");
            if (plate.prefabs == null || plate.prefabs.Length == 0) No("K-06", at, "prefabs が空 / no prefab");
            else
                foreach (var prefab in plate.prefabs)
                    if (string.IsNullOrEmpty(prefab) || !prefab.StartsWith("Assets/", StringComparison.Ordinal) || AssetDatabase.LoadAssetAtPath<GameObject>(prefab) == null)
                        No("K-06", at, "プレハブがない / no prefab at \"" + prefab + "\"");
            if (plate.packages != null)
                foreach (var package in plate.packages)
                    if (string.IsNullOrEmpty(package) || !Regex.IsMatch(package, @"^[a-z0-9][a-z0-9._-]*( \S+)?$"))
                        No("K-04", at, "packages は「パッケージの名前 版」の形 / packages: \"<package name> <version>\": \"" + package + "\"");
            if (plate.syncedBits < 0 || plate.syncedBits > NekoLabBits.Limit) No("K-04", at, "syncedBits が 0〜256 でない / syncedBits not within 0-256: " + plate.syncedBits);
            /* 作った人の PC や個人のことを入れない（名札は、買った人みんなが読む） */
            foreach (var (pattern, what) in Personal)
                if (Regex.IsMatch(text, pattern)) No("K-10", at, "個人の情報に見える文字がある / looks personal: " + what);
        }

        /* K-15：名札の絵（ある時）。商品のフォルダの中（名札と同じ所）にあり、絵として読めるか（NG）。
           元のファイルが 16:9 で横 640 px 以上か（LOOK：窓は 16:9 の枠に描くので、ほかの形は伸びて見える） */
        static void CheckPicture(NekoLabNameplate plate, Action<string, string, string> No, Action<string, string, string> Look)
        {
            if (string.IsNullOrEmpty(plate.image)) return;
            var at = plate.path;
            var folder = (Path.GetDirectoryName(at) ?? "").Replace('\\', '/') + "/";
            if (!plate.image.StartsWith(folder, StringComparison.Ordinal))
            {
                No("K-15", at, "image は名札と同じ商品のフォルダの中に置く / keep the picture in the product's folder: \"" + plate.image + "\"");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(plate.image) == null)
            {
                No("K-15", at, "image の絵がない / no picture at \"" + plate.image + "\"");
                return;
            }
            if (AssetImporter.GetAtPath(plate.image) is TextureImporter importer)
            {
                importer.GetSourceTextureWidthAndHeight(out var w, out var h);
                if (h <= 0 || Mathf.Abs((float)w / h - 16f / 9f) > 0.06f || w < 640)
                    Look("K-15", plate.image, "絵は 16:9・横 640 px 以上に（今 " + w + " x " + h + "。窓は 16:9 の枠に描くので、ほかの形は伸びて見える） / make it 16:9, 640 px wide or more");
            }
        }

        /* （サポート情報が外す行も、これで決める：NekoLabSupport） */
        internal static bool LooksPersonal(string text) => !string.IsNullOrEmpty(text) && Personal.Any(p => Regex.IsMatch(text, p.pattern));

        static readonly (string pattern, string what)[] Personal =
        {
            (@"(?<![A-Za-z])[A-Za-z]:[\\/]", "PC の場所 / a path on a computer"),
            (@"(?i)[\\/](Users|home)[\\/]", "ユーザーのフォルダ / a user's folder"),
            (@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", "メールアドレス / an e-mail address"),
            (@"(?<![\d.])\d{1,3}(\.\d{1,3}){3}(?![\d.])", "IP アドレス / an IP address"),
        };

        /* K-08・K-09：この道具を使うアセンブリは、この道具がある時だけ組み立てられる形か：Define Constraints・
           Version Define（ページのアセンブリなら、範囲の下の端が名札の toolVersion と同じか）・Editor だけか */
        static void CheckGuard(string asmdef, string toolVersion, Action<string, string, string> No)
        {
            var code = toolVersion != null ? "K-08" : "K-09";
            var def = ReadAsmdef(asmdef);
            if (def == null) { No(code, asmdef, "asmdef を読めない / not readable"); return; }
            if (!def.defineConstraints.Contains(Define)) No(code, asmdef, "defineConstraints に " + Define + " がない（Hub がない時にエラーで止まる） / no " + Define + " constraint");
            var vd = def.versionDefines.FirstOrDefault(v => v.name == PackageName && v.define == Define);
            if (vd == null) No(code, asmdef, "versionDefines に " + PackageName + " → " + Define + " がない / no Version Define");
            else
            {
                var range = Regex.Match(vd.expression ?? "", @"^\[([^,\]]+),2\.0\.0\)$");
                if (!range.Success || !NekoLabVersion.IsValid(range.Groups[1].Value))
                    No(code, asmdef, "versionDefines の範囲は「[下の端,2.0.0)」 / the range is \"[<lowest>,2.0.0)\": \"" + vd.expression + "\"");
                else if (toolVersion != null && range.Groups[1].Value != toolVersion)
                    No(code, asmdef, "範囲の下の端 " + range.Groups[1].Value + " が名札の toolVersion " + toolVersion + " と違う / the lowest version differs from the nameplate's toolVersion");
            }
            if (def.includePlatforms.Length != 1 || def.includePlatforms[0] != "Editor")
                No(code, asmdef, "includePlatforms は Editor だけ（アップロードの時の組み立てで止まる） / Editor only, or the upload's build stops");
            if (toolVersion != null && def.autoReferenced) No(code, asmdef, "autoReferenced は false / autoReferenced: false");
        }

        /* K-12（目で見る）と K-14：ページのコードそのもの。窓の見た目に合わせるため、HelpBox や折り返す LabelField を
           使っていないか（狭い窓で日本語がはみ出す）。名札の toolVersion より新しい、この道具の物を使っていないか
           （守りの範囲には入るが、それより古い Hub の入った買った人のプロジェクトで、ページが組み立てられなくなる） */
        static void CheckSource(string asmdef, string toolVersion, Action<string, string, string> No, Action<string, string, string> Look)
        {
            var folder = Path.GetDirectoryName(asmdef)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder)) return;
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs", StringComparison.Ordinal)) continue;
                string code;
                try { code = WithoutComments(File.ReadAllText(path)); }
                catch { continue; }
                foreach (var (word, what) in LookFor)
                    if (code.Contains(word)) Look("K-12", path, what);
                var needs = NeedsHub(code);
                if (needs.version != null && NekoLabVersion.IsValid(toolVersion) && NekoLabVersion.Compare(toolVersion, needs.version) < 0)
                    No("K-14", path, needs.what + " は Hub " + needs.version + " から。名札の toolVersion " + toolVersion + " では、古い Hub の買った人のプロジェクトで組み立てのエラーになる（toolVersion と守りの下の端を " + needs.version
                        + " 以上に） / is in the Hub from " + needs.version + ": raise toolVersion and the guard's lowest version");
            }
        }

        static readonly (string word, string what)[] LookFor =
        {
            ("EditorGUILayout.HelpBox", "Unity の HelpBox → NekoLabUI.Notice に（狭い窓で日本語の文がはみ出す） / Unity's help box: use NekoLabUI.Notice"),
            ("wordWrappedLabel", "折り返す LabelField → NekoLabUI.Text に / a wrapped label: use NekoLabUI.Text"),
            ("wordWrappedMiniLabel", "折り返す小さい LabelField → NekoLabUI.SmallText に / a small wrapped label: use NekoLabUI.SmallText"),
        };

        /* ページのコードが使う、この道具の物と、それが入った最初のバージョン */
        static readonly (string pattern, string version, string what)[] Since =
        {
            (@"\bSupportLines\b", "0.3.0", "SupportLines"),
            (@"\bSupportBlock\b", "0.3.0", "hub.SupportBlock()"),
            (@"\bSupportInfo\b", "0.3.0", "hub.SupportInfo()"),
            (@"\bNekoLabUI\b", "1.0.0-rc.2", "NekoLabUI"),
            (@"\bNekoLabUI\s*\.\s*(InstallStatus|MainButton)\b", "1.0.0-rc.4", "NekoLabUI.InstallStatus / MainButton"),
        };

        /// <summary>ページのコードが使う、この道具の物のうち、いちばん新しい物の最初のバージョンと、その名前（なければ null）。</summary>
        internal static (string version, string what) NeedsHub(string code)
        {
            (string, string) newest = (null, null);
            foreach (var (pattern, version, what) in Since)
                if (Regex.IsMatch(code, pattern) && (newest.Item1 == null || NekoLabVersion.Compare(version, newest.Item1) > 0)) newest = (version, what);
            return newest;
        }

        /* （コメントの中に名前があるだけでは、使ったことにしない） */
        static string WithoutComments(string code) => Regex.Replace(code, @"/\*.*?\*/|//[^\n]*", "", RegexOptions.Singleline);

        /* K-13：この道具のファイルが、公開した時のままか（NekoLabFiles。公開のたびに作る）。商品のプロジェクトで
           書き換えない（直しても、買った人の Hub には入らない）・ファイルを足さない。.meta は比べない（Unity が書き直すことがある） */
        internal static List<(string path, string what)> FileProblems(string root)
        {
            var found = new List<(string, string)>();
            if (NekoLabFiles.Version != NekoLabHub.ToolVersion)
            {
                found.Add(("Editor/NekoLabFiles.cs", "ファイルの一覧の版 " + NekoLabFiles.Version + " が、この Hub の版と違う / the list of files is of another version"));
                return found;
            }
            var expected = new Dictionary<string, string>();
            for (var i = 0; i < NekoLabFiles.List.GetLength(0); i++) expected[NekoLabFiles.List[i, 0]] = NekoLabFiles.List[i, 1];
            foreach (var kv in expected)
            {
                var full = Path.Combine(root, kv.Key);
                if (!File.Exists(full)) found.Add((kv.Key, "Hub のファイルがない（ALCOM で Hub を入れ直す） / missing: reinstall the Hub with ALCOM"));
                else if (Sha256(full) != kv.Value) found.Add((kv.Key, "Hub のファイルが書き換えられている（Hub のファイルは直さない。ALCOM で入れ直す） / changed: never edit the Hub's files; reinstall it with ALCOM"));
            }
            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                var rel = file.Substring(root.Length).TrimStart('/', '\\').Replace('\\', '/');
                if (rel.EndsWith(".meta", StringComparison.Ordinal) || rel == "Editor/NekoLabFiles.cs" || expected.ContainsKey(rel)) continue;
                found.Add((rel, "Hub に無いファイルが足されている（Hub のフォルダに物を置かない） / a file the Hub does not have"));
            }
            return found;
        }

        static string Sha256(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        /* （プロジェクトの中のフォルダだけを読む。この道具はプロジェクトの外を読まない） */
        static bool Inside(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var project = Path.GetFullPath(".").TrimEnd('\\', '/') + Path.DirectorySeparatorChar; /* （エディターはプロジェクトのフォルダで動いている） */
            return Path.GetFullPath(path).StartsWith(project, StringComparison.OrdinalIgnoreCase);
        }

        [Serializable]
        class Asmdef
        {
            public string[] references = new string[0];
            public string[] defineConstraints = new string[0];
            public string[] includePlatforms = new string[0];
            public VersionDefine[] versionDefines = new VersionDefine[0];
            public bool autoReferenced = true;
        }

        [Serializable]
        class VersionDefine
        {
            public string name, expression, define;
        }

        static Asmdef ReadAsmdef(string path)
        {
            var text = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path)?.text;
            if (string.IsNullOrEmpty(text)) return null;
            try { return JsonUtility.FromJson<Asmdef>(text); }
            catch { return null; }
        }
    }
}
