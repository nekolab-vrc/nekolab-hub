// 公開のたびに作る（手で書かない）：パッケージの各ファイル（このファイルと .meta を除く）の SHA-256。
// NekoLabCheck の K-13 が、これと比べる。
namespace NekoLab.Hub
{
    static class NekoLabFiles
    {
        public static readonly string Version = "1.0.1";

        public static readonly string[,] List =
        {
            { "CHANGELOG.md", "3af8b21768b487360ebfedad3dc6b8e306fc7f26aa9019884590cbae80309aec" },
            { "Editor/NekoLab.Hub.Editor.asmdef", "5649bed2f57fa1ad8cdbb33cc857bdcf00bbfb66137bcabd241e199064cfa54a" },
            { "Editor/NekoLabAutoOpen.cs", "c9e854927608bd2d0f4e8bd6c6f6f7064ea0a88365eadad9feb69b2eb3d1a52b" },
            { "Editor/NekoLabBits.cs", "c002b3c89b70a989b66e4d2eff4d4cd90dbbad31b1af7447e0d8e23691be26f7" },
            { "Editor/NekoLabCheck.cs", "c16153ad50492481b159ed6202ffdee0ab1cbb4e5903bfe34b0053f93941ffb7" },
            { "Editor/NekoLabHub.cs", "d78f494fc6b843f99a4d31aef8b32ff1748fceae07840e0ba607756c4dda3619" },
            { "Editor/NekoLabLook.cs", "c382131fea88d89a73e36c0612d33668e9c7c76d1e971dae2e7ab3c565e4a131" },
            { "Editor/NekoLabProduct.cs", "189225f0495a2389bb8d70ad98679b1e71006ad334d150048438b3d7f0491647" },
            { "Editor/NekoLabSupport.cs", "f45b591146e54968e398dd89b3a0d41c0b08d3ddb77850c4f840996ae466320a" },
            { "Editor/NekoLabText.cs", "e1e28d1c27be1cb59233dea7b27642a4b84265335094a20a1b184665929f6b2a" },
            { "Editor/NekoLabUI.cs", "4983d8da9806c19ec1c19b489dd728e4b1146264b8bc1757995ab18a90a85e98" },
            { "Editor/NekoLabVersion.cs", "2f1e7f1a236d3a8c4c77272711c4a821f93ddfd1b532c94fd6014aab05994407" },
            { "LICENSE.md", "865826b4cd8e76774d9d1fbac81c51e526aaa38cc0c36943a4399883ddde73e6" },
            { "package.json", "2c0ecef3369d6d0cedd121fa1ada1981b9aa6eb154446b735283f965bec5fb81" },
        };
    }
}
