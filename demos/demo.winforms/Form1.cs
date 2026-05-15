using InSharpMcp.Adapters.WinForms;
using Microsoft.Extensions.DependencyInjection;

namespace InSharpMcp.Demo.WinForms;

public partial class Form1 : Form
{
    private readonly ServiceProvider _mcpServices;

    public Form1()
    {
        InitializeComponent();
        BuildDemoSurface();
        _mcpServices = BuildMcpServices();
        FormClosed += (_, _) => _mcpServices.Dispose();
    }

    private void BuildDemoSurface()
    {
        Text = "InSharpMcp WinForms Demo";
        Width = 900;
        Height = 720;

        var menu = new MenuStrip
        {
            Name = "DemoMenu",
            AccessibleName = "Demo menu",
        };
        var fileMenu = new ToolStripMenuItem("File") { Name = "FileMenuItem" };
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Reset form") { Name = "ResetMenuItem" });
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Close demo") { Name = "CloseMenuItem" });
        var toolsMenu = new ToolStripMenuItem("Tools") { Name = "ToolsMenuItem" };
        toolsMenu.DropDownItems.Add(new ToolStripMenuItem("Capture state") { Name = "CaptureStateMenuItem" });
        menu.Items.Add(fileMenu);
        menu.Items.Add(toolsMenu);

        var panel = new TableLayoutPanel
        {
            Name = "DemoLayout",
            AccessibleName = "Demo layout",
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(20),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var title = new Label
        {
            Name = "DemoTitle",
            AccessibleName = "Demo title",
            Text = "InSharpMcp WinForms Demo",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
        };
        panel.Controls.Add(title, 0, 0);
        panel.SetColumnSpan(title, 2);

        var subtitle = new Label
        {
            Name = "DemoSubtitle",
            AccessibleName = "Demo subtitle",
            Text = "Stable controls for selector, input, wait, screenshot, accessibility, and event validation.",
            AutoSize = true,
        };
        panel.Controls.Add(subtitle, 0, 1);
        panel.SetColumnSpan(subtitle, 2);

        panel.Controls.Add(CreateButton("PrimaryActionButton", "Primary action"), 0, 2);
        panel.Controls.Add(CreateButton("SecondaryActionButton", "Secondary action"), 1, 2);

        var singleLine = new TextBox
        {
            Name = "SingleLineInput",
            AccessibleName = "Single-line input",
            PlaceholderText = "Type a selector target",
            Dock = DockStyle.Fill,
        };
        panel.Controls.Add(singleLine, 0, 3);
        panel.SetColumnSpan(singleLine, 2);

        var editableText = new TextBox
        {
            Name = "EditableTextArea",
            AccessibleName = "Editable text area",
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = "Edit this multiline text to validate text input and metadata capture.",
            Height = 120,
            Dock = DockStyle.Fill,
        };
        panel.Controls.Add(editableText, 0, 4);
        panel.SetColumnSpan(editableText, 2);

        var optionPanel = new FlowLayoutPanel
        {
            Name = "OptionPanel",
            AccessibleName = "Option panel",
            AutoSize = true,
            Dock = DockStyle.Fill,
        };
        optionPanel.Controls.Add(new CheckBox { Name = "ReadyCheckBox", AccessibleName = "Ready state", Text = "Ready state" });
        optionPanel.Controls.Add(new ComboBox
        {
            Name = "ModeComboBox",
            AccessibleName = "Mode",
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 180,
            DataSource = new[] { "Idle", "Running", "Complete" },
        });
        optionPanel.Controls.Add(new TrackBar
        {
            Name = "ProgressSlider",
            AccessibleName = "Progress",
            Minimum = 0,
            Maximum = 100,
            Value = 42,
            Width = 220,
        });
        optionPanel.Controls.Add(new ProgressBar
        {
            Name = "ProgressBar",
            AccessibleName = "Progress bar",
            Minimum = 0,
            Maximum = 100,
            Value = 42,
            Width = 160,
        });
        panel.Controls.Add(optionPanel, 0, 5);
        panel.SetColumnSpan(optionPanel, 2);

        var lorem = new TextBox
        {
            Name = "LoremIpsumScrollArea",
            AccessibleName = "Lorem ipsum scroll area",
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 180,
            Dock = DockStyle.Fill,
            Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer selector checks need stable text, nested controls, scrollable regions, and repeated paragraphs. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Donec input validation can move focus, type text, and inspect accessible names. Curabitur waits can observe controls appearing, labels changing, and list content remaining deterministic. Praesent commodo cursus magna, vel scelerisque nisl consectetur et. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed posuere consectetur est at lobortis.",
        };
        panel.Controls.Add(lorem, 0, 6);
        panel.SetColumnSpan(lorem, 2);

        var list = new ListBox
        {
            Name = "ValidationList",
            AccessibleName = "Validation list",
            Height = 120,
            Dock = DockStyle.Fill,
        };
        list.Items.AddRange(["Selector target row", "Accessibility target row", "Event capture target row"]);
        panel.Controls.Add(list, 0, 7);
        panel.SetColumnSpan(list, 2);

        MainMenuStrip = menu;
        Controls.Add(panel);
        Controls.Add(menu);
    }

    private Button CreateButton(string name, string text)
    {
        return new Button
        {
            Name = name,
            AccessibleName = text,
            Text = text,
            Dock = DockStyle.Fill,
            Height = 40,
        };
    }

    private ServiceProvider BuildMcpServices()
    {
        var services = new ServiceCollection();
        services.AddInSharpMcpWinFormsAdapter(
            this,
            "InSharpMcp WinForms Demo",
            typeof(Form1).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            "WinForms");
        return services.BuildServiceProvider();
    }
}
