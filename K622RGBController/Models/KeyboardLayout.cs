namespace K622RGBController.Models;

/// <summary>
/// Defines the physical TKL layout of the Redragon K622 keyboard.
/// The Sinowealth chip reads HID packets in COLUMN-MAJOR order.
/// </summary>
public static class KeyboardLayout
{
    public const int NumRows = 6;
    public const int NumCols = 16;

    /// <summary>
    /// 6×16 grid layout. "NAN" = empty slot (padding in the HID packet).
    /// </summary>
    public static readonly string[,] Layout = new string[,]
    {
        { "Esc",    "F1",  "F2",   "F3",   "F4",   "F5",   "F6",   "F7",   "F8",   "F9",  "F10",  "F11",  "F12",  "PrtSc", "Pause", "Del"    },
        { "`",      "1",   "2",    "3",    "4",    "5",    "6",    "7",    "8",    "9",   "0",    "-",    "=",    "Bksp",  "NAN",   "Home"   },
        { "Tab",    "Q",   "W",    "E",    "R",    "T",    "Y",    "U",    "I",    "O",   "P",    "[",    "]",    "\\",    "NAN",   "End"    },
        { "Caps",   "A",   "S",    "D",    "F",    "G",    "H",    "J",    "K",    "L",   ";",    "'",    "NAN",  "Enter", "NAN",   "PgUp"   },
        { "LShift", "Z",   "X",    "C",    "V",    "B",    "N",    "M",    ",",    ".",   "/",    "NAN",  "NAN",  "RShift","Up",    "PgDn"   },
        { "LCtrl",  "Win", "LAlt", "NAN",  "NAN",  "Space","NAN",  "NAN",  "RAlt", "Fn",  "RCtrl","NAN",  "NAN",  "Left",  "Down",  "Right"  },
    };

    /// <summary>
    /// All real key names in column-major order (matching HID packet order).
    /// </summary>
    public static readonly List<string> KeyNames = new();

    /// <summary>
    /// Map: key_name -> index in the flat color array.
    /// </summary>
    public static readonly Dictionary<string, int> KeyIndexMap = new();

    /// <summary>
    /// Map: key_name -> (row, col) position in the grid.
    /// </summary>
    public static readonly Dictionary<string, (int Row, int Col)> KeyPosMap = new();

    /// <summary>
    /// Total addressable key slots.
    /// </summary>
    public static int TotalKeys => KeyNames.Count;

    static KeyboardLayout()
    {
        int idx = 0;
        // Column-major iteration to match Sinowealth packet order
        for (int col = 0; col < NumCols; col++)
        {
            for (int row = 0; row < NumRows; row++)
            {
                string key = Layout[row, col];
                if (key != "NAN")
                {
                    KeyNames.Add(key);
                    KeyIndexMap[key] = idx;
                    KeyPosMap[key] = (row, col);
                    idx++;
                }
            }
        }
    }

    /// <summary>
    /// Visual layout for rendering the keyboard in the GUI.
    /// Each key: (x, y, width, height) in key-units.
    /// </summary>
    public static readonly Dictionary<string, (double X, double Y, double W, double H)> VisualLayout = new()
    {
        // Row 0: Function keys
        ["Esc"]    = (0, 0, 1.0, 1.0),
        ["F1"]     = (2, 0, 1.0, 1.0),
        ["F2"]     = (3, 0, 1.0, 1.0),
        ["F3"]     = (4, 0, 1.0, 1.0),
        ["F4"]     = (5, 0, 1.0, 1.0),
        ["F5"]     = (6.5, 0, 1.0, 1.0),
        ["F6"]     = (7.5, 0, 1.0, 1.0),
        ["F7"]     = (8.5, 0, 1.0, 1.0),
        ["F8"]     = (9.5, 0, 1.0, 1.0),
        ["F9"]     = (11, 0, 1.0, 1.0),
        ["F10"]    = (12, 0, 1.0, 1.0),
        ["F11"]    = (13, 0, 1.0, 1.0),
        ["F12"]    = (14, 0, 1.0, 1.0),
        ["PrtSc"]  = (15.25, 0, 1.0, 1.0),
        ["Pause"]  = (16.25, 0, 1.0, 1.0),
        ["Del"]    = (17.25, 0, 1.0, 1.0),

        // Row 1: Number row
        ["`"]      = (0, 1.3, 1.0, 1.0),
        ["1"]      = (1, 1.3, 1.0, 1.0),
        ["2"]      = (2, 1.3, 1.0, 1.0),
        ["3"]      = (3, 1.3, 1.0, 1.0),
        ["4"]      = (4, 1.3, 1.0, 1.0),
        ["5"]      = (5, 1.3, 1.0, 1.0),
        ["6"]      = (6, 1.3, 1.0, 1.0),
        ["7"]      = (7, 1.3, 1.0, 1.0),
        ["8"]      = (8, 1.3, 1.0, 1.0),
        ["9"]      = (9, 1.3, 1.0, 1.0),
        ["0"]      = (10, 1.3, 1.0, 1.0),
        ["-"]      = (11, 1.3, 1.0, 1.0),
        ["="]      = (12, 1.3, 1.0, 1.0),
        ["Bksp"]   = (13, 1.3, 2.0, 1.0),
        ["Home"]   = (17.25, 1.3, 1.0, 1.0),

        // Row 2: QWERTY
        ["Tab"]    = (0, 2.3, 1.5, 1.0),
        ["Q"]      = (1.5, 2.3, 1.0, 1.0),
        ["W"]      = (2.5, 2.3, 1.0, 1.0),
        ["E"]      = (3.5, 2.3, 1.0, 1.0),
        ["R"]      = (4.5, 2.3, 1.0, 1.0),
        ["T"]      = (5.5, 2.3, 1.0, 1.0),
        ["Y"]      = (6.5, 2.3, 1.0, 1.0),
        ["U"]      = (7.5, 2.3, 1.0, 1.0),
        ["I"]      = (8.5, 2.3, 1.0, 1.0),
        ["O"]      = (9.5, 2.3, 1.0, 1.0),
        ["P"]      = (10.5, 2.3, 1.0, 1.0),
        ["["]      = (11.5, 2.3, 1.0, 1.0),
        ["]"]      = (12.5, 2.3, 1.0, 1.0),
        ["\\"]     = (13.5, 2.3, 1.5, 1.0),
        ["End"]    = (17.25, 2.3, 1.0, 1.0),

        // Row 3: Home row
        ["Caps"]   = (0, 3.3, 1.75, 1.0),
        ["A"]      = (1.75, 3.3, 1.0, 1.0),
        ["S"]      = (2.75, 3.3, 1.0, 1.0),
        ["D"]      = (3.75, 3.3, 1.0, 1.0),
        ["F"]      = (4.75, 3.3, 1.0, 1.0),
        ["G"]      = (5.75, 3.3, 1.0, 1.0),
        ["H"]      = (6.75, 3.3, 1.0, 1.0),
        ["J"]      = (7.75, 3.3, 1.0, 1.0),
        ["K"]      = (8.75, 3.3, 1.0, 1.0),
        ["L"]      = (9.75, 3.3, 1.0, 1.0),
        [";"]      = (10.75, 3.3, 1.0, 1.0),
        ["'"]      = (11.75, 3.3, 1.0, 1.0),
        ["Enter"]  = (12.75, 3.3, 2.25, 1.0),
        ["PgUp"]   = (17.25, 3.3, 1.0, 1.0),

        // Row 4: Shift row
        ["LShift"] = (0, 4.3, 2.25, 1.0),
        ["Z"]      = (2.25, 4.3, 1.0, 1.0),
        ["X"]      = (3.25, 4.3, 1.0, 1.0),
        ["C"]      = (4.25, 4.3, 1.0, 1.0),
        ["V"]      = (5.25, 4.3, 1.0, 1.0),
        ["B"]      = (6.25, 4.3, 1.0, 1.0),
        ["N"]      = (7.25, 4.3, 1.0, 1.0),
        ["M"]      = (8.25, 4.3, 1.0, 1.0),
        [","]      = (9.25, 4.3, 1.0, 1.0),
        ["."]      = (10.25, 4.3, 1.0, 1.0),
        ["/"]      = (11.25, 4.3, 1.0, 1.0),
        ["RShift"] = (12.25, 4.3, 2.75, 1.0),
        ["Up"]     = (16.25, 4.3, 1.0, 1.0),
        ["PgDn"]   = (17.25, 4.3, 1.0, 1.0),

        // Row 5: Bottom row
        ["LCtrl"]  = (0, 5.3, 1.25, 1.0),
        ["Win"]    = (1.25, 5.3, 1.25, 1.0),
        ["LAlt"]   = (2.5, 5.3, 1.25, 1.0),
        ["Space"]  = (3.75, 5.3, 6.25, 1.0),
        ["RAlt"]   = (10.0, 5.3, 1.25, 1.0),
        ["Fn"]     = (11.25, 5.3, 1.25, 1.0),
        ["RCtrl"]  = (12.5, 5.3, 1.25, 1.0),
        ["Left"]   = (15.25, 5.3, 1.0, 1.0),
        ["Down"]   = (16.25, 5.3, 1.0, 1.0),
        ["Right"]  = (17.25, 5.3, 1.0, 1.0),
    };

    /// <summary>
    /// Short display labels for keys with long names.
    /// </summary>
    public static readonly Dictionary<string, string> DisplayLabels = new()
    {
        ["LShift"] = "⇧",
        ["RShift"] = "⇧",
        ["LCtrl"]  = "Ctrl",
        ["RCtrl"]  = "Ctrl",
        ["LAlt"]   = "Alt",
        ["RAlt"]   = "Alt",
        ["Space"]  = "━━━━━━",
        ["Bksp"]   = "⌫",
        ["Enter"]  = "↵",
        ["Caps"]   = "⇪",
        ["PrtSc"]  = "PrSc",
        ["Pause"]  = "⏸",
        ["Left"]   = "←",
        ["Right"]  = "→",
        ["Up"]     = "↑",
        ["Down"]   = "↓",
    };
}
