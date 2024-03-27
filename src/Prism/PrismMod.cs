﻿using System;
using JetBrains.Annotations;
using Terraria;
using Terraria.ModLoader;
using Veldrid;

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

        public PrismMod() {
            veldridDevice = VeldridStartup.CreateGraphicsDevice(Main.instance, new GraphicsDeviceOptions { PreferStandardClipSpaceYDirection = true, PreferDepthRangeZeroToOne = true }) ?? throw new PlatformNotSupportedException("Failed to initialize graphics device.");
        }

        public override void Load() {
            base.Load();

            Logger.Debug("Shader inputs:");
            Logger.Debug("Vertex: " + vertex_code);
            Logger.Debug("Fragment: " + fragment_code);
        }
    }
}