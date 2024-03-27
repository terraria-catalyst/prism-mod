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
    internal static class Unused;
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

        public override void Load() {
            base.Load();

            var lib = NativeLibrary.Load(Path.GetFullPath(ExtractNativeDependencies()));
            Logger.Debug($"Loaded spirv-cross lib @ 0x{lib:X2}");

            Logger.Debug("Shader inputs:");
            Logger.Debug("Vertex: " + vertex_code);
            Logger.Debug("Fragment: " + fragment_code);

            var vertexShader = CompileGlslToSpirv(vertex_code, null, ShaderStages.Vertex, new GlslCompileOptions());
            var fragmentShader = CompileGlslToSpirv(fragment_code, null, ShaderStages.Fragment, new GlslCompileOptions());
            var (vertexBytes, fragmentBytes) = CompileShaders(vertexShader, fragmentShader);
            Logger.Debug("Vertex SPIR-V (converted): " + Encoding.UTF8.GetString(vertexBytes));
            Logger.Debug("Fragment SPIR-V (converted): " + Encoding.UTF8.GetString(fragmentBytes));
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

        private static SpirvCompilationResult CompileGlslToSpirv(string sourceText, string? fileName, ShaderStages stage, GlslCompileOptions options) {
            return SpirvCompilation.CompileGlslToSpirv(sourceText, fileName, stage, options);
        }

        private static (byte[] vertex, byte[] fragment) CompileShaders(SpirvCompilationResult vertex, SpirvCompilationResult fragment) {
            var vertexBytes = vertex.SpirvBytes;
            var fragmentBytes = fragment.SpirvBytes;

            var backend = GetPlatformGraphicsBackend();
            if (backend != GraphicsBackend.Vulkan) {
                var compileTarget = GetPlatformGraphicsBackend() switch {
                    GraphicsBackend.Direct3D11 => CrossCompileTarget.HLSL,
                    GraphicsBackend.OpenGL => CrossCompileTarget.GLSL,
                    _ => throw new NotSupportedException("Unsupported graphics backend.")
                };

                var result = SpirvCompilation.CompileVertexFragment(
                    vertexBytes,
                    fragmentBytes,
                    compileTarget,
                    new CrossCompileOptions {
                        NormalizeResourceNames = true,
                        InvertVertexOutputY = compileTarget == CrossCompileTarget.HLSL,
                    }
                );

                vertexBytes = Encoding.UTF8.GetBytes(result.VertexShader);
                fragmentBytes = Encoding.UTF8.GetBytes(result.FragmentShader);
            }

            return (vertexBytes, fragmentBytes);
        }

        private static GraphicsBackend GetPlatformGraphicsBackend() {
            var preferred = VeldridStartup.GetGraphicsBackend();
            if (preferred is GraphicsBackend.Direct3D11 or null && OperatingSystem.IsWindows())
                return GraphicsBackend.Direct3D11;

            if (preferred is GraphicsBackend.OpenGL or null)
                return GraphicsBackend.OpenGL;

            return GraphicsBackend.Vulkan;
        }
    }
}
