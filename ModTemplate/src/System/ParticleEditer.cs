using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace AWearableLight.System
{
    public class GuiDialogParticleEditor : GuiDialog
    {
        public Vec3d offset = new Vec3d(0, 0, 0);

        public GuiDialogParticleEditor(ICoreClientAPI capi) : base(capi)
        {
            SetupDialog();
        }

        public override string ToggleKeyCombinationCode => string.Empty;

        private void SetupDialog()
        {
            ElementBounds dialogBounds = ElementStdBounds
                .AutosizedMainDialog
                .WithFixedSize(270, 220)
                .WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fill;

            SingleComposer = capi.Gui
                .CreateCompo("particleeditor", dialogBounds)

                // Background
                .AddDialogBG(bgBounds.WithFixedSize(270, 200).WithFixedPadding(10).WithSizing(ElementSizing.Fixed))

                // Title
                .AddDialogTitleBar("Particle Editor", OnClose)

                // Current value
                .AddDynamicText(
                    $"Current: {offset.X:0.00}, {offset.Y:0.00}, {offset.Z:0.00}",
                    CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(10, 35, 280, 20),
                    "current"
                )

                // X
                .AddStaticText("X", CairoFont.WhiteDetailText(), ElementBounds.Fixed(10, 70, 20, 20))
                .AddNumberInput(ElementBounds.Fixed(40, 70, 100, 25), OnInputX, key: "x")

                // Y
                .AddStaticText("Y", CairoFont.WhiteDetailText(), ElementBounds.Fixed(10, 105, 20, 20))
                .AddNumberInput(ElementBounds.Fixed(40, 105, 100, 25), OnInputY, key: "y")

                // Z
                .AddStaticText("Z", CairoFont.WhiteDetailText(), ElementBounds.Fixed(10, 140, 20, 20))
                .AddNumberInput(ElementBounds.Fixed(40, 140, 100, 25), OnInputZ, key: "z")

                // Buttons
                .AddSmallButton("Reset", OnReset, ElementBounds.Fixed(170, 70, 100, 30))
                .AddSmallButton("Copy", OnCopy, ElementBounds.Fixed(170, 110, 100, 30))

                .Compose(true);

            // Initial values
            SingleComposer.GetNumberInput("x")?.SetValue((float)offset.X);
            SingleComposer.GetNumberInput("y")?.SetValue((float)offset.Y);
            SingleComposer.GetNumberInput("z")?.SetValue((float)offset.Z);
        }

        public override bool TryOpen()
        {
            SingleComposer?.Dispose();

            SetupDialog();

            return base.TryOpen();
        }

        private void OnInputX(string text)
        {
            if (float.TryParse(text, out float value))
            {
                offset.X = value;
                UpdateCurrentText();
            }
        }

        private void OnInputY(string text)
        {
            if (float.TryParse(text, out float value))
            {
                offset.Y = value;
                UpdateCurrentText();
            }
        }

        private void OnInputZ(string text)
        {
            if (float.TryParse(text, out float value))
            {
                offset.Z = value;
                UpdateCurrentText();
            }
        }

        public void OnClose()
        {
            TryClose();
        }

        private void UpdateCurrentText()
        {
            SingleComposer.GetDynamicText("current")
                ?.SetNewText(
                    $"Current: {offset.X:0.00}, {offset.Y:0.00}, {offset.Z:0.00}"
                );
        }

        private bool OnReset()
        {
            offset = new Vec3d(0, 0, 0);
            UpdateCurrentText();
            return true;
        }

        private bool OnCopy()
        {
            capi.Input.ClipboardText =
                $"{offset.X:0.###}, {offset.Y:0.###}, {offset.Z:0.###}";

            return true;
        }
    }
}
