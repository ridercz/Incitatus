using System.Text;

namespace Altairis.Incitatus.Data;

public class SqlServerQueryTranslator {
    private static readonly string[] OPERATORS_BINARY = { "and", "or" };
    private static readonly string[] OPERATORS_UNARY = { "not" };

    public static string ToSqlQuery(string fulltextQuery) {
        if (fulltextQuery == null) throw new ArgumentNullException(nameof(fulltextQuery));
        if (string.IsNullOrWhiteSpace(fulltextQuery)) throw new ArgumentException("Value cannot be empty or whitespace only string.", nameof(fulltextQuery));

        // Generate tokens from string
        var tokens = TokenizeQuery(fulltextQuery);

        // Generate query from tokens
        return ParseQueryTokens(tokens);
    }

    private static IEnumerable<string> TokenizeQuery(string s) {
        if (s == null) throw new ArgumentNullException(nameof(s));
        if (string.IsNullOrWhiteSpace(s)) throw new ArgumentException("Value cannot be empty or whitespace only string.", nameof(s));

        s = s.ToLower();

        var lastToken = string.Empty;
        var isInQuote = false;

        for (var i = 0; i < s.Length; i++) {
            var c = s[i];
            if (char.IsWhiteSpace(c)) {
                // Character is whitespace
                if (isInQuote) {
                    // Add space when in quote
                    lastToken += " ";
                } else if (!string.IsNullOrWhiteSpace(lastToken)) {
                    // End of token - add it
                    yield return lastToken;
                    lastToken = string.Empty;
                }
            } else if (c == '"') {
                if (isInQuote) {
                    // End of quote - close quote and add token
                    if (!string.IsNullOrWhiteSpace(lastToken)) yield return $"\"{lastToken}\"";
                    lastToken = string.Empty;
                    isInQuote = false;
                } else {
                    // Open quote
                    isInQuote = true;
                }
            } else if (c == '(' || c == ')') {
                // Bracket
                if (isInQuote) lastToken += c;
                else yield return c.ToString();
            } else {
                // Other char
                lastToken += c;
            }
        }

        // Add last token if not empty
        if (!string.IsNullOrWhiteSpace(lastToken)) {
            if (isInQuote) lastToken = $"\"{lastToken}\"";
            yield return lastToken;
        }
    }

    private static string ParseQueryTokens(IEnumerable<string> tokenList) {
        var tokens = tokenList.ToArray();
        var qsb = new StringBuilder();
        var lastTokenIsOperator = false;

        for (var i = 0; i < tokens.Length; i++) {
            var token = tokens[i];

            if (OPERATORS_BINARY.Contains(token)) {
                // Token is binary operator
                if (lastTokenIsOperator) continue;  // Ignore two consequent operators
                if (i + 1 == tokens.Length) continue; // Ignore operator at end of query

                qsb.Append(token.ToUpper());
                qsb.Append(' ');
                lastTokenIsOperator = true;
            } else if (OPERATORS_UNARY.Contains(token)) {
                // Token is unary operator
                if (i + 1 == tokens.Length) continue; // Ignore operator at end of query

                if (!lastTokenIsOperator && i > 0) qsb.Append("AND ");
                qsb.Append(token.ToUpper());
                qsb.Append(' ');
                lastTokenIsOperator = true;
            } else if (token.StartsWith("\"", StringComparison.Ordinal)) {
                // Token is phrase
                qsb.Append(token);
                qsb.Append(' ');
                lastTokenIsOperator = false;
            } else {
                // Token is something else
                if (!lastTokenIsOperator && i > 0) qsb.Append("AND ");

                qsb.Append($"FORMSOF(INFLECTIONAL,{token}) ");
                lastTokenIsOperator = false;
            }
        }

        return qsb.ToString();
    }

}