using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace NekoLab.Hub
{
    /// <summary>
    /// アップロードする時のアバター全体の同期パラメーターの使用量（bit）を、出どころごとに数えてゲージで出す：
    /// アバター自身の Expression Parameters・各 NekoLAB の商品の分（商品のインスタンスの下の Modular Avatar のパラメーター）・
    /// ほかの Modular Avatar のパラメーターと、メニューの項目が自分で作る物・VRCFury の分（コンポーネントから読める範囲で。目安）。
    /// 同じ名前は、何度出てきても1回だけ数える。Modular Avatar と VRCFury は参照せず名前で読むので、
    /// どちらかがない時や形が変わった時も、窓は組み立てられ、数えなかった物を知らせる。
    /// </summary>
    public static class NekoLabBits
    {
        /// <summary>VRChat の、アバター1体の同期パラメーターの上限（bit）。</summary>
        public const int Limit = 256;

        public struct Part
        {
            public string label;
            public int bits;
            public Color colour;
        }

        public class Tally
        {
            public readonly List<Part> parts = new List<Part>();
            public int Total => parts.Sum(p => p.bits);
            public bool vrcFury, vrcFuryCompress;
            /// <summary>Modular Avatar がプロジェクトにあるか。読めたか（コンポーネントの形が変わっていると false）。</summary>
            public bool modularAvatar, modularAvatarRead;
        }

        static readonly Color[] ProductColours = { new Color(0.36f, 0.78f, 0.95f), new Color(0.98f, 0.66f, 0.3f), new Color(0.75f, 0.55f, 0.95f), new Color(0.5f, 0.9f, 0.6f) };

        static int Cost(VRCExpressionParameters.ValueType t) => t == VRCExpressionParameters.ValueType.Bool ? 1 : 8;
        /* Modular Avatar の ParameterSyncType を、名前で読む */
        static int Cost(string syncType) => syncType == "Bool" ? 1 : syncType == "NotSynced" ? 0 : 8;

        public static Tally Count(VRCAvatarDescriptor avatar, IList<(string label, GameObject root)> products)
        {
            var tally = new Tally { modularAvatar = MA.Present, modularAvatarRead = MA.Readable };
            if (avatar == null) return tally;
            var names = new HashSet<string>();
            /* アバター自身の分 */
            var own = 0;
            if (avatar.expressionParameters != null && avatar.expressionParameters.parameters != null)
                foreach (var p in avatar.expressionParameters.parameters)
                {
                    if (p == null || string.IsNullOrEmpty(p.name) || !names.Add(p.name)) continue;
                    if (p.networkSynced) own += Cost(p.valueType);
                }
            tally.parts.Add(new Part { label = NekoLabText.L("bits.own"), bits = own, colour = new Color(0.62f, 0.64f, 0.68f) });

            /* Modular Avatar のパラメーター。入っている商品の分（ほかのギミックの分）として数える。
               Modular Avatar がない時や読めなかった時は数えない（ゲージがそう知らせる） */
            if (MA.Readable)
            {
                int OwnerOf(Component c)
                {
                    for (var k = 0; k < products.Count; k++)
                        if (products[k].root != null && c.transform.IsChildOf(products[k].root.transform)) return k;
                    return -1;
                }
                var perProduct = new int[products.Count];
                var other = 0;
                void Add(Component at, int bits)
                {
                    var k = OwnerOf(at);
                    if (k >= 0) perProduct[k] += bits; else other += bits;
                }
                foreach (var c in Uploaded(avatar, MA.Parameters))
                    foreach (var p in MA.List.GetValue(c) as IList ?? new object[0])
                    {
                        var nameOrPrefix = (string)MA.NameOrPrefix.GetValue(p);
                        var prefix = (bool)MA.IsPrefix.GetValue(p);
                        var sync = MA.SyncType.GetValue(p).ToString();
                        var name = Final(p);
                        if (prefix || sync == "NotSynced") { if (!prefix && !string.IsNullOrEmpty(nameOrPrefix)) names.Add(name); continue; }
                        /* （内部用の物は Modular Avatar が名前を付け直すので、ほかと重ならない） */
                        if (!(bool)MA.Internal.GetValue(p) && !names.Add(name)) continue;
                        if (!(bool)MA.LocalOnly.GetValue(p)) Add(c, Cost(sync));
                    }
                /* メニューの項目が自分で作る物：どこにも宣言のないパラメーターのトグルやボタンは、Modular Avatar が作る。
                   止めていなければ同期する（bool。値が 1 を超える時や、いくつかの項目が自動の値で同じ物を使う時は int） */
                var made = new Dictionary<string, (Component at, bool synced, bool wide)>();
                foreach (var m in Uploaded(avatar, MA.MenuItem))
                {
                    if (!(MA.Control.GetValue(m) is VRCExpressionsMenu.Control control)) continue;
                    if (control.type != VRCExpressionsMenu.Control.ControlType.Toggle && control.type != VRCExpressionsMenu.Control.ControlType.Button) continue;
                    var name = control.parameter?.name ?? "";
                    if (string.IsNullOrEmpty(name)) name = "(auto) " + m.GetInstanceID();
                    if (names.Contains(name)) continue;
                    var synced = (bool)MA.IsSynced.GetValue(m);
                    var automatic = MA.AutomaticValue != null && (bool)MA.AutomaticValue.GetValue(m);
                    var wide = !automatic && control.value > 1.001f;
                    made[name] = made.TryGetValue(name, out var was) ? (was.at, was.synced || synced, was.wide || wide || automatic) : (m, synced, wide);
                }
                foreach (var kv in made)
                    if (kv.Value.synced) Add(kv.Value.at, kv.Value.wide ? 8 : 1);
                for (var k = 0; k < products.Count; k++)
                    tally.parts.Add(new Part { label = products[k].label, bits = perProduct[k], colour = ProductColours[k % ProductColours.Length] });
                tally.parts.Add(new Part { label = NekoLabText.L("bits.ma"), bits = other, colour = new Color(0.86f, 0.82f, 0.4f) });
            }

            /* VRCFury（プロジェクトに入っている時） */
            var vf = VrcFury(avatar, names, out tally.vrcFury, out tally.vrcFuryCompress);
            if (tally.vrcFury) tally.parts.Add(new Part { label = NekoLabText.L("bits.vrcf"), bits = vf, colour = new Color(0.95f, 0.45f, 0.55f) });
            return tally;
        }

        static string Final(object p)
        {
            var remapTo = (string)MA.RemapTo.GetValue(p);
            return string.IsNullOrEmpty(remapTo) ? (string)MA.NameOrPrefix.GetValue(p) : remapTo;
        }

        /* アバターの、アップロードで残るコンポーネント：EditorOnly のタグの付いたオブジェクトの上にも下にもない物
           （Modular Avatar が動く前に、子と一緒に消されるため。NDMF の RemoveEditorOnlyPass） */
        static IEnumerable<Component> Uploaded(VRCAvatarDescriptor avatar, Type type) =>
            avatar.GetComponentsInChildren(type, true).Where(c => !UnderEditorOnly(c.transform, avatar.transform));

        static bool UnderEditorOnly(Transform t, Transform root)
        {
            for (; t != null; t = t.parent)
            {
                if (t.CompareTag("EditorOnly")) return true;
                if (t == root) break;
            }
            return false;
        }

        /* Modular Avatar の2つのコンポーネントと、数える時に読むフィールド。名前で1回だけ探す（参照しないので、
           Hub の組み立ては Modular Avatar に頼らない）。全部のフィールドが思った型である時だけ「読める」とする */
        static class MA
        {
            public static readonly Type Parameters = Find("nadena.dev.modular_avatar.core.ModularAvatarParameters");
            public static readonly Type MenuItem = Find("nadena.dev.modular_avatar.core.ModularAvatarMenuItem");
            public static readonly FieldInfo List = Parameters?.GetField("parameters");
            static readonly Type Config = List != null && List.FieldType.IsGenericType ? List.FieldType.GetGenericArguments()[0] : null;
            public static readonly FieldInfo NameOrPrefix = Config?.GetField("nameOrPrefix"), RemapTo = Config?.GetField("remapTo"),
                IsPrefix = Config?.GetField("isPrefix"), Internal = Config?.GetField("internalParameter"),
                SyncType = Config?.GetField("syncType"), LocalOnly = Config?.GetField("localOnly");
            public static readonly FieldInfo Control = MenuItem?.GetField("Control"), IsSynced = MenuItem?.GetField("isSynced"),
                AutomaticValue = MenuItem?.GetField("automaticValue"); /* （ないバージョンもある。ない時は、書いてある値のまま） */

            public static bool Present => Parameters != null || MenuItem != null;
            public static readonly bool Readable = Parameters != null && MenuItem != null
                && List != null && typeof(IList).IsAssignableFrom(List.FieldType)
                && Is<string>(NameOrPrefix) && Is<string>(RemapTo) && Is<bool>(IsPrefix) && Is<bool>(Internal) && Is<bool>(LocalOnly)
                && SyncType != null && SyncType.FieldType.IsEnum && Enum.GetNames(SyncType.FieldType).Contains("NotSynced") && Enum.GetNames(SyncType.FieldType).Contains("Bool")
                && Is<VRCExpressionsMenu.Control>(Control) && Is<bool>(IsSynced) && (AutomaticValue == null || Is<bool>(AutomaticValue));

            static bool Is<T>(FieldInfo f) => f != null && f.FieldType == typeof(T);
            static Type Find(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);
        }

        /* VRCFury のコンポーネントを、データとして読む（型は名前で探す。参照しない）。トグルは同期する bool 1つ
           （スライダーなら float）。Full Controller は、そのパラメーターの一覧を足す。それ以外に作られる物は数えない */
        static int VrcFury(VRCAvatarDescriptor avatar, HashSet<string> names, out bool present, out bool compress)
        {
            present = false;
            compress = false;
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("VF.Model.VRCFury", false)).FirstOrDefault(t => t != null);
            if (type == null) return 0;
            var components = avatar.GetComponentsInChildren(type, true);
            present = components.Length > 0;
            var bits = 0;
            foreach (var c in components)
            {
                var so = new SerializedObject(c);
                var it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ManagedReference) continue;
                    var kind = it.managedReferenceFullTypename ?? "";
                    if (kind.EndsWith(".Toggle"))
                    {
                        var global = it.FindPropertyRelative("useGlobalParam");
                        var globalName = it.FindPropertyRelative("globalParam");
                        if (global != null && global.boolValue && globalName != null && !names.Add(globalName.stringValue)) continue;
                        var slider = it.FindPropertyRelative("slider");
                        bits += slider != null && slider.boolValue ? 8 : 1;
                    }
                    else if (kind.EndsWith(".FullController"))
                    {
                        var end = it.GetEndProperty();
                        var inner = it.Copy();
                        while (inner.Next(true) && !SerializedProperty.EqualContents(inner, end))
                            if (inner.propertyType == SerializedPropertyType.ObjectReference && inner.objectReferenceValue is VRCExpressionParameters list && list.parameters != null)
                                foreach (var p in list.parameters)
                                    if (p != null && p.networkSynced && !string.IsNullOrEmpty(p.name) && names.Add(p.name)) bits += Cost(p.valueType);
                    }
                    else if (kind.Contains("Compress") || kind.Contains("UnlimitedParameters")) compress = true;
                }
            }
            return bits;
        }

        /* ゲージ（カードの上）：見出しの行に合計と残り（ギミックを足す人に大事なのは、上限までの残り）、
           0 から上限までの棒に出どころごとの色、その下に出どころの一覧、数えなかった物。説明は「?」の中（一度に出す文を少なく） */
        public static void DrawGauge(Tally tally, VRCAvatarDescriptor avatar)
        {
            NekoLabLook.BeginCard();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(NekoLabText.L("bits.head"), EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
            NekoLabLook.Help(NekoLabText.L("bits.what"));
            GUILayout.FlexibleSpace();
            if (avatar != null)
            {
                var over = tally.Total > Limit;
                GUILayout.Label(tally.Total + " / " + Limit + " bit", over ? NekoLabLook.Bad : EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
                if (!over) GUILayout.Label(string.Format(NekoLabText.L("bits.left"), Limit - tally.Total), NekoLabLook.SmallMutedLine, GUILayout.ExpandWidth(false));
            }
            EditorGUILayout.EndHorizontal();
            if (avatar == null)
            {
                NekoLabLook.Text(NekoLabText.L("bits.noavatar"), NekoLabLook.SmallMuted);
                NekoLabLook.EndCard();
                return;
            }
            GUILayout.Space(2f);
            var r = GUILayoutUtility.GetRect(0f, 12f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(r, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, NekoLabLook.Edge, 0f, 2f);
                float X(float bits) => r.x + r.width * Mathf.Clamp01(bits / Limit);
                var at = 0f;
                foreach (var p in tally.parts.Where(p => p.bits > 0))
                {
                    EditorGUI.DrawRect(new Rect(X(at), r.y + 1f, Mathf.Max(1f, X(at + p.bits) - X(at)), r.height - 2f), p.colour);
                    at += p.bits;
                }
            }
            GUILayout.Space(4f);
            foreach (var p in tally.parts)
            {
                EditorGUILayout.BeginHorizontal();
                var dot = GUILayoutUtility.GetRect(14f, 16f, GUILayout.Width(14f));
                NekoLabLook.Disc(new Rect(dot.x + 1f, dot.y + 4f, 8f, 8f), p.colour);
                GUILayout.Label(p.label, NekoLabLook.SmallLine);
                GUILayout.FlexibleSpace();
                GUILayout.Label(p.bits + " bit", NekoLabLook.SmallMutedLine, GUILayout.ExpandWidth(false));
                EditorGUILayout.EndHorizontal();
            }
            if (tally.Total > Limit) NekoLabLook.Text(NekoLabText.L("bits.over"), NekoLabLook.Bad);
            if (!tally.modularAvatar) NekoLabLook.Text(NekoLabText.L("bits.noma"), NekoLabLook.SmallMuted);
            else if (!tally.modularAvatarRead) NekoLabLook.Text(NekoLabText.L("bits.maread"), NekoLabLook.Warn);
            if (tally.vrcFury) NekoLabLook.Text(NekoLabText.L(tally.vrcFuryCompress ? "bits.vrcf.compress" : "bits.vrcf.note"), NekoLabLook.SmallMuted);
            NekoLabLook.EndCard();
        }
    }
}
