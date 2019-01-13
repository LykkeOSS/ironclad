// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Extensions
{
    using System.Text;

    internal static class SnakeCaseStringExtensions
    {
        public static string ToSnakeCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            value = value.Trim();

            var length = value.Length;
            var addedByLower = false;
            var stringBuilder = new StringBuilder();

            for (var i = 0; i < length; i++)
            {
                var currentChar = value[i];

                if (char.IsWhiteSpace(currentChar))
                {
                    continue;
                }

                if (currentChar.Equals('_'))
                {
                    stringBuilder.Append('_');
                    continue;
                }

                bool isLastChar = i + 1 == length,
                        isFirstChar = i == 0,
                        nextIsUpper = false,
                        nextIsLower = false;

                if (!isLastChar)
                {
                    nextIsUpper = char.IsUpper(value[i + 1]);
                    nextIsLower = !nextIsUpper && !value[i + 1].Equals('_');
                }

                if (!char.IsUpper(currentChar))
                {
                    stringBuilder.Append(char.ToLowerInvariant(currentChar));

                    if (nextIsUpper)
                    {
                        stringBuilder.Append('_');
                        addedByLower = true;
                    }

                    continue;
                }

                if (nextIsLower && !addedByLower && !isFirstChar)
                {
                    stringBuilder.Append('_');
                }

                addedByLower = false;

                stringBuilder.Append(char.ToLowerInvariant(currentChar));
            }

            return stringBuilder.ToString();
        }
    }
}
