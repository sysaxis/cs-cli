using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;


namespace CLI
{
    class ColumnInfo
    {
        public string PropertyName { get; set; }
        public string ColumnName { get; set; }
        public string Format { get; set; }

        public PropertyInfo PropertyInfo { get; private set; }

        public bool IsEnumerable { get; private set; }

        /// <summary>
        /// Parses column info from format: Property->Column(format)
        /// </summary>
        /// <param name="definition"></param>
        public ColumnInfo(string definition)
        {
            var regex = new Regex("(?<Property>\\w+)(->(?<Column>[\\w ]+))?(\\((?<Format>[\\w\\-:\\.% ]+)\\))?", RegexOptions.ECMAScript);
            var match = regex.Match(definition);

            Group g = match.Groups["Property"];
            PropertyName = g.Value;

            if (PropertyName == "")
            {
                throw new Exception("cannot parse property name");
            }

            g = match.Groups["Format"];
            Format = g.Value;

            if ((g = match.Groups["Column"]).Value != "")
            {
                ColumnName = g.Value;
            }
            else
            {
                ColumnName = PropertyName;
            }
        }

        public void SetPropertyInfo(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
            IsEnumerable = typeof(IEnumerable<object>).IsAssignableFrom(propertyInfo.PropertyType);
        }
    }

    public static class ConsoleUtil
    {

        public static string PromptInput(string prompt, bool hideInput = false)
        {
            string value = "";

            Console.Write(prompt);
            int pos = Console.CursorLeft;

            ConsoleKeyInfo keyInfo;
            while ((keyInfo = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if (keyInfo.Key == ConsoleKey.Backspace && value.Length > 0)
                {
                    value = value.Substring(0, value.Length - 1);

                    if (!hideInput) Console.Write("\b \b");
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    value += keyInfo.KeyChar;

                    if (!hideInput) Console.Write(keyInfo.KeyChar);
                }
            }

            Console.WriteLine();

            return value;
        }

        public static T PromptInput<T>(string prompt, bool hideInput = false)
        {
            string value = PromptInput(prompt, hideInput);

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static void PrintList<T>(IEnumerable<T> valueList, params string[] columns)
        {
            PrintList(Console.WriteLine, valueList, columns);
        }

        public static void PrintList<T>(Action<string> writeLine, IEnumerable<T> valueList, params string[] columns)
        {
            var columnInfos = columns.ToList()
                .ConvertAll(c => new ColumnInfo(c));

            string headerFormat = "";
            string lineFormat = "";

            for (int k = 0; k < columnInfos.Count; k++)
            {
                string columnFormat = $"{{{k},{(columnInfos[k].Format.Length > 0 ? columnInfos[k].Format : "-" + columnInfos[k].ColumnName.Length.ToString())}}}";
                headerFormat += (k == 0 ? "" : " | ") + columnFormat;
                lineFormat += (k == 0 ? "" : "   ") + columnFormat;
            }

            string[] headers = columnInfos.ConvertAll(c => c.ColumnName).ToArray();
            writeLine(string.Format(headerFormat, headers));

            if (valueList.Count() == 0) return;

            var objType = typeof(T);
            var properties = objType.GetProperties();

            foreach (var columnInfo in columnInfos)
            {
                columnInfo.SetPropertyInfo(properties.First(p => p.Name == columnInfo.PropertyName));
            }

            foreach (var listItem in valueList)
            {
                object[] values = new object[columnInfos.Count];
                int cIndex = 0;
                foreach (var col in columnInfos)
                {
                    object value = col.PropertyInfo.GetValue(listItem);

                    if (col.IsEnumerable)
                    {
                        value = ((IEnumerable<object>)value).Aggregate("", (s, e) => s + (s == "" ? "" : ";") + e);
                    }

                    values[cIndex++] = value;
                }

                writeLine(string.Format(lineFormat, values));
            }
        }

    }

}
