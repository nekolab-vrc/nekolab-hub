# 変更の記録 / Changelog

## 1.0.0（最初の正式な版 / first release）
- 最初の正式な版です。動きは練習の版 1.0.0-rc.5 と同じです。コードの中の説明の文を整理して日本語にし、開発用の点検の知らせを1つ（K-12）短くしました
  / The first release. It works as the practice release 1.0.0-rc.5 did; the explanations in the code were tidied and are now in Japanese, and one message of the development check (K-12) made shorter
- できること：プロジェクトに入っている NekoLAB の商品の一覧と、商品ごとのページ（アバターへの導入など）・同期パラメーターの使用量・質問の時の「サポート情報をコピー」・日本語と英語の切り替え
  / What it does: lists the NekoLAB products in your project, each with its own pages (putting it on your avatar, and more); the synced parameters used; "Copy support info" for your questions; Japanese and English

## 1.0.0-rc.5（練習の版 / practice release）
- 商品の絵：名札に `image`（絵の場所。16:9）と `summary`（何の商品か1行。日本語と英語）を足した。窓が、商品の各タブの上に絵と1行を、左の一覧の名前の横に小さな絵を出す（どの商品も同じ場所・大きさ）。足しただけで、前の名札もそのまま読む
  / The product's picture: the nameplate's new `image` (16:9) and `summary` (one line, Japanese and English). The window shows them at the top of every tab of the product, and the picture small beside its name in the list. Only added: older nameplates read as before
- 導入の状態 `NekoLabUI.InstallStatus` を、幅いっぱいの色の帯にした（導入済み＝緑、未導入＝灰）。呼び方は同じ
  / `NekoLabUI.InstallStatus` is now a band across the place (installed: green; not: grey). Called as before
- 点検 K-15：名札の絵が、商品のフォルダの中にあるか（NG）・16:9 で横 640 px 以上か（LOOK）
  / Check K-15: the nameplate's picture in the product's folder, and there (NG); 16:9 and 640 px wide or more (LOOK)
- 左の一覧を少し広くした（小さな絵のため）。絵のない商品にも同じ大きさの空の枠を置いて、名前の位置をそろえた
  / The list on the left is a little wider (for the small pictures); a product without one keeps an empty frame, so the names line up
- 明るいテーマで見にくかった所を直した：「アバターを選んでください」を地の付いた札に、「使い方」のまだの段階の数字を濃い色に、注意の文の橙を少し濃く
  / Easier to read in the light skin: "Pick your avatar" on a ground of its own, the numbers of the steps not reached yet in a dark colour, the amber a little darker
- 選んでいたアバターがシーンから消えた時に、アバターの欄が「Missing」のまま残っていたのを直した
  / Fixed: the avatar's field stayed "Missing" after the avatar was taken out of the scene
- 注意の箱（`NekoLabUI.Notice`）をカードの中で使った時は、枠を付けずに色の帯と文だけにした
  / `NekoLabUI.Notice` inside a card: the bar and the words only, no box of its own

## 1.0.0-rc.4（練習の版 / practice release）
- 1.0.0-rc.3 は公開しなかった（手元の試しで、点検のコードに Unity の注意（CS0162）が見つかったため）。rc.3 の中身に、その直しを入れた版
  / 1.0.0-rc.3 was never published (a compiler warning, CS0162, was found in the check's code before): this is rc.3 with that fixed
- 直し：点検（K-13）の中で、組み立ての時に値が決まる2つを比べていたため、Unity が「通らないコード」の注意を出していた
  / Fixed: the check (K-13) compared two values fixed at compile time, and Unity warned of unreachable code

### rc.3 として作った物（公開していない / made as rc.3, never published）
- 商品のページの部品を2つ足した：`NekoLabUI.InstallStatus`（導入済み／未導入の表示）・`NekoLabUI.MainButton`（ページの主なボタン）。足しただけで、前の商品はそのまま動く
  / Added two parts for a product's page: `NekoLabUI.InstallStatus` (installed or not) and `NekoLabUI.MainButton` (the page's main button). Only added: the products made before work as they did
- 点検に3つ足した：ページで Unity の HelpBox や折り返す LabelField を使っていたら知らせる（K-12。数えない）／Hub のファイルが書き換えられていたら NG（K-13）／ページが、名札の toolVersion より新しい Hub の物を使っていたら NG（K-14）
  / Three more checks: a page using Unity's help box or a wrapped label is pointed out (K-12, not counted); the Hub's files changed (K-13); a page using something of a newer Hub than its nameplate's toolVersion (K-14)

## 1.0.0-rc.2（練習の版 / practice release）
- 窓の見た目を作り直した：上の帯と左の一覧を濃い色に、ホームの中身をカードに分けた。説明の長い文は「?」にマウスを乗せると出る。「使い方」の3つの段階と、同期パラメーターの色の棒
  / The window's look made again: the bar on top and the list on the left darker, the home's parts on cards. The longer explanations show under the mouse on a "?". The three steps of "How to use", and the synced parameters as a bar of colours
- 狭い窓で、文が窓の外にはみ出したり、「NekoLAB」などの英単語だけの行ができたりしていたのを直した
  / Fixed: in a narrow window, text went past the window's edge, and a Latin word such as "NekoLAB" stood alone on its line
- 窓の文言を見直した（「導入済み」「バージョン」など）
  / The window's Japanese words reviewed
- 商品のページから使える部品 `NekoLabUI` を足した（カード・見出しと「?」・折り返す文・注意の箱）。足しただけで、前の商品はそのまま動く
  / Added `NekoLabUI`, the window's parts for a product's page (a card, a heading with its "?", wrapped text, a notice). Only added: the products made before work as they did
- 左の一覧：名前を一覧の幅で出し（入らない時は「…」、マウスを乗せると全部）、バージョンを2行目に。タブが1つの商品では、タブの帯を出さない
  / The list: a product's name on the list's whole width (cut with "…" when it does not fit, all of it under the mouse), its version on the second line. No bar of tabs for a product with one tab

## 1.0.0-rc.1（練習の版 / practice release）
- 最初の公開（1.0.0）の前の練習の版。中身は 0.3.0 と同じで、点検の説明の文字を1つ直した
  / A practice release before the first release (1.0.0): the same as 0.3.0, with one wording of the check fixed

## 0.3.0（開発中・公開していない / development only, not published）
- 「サポート用の情報をコピー」を足した（ホームのボタン。商品ページからも `hub.SupportBlock()` で出せる）。入るのは、ソフトと商品の版・アバターに入っているか・同期の使用量だけ。個人の情報に見える行は外す
  / Added "Copy the support info" (a button on the home page; a product's page may show it with `hub.SupportBlock()`): the versions of the software and the products, whether each is on the avatar, the synced data in use. A line that looks personal is left out
- 商品ページが、サポート用の情報に自分の行を足せるようにした（`SupportLines`）
  / A product's page may add lines of its own to the support info (`SupportLines`)
- 商品のページのコードが止まっても（Id がない・作れない・タブの名前がない）、窓とほかの商品は動くようにした
  / A product's page that fails (no Id, cannot be made, no tab names) no longer stops the window or the other products

## 0.2.0（開発中・公開していない / development only, not published）
- 版の番号を、練習の版（`1.0.0-rc.1` など）も含めて正しく比べるようにした
  / Version numbers are compared correctly, pre-releases (`1.0.0-rc.1`) included
- 商品が新しい NekoLAB Hub を必要とする時は、商品のページが出ない時も「NekoLAB Hub を更新してください」と出すようにした
  / When a product needs a newer NekoLAB Hub, the window says so even when the product's pages are not there
- 同期の使用量：40〜50 の目安の印と注意をやめ、上限（256）までの残りを出すようにした。EditorOnly の物の下にある物も数えないようにした
  / Synced data: the 40-50 guide is gone; what is left before the limit (256) is shown. Objects under an EditorOnly object are no longer counted
- 商品を入れた時と更新した時に、この画面が1回だけ自動で開くようにした（今までは各商品がしていた）。ホームで止められる
  / The window opens by itself once when a product is imported or updated (each product did this until now). It can be turned off on the home page
- Modular Avatar を名前で読むようにし、要る物から外した（Modular Avatar がない時や、形が変わった時も、この画面は止まらず、数えなかった物を知らせる）
  / Modular Avatar is read by name and no longer required: without it, or after it changes, the window still works and says what it did not count
- 言語の設定と、自動で開いた記録を、PC ではなくプロジェクトの中（UserSettings）に置くようにした
  / The language and what was opened by itself are kept in the project (UserSettings), not on the computer
- 商品の開発用の点検（`NekoLabCheck.Report()`）を足した。メニューはない（買った人は使わない）
  / Added the check for the products' development (`NekoLabCheck.Report()`); no menu, buyers never run it
- この道具のまとまり（asmdef）を、自動では参照されないようにした（使えるのは、守りの付いた商品ページのまとまりだけ）
  / This tool's assembly is no longer referenced automatically: only the products' guarded page assemblies use it

## 0.1.0（開発中・公開していない / development only, not published）
- 「はじめに」の窓を、パッケージにした
  / The NekoLAB window made into this package
