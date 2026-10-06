using Image2Gdram.Core.Text;

namespace Image2Gdram.Core.Fonts.Import;

/// <summary>
/// Разбор текста C и A51 на массивы байтов (п. 4.2.2 ТЗ, источник 4; решения D-17, N-52).
/// Ошибка значения остаётся на массиве; незакрытый комментарий, строка или скобки — исключение на весь файл.
/// </summary>
public static class ArrayImportParser
{
    public static IReadOnlyList<ImportedArray> Parse(string text, ImportSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (syntax is not (ImportSyntax.C or ImportSyntax.Asm))
        {
            throw new ArgumentOutOfRangeException(nameof(syntax), syntax, "Unknown import syntax.");
        }

        List<Token> tokens = new Scanner(text, syntax).Scan();
        return syntax == ImportSyntax.C ? new CParser(text, tokens).Parse() : new AsmParser(text, tokens).Parse();
    }

    private enum TokenKind
    {
        Identifier,
        Number,
        String,
        LBrace,
        RBrace,
        LBracket,
        RBracket,
        LParen,
        RParen,
        Equals,
        Comma,
        Semicolon,
        Colon,
        Other,
        Newline,
        Eof,
    }

    private sealed class Token
    {
        public Token(TokenKind kind, int line, int start, int end)
        {
            Kind = kind;
            Line = line;
            Start = start;
            End = end;
        }

        public TokenKind Kind { get; }

        public int Line { get; }

        public int Start { get; }

        public int End { get; }

        public byte Number { get; set; }

        public byte[]? Bytes { get; set; }

        public ArrayImportErrorKind? Error { get; set; }

        public string? ErrorToken { get; set; }
    }

    private readonly struct Element
    {
        private Element(byte[]? bytes, ArrayImportIssue? error)
        {
            Bytes = bytes;
            Error = error;
        }

        public byte[]? Bytes { get; }

        public ArrayImportIssue? Error { get; }

        public static Element Ok(byte[] bytes) => new(bytes, null);

        public static Element Fail(ArrayImportErrorKind kind, int line, string token) =>
            new(null, new ArrayImportIssue(kind, line, token));
    }

    private sealed class ArrayBuilder
    {
        private readonly List<byte> _values = new();

        public ArrayBuilder(string? name, int line)
        {
            Name = name;
            Line = line;
        }

        public string? Name { get; }

        public int Line { get; }

        public ArrayImportIssue? Error { get; private set; }

        public void Add(byte[]? bytes)
        {
            if (Error is not null || bytes is null)
            {
                return;
            }

            _values.AddRange(bytes);
        }

        public void Fail(ArrayImportIssue issue)
        {
            Error ??= issue;
            _values.Clear();
        }

        public ImportedArray ToArray(ImportedArrayKind kind) =>
            new(Name, kind, Line, Error is null ? _values.ToArray() : Array.Empty<byte>(), Error);
    }

    private sealed class Scanner
    {
        private readonly string _text;
        private readonly ImportSyntax _syntax;
        private readonly List<Token> _tokens = new();
        private int _i;
        private int _line = 1;
        private bool _lineStart = true;

        public Scanner(string text, ImportSyntax syntax)
        {
            _text = text;
            _syntax = syntax;
        }

        public List<Token> Scan()
        {
            while (_i < _text.Length)
            {
                SkipSpaces();
                if (_i >= _text.Length)
                {
                    break;
                }

                if (IsNewline(_i))
                {
                    EmitNewline();
                    _lineStart = true;
                    continue;
                }

                if (_lineStart && _text[_i] == '#')
                {
                    SkipPreprocessor();
                    _lineStart = true;
                    continue;
                }

                if (Match("/*"))
                {
                    SkipBlock();
                    continue;
                }

                if (Match("//"))
                {
                    SkipUntilNewline();
                    continue;
                }

                if (_syntax == ImportSyntax.Asm && _text[_i] == ';')
                {
                    _i++;
                    SkipUntilNewline();
                    continue;
                }

                _lineStart = false;
                if (_text[_i] is '\'' or '"')
                {
                    _tokens.Add(ScanString());
                    continue;
                }

                if (IsDigit(_text[_i]) || (_text[_i] == '-' && _i + 1 < _text.Length && IsDigit(_text[_i + 1])))
                {
                    _tokens.Add(ScanNumber());
                    continue;
                }

                if (IsIdentStart(_text[_i]))
                {
                    _tokens.Add(ScanIdent());
                    continue;
                }

                _tokens.Add(ScanPunct());
            }

            return _tokens;
        }

        private void SkipSpaces()
        {
            while (_i < _text.Length && _text[_i] is ' ' or '\t')
            {
                _i++;
            }
        }

        private void SkipUntilNewline()
        {
            while (_i < _text.Length && !IsNewline(_i))
            {
                _i++;
            }
        }

        private void SkipPreprocessor()
        {
            _i++;
            while (_i < _text.Length)
            {
                if (IsNewline(_i))
                {
                    ConsumeNewline();
                    return;
                }

                if (Match("/*"))
                {
                    SkipBlock();
                    continue;
                }

                if (Match("//"))
                {
                    SkipUntilNewline();
                    continue;
                }

                if (_text[_i] is '\'' or '"')
                {
                    ScanString();
                    continue;
                }

                _i++;
            }
        }

        private void SkipBlock()
        {
            int openLine = _line;
            _i += 2;
            while (_i < _text.Length)
            {
                if (IsNewline(_i))
                {
                    ConsumeNewline();
                    continue;
                }

                if (_text[_i] == '*' && _i + 1 < _text.Length && _text[_i + 1] == '/')
                {
                    _i += 2;
                    return;
                }

                _i++;
            }

            throw new ArrayImportException(ArrayImportErrorKind.UnterminatedComment, openLine, "/*");
        }

        private Token ScanString()
        {
            int start = _i;
            int line = _line;
            char quote = _text[_i++];
            var bytes = new List<byte>();
            string? bad = null;
            while (_i < _text.Length)
            {
                if (IsNewline(_i))
                {
                    throw new ArrayImportException(ArrayImportErrorKind.UnterminatedString, line, quote.ToString());
                }

                char c = _text[_i];
                if (c == quote)
                {
                    if (_i + 1 < _text.Length && _text[_i + 1] == quote)
                    {
                        Append(quote, bytes, ref bad);
                        _i += 2;
                        continue;
                    }

                    _i++;
                    var closed = new Token(TokenKind.String, line, start, _i);
                    if (bad is not null)
                    {
                        closed.Error = ArrayImportErrorKind.CharacterNotInCp1251;
                        closed.ErrorToken = bad;
                    }
                    else
                    {
                        closed.Bytes = bytes.ToArray();
                    }

                    return closed;
                }

                Append(c, bytes, ref bad);
                _i++;
            }

            throw new ArrayImportException(ArrayImportErrorKind.UnterminatedString, line, quote.ToString());
        }

        private static void Append(char c, List<byte> bytes, ref string? bad)
        {
            if (Cp1251.TryFromDecoded(c, out byte code))
            {
                bytes.Add(code);
            }
            else
            {
                bad ??= c.ToString();
            }
        }

        private Token ScanNumber()
        {
            int start = _i;
            int line = _line;
            if (_text[_i] == '-')
            {
                _i++;
            }

            while (_i < _text.Length && IsIdentPart(_text[_i]))
            {
                _i++;
            }

            var token = new Token(TokenKind.Number, line, start, _i);
            string lexeme = _text.Substring(start, _i - start);
            if (Numbers.TryParse(lexeme, _syntax, out byte value, out ArrayImportErrorKind error))
            {
                token.Number = value;
            }
            else
            {
                token.Error = error;
            }

            return token;
        }

        private Token ScanIdent()
        {
            int start = _i;
            int line = _line;
            _i++;
            while (_i < _text.Length && IsIdentPart(_text[_i]))
            {
                _i++;
            }

            return new Token(TokenKind.Identifier, line, start, _i);
        }

        private Token ScanPunct()
        {
            int start = _i;
            int line = _line;
            char c = _text[_i++];
            TokenKind kind = c switch
            {
                '{' => TokenKind.LBrace,
                '}' => TokenKind.RBrace,
                '[' => TokenKind.LBracket,
                ']' => TokenKind.RBracket,
                '(' => TokenKind.LParen,
                ')' => TokenKind.RParen,
                '=' => TokenKind.Equals,
                ',' => TokenKind.Comma,
                ';' => TokenKind.Semicolon,
                ':' => TokenKind.Colon,
                _ => TokenKind.Other,
            };
            return new Token(kind, line, start, _i);
        }

        private void EmitNewline()
        {
            int start = _i;
            int line = _line;
            ConsumeNewline();
            _tokens.Add(new Token(TokenKind.Newline, line, start, _i));
        }

        private void ConsumeNewline()
        {
            if (_i < _text.Length && _text[_i] == '\r')
            {
                _i++;
            }

            if (_i < _text.Length && _text[_i] == '\n')
            {
                _i++;
            }

            _line++;
        }

        private bool IsNewline(int index) => index < _text.Length && _text[index] is '\r' or '\n';

        private bool Match(string value) =>
            _i + value.Length <= _text.Length && _text.AsSpan(_i, value.Length).SequenceEqual(value);
    }

    private static class Numbers
    {
        public static bool TryParse(string text, ImportSyntax syntax, out byte value, out ArrayImportErrorKind error)
        {
            value = 0;
            bool negative = false;
            string body = text;
            if (body.StartsWith('-'))
            {
                negative = true;
                body = body[1..];
            }

            if (syntax == ImportSyntax.C)
            {
                body = StripCSuffix(body);
            }

            if (body.Length == 0)
            {
                error = ArrayImportErrorKind.InvalidNumber;
                return false;
            }

            if (StartsWith(body, "0x"))
            {
                return Finish(body[2..], 16, hex: true, negative, out value, out error);
            }

            if (body.Length >= 2 && body[^1] is 'h' or 'H')
            {
                string digits = body[..^1];
                if (digits.Length == 0 || !IsDigit(digits[0]))
                {
                    error = ArrayImportErrorKind.InvalidNumber;
                    return false;
                }

                return Finish(digits, 16, hex: true, negative, out value, out error);
            }

            if (body.Length > 2 && StartsWith(body, "0b"))
            {
                return Finish(body[2..], 2, hex: false, negative, out value, out error);
            }

            if (body.Length >= 2 && body[^1] is 'b' or 'B')
            {
                return Finish(body[..^1], 2, hex: false, negative, out value, out error);
            }

            if (syntax == ImportSyntax.C && body.Length > 1 && body[0] == '0')
            {
                if (body.Any(c => c is '8' or '9'))
                {
                    error = ArrayImportErrorKind.InvalidNumber;
                    return false;
                }

                return Finish(body, 8, hex: false, negative, out value, out error);
            }

            return Finish(body, 10, hex: false, negative, out value, out error);
        }

        private static bool Finish(string digits, int radix, bool hex, bool negative, out byte value, out ArrayImportErrorKind error)
        {
            value = 0;
            if (!TryAccumulate(digits, radix, hex, out long magnitude, out bool overflow))
            {
                error = ArrayImportErrorKind.InvalidNumber;
                return false;
            }

            if (overflow || magnitude > 255 || (negative && magnitude != 0))
            {
                error = ArrayImportErrorKind.ValueOutOfRange;
                return false;
            }

            value = (byte)magnitude;
            error = default;
            return true;
        }

        private static bool TryAccumulate(string digits, int radix, bool hex, out long value, out bool overflow)
        {
            value = 0;
            overflow = false;
            if (digits.Length == 0)
            {
                return false;
            }

            foreach (char c in digits)
            {
                int digit = Digit(c, hex);
                if (digit < 0 || digit >= radix)
                {
                    return false;
                }

                if (overflow)
                {
                    continue;
                }

                if (value > (long.MaxValue - digit) / radix)
                {
                    overflow = true;
                    continue;
                }

                value = (value * radix) + digit;
            }

            return true;
        }

        private static int Digit(char c, bool hex)
        {
            if (IsDigit(c))
            {
                return c - '0';
            }

            if (!hex)
            {
                return -1;
            }

            return c switch
            {
                >= 'a' and <= 'f' => c - 'a' + 10,
                >= 'A' and <= 'F' => c - 'A' + 10,
                _ => -1,
            };
        }

        private static string StripCSuffix(string body)
        {
            int i = body.Length;
            int u = 0;
            int l = 0;
            while (i > 0)
            {
                char c = body[i - 1];
                if (c is 'u' or 'U')
                {
                    if (u == 1)
                    {
                        break;
                    }

                    u++;
                }
                else if (c is 'l' or 'L')
                {
                    if (l == 2)
                    {
                        break;
                    }

                    l++;
                }
                else
                {
                    break;
                }

                i--;
            }

            return i == body.Length || i == 0 ? body : body[..i];
        }

        private static bool StartsWith(string text, string prefix) =>
            text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private abstract class ParserBase
    {
        private readonly string _text;
        protected readonly List<Token> Tokens;
        protected readonly ImportSyntax Syntax;
        private static readonly Token Eof = new(TokenKind.Eof, 0, 0, 0);

        protected ParserBase(string text, List<Token> tokens, ImportSyntax syntax)
        {
            _text = text;
            Tokens = tokens;
            Syntax = syntax;
        }

        protected string Slice(int start, int end) => _text.Substring(start, end - start);

        protected string TextOf(Token token) => Slice(token.Start, token.End);

        protected bool IsDb(Token token) =>
            token.Kind == TokenKind.Identifier && TextOf(token).Equals("DB", StringComparison.OrdinalIgnoreCase);

        protected Element Interpret(Token token)
        {
            string lexeme = TextOf(token);
            if (token.Kind == TokenKind.Number)
            {
                return token.Error is ArrayImportErrorKind error
                    ? Element.Fail(error, token.Line, token.ErrorToken ?? lexeme)
                    : Element.Ok(new[] { token.Number });
            }

            if (token.Kind == TokenKind.String)
            {
                if (Syntax == ImportSyntax.C)
                {
                    return Element.Fail(ArrayImportErrorKind.InvalidNumber, token.Line, lexeme);
                }

                return token.Error is ArrayImportErrorKind error
                    ? Element.Fail(error, token.Line, token.ErrorToken ?? lexeme)
                    : Element.Ok(token.Bytes ?? Array.Empty<byte>());
            }

            return Element.Fail(ArrayImportErrorKind.InvalidNumber, token.Line, lexeme);
        }

        protected Element InterpretRange(int from, int last)
        {
            if (from == last)
            {
                return Interpret(Tokens[from]);
            }

            Token first = Tokens[from];
            return Element.Fail(ArrayImportErrorKind.InvalidNumber, first.Line, Slice(first.Start, Tokens[last].End));
        }

        protected static bool IsCBoundary(TokenKind kind) =>
            kind is TokenKind.Comma or TokenKind.RBrace or TokenKind.LBrace;

        protected int NextSignificant(int index)
        {
            while (index < Tokens.Count && Tokens[index].Kind == TokenKind.Newline)
            {
                index++;
            }

            return index;
        }

        protected Token EofToken => Eof;
    }

    private sealed class CParser : ParserBase
    {
        private int _index;
        private readonly List<Token> _pending = new();
        private readonly List<int> _openLines = new();
        private readonly List<ImportedArray> _arrays = new();

        public CParser(string text, List<Token> tokens)
            : base(text, tokens, ImportSyntax.C)
        {
        }

        public IReadOnlyList<ImportedArray> Parse()
        {
            while (true)
            {
                Token token = Peek();
                if (token.Kind == TokenKind.Eof)
                {
                    break;
                }

                if (token.Kind == TokenKind.Equals && NextIsBrace())
                {
                    Token equals = Take();
                    Token brace = Take();
                    (string? name, int nameLine) = ExtractName();
                    (List<byte> values, ArrayImportIssue? error) = ParseList(brace.Line);
                    int line = name is null ? equals.Line : nameLine;
                    _arrays.Add(new ImportedArray(
                        name,
                        ImportedArrayKind.CInitializer,
                        line,
                        error is null ? values.ToArray() : Array.Empty<byte>(),
                        error));
                    _pending.Clear();
                    continue;
                }

                token = Take();
                switch (token.Kind)
                {
                    case TokenKind.Semicolon:
                        _pending.Clear();
                        break;
                    case TokenKind.LBrace:
                        _openLines.Add(token.Line);
                        _pending.Clear();
                        break;
                    case TokenKind.RBrace:
                        if (_openLines.Count == 0)
                        {
                            throw new ArrayImportException(ArrayImportErrorKind.UnbalancedBraces, token.Line, "}");
                        }

                        _openLines.RemoveAt(_openLines.Count - 1);
                        _pending.Clear();
                        break;
                    default:
                        _pending.Add(token);
                        break;
                }
            }

            if (_openLines.Count > 0)
            {
                throw new ArrayImportException(ArrayImportErrorKind.UnbalancedBraces, _openLines[^1], "{");
            }

            return _arrays;
        }

        private (List<byte> Values, ArrayImportIssue? Error) ParseList(int openLine)
        {
            var values = new List<byte>();
            ArrayImportIssue? error = null;
            bool needValue = false;
            bool seen = false;
            while (true)
            {
                Token token = Peek();
                if (token.Kind == TokenKind.Eof)
                {
                    throw new ArrayImportException(ArrayImportErrorKind.UnbalancedBraces, openLine, "{");
                }

                if (token.Kind == TokenKind.RBrace)
                {
                    Take();
                    return (values, error);
                }

                if (token.Kind == TokenKind.Comma)
                {
                    if (needValue || !seen)
                    {
                        Fail(new ArrayImportIssue(ArrayImportErrorKind.InvalidNumber, token.Line, string.Empty));
                    }

                    Take();
                    needValue = true;
                    seen = true;
                    continue;
                }

                if (token.Kind == TokenKind.LBrace)
                {
                    Take();
                    (List<byte> inner, ArrayImportIssue? innerError) = ParseList(token.Line);
                    if (innerError is not null)
                    {
                        Fail(innerError);
                    }
                    else if (error is null)
                    {
                        values.AddRange(inner);
                    }

                    needValue = false;
                    seen = true;
                    continue;
                }

                Element element = ReadElement();
                if (element.Error is not null)
                {
                    Fail(element.Error);
                }
                else if (error is null)
                {
                    values.AddRange(element.Bytes!);
                }

                needValue = false;
                seen = true;
            }

            void Fail(ArrayImportIssue issue)
            {
                error ??= issue;
                values.Clear();
            }
        }

        private Element ReadElement()
        {
            Peek();
            int from = _index;
            int next = NextSignificant(from + 1);
            if (next >= Tokens.Count || IsCBoundary(Tokens[next].Kind))
            {
                _index = next;
                return Interpret(Tokens[from]);
            }

            int paren = 0;
            int i = from;
            int last = from;
            while (i < Tokens.Count)
            {
                Token token = Tokens[i];
                if (token.Kind == TokenKind.Newline)
                {
                    i++;
                    continue;
                }

                if (paren == 0 && IsCBoundary(token.Kind))
                {
                    break;
                }

                if (token.Kind == TokenKind.LParen)
                {
                    paren++;
                }
                else if (token.Kind == TokenKind.RParen && paren > 0)
                {
                    paren--;
                }

                last = i;
                i++;
            }

            _index = i;
            return InterpretRange(from, last);
        }

        private (string? Name, int Line) ExtractName()
        {
            int limit = _pending.Count;
            for (int i = 0; i < _pending.Count; i++)
            {
                if (_pending[i].Kind == TokenKind.LBracket)
                {
                    limit = i;
                    break;
                }
            }

            for (int i = limit - 1; i >= 0; i--)
            {
                if (_pending[i].Kind == TokenKind.Identifier)
                {
                    return (TextOf(_pending[i]), _pending[i].Line);
                }
            }

            return (null, 0);
        }

        private bool NextIsBrace()
        {
            int next = NextSignificant(_index + 1);
            return next < Tokens.Count && Tokens[next].Kind == TokenKind.LBrace;
        }

        private Token Peek()
        {
            while (_index < Tokens.Count && Tokens[_index].Kind == TokenKind.Newline)
            {
                _index++;
            }

            return _index < Tokens.Count ? Tokens[_index] : EofToken;
        }

        private Token Take()
        {
            Token token = Peek();
            if (token.Kind != TokenKind.Eof)
            {
                _index++;
            }

            return token;
        }
    }

    private sealed class AsmParser : ParserBase
    {
        private readonly List<ImportedArray> _arrays = new();
        private ArrayBuilder? _current;
        private string? _pendingName;
        private int _pendingLine;

        public AsmParser(string text, List<Token> tokens)
            : base(text, tokens, ImportSyntax.Asm)
        {
        }

        public IReadOnlyList<ImportedArray> Parse()
        {
            int i = 0;
            while (i < Tokens.Count)
            {
                if (Tokens[i].Kind == TokenKind.Newline)
                {
                    i++;
                    continue;
                }

                int start = i;
                while (i < Tokens.Count && Tokens[i].Kind != TokenKind.Newline)
                {
                    i++;
                }

                Statement(start, i);
                if (i < Tokens.Count && Tokens[i].Kind == TokenKind.Newline)
                {
                    i++;
                }
            }

            Finish();
            return _arrays;
        }

        private void Statement(int start, int end)
        {
            int i = start;
            string? label = null;
            int labelLine = 0;
            while (i + 1 < end && Tokens[i].Kind == TokenKind.Identifier && Tokens[i + 1].Kind == TokenKind.Colon)
            {
                label = TextOf(Tokens[i]);
                labelLine = Tokens[i].Line;
                i += 2;
            }

            if (i + 1 < end && Tokens[i].Kind == TokenKind.Identifier && !IsDb(Tokens[i]) && IsDb(Tokens[i + 1]))
            {
                label = TextOf(Tokens[i]);
                labelLine = Tokens[i].Line;
                i++;
            }

            if (i < end && IsDb(Tokens[i]))
            {
                Begin(label, labelLine, Tokens[i].Line);
                ReadOperands(i + 1, end);
                return;
            }

            if (label is not null && i >= end)
            {
                Finish();
                _pendingName = label;
                _pendingLine = labelLine;
                return;
            }

            Finish();
            _pendingName = null;
        }

        private void Begin(string? label, int labelLine, int dbLine)
        {
            if (_current is not null && label is not null)
            {
                Finish();
            }

            if (_current is null)
            {
                string? name = label ?? _pendingName;
                int line = label is not null ? labelLine : _pendingName is not null ? _pendingLine : dbLine;
                _current = new ArrayBuilder(name, line);
                _pendingName = null;
            }
        }

        private void ReadOperands(int start, int end)
        {
            bool needValue = false;
            bool seen = false;
            int i = start;
            while (i < end)
            {
                Token token = Tokens[i];
                if (token.Kind == TokenKind.Comma)
                {
                    if (needValue || !seen)
                    {
                        _current!.Fail(new ArrayImportIssue(ArrayImportErrorKind.InvalidNumber, token.Line, string.Empty));
                    }

                    needValue = true;
                    seen = true;
                    i++;
                    continue;
                }

                int from = i;
                int paren = 0;
                int last = i;
                while (i < end)
                {
                    Token current = Tokens[i];
                    if (paren == 0 && current.Kind == TokenKind.Comma)
                    {
                        break;
                    }

                    if (current.Kind == TokenKind.LParen)
                    {
                        paren++;
                    }
                    else if (current.Kind == TokenKind.RParen && paren > 0)
                    {
                        paren--;
                    }

                    last = i;
                    i++;
                }

                Element element = InterpretRange(from, last);
                if (element.Error is not null)
                {
                    _current!.Fail(element.Error);
                }
                else
                {
                    _current!.Add(element.Bytes);
                }

                needValue = false;
                seen = true;
            }
        }

        private void Finish()
        {
            if (_current is null)
            {
                return;
            }

            _arrays.Add(_current.ToArray(ImportedArrayKind.AsmDb));
            _current = null;
        }
    }

    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    private static bool IsIdentStart(char c) => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or '_' or '$' or '?';

    private static bool IsIdentPart(char c) => IsIdentStart(c) || IsDigit(c);
}
