namespace Image2Gdram.Core.Output;

/// <summary>Зарегистрированные имена, с которыми не может совпадать имя массива (п. 4.4.2 ТЗ, решение N-06).</summary>
public static class ReservedWords
{
    /// <summary>Ключевые слова C89/C99 (с учётом регистра).</summary>
    public static IReadOnlySet<string> CKeywords { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "auto", "break", "case", "char", "const", "continue", "default", "do", "double", "else", "enum",
        "extern", "float", "for", "goto", "if", "inline", "int", "long", "register", "restrict", "return",
        "short", "signed", "sizeof", "static", "struct", "switch", "typedef", "union", "unsigned", "void",
        "volatile", "while", "_Bool", "_Complex", "_Imaginary",
    };

    /// <summary>Расширения языка Keil C51 (с учётом регистра).</summary>
    public static IReadOnlySet<string> KeilC51Keywords { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "_at_", "alien", "bdata", "bit", "code", "compact", "data", "far", "idata", "interrupt", "large",
        "pdata", "_priority_", "reentrant", "sbit", "sfr", "sfr16", "small", "_task_", "using", "xdata",
    };

    /// <summary>Имена типов <c>&lt;stdint.h&gt;</c> (с учётом регистра).</summary>
    public static IReadOnlySet<string> StdintTypeNames { get; } = BuildStdint();

    /// <summary>Регистры, мнемоники 8051, директивы и операторы A51 (без учёта регистра).</summary>
    public static IReadOnlySet<string> A51Words { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Регистры.
        "A", "AB", "C", "DPTR", "PC",
        "R0", "R1", "R2", "R3", "R4", "R5", "R6", "R7",
        "AR0", "AR1", "AR2", "AR3", "AR4", "AR5", "AR6", "AR7",

        // Мнемоники 8051.
        "ACALL", "ADD", "ADDC", "AJMP", "ANL", "CALL", "CJNE", "CLR", "CPL", "DA", "DEC", "DIV", "DJNZ",
        "INC", "JB", "JBC", "JC", "JMP", "JNB", "JNC", "JNZ", "JZ", "LCALL", "LJMP", "MOV", "MOVC", "MOVX",
        "MUL", "NOP", "ORL", "POP", "PUSH", "RET", "RETI", "RL", "RLC", "RR", "RRC", "SETB", "SJMP", "SUBB",
        "SWAP", "XCH", "XCHD", "XRL",

        // Директивы.
        "AT", "BIT", "BSEG", "CODE", "CSEG", "DATA", "DB", "DBIT", "DS", "DSEG", "DW", "END", "EQU", "EXTRN",
        "IDATA", "ISEG", "NAME", "ORG", "PUBLIC", "RSEG", "SEGMENT", "SET", "USING", "XDATA", "XSEG",
        "INBLOCK", "INPAGE", "PAGE", "BITADDRESSABLE", "UNIT", "OVERLAYABLE",
        "MACRO", "ENDM", "LOCAL", "REPT", "IRP", "IRPC", "EXITM", "IF", "ELSE", "ELSEIF", "ENDIF",

        // Операторы.
        "AND", "OR", "XOR", "NOT", "MOD", "SHL", "SHR", "EQ", "NE", "LT", "LE", "GT", "GE", "HIGH", "LOW", "NUL",
    };

    private static HashSet<string> BuildStdint()
    {
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            "intptr_t", "uintptr_t", "intmax_t", "uintmax_t",
        };
        foreach (int bits in new[] { 8, 16, 32, 64 })
        {
            foreach (string prefix in new[] { "int", "int_least", "int_fast" })
            {
                names.Add($"{prefix}{bits}_t");
                names.Add($"u{prefix}{bits}_t");
            }
        }

        return names;
    }
}
