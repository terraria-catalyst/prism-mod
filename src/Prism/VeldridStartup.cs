using System;
using Microsoft.Xna.Framework;
using SDL2;
using Veldrid;
using Veldrid.OpenGL;

namespace TeamCatalyst.Prism;

internal static class VeldridStartup {
    public static GraphicsDevice? CreateGraphicsDevice(Game game, GraphicsDeviceOptions options, Action<string>? log = null) {
        var backend = GetGraphicsBackend();
        log?.Invoke("Got [preferred] backend: " + backend);

        if (backend is { } and not GraphicsBackend.Direct3D11 and not GraphicsBackend.OpenGL and not GraphicsBackend.Vulkan)
            throw new NotSupportedException("Unsupported graphics backend: " + backend);

        try {
            return backend switch {
                GraphicsBackend.Direct3D11 => CreateD3D11Device(game, options),
                GraphicsBackend.OpenGL => CreateOpenGlDevice(game, options),
                GraphicsBackend.Vulkan => CreateVulkanDevice(game, options),
                _ => throw new NotSupportedException("Unsupported graphics backend: " + backend),
            };
        }
        catch (Exception e) {
            log?.Invoke("Failed to create [preferred] graphics device: " + e);
            // return null;
        }

        // Manually try in descending order of priority.

        try {
            log?.Invoke("Trying Direct3D11 device...");
            return CreateD3D11Device(game, options);
        }
        catch (Exception e) {
            log?.Invoke("Failed to create Direct3D11 device: " + e);
        }

        try {
            log?.Invoke("Trying OpenGL device...");
            return CreateOpenGlDevice(game, options);
        }
        catch (Exception e) {
            log?.Invoke("Failed to create OpenGL device: " + e);
        }

        try {
            log?.Invoke("Trying Vulkan device...");
            return CreateVulkanDevice(game, options);
        }
        catch (Exception e) {
            log?.Invoke("Failed to create Vulkan device: " + e);
        }

        log?.Invoke("Failed to create any graphics device.");
        return null;
    }

    internal static GraphicsBackend? GetGraphicsBackend() {
        // Set by /gldevice:%s
        var forceDriver = Environment.GetEnvironmentVariable("FNA3D_FORCE_DRIVER");
        return forceDriver switch {
            // In order of priority:
            "D3D11" => GraphicsBackend.Direct3D11,
            "OpenGL" => GraphicsBackend.OpenGL,
            "Vulkan" => GraphicsBackend.Vulkan,
            _ => null,
        };
    }

    private static GraphicsDevice CreateD3D11Device(Game game, GraphicsDeviceOptions options) {
        return GraphicsDevice.CreateD3D11(options, new SwapchainDescription(GetSwapchainSource(game.Window.Handle), (uint)game.Window.ClientBounds.Width, (uint)game.Window.ClientBounds.Height, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat));
    }

    private static GraphicsDevice CreateVulkanDevice(Game game, GraphicsDeviceOptions options) {
        return GraphicsDevice.CreateVulkan(options, new SwapchainDescription(GetSwapchainSource(game.Window.Handle), (uint)game.Window.ClientBounds.Width, (uint)game.Window.ClientBounds.Height, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat));
    }

    private static GraphicsDevice CreateOpenGlDevice(Game game, GraphicsDeviceOptions options) {
        // TODO: Do we need to set variables?

        var platformInfo = new OpenGLPlatformInfo(
            SDL.SDL_GL_GetCurrentContext(),
            SDL.SDL_GL_GetProcAddress,
            context => SDL.SDL_GL_MakeCurrent(game.Window.Handle, context),
            SDL.SDL_GL_GetCurrentContext,
            () => SDL.SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero),
            SDL.SDL_GL_DeleteContext,
            () => SDL.SDL_GL_SwapWindow(game.Window.Handle),
            sync => SDL.SDL_GL_SetSwapInterval(sync ? 1 : 0)
        );

        return GraphicsDevice.CreateOpenGL(options, platformInfo, (uint)game.Window.ClientBounds.Width, (uint)game.Window.ClientBounds.Height);
    }

    private static SwapchainSource GetSwapchainSource(nint handle) {
        SDL.SDL_SysWMinfo info = new();
        SDL.SDL_GetVersion(out info.version);
        SDL.SDL_GetWindowWMInfo(handle, ref info);

        return info.subsystem switch {
            SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS => SwapchainSource.CreateWin32(info.info.win.window, info.info.win.hinstance),
            SDL.SDL_SYSWM_TYPE.SDL_SYSWM_X11 => SwapchainSource.CreateXlib(info.info.x11.display, info.info.x11.window),
            SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WAYLAND => SwapchainSource.CreateWayland(info.info.wl.display, info.info.wl.surface),
            SDL.SDL_SYSWM_TYPE.SDL_SYSWM_COCOA => SwapchainSource.CreateNSWindow(info.info.cocoa.window),
            _ => throw new PlatformNotSupportedException("Unsupported SDL2 subsystem: " + info.subsystem),
        };
    }
}
