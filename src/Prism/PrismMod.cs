using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Terraria;
using Terraria.ModLoader;
using Veldrid;
using Veldrid.SPIRV;

namespace Prism {
    [UsedImplicitly]
    internal static class Unused { }
}

namespace TeamCatalyst.Prism {
    public sealed class PrismMod : Mod {
        private const string vertex_code = """

                                           #version 450

                                           layout(location = 0) in vec2 Position;
                                           layout(location = 1) in vec4 Color;

                                           layout(location = 0) out vec4 fsin_Color;

                                           void main()
                                           {
                                               gl_Position = vec4(Position, 0, 1);
                                               fsin_Color = Color;
                                           }
                                           """;

        private const string fragment_code = """

                                             #version 450

                                             layout(location = 0) in vec4 fsin_Color;
                                             layout(location = 0) out vec4 fsout_Color;

                                             void main()
                                             {
                                                 fsout_Color = fsin_Color;
                                             }
                                             """;

        private readonly GraphicsDevice veldridDevice;

        public ResourceFactory ResourceFactory => veldridDevice.ResourceFactory;

        public PrismMod() {
            veldridDevice = VeldridStartup.CreateGraphicsDevice(Main.instance, new GraphicsDeviceOptions { PreferStandardClipSpaceYDirection = true, PreferDepthRangeZeroToOne = true }) ?? throw new PlatformNotSupportedException("Failed to initialize graphics device.");
        }

        public override void Load() {
            base.Load();

            var lib = NativeLibrary.Load(Path.GetFullPath(ExtractNativeDependencies()));
            Logger.Debug($"Loaded spirv-cross lib @ 0x{lib:X2}");

            Logger.Debug("Shader inputs:");
            Logger.Debug("Vertex: " + vertex_code);
            Logger.Debug("Fragment: " + fragment_code);

            var vertexDesc = new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertex_code), "main");
            var fragmentDesc = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragment_code), "main");
            var shaders = ResourceFactory.CreateFromSpirv(vertexDesc, fragmentDesc);

            foreach (var shader in shaders) {
                _ = shader;
            }
        }

        private string ExtractNativeDependencies() {
            var platformName = OperatingSystem.IsMacOS()
                ? "osx"
                : OperatingSystem.IsWindows()
                    ? "win"
                    : "linux";

            var ext = OperatingSystem.IsMacOS()
                ? "dylib"
                : OperatingSystem.IsWindows()
                    ? "dll"
                    : "so";

            var fileName = "libveldrid-spirv." + ext;

            if (!OperatingSystem.IsMacOS())
                platformName += "-x" + (Environment.Is64BitProcess ? "64" : "32");

            if (System.IO.File.Exists(fileName))
                System.IO.File.Delete(fileName);

            using var dll = GetFileStream($"lib/native/{platformName}/{fileName}");
            using var fs = System.IO.File.OpenWrite(fileName);
            dll.CopyTo(fs);
            return fileName;
        }
    }
}
