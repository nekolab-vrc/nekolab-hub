// 公開のたびに作る（手で書かない）：パッケージの各ファイル（このファイルと .meta を除く）の SHA-256。
// NekoLabCheck の K-13 が、これと比べる。
namespace NekoLab.Hub
{
    static class NekoLabFiles
    {
        public static readonly string Version = "1.0.0";

        public static readonly string[,] List =
        {
            { "CHANGELOG.md", "cfd1fcd43c85b424a99aa09cace6d9131b3f7b988668425f98c982a5166a52d1" },
            { "Editor/NekoLab.Hub.Editor.asmdef", "5649bed2f57fa1ad8cdbb33cc857bdcf00bbfb66137bcabd241e199064cfa54a" },
            { "Editor/NekoLabAutoOpen.cs", "c9e854927608bd2d0f4e8bd6c6f6f7064ea0a88365eadad9feb69b2eb3d1a52b" },
            { "Editor/NekoLabBits.cs", "81dd9f59a75813db48255c31b70f635ba2a0c7170fe64c1bd90c848a49fdd3b9" },
            { "Editor/NekoLabCheck.cs", "c16153ad50492481b159ed6202ffdee0ab1cbb4e5903bfe34b0053f93941ffb7" },
            { "Editor/NekoLabHub.cs", "5b51ceeaee727385e410e3561110e2aa2c2e9df76b97f41ed47fbcd7d5c4261a" },
            { "Editor/NekoLabLook.cs", "c4cf9b77772e9ac3e15c23c92a7f00951d0762c9833e629845904c000b21da22" },
            { "Editor/NekoLabProduct.cs", "491ecffb7698d3cdf4d309deafae66d7af49b94e33ec852a323d7e64c1b8a8aa" },
            { "Editor/NekoLabSupport.cs", "f45b591146e54968e398dd89b3a0d41c0b08d3ddb77850c4f840996ae466320a" },
            { "Editor/NekoLabText.cs", "18ee90d0be96311517bb5f63d6d584164f956770a4873a60b0bb76dbc73cb9ec" },
            { "Editor/NekoLabUI.cs", "4983d8da9806c19ec1c19b489dd728e4b1146264b8bc1757995ab18a90a85e98" },
            { "Editor/NekoLabVersion.cs", "2f1e7f1a236d3a8c4c77272711c4a821f93ddfd1b532c94fd6014aab05994407" },
            { "LICENSE.md", "865826b4cd8e76774d9d1fbac81c51e526aaa38cc0c36943a4399883ddde73e6" },
            { "package.json", "df9933e9707d4c2b6a1f304f94e40e3ab5819289f36f071b8fb3770172618ee0" },
        };
    }
}
