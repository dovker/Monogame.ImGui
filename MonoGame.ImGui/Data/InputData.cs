using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;

namespace MonoGame.ImGui.Data; 

/// <summary>
///     Contains the GUIRenderer's input data elements.
/// </summary>
public class InputData {
    public List<int> KeyMap;
    public int Scrollwheel;

    public InputData() {
        Scrollwheel = 0;
        KeyMap = new List<int>();
    }

    public void Update(Game game) {
        if (!game.IsActive)
            return;
        
        var io = ImGuiNET.ImGui.GetIO();
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        for (var i = 0; i < KeyMap.Count; i++)
            io.KeysDown[KeyMap[i]] = keyboard.IsKeyDown((Keys) KeyMap[i]);

        io.KeyShift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        io.KeyCtrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        io.KeyAlt = keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);
        io.KeySuper = keyboard.IsKeyDown(Keys.LeftWindows) || keyboard.IsKeyDown(Keys.RightWindows);

        io.DisplaySize = new Vector2(device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight);
        io.DisplayFramebufferScale = new Vector2(1f, 1f);

        io.MousePos = new Vector2(mouse.X, mouse.Y);

        io.MouseDown[0] = mouse.LeftButton == ButtonState.Pressed;
        io.MouseDown[1] = mouse.RightButton == ButtonState.Pressed;
        io.MouseDown[2] = mouse.MiddleButton == ButtonState.Pressed;

        var scrollDelta = mouse.ScrollWheelValue - Scrollwheel;
        io.MouseWheel = scrollDelta > 0 ? 1 : scrollDelta < 0 ? -1 : 0;
        Scrollwheel = mouse.ScrollWheelValue;

        UpdateCursor();
    }
    
    private void UpdateCursor() {
        MouseCursor mouseCursor = ImGui.GetMouseCursor() switch {
            ImGuiMouseCursor.None => MouseCursor.Arrow,
            ImGuiMouseCursor.Arrow => MouseCursor.Arrow,
            ImGuiMouseCursor.TextInput => MouseCursor.IBeam,
            ImGuiMouseCursor.ResizeAll => MouseCursor.SizeAll,
            ImGuiMouseCursor.ResizeNS => MouseCursor.SizeNS,
            ImGuiMouseCursor.ResizeEW => MouseCursor.SizeWE,
            ImGuiMouseCursor.ResizeNESW => MouseCursor.SizeNESW,
            ImGuiMouseCursor.ResizeNWSE => MouseCursor.SizeNWSE,
            ImGuiMouseCursor.Hand => MouseCursor.Hand,
            ImGuiMouseCursor.NotAllowed => MouseCursor.No,
            ImGuiMouseCursor.COUNT => MouseCursor.Wait,
            _ => throw new ArgumentOutOfRangeException()
        };

        Mouse.PlatformSetCursor(mouseCursor);
    }

    public InputData Initialize(Game game) {
        var io = ImGuiNET.ImGui.GetIO();

        KeyMap.Add(io.KeyMap[(int) ImGuiKey.Tab] = (int) Keys.Tab);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.LeftArrow] = (int) Keys.Left);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.RightArrow] = (int) Keys.Right);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.UpArrow] = (int) Keys.Up);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.DownArrow] = (int) Keys.Down);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.PageUp] = (int) Keys.PageUp);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.PageDown] = (int) Keys.PageDown);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.Home] = (int) Keys.Home);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.End] = (int) Keys.End);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.Delete] = (int) Keys.Delete);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.Backspace] = (int) Keys.Back);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.Enter] = (int) Keys.Enter);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.Escape] = (int) Keys.Escape);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.A] = (int) Keys.A);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.C] = (int) Keys.C);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.V] = (int) Keys.V);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.X] = (int) Keys.X);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.Y] = (int) Keys.Y);
        KeyMap.Add(io.KeyMap[(int) ImGuiKey.Z] = (int) Keys.Z);

        game.Window.TextInput += (sender, args) => {
            if (args.Character != '\t')
                io.AddInputCharacter(args.Character);
        };

        io.Fonts.AddFontDefault();
        return this;
    }
}