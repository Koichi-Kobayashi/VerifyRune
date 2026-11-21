# VerifyRune

`System.Text.Rune` の検証用コンソールアプリです。.NET 6 と .NET 11 で同じコードをビルドし、API の呼び出し先や Unicode の扱いを確認します。

## 確認できること

- `StringBuilder.Append(rune)` は `net6.0` では `Append(object)`、`net11.0` では `Append(Rune)` に解決されます。
- KanjiVariants の `AppendRune` と `StringBuilder.Append(Rune)` で、補助平面の漢字や IVS（漢字と異体字セレクターの並び）を追記した結果が一致します。
- KanjiVariants が不正な UTF-16 を解析・置換・診断でどう扱うか確認できます。
- .NET 11 の `Contains(Rune)` と `IndexOf(Rune)`、および `RunePosition` の開始位置・長さ・`WasReplaced` を確認できます。

## セットアップ

.NET 6 SDK と .NET 11 SDK をインストールしてください。ビルドには .NET 11 SDK を使います。本プロジェクトは隣の `KanjiVariants` からソースを読み込んでコンパイルします。`KanjiText.AppendRune` の実装も検証するため、以下の手順で検証時のコミットを指定します。

`VerifyRune` をクローンした状態で、そのリポジトリのルートから次を実行します。隣に検証用の `KanjiVariants` が用意されている場合は、この手順は不要です。

```sh
git clone https://github.com/Koichi-Kobayashi/KanjiVariants.git ../KanjiVariants
git -C ../KanjiVariants switch --detach e8aa16c1d7df199a61dbf9d72e6e916216cfbe08
```

```text
親フォルダー/
├── KanjiVariants/
│   └── KanjiVariants/
└── VerifyRune/
    ├── VerifyRune.slnx
    └── VerifyRune/
        └── VerifyRune.csproj
```

## 実行

### Visual Studio で実行

1. `VerifyRune.slnx` を Visual Studio で開きます。
2. 起動プロジェクトを `VerifyRune` にし、ツールバーの実行先の一覧で `.NET 6.0` または `.NET 11.0` を選びます。
3. `Ctrl+F5` を押すか、［デバッグ］→［デバッグなしで開始］を選びます。

`PASS` や `Bound overload` は、アプリが開くコンソールウィンドウに表示されます。選んだ .NET のバージョンは出力の先頭で確認できます。Visual Studio の［出力］ウィンドウにはビルドなどのメッセージが表示されます。

### ターミナルで実行

`VerifyRune.slnx` があるフォルダーから実行します。

```sh
dotnet run --project VerifyRune/VerifyRune.csproj --framework net6.0 --configuration Release
dotnet run --project VerifyRune/VerifyRune.csproj --framework net11.0 --configuration Release
```

検証に成功した項目には `PASS` が表示されます。`Bound overload` の行で、コンパイル時に選ばれた `Append` のオーバーロードを確認できます。

### 参考資料

- [Microsoft Learn「.NET の文字エンコーディング」](https://learn.microsoft.com/ja-jp/dotnet/standard/base-types/character-encoding-introduction)
- [Microsoft Learn「.NET 11 のライブラリの新機能」](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#string-and-character-enhancements)
- [.NET Runtime の `StringBuilder.Append(Rune)` 実装](https://github.com/dotnet/runtime/blob/v11.0.0/src/libraries/System.Private.CoreLib/src/System/Text/StringBuilder.cs#L963-L974)
