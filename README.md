# AES-256 暗号化サンプル (.NET)

固定のハードコードされた鍵を使用して、入力文字列を **AES-256-CBC** で暗号化し **Base64** 形式で出力するプログラムのサンプルです。

> **注意:** ハードコードされた鍵・IV は学習・デモ目的です。実運用環境では使用しないでください。

---

## プロジェクト構成

```
.
├── CSharp-AES/          # C# 実装（net48: JIT / net10.0: R2R・AOT・R2R Stomped）
│   ├── CSharp-AES.csproj
│   └── Program.cs
├── VBNet-AES/           # VB.NET 実装（net48: JIT）
│   ├── VBNet-AES.vbproj
│   └── Program.vb
└── R2RStomper/          # R2R stomping ツール（dnlib 使用）
    ├── R2RStomper.csproj
    └── Program.cs
```

---

## 暗号化仕様

| 項目               | 値                              |
|--------------------|---------------------------------|
| アルゴリズム       | AES-256-CBC                     |
| 鍵長               | 256 bit（32 バイト、ハードコード） |
| IV                 | 128 bit（16 バイト、ハードコード） |
| パディング         | PKCS7                           |
| 出力形式           | Base64                          |
| 入力エンコーディング | UTF-8                          |

---

## 使い方

コマンドライン引数、または標準入力で文字列を渡します。

```bash
# コマンドライン引数で渡す場合
<実行ファイル> "暗号化したい文字列"

# 標準入力で渡す場合（引数なし）
echo "暗号化したい文字列" | <実行ファイル>
```

---

## .NET 実行ファイルの構造と dnSpy/ILSpy での解析

### .NET Framework（net48）の場合

```
App.exe  ── マネージド PE ファイル（CLR ヘッダーあり、IL を含む）
App.dll  ── 同じく IL アセンブリ（どちらも dnSpy で開ける）
```

`.exe` 自体が IL アセンブリであるため、**dnSpy/ILSpy に `.exe` を直接読み込める**。

### .NET 5 以降（net10.0）の場合

```
App.exe  ── C++ 製のネイティブ起動ランチャー（IL なし、dnSpy では読めない）
App.dll  ── IL アセンブリ（dnSpy/ILSpy の解析対象はこちら）
```

### コンパイル方式別の解析結果の違い

| 方式 | ターゲット | dnSpy で開くファイル | 見え方 |
|---|---|---|---|
| VB.NET JIT | net48 | `.exe` または `.dll` | 完全な IL・ほぼ元のソースに近い逆コンパイル結果 |
| C# JIT | net48 | `.exe` または `.dll` | 完全な IL・ほぼ元のソースに近い逆コンパイル結果 |
| C# Ready to Run | net10.0 | `.dll`（20 KB） | IL + 事前コンパイル済みネイティブコードが混在 |
| C# Native AOT | net10.0 | `.exe`（1 MB） | IL なし・純ネイティブ（逆コンパイル不可） |

---

## コンパイル・実行方法

### 1. VB.NET — JIT（.NET Framework 4.8）

#### `dotnet run` で直接実行

```bash
cd VBNet-AES
dotnet run -- "Hello, World!"
```

#### 発行して実行

```bash
cd VBNet-AES
dotnet publish -c Release -o ./publish
./publish/VBNet-AES "Hello, World!"
```

> dnSpy/ILSpy で解析する場合は `./publish/VBNet-AES.exe` または `./publish/VBNet-AES.dll` を開いてください。

---

### 2. C# — JIT（.NET Framework 4.8）

#### `dotnet run` で直接実行

```bash
cd CSharp-AES
dotnet run -f net48 -- "Hello, World!"
```

#### 発行して実行

```bash
cd CSharp-AES
dotnet publish -c Release -f net48 -o ./publish/jit
./publish/jit/CSharp-AES "Hello, World!"
```

> dnSpy/ILSpy で解析する場合は `./publish/jit/CSharp-AES.exe` または `./publish/jit/CSharp-AES.dll` を開いてください。

---

### 3. C# — Ready to Run（.NET 10）

IL に加えてプラットフォーム固有のネイティブコードを `.dll` へ事前に埋め込むことで、JIT の起動コストを削減します。JIT フォールバックを保持するため .NET ランタイムは引き続き必要です。

> R2R は .NET 5 以降の機能であり .NET Framework では利用できません（.NET Framework の相当機能は実行後に NGEN を別途実行する方式です）。

#### 発行して実行

```bash
cd CSharp-AES
dotnet publish -c Release -f net10.0 -r win-x64 --self-contained false -p:PublishReadyToRun=true -o ./publish/r2r
./publish/r2r/CSharp-AES "Hello, World!"
```

> `-r` には実行環境の [RID (Runtime Identifier)](https://learn.microsoft.com/ja-jp/dotnet/core/rid-catalog) を指定します。
>
> | OS             | RID 例          |
> |----------------|-----------------|
> | Windows x64    | `win-x64`       |
> | Windows ARM64  | `win-arm64`     |
> | Linux x64      | `linux-x64`     |
> | Linux ARM64    | `linux-arm64`   |
> | macOS x64      | `osx-x64`       |
> | macOS ARM64    | `osx-arm64`     |
>
> dnSpy/ILSpy で解析する場合は `./publish/r2r/CSharp-AES.dll` を開いてください。IL と R2R ネイティブコードの両方が確認できます。

---

### 4. C# — Native AOT（.NET 10）

完全なネイティブバイナリを生成します。IL が存在しないため dnSpy/ILSpy での逆コンパイルはできません。

> Native AOT は .NET Framework では利用できません。また VB.NET でも未サポートです（.NET 10 時点）。

#### 前提条件（ビルド時のみ必要）

| OS      | 必要なツール                                                    |
|---------|-----------------------------------------------------------------|
| Windows | Visual Studio Build Tools（「C++ によるデスクトップ開発」ワークロード） |
| Linux   | `clang`, `zlib1g-dev`, `libssl-dev`                            |
| macOS   | Xcode Command Line Tools（`xcode-select --install`）           |

#### 発行して実行

Windows では `vswhere.exe` が PATH に含まれていない場合、リンク時にエラーになることがあります。その場合は以下のように Visual Studio Installer ディレクトリを PATH に追加してから発行してください。

```powershell
# Windows: vswhere.exe を PATH に追加（PowerShell）
$env:PATH = "C:\Program Files (x86)\Microsoft Visual Studio\Installer;" + $env:PATH
```

```bash
cd CSharp-AES
dotnet publish -c Release -f net10.0 -r win-x64 -p:EnableAot=true -o ./publish/aot
./publish/aot/CSharp-AES "Hello, World!"
```

> `-p:EnableAot=true` はプロジェクト内の条件分岐で `PublishAot=true` を net10.0 向けにのみ有効化するカスタムプロパティです。
> これにより、マルチターゲット（net48 + net10.0）のプロジェクトで net48 側に AOT 検証エラーが波及するのを防いでいます。

---

### 5. C# — R2R Stomping（.NET 10）

R2R stomping は、マネージド IL と R2R ネイティブコードを意図的に乖離させる技術です（[VB2023 論文](https://www.virusbulletin.com/virusbulletin/2023/09/vb2023-paper-hiding-process-injection-behind-net-runtime-ready-run/) 参照）。

#### 仕組み

| レイヤー | コード |
|----------|--------|
| **IL（dnSpy が見るもの）** | `KeyA`（囮キー）で AES 暗号化、ヘルパーメソッドは空スタブ |
| **R2R ネイティブ（実際の実行）** | `"This is hidden message"` を表示し、`KEY_B`（隠し鍵）で AES 暗号化 |

`GetRealKey()` / `WriteHiddenMessage()` は **`AggressiveInlining`** でネイティブコードに展開されるため、`Main` の IL には呼び出しが残らず、スタンプ後の IL ではヘルパーメソッドは空スタブとして残るだけです。

#### ビルド・実行手順

```powershell
# Step 1: KEY_B を含む R2R DLL をビルド（stomper 用の中間成果物）
cd CSharp-AES
dotnet publish -c Release -f net10.0 -r win-x64 --self-contained false `
    -p:PublishReadyToRun=true -p:UseRealKey=true -o ../temp/r2r-real

# Step 2: R2RStomper をビルド
cd ../R2RStomper
dotnet build -c Release

# Step 3: stomping を実行
dotnet run -c Release -- ../temp/r2r-real/CSharp-AES.dll ../publish/csharp-r2r-stomped/CSharp-AES.dll

# Step 4: AppHost をコピー
Copy-Item ../temp/r2r-real/CSharp-AES.exe ../publish/csharp-r2r-stomped/
```

#### 実行結果の比較

```powershell
# R2R ネイティブ（デフォルト）— KEY_B で暗号化＋隠しメッセージ
.\publish\csharp-r2r-stomped\CSharp-AES.exe "Hello, World!"
# This is hidden message
# pS/yJaOm85nPCfBkoA88Lg==

# JIT フォールバック（R2R 無効）— IL の KeyA で暗号化
$env:DOTNET_ReadyToRun = "0"
.\publish\csharp-r2r-stomped\CSharp-AES.exe "Hello, World!"
# UQecBbw7uM5BaUfDyshHgw==
$env:DOTNET_ReadyToRun = $null
```

> dnSpy で `publish/csharp-r2r-stomped/CSharp-AES.dll` を開くと、`Main` は `KeyA` を使用しており、`GetRealKey()` / `WriteHiddenMessage()` は空スタブとして表示されます。`KeyB` という名前のフィールドは存在しません。

---

## 必要な環境

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 以上（.NET Framework 4.8 向けビルドも包含）
- Ready to Run・Native AOT でのビルドには、対象 OS の C++ ビルドツールが別途必要です（上記参照）

---

## 各コンパイル方式の比較

| 方式              | 言語   | ターゲット | `.exe` の種類         | dnSpy 逆コンパイル        |
|-------------------|--------|------------|-----------------------|---------------------------|
| JIT               | VB.NET | net48      | マネージド PE（IL 含む）| **可（.exe を直接開ける）** |
| JIT               | C#     | net48      | マネージド PE（IL 含む）| **可（.exe を直接開ける）** |
| Ready to Run      | C#     | net10.0    | C++ AppHost + .dll    | 可（.dll を開く、IL + R2R） |
| R2R Stomped       | C#     | net10.0    | C++ AppHost + .dll    | IL は KeyA のみ表示（KEY_B は不可視） |
| Native AOT        | C#     | net10.0    | 純ネイティブ           | **不可**（IL なし）        |
