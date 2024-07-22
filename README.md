# Unity DXR Sample

このリポジトリは、UnityでDirectX Raytracing (DXR) を試すためのサンプルプロジェクトです。このサンプルは、リアルタイムレイトレーシングの基本的な実装を示しており、UnityでのDXRの使用方法を学ぶための出発点として作成しました

## 特徴

- DXRを使用したほぼ最小構成のレイトレーサー(HelloDXR)
- ビルトインパイプライン機能を使用した簡単なパストレーサー(SimplePathTracer)

## 実行環境（動作確認済み,より弱い環境でも動く可能性大）

- Unity 2023.2.21 (Unity 2023.2.20はmainを参照)
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

## 注意

- URP相当の機能は使用していませんが, 設定ファイル等は含まれています（自分で作成した記憶はないので、レイトレを使用したときに自動生成した?）
- 現在のバージョン（Unity 2023.2.20）ではビルドを行うとシェーダテーブルの情報がが消えてしまうバグを確認しています.
  原因は確認していないため, どなたか対応方法が分かった方がいればIssuesに投げてもらえると助かります.

## ライセンス

このプロジェクトはMITライセンスの下で公開されています。詳細はLICENSEファイルをご覧ください。

## 貢献

- カメラの移動用スクリプトは, ねこますさん( https://qiita.com/Nekomasu )の記事( https://qiita.com/Nekomasu/items/f195db36a2516e0dd460 )を一部改変したものを使用しています.
- 本プロジェクトで使用している3DモデルはMorgan McGuire's Computer Graphics Archive(https://casual-effects.com/data)から, ダウンロードしたものを使用しています.
- 以下の記事を参考にしています
  - INedelcu氏のプロジェクト(https://github.com/INedelcu/PathTracingDemo)
  - Unityフォーラムの議論　（https://forum.unity.com/threads/dxr-raytracing-effect-from-scratch.794928/）

このサンプルプロジェクトへの貢献は大歓迎です。プルリクエストやイシューを通じて、改善点や機能追加の提案をお願いします。

## サポート

プロジェクトに関する質問やサポートが必要な場合は、Issuesセクションにてお知らせください。


