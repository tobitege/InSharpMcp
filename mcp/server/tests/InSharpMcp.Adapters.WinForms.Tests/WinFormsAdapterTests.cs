using InSharpMcp.Adapters.WinForms;
using InSharpMcp.Contracts;
using System.Text.Json;
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
                TestContext.Current.CancellationToken);

            Assert.True(snapshotResult.Success);
            var snapshot = Assert.IsType<UiTreeSnapshot>(snapshotResult.Data);
            Assert.Equal("0", snapshot.Root.ElementIdentifier);
            Assert.NotNull(snapshot.Root.Children);
            Assert.NotEmpty(snapshot.Root.Children);

            var metadataResult = await inspector.GetElementMetadataAsync(
                "0/0",
                new ToolLimits { MaxDepth = 3, MaxNodes = 20, MaxTextCharacters = 128 },
                TestContext.Current.CancellationToken);

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

            var result = await invoker.InvokeDefaultActionAsync("0/0", TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.True(clicked);
        });

    [Fact]
    public Task ElementLookup_UsesPathWithoutDefaultNodeBudget()
        => RunStaAsync(async () =>
        {
            using var form = new Form { Name = "RootForm" };
            var clicked = false;
            for (var index = 0; index < 600; index++)
            {
                var button = new Button
                {
                    Name = $"Button{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                    Text = $"Button {index.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                };

                if (index == 599)
                {
                    button.Tag = new PathLookupDataContext("late");
                    button.Click += (_, _) => clicked = true;
                }

                form.Controls.Add(button);
            }

            form.Show();
            Application.DoEvents();
            var dispatcher = new WinFormsUiDispatcher(form);
            var inspector = new WinFormsVisualTreeInspector(form, dispatcher);
            var invoker = new WinFormsAutomationPeerInvoker(form, dispatcher);
            var snapshotResult = await inspector.GetVisualTreeSnapshotAsync(
                new ToolLimits { MaxDepth = 2, MaxNodes = 700 },
                TestContext.Current.CancellationToken);

            Assert.True(snapshotResult.Success);

            var metadataResult = await inspector.GetElementMetadataAsync(
                "0/599",
                new ToolLimits { MaxNodes = 1 },
                TestContext.Current.CancellationToken);

            Assert.True(metadataResult.Success);
            var metadata = Assert.IsType<ElementMetadata>(metadataResult.Data);
            Assert.Equal("Button599", metadata.Name);

            var dataContextResult = await inspector.GetElementDataContextAsync(
                "0/599",
                new ToolLimits { MaxNodes = 1 },
                TestContext.Current.CancellationToken);

            Assert.True(dataContextResult.Success);
            var dataContext = Assert.IsType<DataContextMetadata>(dataContextResult.Data);
            Assert.EndsWith(nameof(PathLookupDataContext), dataContext.TypeName, StringComparison.Ordinal);

            var invokeResult = await invoker.InvokeDefaultActionAsync("0/599", TestContext.Current.CancellationToken);

            Assert.True(invokeResult.Success);
            Assert.True(clicked);
        });

    [Fact]
    public Task ElementPropertyEditor_SetsElementAndDataContextProperties()
        => RunStaAsync(async () =>
        {
            using var form = CreateForm();
            var button = Assert.IsType<Button>(form.Controls[0]);
            var dataContext = new MutableDataContext { Title = "Before" };
            button.Tag = dataContext;
            var dispatcher = new WinFormsUiDispatcher(form);
            var editor = new WinFormsElementPropertyEditor(form, dispatcher);
            using var textValue = JsonDocument.Parse("\"Updated action\"");
            using var dataContextValue = JsonDocument.Parse("true");

            var elementResult = await editor.SetElementPropertyAsync(
                "0/0",
                ElementPropertyTarget.Element,
                nameof(Button.Text),
                textValue.RootElement,
                TestContext.Current.CancellationToken);
            var dataContextResult = await editor.SetElementPropertyAsync(
                "0/0",
                ElementPropertyTarget.DataContext,
                nameof(MutableDataContext.IsDirty),
                dataContextValue.RootElement,
                TestContext.Current.CancellationToken);

            Assert.True(elementResult.Success);
            Assert.Equal("Updated action", button.Text);
            Assert.True(dataContextResult.Success);
            Assert.True(dataContext.IsDirty);
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

            var result = await simulator.PointerClickAsync(7, 11, TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            var expected = form.PointToScreen(new System.Drawing.Point(7, 11));
            Assert.Equal(expected.X, input.ScreenX);
            Assert.Equal(expected.Y, input.ScreenY);
        });

    [Fact]
    public Task PointerInputSimulator_RejectsOutOfRootCoordinates() =>
        RunStaAsync(async () =>
        {
            using var form = CreateForm();
            form.CreateControl();
            var dispatcher = new WinFormsUiDispatcher(form);
            var input = new RecordingWinFormsInputInjector();
            var simulator = new WinFormsPointerInputSimulator(form, dispatcher, input);

            var result = await simulator.PointerClickAsync(form.ClientSize.Width, 1, TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Equal("out_of_bounds", result.ErrorCode);
            Assert.Null(input.ScreenX);
            Assert.Null(input.ScreenY);
        });

    [Fact]
    public Task VisualTreeInspector_ReturnsScrolledRootRelativeBounds() =>
        RunStaAsync(async () =>
        {
            using var form = CreateScrolledForm(out var button);
            form.Show();
            ((Panel)form.Controls[0]).AutoScrollPosition = new System.Drawing.Point(0, 70);
            Application.DoEvents();
            var dispatcher = new WinFormsUiDispatcher(form);
            var inspector = new WinFormsVisualTreeInspector(form, dispatcher);

            var result = await inspector.GetElementMetadataAsync(
                "0/0/0",
                new ToolLimits(),
                TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            var metadata = Assert.IsType<ElementMetadata>(result.Data);
            var expected = form.PointToClient(button.PointToScreen(System.Drawing.Point.Empty));
            Assert.Equal(expected.X, metadata.Bounds?.X);
            Assert.Equal(expected.Y, metadata.Bounds?.Y);
            Assert.Equal(button.Width, metadata.Bounds?.Width);
            Assert.Equal(button.Height, metadata.Bounds?.Height);
        });

    [Fact]
    public Task ElementClick_ClicksScrolledNestedControlVisibleCenter() =>
        RunStaAsync(async () =>
        {
            using var form = CreateScrolledForm(out var button);
            form.Show();
            ((Panel)form.Controls[0]).AutoScrollPosition = new System.Drawing.Point(0, 70);
            Application.DoEvents();
            var dispatcher = new WinFormsUiDispatcher(form);
            var input = new RecordingWinFormsInputInjector();
            var simulator = new WinFormsPointerInputSimulator(form, dispatcher, input);

            var result = await simulator.ElementClickAsync("0/0/0", TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            var panel = (Panel)form.Controls[0];
            var buttonRect = new Rectangle(button.PointToScreen(System.Drawing.Point.Empty), button.Size);
            var panelClientRect = new Rectangle(panel.PointToScreen(System.Drawing.Point.Empty), panel.ClientSize);
            var visible = Rectangle.Intersect(buttonRect, panelClientRect);
            var expected = new System.Drawing.Point(
                visible.Left + visible.Width / 2,
                visible.Top + visible.Height / 2);
            Assert.Equal(expected.X, input.ScreenX);
            Assert.Equal(expected.Y, input.ScreenY);
        });

    [Fact]
    public Task ElementClick_RejectsDisabledControl()
        => RunStaAsync(async () =>
        {
            using var form = CreateScrolledForm(out var button);
            button.Enabled = false;
            form.Show();
            ((Panel)form.Controls[0]).AutoScrollPosition = new System.Drawing.Point(0, 70);
            Application.DoEvents();
            var dispatcher = new WinFormsUiDispatcher(form);
            var input = new RecordingWinFormsInputInjector();
            var simulator = new WinFormsPointerInputSimulator(form, dispatcher, input);

            var result = await simulator.ElementClickAsync("0/0/0", TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Equal("not_clickable", result.ErrorCode);
            Assert.Null(input.ScreenX);
            Assert.Null(input.ScreenY);
        });

    [Fact]
    public Task ElementClick_RejectsHiddenAndEmptyControls()
        => RunStaAsync(async () =>
        {
            using var form = CreateForm();
            var hiddenButton = new Button
            {
                Name = "HiddenButton",
                Visible = false,
                Size = new System.Drawing.Size(50, 24),
            };
            var emptyButton = new Button
            {
                Name = "EmptyButton",
                Location = new System.Drawing.Point(60, 0),
                Size = new System.Drawing.Size(0, 24),
            };
            form.Controls.Add(hiddenButton);
            form.Controls.Add(emptyButton);
            form.Show();
            Application.DoEvents();
            var dispatcher = new WinFormsUiDispatcher(form);
            var input = new RecordingWinFormsInputInjector();
            var simulator = new WinFormsPointerInputSimulator(form, dispatcher, input);

            var hiddenResult = await simulator.ElementClickAsync("0/1", TestContext.Current.CancellationToken);
            var emptyResult = await simulator.ElementClickAsync("0/2", TestContext.Current.CancellationToken);

            Assert.False(hiddenResult.Success);
            Assert.Equal("not_clickable", hiddenResult.ErrorCode);
            Assert.False(emptyResult.Success);
            Assert.Equal("not_clickable", emptyResult.ErrorCode);
            Assert.Null(input.ScreenX);
            Assert.Null(input.ScreenY);
        });

    [Fact]
    public Task PointerInputSimulator_ForwardsKeyAndTextInput() =>
        RunStaAsync(async () =>
        {
            using var form = CreateForm();
            var dispatcher = new WinFormsUiDispatcher(form);
            var input = new RecordingWinFormsInputInjector();
            var simulator = new WinFormsPointerInputSimulator(form, dispatcher, input);

            var keyResult = await simulator.KeyPressAsync("enter", ["ctrl"], TestContext.Current.CancellationToken);
            var textResult = await simulator.TypeTextAsync("hello", TestContext.Current.CancellationToken);

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

            var result = await provider.CaptureScreenshotAsync(TestContext.Current.CancellationToken);

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

    private static Form CreateScrolledForm(out Button button)
    {
        var form = new Form
        {
            Name = "RootForm",
            ClientSize = new System.Drawing.Size(220, 140),
        };
        var panel = new Panel
        {
            Name = "ScrollHost",
            AutoScroll = true,
            Location = new System.Drawing.Point(20, 15),
            Size = new System.Drawing.Size(100, 50),
        };
        button = new Button
        {
            Name = "TargetButton",
            Text = "Target",
            Location = new System.Drawing.Point(10, 80),
            Size = new System.Drawing.Size(50, 24),
        };
        panel.Controls.Add(button);
        form.Controls.Add(panel);
        panel.AutoScrollPosition = new System.Drawing.Point(0, 70);
        return form;
    }

    private sealed record PathLookupDataContext(string Value);

    private sealed class MutableDataContext
    {
        public string Title { get; set; } = string.Empty;

        public bool IsDirty { get; set; }
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
