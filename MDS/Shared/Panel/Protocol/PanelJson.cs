using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// A small JSON writer and reader of our own: JsonUtility's overwrite is stripped on the IL2CPP game, and reflection is denied.
// The reader returns plain trees: Dictionary<string, object>, List<object>, string, double, bool or null.
namespace MDS.Shared.Panel.Protocol
{
    public sealed class JsonWriter
    {
        private readonly StringBuilder _text = new();
        private readonly Stack<bool> _hasItem = new();
        private bool _afterName;

        public override string ToString() => _text.ToString();

        public JsonWriter BeginObject() => Open('{');

        public JsonWriter EndObject() => Close('}');

        public JsonWriter BeginArray() => Open('[');

        public JsonWriter EndArray() => Close(']');

        public JsonWriter Name(string name)
        {
            Separate();
            AppendString(name);
            _text.Append(':');
            _afterName = true;
            return this;
        }

        public JsonWriter String(string value)
        {
            Separate();
            AppendString(value ?? string.Empty);
            return this;
        }

        public JsonWriter Number(double value)
        {
            Separate();
            _text.Append(value.ToString("R", CultureInfo.InvariantCulture));
            return this;
        }

        public JsonWriter Bool(bool value)
        {
            Separate();
            _text.Append(value ? "true" : "false");
            return this;
        }

        private JsonWriter Open(char bracket)
        {
            Separate();
            _text.Append(bracket);
            _hasItem.Push(false);
            return this;
        }

        private JsonWriter Close(char bracket)
        {
            _hasItem.Pop();
            _text.Append(bracket);
            return this;
        }

        // A value right after its name takes no comma; any other item after the first in its container does.
        private void Separate()
        {
            if (_afterName)
            {
                _afterName = false;
                return;
            }
            if (_hasItem.Count == 0) return;

            if (_hasItem.Peek()) _text.Append(',');
            _hasItem.Pop();
            _hasItem.Push(true);
        }

        private void AppendString(string value)
        {
            _text.Append('"');
            foreach (char c in value) AppendEscaped(c);
            _text.Append('"');
        }

        private void AppendEscaped(char c)
        {
            switch (c)
            {
                case '"': _text.Append("\\\""); break;
                case '\\': _text.Append("\\\\"); break;
                case '\n': _text.Append("\\n"); break;
                case '\r': _text.Append("\\r"); break;
                case '\t': _text.Append("\\t"); break;
                default:
                    if (c < ' ') _text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else _text.Append(c);
                    break;
            }
        }
    }

    public sealed class JsonReader
    {
        private const int MaxDepth = 32;

        private readonly string _text;
        private int _at;
        private int _depth;

        private JsonReader(string text)
        {
            _text = text ?? string.Empty;
        }

        // Anything a client or another mod sent is hostile input, so a malformed text gives false, never an exception.
        public static bool TryParse(string text, out object value)
        {
            value = null;
            try
            {
                var reader = new JsonReader(text);
                value = reader.ReadValue();
                reader.SkipSpace();
                return reader._at == reader._text.Length;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private object ReadValue()
        {
            SkipSpace();
            if (_at >= _text.Length) throw new FormatException("Unexpected end.");

            char c = _text[_at];
            if (c == '{') return ReadObject();
            if (c == '[') return ReadArray();
            if (c == '"') return ReadString();
            if (c == 't' || c == 'f' || c == 'n') return ReadWord();
            return ReadNumber();
        }

        private Dictionary<string, object> ReadObject()
        {
            Enter();
            var result = new Dictionary<string, object>();
            _at++;
            SkipSpace();
            if (Peek() == '}') return Leave(result);

            while (true)
            {
                SkipSpace();
                string name = ReadString();
                Expect(':');
                result[name] = ReadValue();
                SkipSpace();
                if (Peek() == '}') return Leave(result);
                Expect(',');
            }
        }

        private List<object> ReadArray()
        {
            Enter();
            var result = new List<object>();
            _at++;
            SkipSpace();
            if (Peek() == ']') return Leave(result);

            while (true)
            {
                result.Add(ReadValue());
                SkipSpace();
                if (Peek() == ']') return Leave(result);
                Expect(',');
            }
        }

        private string ReadString()
        {
            if (Peek() != '"') throw new FormatException("Expected a string.");

            var result = new StringBuilder();
            _at++;
            while (_at < _text.Length && _text[_at] != '"')
            {
                char c = _text[_at++];
                result.Append(c == '\\' ? ReadEscape() : c);
            }
            if (_at >= _text.Length) throw new FormatException("Unclosed string.");

            _at++;
            return result.ToString();
        }

        private char ReadEscape()
        {
            if (_at >= _text.Length) throw new FormatException("Unfinished escape.");

            char c = _text[_at++];
            switch (c)
            {
                case 'n': return '\n';
                case 'r': return '\r';
                case 't': return '\t';
                case 'b': return '\b';
                case 'f': return '\f';
                case 'u': return ReadUnicode();
                default: return c;
            }
        }

        private char ReadUnicode()
        {
            const int Digits = 4;
            if (_at + Digits > _text.Length) throw new FormatException("Short unicode escape.");

            string hex = _text.Substring(_at, Digits);
            _at += Digits;
            if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code)) throw new FormatException("Bad unicode escape.");
            return (char)code;
        }

        private object ReadWord()
        {
            if (TryTake("true")) return true;
            if (TryTake("false")) return false;
            if (TryTake("null")) return null;
            throw new FormatException("Unknown word.");
        }

        private double ReadNumber()
        {
            int start = _at;
            while (_at < _text.Length && "+-0123456789.eE".IndexOf(_text[_at]) >= 0) _at++;
            string number = _text.Substring(start, _at - start);
            if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) throw new FormatException("Bad number.");
            return value;
        }

        private bool TryTake(string word)
        {
            if (string.CompareOrdinal(_text, _at, word, 0, word.Length) != 0) return false;

            _at += word.Length;
            return true;
        }

        private void Enter()
        {
            if (++_depth > MaxDepth) throw new FormatException("Nested too deep.");
        }

        private T Leave<T>(T value)
        {
            _at++;
            _depth--;
            return value;
        }

        private void Expect(char c)
        {
            SkipSpace();
            if (Peek() != c) throw new FormatException($"Expected '{c}'.");
            _at++;
        }

        private char Peek() => _at < _text.Length ? _text[_at] : '\0';

        private void SkipSpace()
        {
            while (_at < _text.Length && char.IsWhiteSpace(_text[_at])) _at++;
        }
    }
}
