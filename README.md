# Unity DXR Sample

このリポジトリは、UnityでDirectX Raytracing (DXR) を試すためのサンプルプロジェクトです。このサンプルは、リアルタイムレイトレーシングの基本的な実装を示しており、UnityでのDXRの使用方法を学ぶための出発点として作成しました

## 特徴

- DXRを使用した基本的なレイトレーシングシーン(HelloDXR)
- ビルトインパイプラインを使用した簡単なパストレーサー(SimplePathTracer)

## 実行環境（動作確認済み,それ以下でも動く可能性大）

- Unity 2023.2.20
- Windows 11
- NVIDIA Geforce RTX 4080 (NVIDIA RTX 20シリーズ以降ならおそらく動作)

## セットアップ

1. Unity Hubを開きます。
2. 'Add' ボタンをクリックして、ダウンロードしたプロジェクトフォルダを選択します
3. Unityエディタでプロジェクトを開きます。
4. 'Editor' > 'Player' > 'Other Settings' > 'Graphics API for Windows'がDirectX12になっているか確認
5. 'Editor' > 'Player' > 'Other Settings' > 'Rendering'が'Linear'になっているか確認
(GammaでもRenderTextureの設定を適切にすれば動作するはずだが, 面倒なのでLinearにしている)

## 使用方法

プロジェクトを開いたら、`Scenes`フォルダ内のサンプルシーンをロードして実行してください。シーン内には、DXRを使用したレイトレーシングのデモが含まれています。

## ライセンス

このプロジェクトはMITライセンスの下で公開されています。詳細はLICENSEファイルをご覧ください。

## 貢献

- カメラの移動用スクリプトは, ねこますさん( https://qiita.com/Nekomasu )の記事( https://qiita.com/Nekomasu/items/f195db36a2516e0dd460 )を一部改変したものを使用しています.
- 本プロジェクトで使用しているCornellBox SceneはMorgan McGuire's Computer Graphics Archive(https://casual-effects.com/data)から, ダウンロードしたものを使用しています.

このサンプルプロジェクトへの貢献は大歓迎です。プルリクエストやイシューを通じて、改善点や機能追加の提案をお願いします。

## サポート

プロジェクトに関する質問やサポートが必要な場合は、Issuesセクションにてお知らせください。

## 

