using InSharpMcp.Adapters.WinForms;
using InSharpMcp.Contracts;
using System.Windows.Forms;

namespace InSharpMcp.Adapters.WinForms.Tests;

public sealed class WinFormsAdapterTests
{
    [Fact]
    public Task VisualTreeInspector_ReturnsControlMetadata() =>
        RunStaAsync(async () =>
        {
            using var form = CreateForm();
            var dispatcher = new WinFormsUiDispatcher(form);
            var inspector = new WinFormsVisualTreeInspector(form, dispatcher);

            var snapshotResult = await inspector.GetVisualTreeSnapshotAsync(
                new ToolLimits { MaxDepth = 3, MaxNodes = 20, MaxTextCharacters = 128 },
                CancellationToken.None);

            Assert.True(snapshotResult.Success);
            var snapshot = Assert.IsType<UiTreeSnapshot>(snapshotResult.Data);
            Assert.Equal("0", snapshot.Root.ElementIdentifier);
            Assert.NotNull(snapshot.Root.Children);
            Assert.NotEmpty(snapshot.Root.Children);

            var metadataResult = await inspector.GetElementMetadataAsync(
                "0/0",
                new ToolLimits { MaxDepth = 3, MaxNodes = 20, MaxTextCharacters = 128 },
                CancellationToken.None);

            Assert.True(metadataResult.Success);
            var metadata = Assert.IsType<ElementMetadata>(metadataResult.Data);
            Assert.Equal("PrimaryActionButton", metadata.Name);
            Assert.Equal("Primary action", metadata.AutomationId);
            Assert.Equal("Primary action", metadata.Text);
        });

    [Fact]
    public Task AutomationInvoker_PerformsButtonDefaultAction() =>
        RunStaAsync(async () =>
        {
            using var form = CreateForm();
            form.Show();
            Application.DoEvents();
            var button = Assert.IsType<Button>(form.Controls[0]);
            var clicked = false;
            button.Click += (_, _) => clicked = true;

            var dispatcher = new WinFormsUiDispatcher(form);
            var invoker = new WinFormsAutomationPeerInvoker(form, dispatcher);

            var result = await invoker.InvokeDefaultActionAsync("0/0", CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(clicked);
        });

    [Fact]
    public Task PointerInputSimulator_TranslatesClientCoordinatesToScreenCoordinates() =>
        RunStaAsync(async () =>
        {
            using var form = CreateForm();
            form.CreateControl();
            var dispatcher = new WinFormsUiDispatcher(form);
            var input = new RecordingWinFormsInputInjector();
            var simulator = new WinFormsPointerInputSimulator(form, dispatcher, input);

            var result = await simulator.PointerClickAsync(7, 11, CancellationToken.None);

            Assert.True(result.Success);
            var expected = form.PointToScreen(new System.Drawing.Point(7, 11));
            Assert.Equal(expected.X, input.ScreenX);
            Assert.Equal(expected.Y, input.ScreenY);
        });

    [Fact]
    public Task PointerInputSimulator_ForwardsKeyAndTextInput() =>
        RunStaAsync(async () =>
        {
            using var form = CreateForm();
            var dispatcher = new WinFormsUiDispatcher(form);
            var input = new RecordingWinFormsInputInjector();
            var simulator = new WinFormsPointerInputSimulator(form, dispatcher, input);

            var keyResult = await simulator.KeyPressAsync("enter", ["ctrl"], CancellationToken.None);
            var textResult = await simulator.TypeTextAsync("hello", CancellationToken.None);

            Assert.True(keyResult.Success);
            Assert.True(textResult.Success);
            Assert.Equal("enter", input.Key);
            Assert.Equal(["ctrl"], input.Modifiers);
            Assert.Equal("hello", input.Text);
        });

    [Fact]
    public Task ScreenshotProvider_CapturesPngBytes() =>
        RunStaAsync(async () =>
        {
            using var form = CreateForm();
            form.CreateControl();

            var dispatcher = new WinFormsUiDispatcher(form);
            var provider = new WinFormsScreenshotProvider(form, dispatcher);

            var result = await provider.CaptureScreenshotAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(result.PngBytes);
            Assert.NotEmpty(result.PngBytes);
            Assert.Equal(0x89, result.PngBytes[0]);
            Assert.Equal((byte)'P', result.PngBytes[1]);
            Assert.Equal((byte)'N', result.PngBytes[2]);
            Assert.Equal((byte)'G', result.PngBytes[3]);
        });

    private static Form CreateForm()
    {
        var form = new Form
        {
            Name = "RootForm",
            AccessibleName = "Root form",
            Width = 320,
            Height = 200,
        };
        form.Controls.Add(new Button
        {
            Name = "PrimaryActionButton",
            AccessibleName = "Primary action",
            Text = "Primary action",
            Width = 160,
            Height = 40,
        });
        return form;
    }

    private static Task RunStaAsync(Func<Task> action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                action().GetAwaiter().GetResult();
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private sealed class RecordingWinFormsInputInjector : IWinFormsInputInjector
    {
        public int? ScreenX { get; private set; }

        public int? ScreenY { get; private set; }

        public string? Key { get; private set; }

        public IReadOnlyList<string>? Modifiers { get; private set; }

        public string? Text { get; private set; }

        public ToolResult PointerClick(int screenX, int screenY)
        {
            ScreenX = screenX;
            ScreenY = screenY;
            return ToolResult.Ok("Pointer click sent.");
        }

        public ToolResult KeyPress(string key, IReadOnlyList<string> modifiers)
        {
            Key = key;
            Modifiers = modifiers.ToArray();
            return ToolResult.Ok("Key press sent.");
        }

        public ToolResult TypeText(string text)
        {
            Text = text;
            return ToolResult.Ok("Text input sent.");
        }
    }
}
