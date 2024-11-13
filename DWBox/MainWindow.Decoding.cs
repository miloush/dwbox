using System.Collections.Generic;
using System;
using System.Globalization;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace DWBox
{
    public partial class MainWindow
    {
        private static string Decode(string text)
        {
            string[] tokens = text.Split(' ');
            uint cp;

            StringBuilder output = new StringBuilder(text.Length);
            foreach (string token in tokens)
            {
                if (string.IsNullOrEmpty(token))
                    output.Append(' ');
                else if (token.Length >= 3 && uint.TryParse(token, NumberStyles.HexNumber, null, out cp) && IsValidCodepointOrSurrogate(cp))
                    output.Append(ToString(cp));
                else if (token.StartsWith("U+") && uint.TryParse(token.Substring(2), NumberStyles.HexNumber, null, out cp) && IsValidCodepointOrSurrogate(cp))
                    output.Append(ToString(cp));
                else if (DecodeAcronym(token) is string acronym)
                    output.Append(acronym);
                else
                    output.Append(token);
            }

            text = output.ToString();
            output.Length = 0;

            int index = -1;
            int chunkStart = 0;
            while (index < text.Length)
            {
                index = text.IndexOf('\\', index + 1);
                if (index < 0 || index + 1 >= text.Length)
                    break;

                switch (text[index + 1])
                {
                    case 'n':
                    case 'r':
                    case 't':
                    case '\\':
                        output.Append(text.Substring(chunkStart, index - chunkStart));
                        index += 1;
                        output.Append(text[index] switch { 'n' => '\n', 'r' => '\r', 't' => '\t', _ => text[index] });
                        chunkStart = index + 1;
                        break;
                    case 'u' when index + 3 < text.Length: // \uXXXX..\uXXXX
                        int hexLength = Math.Min(GetHexLengthAt(text, index + 2), 6);
                        if (hexLength >= 2)
                        {
                            uint startCode = uint.Parse(text.Substring(index + 2, hexLength), NumberStyles.HexNumber);
                            if (IsValidCodepointOrSurrogate(startCode))
                            {
                                output.Append(text.Substring(chunkStart, index - chunkStart));
                                index += 1 + hexLength;

                                uint endCode = startCode;
                                if (index + 5 < text.Length &&
                                    text[index + 1] == '.' &&
                                    text[index + 2] == '.' &&
                                    text[index + 3] == '\\' &&
                                    text[index + 4] == 'u')
                                {
                                    hexLength = Math.Min(GetHexLengthAt(text, index + 5), 6);
                                    if (hexLength >= 2)
                                    {
                                        endCode = uint.Parse(text.Substring(index + 5, hexLength), NumberStyles.HexNumber);
                                        if (endCode < startCode || !IsValidCodepointOrSurrogate(endCode))
                                            endCode = startCode;
                                        else
                                            index += 4 + hexLength;
                                    }
                                }

                                for (cp = startCode; cp <= endCode; cp++)
                                    if (ToString(cp) is string su)
                                        output.Append(su);

                                chunkStart = index + 1;
                            }
                        }
                        break;
                }
            }

            output.Append(text.Substring(chunkStart));

            return output.ToString();
        }

        private static string DecodeAcronym(string s)
        {
            switch (s)
            {
                case "NBSP": return "\u00A0";
                case "CGJ": return "\u034F";
                case "ZWSP": return "\u200B";
                case "ZWNJ": return "\u200C";
                case "ZWJ": return "\u200D";
                case "LRM": return "\u200E";
                case "RLM": return "\u200F";
                case "LRE": return "\u202A";
                case "RLE": return "\u202B";
                case "PDF": return "\u202C";
                case "LRO": return "\u202D";
                case "RLO": return "\u202E";
                case "NNBSP": return "\u202F";
                case "LRI": return "\u2066";
                case "RLI": return "\u2067";
                case "FSI": return "\u2068";
                case "PDI": return "\u2069";
                case "ISS": return "\u206A";
                case "ASS": return "\u206B";
                case "IAFS": return "\u206C";
                case "AAFS": return "\u206D";
                case "NADS": return "\u206E";
                case "NODS": return "\u206F";
                case "ZWNBSP": return "\uFEFF";
                default: return null;
            }
        }

        private static int GetHexLengthAt(string s, int index)
        {
            int i;
            for (i = index; i < s.Length; i++)
            {
                char c = s[i];
                if (IsHex(c))
                    continue;
                break;
            }
            return i - index;
        }

        private static bool IsHex(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'a' && c <= 'f') ||
                   (c >= 'A' && c <= 'F');
        }

        private static bool IsValidCodepoint(uint cp)
        {
            const uint Plane16End = 0x10FFFF;
            const uint HighSurrogateStart = 0xD800;
            const uint LowSurrogateEnd = 0xDFFF;

            return cp < Plane16End && (cp < HighSurrogateStart || cp > LowSurrogateEnd);
        }
        private static bool IsValidCodepointOrSurrogate(uint cp)
        {
            const uint Plane16End = 0x10FFFF;
            return cp < Plane16End;
        }

        private static string ToString(uint cp)
        {
            if (IsValidCodepoint(cp))
                return char.ConvertFromUtf32((int)cp);
            
            else if (cp < char.MaxValue) // surrogate
                return ((char)cp).ToString();

            return null;
        }

        private static IEnumerable<int> ToCodepoints(string s)
        {
            if (string.IsNullOrEmpty(s))
                yield break;

            int i = 0;
            while (i < s.Length)
            {
                if (char.IsSurrogate(s, i))
                    if (char.IsSurrogatePair(s, i))
                        yield return char.ConvertToUtf32(s, i++);
                    else
                        yield return s[i];

                else
                    yield return s[i];

                i++;
            }
        }


        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.SystemKey == Key.X && sender is TextBox textbox)
            {
                if (_decode.IsChecked == true)
                {
                    // we already know that hex values must be on their own (space separated)
                    // if we are in between spaces, Alt+X ought to do nothing
                    // if there are characters either side, we are in a token
                    //   if the token is a valid codepoint or codepoint range, we convert it to characters
                    //   otherwise, we convert it to hex
                    // if there is multiple tokens in selection
                    //   if they are all encoded, convert to characters
                    //   otherwise, convert unencoded to hex

                    ExpandToken(textbox);

                    if (string.IsNullOrWhiteSpace(textbox.SelectedText))
                        return;

                    string[] tokens = textbox.SelectedText.Split();
                    string[] decodedTokens = new string[tokens.Length];

                    bool hasDecodedTokens = false;
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        decodedTokens[i] = Decode(tokens[i]);
                        if (decodedTokens[i] == tokens[i])
                            hasDecodedTokens = true;
                    }

                    if (!hasDecodedTokens) // all tokens are hex or otherwise encoded, so we convert to characters
                    {
                        textbox.SelectedText = string.Join(" ", decodedTokens); // normalizes whitespace, might be undesirable
                        e.Handled = true;
                        return;
                    }

                    // at least one token is characters, convert them to hex, but leave encoded as they are
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        if (tokens[i] == decodedTokens[i])
                            tokens[i] = string.Join(" ", ToCodepoints(tokens[i]).Select(cp => cp.ToString("x3")));
                    }

                    textbox.SelectedText = string.Join(" ", tokens);
                    e.Handled = true;
                    return;
                }
                else
                {
                    // if decoding is disabled
                    //    if nothing is selected, select as much to the left as it makes a valid cp
                    // if selection is a valid hex number, convert to character
                    // if selection is a single codepoint or acronym, convert to hex
                    // otherwise do nothing

                    if (textbox.SelectedText.Length < 1)
                        if (!ExpandCodepointLeft(textbox))
                            ExpandCharacterLeft(textbox);

                    if (textbox.SelectedText.Length < 1)
                        return;

                    if (uint.TryParse(textbox.SelectedText, NumberStyles.HexNumber, null, out uint cp))
                    {
                        if (IsValidCodepoint(cp))
                        {
                            textbox.SelectedText = char.ConvertFromUtf32((int)cp);
                            e.Handled = true;
                        }

                        return;
                    }

                    if (textbox.SelectedText.Length == 1 ||
                        (textbox.SelectedText.Length == 2 && char.IsSurrogatePair(textbox.SelectedText, 0)))
                    {
                        textbox.SelectedText = char.ConvertToUtf32(textbox.SelectedText, 0).ToString("X4");
                        e.Handled = true;
                    }

                    if (DecodeAcronym(textbox.SelectedText) is string acronym)
                    {
                        textbox.SelectedText = acronym;
                        e.Handled = true;
                    }
                }
            }
        }

        private static bool ExpandToken(TextBox textbox)
        {
            int start = textbox.CaretIndex;
            int length = textbox.SelectedText.Length;
            bool changed = false;

            while (start > 0 && !char.IsWhiteSpace(textbox.Text[start - 1]))
            {
                start--;
                length++;
                changed = true;
            }

            while (start + length < textbox.Text.Length && !char.IsWhiteSpace(textbox.Text[start + length]))
            {
                length++;
                changed = true;
            }

            if (changed)
                textbox.Select(start, length);

            return changed;
        }

        private static bool ExpandCodepointLeft(TextBox textbox)
        {
            int start = textbox.CaretIndex;
            int length = textbox.SelectedText.Length;
            bool changed = false;

            while (start > 0 && IsHex(textbox.Text[start - 1]))
            {
                start--;
                length++;
                changed = true;
            }

            if (changed)
                textbox.Select(start, length);

            return changed;
        }

        private static bool ExpandCharacterLeft(TextBox textbox)
        {
            int start = textbox.CaretIndex;
            int length = textbox.SelectedText.Length;

            if (start < 1)
                return false;

            start--;
            length++;

            if (start > 0 && char.IsSurrogatePair(textbox.Text, start - 1))
            {
                start--;
                length++;
            }

            textbox.Select(start, length);
            return true;
        }
    }
}
