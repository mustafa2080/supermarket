using System.Drawing;
using System.Windows.Forms;

namespace supermarket.Theme;

internal static class AppTheme
{
    public static readonly Color Primary = ColorTranslator.FromHtml("#1B4F72");
    public static readonly Color Secondary = ColorTranslator.FromHtml("#2E86C1");
    public static readonly Color Accent = ColorTranslator.FromHtml("#F39C12");
    public static readonly Color Success = ColorTranslator.FromHtml("#1E8449");
    public static readonly Color Danger = ColorTranslator.FromHtml("#C0392B");
    public static readonly Color Warning = ColorTranslator.FromHtml("#D4AC0D");
    public static readonly Color Background = ColorTranslator.FromHtml("#F4F6F7");
    public static readonly Color Surface = Color.White;
    public static readonly Color DarkText = ColorTranslator.FromHtml("#1A1A2E");
    public static readonly Color MutedText = ColorTranslator.FromHtml("#717D7E");
    public static readonly Color Border = ColorTranslator.FromHtml("#AED6F1");

    public static Font TitleFont { get; } = new("Tahoma", 18F, FontStyle.Bold);
    public static Font SectionFont { get; } = new("Tahoma", 12F, FontStyle.Bold);
    public static Font BodyFont { get; } = new("Tahoma", 10F, FontStyle.Regular);
    public static Font SmallFont { get; } = new("Tahoma", 8F, FontStyle.Regular);
    public static Font ButtonFont { get; } = new("Tahoma", 10F, FontStyle.Bold);

    public static Label CreateFieldLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font("Tahoma", 9F, FontStyle.Bold),
            ForeColor = Primary,
            Text = text,
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(3, 6, 3, 6)
        };
    }

    public static TextBox CreateTextBox(string? placeholder = null)
    {
        var textBox = new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            Font = BodyFont,
            BackColor = Surface,
            ForeColor = DarkText,
            Margin = new Padding(3, 3, 3, 10)
        };

        if (!string.IsNullOrWhiteSpace(placeholder))
        {
            textBox.PlaceholderText = placeholder;
        }

        return textBox;
    }

    public static ComboBox CreateComboBox()
    {
        return new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = BodyFont,
            BackColor = Surface,
            ForeColor = DarkText,
            Margin = new Padding(3, 3, 3, 10)
        };
    }

    public static NumericUpDown CreateNumericInput(int decimalPlaces = 2, decimal increment = 1)
    {
        return new NumericUpDown
        {
            DecimalPlaces = decimalPlaces,
            Increment = increment,
            Maximum = 999999999,
            Minimum = 0,
            ThousandsSeparator = true,
            Font = BodyFont,
            Margin = new Padding(3, 3, 3, 10)
        };
    }

    public static void StylePrimaryButton(Button button)
    {
        button.BackColor = Primary;
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = ButtonFont;
        button.Height = 40;
        button.Cursor = Cursors.Hand;
    }

    public static void StyleSecondaryButton(Button button)
    {
        button.BackColor = Surface;
        button.ForeColor = Primary;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.Font = ButtonFont;
        button.Height = 40;
        button.Cursor = Cursors.Hand;
    }

    public static Panel CreateCard()
    {
        return new Panel
        {
            BackColor = Surface,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(12),
            Padding = new Padding(18)
        };
    }

    public static void StyleInfoPanel(Panel panel)
    {
        panel.BackColor = ColorTranslator.FromHtml("#EBF5FB");
        panel.BorderStyle = BorderStyle.FixedSingle;
        panel.Padding = new Padding(14);
    }
}
