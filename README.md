# sd-png-info-utility

## License

MIT License

## なにが出来るもの？

Stable Diffusion で作成した画像の PNG Info を読み取ったり、画像に PNG Info を書き込んだりするコマンドラインツール

## 注意事項

やっつけで作ったのでエラー出力やヘルプメッセージが雑、十分にテストしてない。バグは報告してくれればたぶん直す。
ウイルスなど不安に思う人は、コード量は少ないので全部読んで安全であることを確認してください。

## 動作環境

Windows 専用 (dotnet core に書き換えるモチベーションがない)
.NET Framework 4.8 のランタイムが必要

## ビルド方法

Visual Studio Community などで sd-png-info-utility.sln を開けばビルドできます。
.NET Framework 4.8 の開発者パックが必要

## 使い方

ビルドすると sdimg.exe ができるのでこれをコマンドで呼び出して
`sdimg.exe {コマンド名} {引数...}`
コマンド名は WritePngInfo または ReadPngInfo (大文字小文字は無視)

`sdimg --help` や `sdimg {コマンド名} --help` でヘルプ表示

## 使い方の例 (詳しくは --help オプションつけてヘルプテキスト見てください)

### ReadPngInfo: PNG Info 読み込み (入力は PNG と JPEG に対応)

- `sdimg readpnginfo C:/x/a.png`: 指定した画像の PNG Info を標準出力する
- `sdimg read C:/x/a.png`: readpnginfo は read でも OK
- `sdimg read C:/x/b.png C:/x/c.jpg`: 複数ファイルを指定可能
- `sdimg read C:/x/d.png C:/x/e.jpg --output C:/x/f.txt --overwrite`: 読み込んだ PNG Info をファイル (BOM なし UTF8) に上書き出力
- `sdimg read C:/x/g.png --output ?temporary-_HOGE.md --open`: 読み込んだ PNG Info を 一時ファイル ({ランダムな英数字}\_HOGE.md) に保存後にファイルを開く
- `sdimg read C:/x/a.png --pause --pause-message Enterで終了!!!`: 処理終了後に「Enter で終了!!!」と標準出力して、Enter を押すまで待機

### WritePngInfo: PNG Info 書き込み (入力画像はだいたい何でも OK。出力は PNG と JPEG に対応)

- `sdimg writepnginfo --input C:/x/i.webp --pnginfo C:/x/p.txt --output C:/x/o.png`: p.txt を PNG Info として i.webp に書き込んだ画像を o.png に保存する
- `sdimg write -i C:/x/i.webp -p C:/x/p.txt -o C:/x/o.png --overwrite`: 上書きすること以外は、同上
- `sdimg write -i C:/x/i.webp -p C:/x/p.txt -o C:/x/o.jpg --quality 50`: output(o)オプションの値の末尾が &quot;.jpe?g&quot; の場合は quality オプションの指定で品質を指定
