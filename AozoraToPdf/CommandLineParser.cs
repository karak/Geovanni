// $Id$
#region This source code is licensed under MIT License
/*
 * The MIT License
 * 
 * Copyright © 2008 Mayuki Sawatari <mayuki@misuzilla.org>
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
*/
#endregion
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Misuzilla.Utilities
{
    public class CommandLineParser<T> where T : class
    {
        private Type _type;
        private Dictionary<String, PropertyInfo> _availableOptions = new Dictionary<string, PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
        private List<String> _mandatoryOptions = new List<string>();
        private String _defaultArgumentsPropertyName = null;

        /// <summary>
        /// コマンドラインオプション名の先頭につける文字列を指定します。
        /// </summary>
        public String Prefix { get; set; }

        /// <summary>
        /// コマンドラインオプション名で使う区切り文字を指定します。
        /// </summary>
        public Char Delimiter { get; set; }

        /// <summary>
        /// コマンドラインオプション名を区切り文字と小文字で構成するかどうかを指定します。
        /// </summary>
        public Boolean DelimiterizeAndToLowerCase { get; set; }

        /// <summary>
        /// パラメータ名と値とをつなぐ文字列を指定します。
        /// </summary>
        public Char[] ValueParamDelimiter { get; set; }

        /// <summary>
        /// 不正なコマンドラインオプション以外を許可しないかどうかを指定します。
        /// </summary>
        public Boolean Strict { get; set; }

        /// <summary>
        /// コマンドラインパーサーのインスタンスを初期化します。
        /// </summary>
        public CommandLineParser()
            : this('-')
        { }

        /// <summary>
        /// コマンドラインパーサーのインスタンスを初期化します。
        /// </summary>
        /// <param name="delimiter">コマンドラインオプション名の区切り文字</param>
        public CommandLineParser(Char delimiter)
        {
            DelimiterizeAndToLowerCase = true;
            Delimiter = delimiter;
            Prefix = delimiter.ToString() + delimiter;
            ValueParamDelimiter = new char[] { '=', ':' };
            Strict = false;

            _type = typeof(T);

            Object[] attrs = _type.GetCustomAttributes(typeof(DefaultPropertyAttribute), true);
            if (attrs.Length > 0)
            {
                _defaultArgumentsPropertyName = (attrs[0] as DefaultPropertyAttribute).Name;
                PropertyInfo propInfo = _type.GetProperty(_defaultArgumentsPropertyName);
                if (propInfo == null)
                {
                    throw new ArgumentException(String.Format("No such property {0} for default args", _defaultArgumentsPropertyName));
                }
                if (propInfo.PropertyType != typeof(String[]))
                {
                    throw new ArgumentException("a default arguments property type must be String[]");
                }
            }

            foreach (PropertyInfo pi in _type.GetProperties())
            {
                if (_defaultArgumentsPropertyName == pi.Name)
                    continue;

                foreach (String optName in GetOptionNames(pi))
                    _availableOptions.Add(optName, pi);

                Object defaultValue;
                if (!GetDefaultValue(pi, out defaultValue))
                {
                    _mandatoryOptions.Add(pi.Name);
                }
            }
        }

        /// <summary>
        /// コマンドラインオプションの一覧をコンソールに出力します。
        /// </summary>
        public void ShowHelp()
        {
            Int32 maxLen = 0;
            Dictionary<String, String> optionHelps = new Dictionary<string, string>();

            foreach (PropertyInfo pi in _type.GetProperties())
            {
                if (pi.Name == _defaultArgumentsPropertyName)
                    continue;

                Object defaultValue;
                Boolean hasDefaultValue = GetDefaultValue(pi, out defaultValue);
                String defaultOrRequired = (hasDefaultValue ? String.Format("(Default: {0})", defaultValue) : "(Required)");
                String[] optionNames = GetOptionNames(pi);
                String optionName = (DelimiterizeAndToLowerCase ? ToLowerAndDelimiterize(optionNames[0]) : optionNames[0]);
                String keyName;
                if (pi.PropertyType == typeof(Boolean))
                {
                    //keyName = String.Format("{0}{1}{2}<true|false>", Prefix, optionName, ValueParamSeparator[0]);
                    keyName = String.Format("{0}{1}", Prefix, optionName);
                }
                else
                {
                    keyName = String.Format("{0}{1}{2}({3})", Prefix, optionName, ValueParamDelimiter[0], pi.PropertyType.Name);
                }
                optionHelps[keyName] = GetDescription(pi) + " " + defaultOrRequired;

                if (maxLen < keyName.Length)
                    maxLen = keyName.Length;
            }

            foreach (String key in optionHelps.Keys)
            {
                Console.WriteLine("{0," + (-maxLen) + "}: {1}", key, optionHelps[key]);
            }
        }

        /// <summary>
        /// コマンドラインオプションを解析して値と解析に成功したかどうかを返します。
        /// </summary>
        /// <param name="args"></param>
        /// <param name="returnOptions"></param>
        /// <returns></returns>
        public Boolean TryParse(String[] args, out T returnOptions)
        {
            try
            {
                returnOptions = Parse(args);
            }
            catch (ArgumentException)
            {
                returnOptions = default(T);
                return false;
            }

            return (returnOptions != null);
        }

        /// <summary>
        /// コマンドラインオプションを解析して値を返します。
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public T Parse(String[] args)
        {
            List<String> defaultArgument = new List<string>();
            List<String> mandatories = new List<string>(_mandatoryOptions);

            T returnValue = CreateAndInitlizeInstance();

            for (Int32 i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith(Prefix))
                {
                    defaultArgument.Add(args[i]);
                    continue;
                }

                String[] parts = args[i].Split(ValueParamDelimiter, 2);
                parts[0] = parts[0].Substring(Prefix.Length);
                String memberName = (DelimiterizeAndToLowerCase ? ToUpperCamelCase(parts[0]) : parts[0]);

                // Help
                if (String.Compare(memberName, "help", true) == 0)
                    return default(T);

                if (!_availableOptions.ContainsKey(memberName) && !_availableOptions.ContainsKey(parts[0]))
                {
                    //Debug.WriteLine(String.Format("Unknown option '{0}'", parts[0]));
                    if (Strict)
                    {
                        throw new ArgumentException("invalid argument", parts[0]);
                    }
                    continue;
                }

                PropertyInfo optionProperty = _availableOptions[_availableOptions.ContainsKey(memberName) ? memberName : parts[0]];
                if (parts.Length == 1)
                {
                    // Boolean
                    if (optionProperty.PropertyType != typeof(Boolean))
                    {
                        throw new ArgumentException("Option type is a not Boolean", parts[0]);
                    }
                    optionProperty.SetValue(returnValue, true, null);
                }
                else
                {
                    // Object
                    TypeConverter typeConv = TypeDescriptor.GetConverter(optionProperty.PropertyType);
                    optionProperty.SetValue(returnValue, typeConv.ConvertFromString(parts[1]), null);
                }

                if (mandatories.Contains(optionProperty.Name))
                {
                    mandatories.Remove(optionProperty.Name);
                }
            }

            if (mandatories.Count != 0)
            {
                throw new ArgumentException("Options are missing");
            }

            if (_defaultArgumentsPropertyName != null)
            {
                PropertyInfo propInfo = _type.GetProperty(_defaultArgumentsPropertyName);
                propInfo.SetValue(returnValue, defaultArgument.ToArray(), null);
            }

            return returnValue;
        }

        #region Private Methods
        private T CreateAndInitlizeInstance()
        {
            T returnValue = Activator.CreateInstance<T>();
            foreach (PropertyInfo pi in _availableOptions.Values)
            {
                Object defaultValue;
                if (GetDefaultValue(pi, out defaultValue))
                    pi.SetValue(returnValue, defaultValue, null);
            }
            return returnValue;
        }

        private String ToUpperCamelCase(String s)
        {
            StringBuilder sb = new StringBuilder();
            for (Int32 i = 0; i < s.Length; i++)
            {
                if (s[i] == Delimiter)
                    continue;

                if (i == 0 || s[i - 1] == Delimiter)
                    sb.Append(Char.ToUpper(s[i]));
                else
                    sb.Append(s[i]);
            }
            return sb.ToString();
        }

        private String ToLowerAndDelimiterize(String s)
        {
            StringBuilder sb = new StringBuilder();
            for (Int32 i = 0; i < s.Length; i++)
            {
                if (i != 0 && Char.IsUpper(s[i]))
                {
                    sb.Append(Delimiter);
                }
                sb.Append(Char.ToLower(s[i]));
            }
            return sb.ToString();
        }

        private static String GetDescription(MemberInfo memberInfo)
        {
            Object[] attrs = memberInfo.GetCustomAttributes(typeof(DescriptionAttribute), true);
            return (attrs.Length == 0) ? "" : ((DescriptionAttribute)attrs[0]).Description;
        }

        private static Boolean GetDefaultValue(MemberInfo memberInfo, out Object value)
        {
            Object[] attrs = memberInfo.GetCustomAttributes(typeof(DefaultValueAttribute), true);
            if (attrs.Length == 0)
            {
                value = null;
                return false;
            }

            DefaultValueAttribute attr = attrs[0] as DefaultValueAttribute;
            value = attr.Value;
            return true;
        }

        private static String[] GetOptionNames(MemberInfo memberInfo)
        {
            Object[] attrs = memberInfo.GetCustomAttributes(typeof(OptionNameAliasAttribute), true);
            if (attrs.Length == 0)
                return new string[] { memberInfo.Name };

            List<String> optionNames = new List<string>();
            foreach (OptionNameAliasAttribute optNameAttr in attrs)
            {
                optionNames.Add(optNameAttr.Name);
            }
            optionNames.Add(memberInfo.Name);
            return optionNames.ToArray();
        }
        #endregion
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class OptionNameAliasAttribute : Attribute
    {
        public String Name { get; set; }
        public OptionNameAliasAttribute(String name)
        {
            Name = name;
        }
    }
}