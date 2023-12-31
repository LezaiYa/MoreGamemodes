using Csv;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MoreGamemodes
{
    public static class Translator
    {
        public static Dictionary<string, Dictionary<int, string>> translateMaps;
        public const string LANGUAGE_FOLDER_NAME = "Language";
        public static void Init()
        {
            Logger.Info("加载语言文件...", "Translator");
            LoadLangs();
            Logger.Info("加载语言文件成功", "Translator");
        }
        public static void LoadLangs()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("MoreGamemodes.Resources.String.csv");
            translateMaps = new Dictionary<string, Dictionary<int, string>>();

            var options = new CsvOptions()
            {
                HeaderMode = HeaderMode.HeaderPresent,
                AllowNewLineInEnclosedFieldValues = false,
            };
            foreach (var line in CsvReader.ReadFromStream(stream, options))
            {
                if (line.Values[0][0] == '#') continue;
                try
                {
                    Dictionary<int, string> dic = new();
                    for (int i = 1; i < line.ColumnCount; i++)
                    {
                        int id = int.Parse(line.Headers[i]);
                        dic[id] = line.Values[i].Replace("\\n", "\n").Replace("\\r", "\r");
                    }
                    if (!translateMaps.TryAdd(line.Values[0], dic))
                        Logger.Warn($"待翻译的 CSV 文件中存在重复项：第{line.Index}行 => \"{line.Values[0]}\"", "Translator");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"翻译文件错误：第{line.Index}行 => \"{line.Values[0]}\"", "Translator");
                    Logger.Warn(ex.ToString(), "Translator");
                }
            }

            // カスタム翻訳ファイルの読み込み
            if (!Directory.Exists(LANGUAGE_FOLDER_NAME)) Directory.CreateDirectory(LANGUAGE_FOLDER_NAME);

            // 翻訳テンプレートの作成
            CreateTemplateFile();
            foreach (var lang in Enum.GetValues(typeof(SupportedLangs)))
            {
                if (File.Exists(@$"./{LANGUAGE_FOLDER_NAME}/{lang}.dat"))
                    LoadCustomTranslation($"{lang}.dat", (SupportedLangs)lang);
            }
        }
        public static string GetString(string s, Dictionary<string, string> replacementDic = null)
        {
            var langId = TranslationController.InstanceExists ? TranslationController.Instance.currentLanguage.languageID : SupportedLangs.English;
            string str = GetString(s, langId);
            if (replacementDic != null)
                foreach (var rd in replacementDic)
                {
                    str = str.Replace(rd.Key, rd.Value);
                }
            return str;
        }
        public static string GetString(string str, SupportedLangs langId)
        {
            var res = $"<INVALID:{str}>";
            try
            {
                if (translateMaps.TryGetValue(str, out var dic) && (!dic.TryGetValue((int)langId, out res) || res == "")) //strに該当する&無効なlangIdかresが空
                {
                    res = $"*{dic[0]}";
                }
                if (!translateMaps.ContainsKey(str)) //translateMapsにない場合、StringNamesにあれば取得する
                {
                    var stringNames = Enum.GetValues(typeof(StringNames)).Cast<StringNames>().Where(x => x.ToString() == str);
                    if (stringNames != null && stringNames.Count() > 0)
                        res = GetString(stringNames.FirstOrDefault());
                }
            }
            catch (Exception Ex)
            {
                Logger.Fatal($"在 String.csv 中的 [{str}] 处发生错误。", "Translator");
                Logger.Error("错误如下:\n" + Ex.ToString(), "Translator");
            }
            return res;
        }
        public static string GetString(StringNames stringName)
            => DestroyableSingleton<TranslationController>.Instance.GetString(stringName, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
        public static void LoadCustomTranslation(string filename, SupportedLangs lang)
        {
            string path = @$"./{LANGUAGE_FOLDER_NAME}/{filename}";
            if (File.Exists(path))
            {
                Logger.Info($"加载自定义翻译文件：{filename}", "LoadCustomTranslation");
                using StreamReader sr = new(path, Encoding.GetEncoding("UTF-8"));
                string text;
                string[] tmp = { };
                while ((text = sr.ReadLine()) != null)
                {
                    tmp = text.Split(":");
                    if (tmp.Length > 1 && tmp[1] != "")
                    {
                        try
                        {
                            translateMaps[tmp[0]][(int)lang] = tmp.Skip(1).Join(delimiter: ":").Replace("\\n", "\n").Replace("\\r", "\r");
                        }
                        catch (KeyNotFoundException)
                        {
                            Logger.Warn($"无效密钥：{tmp[0]}", "LoadCustomTranslation");
                        }
                    }
                }
            }
            else
            {
                Logger.Error($"找不到自定义翻译文件：{filename}", "LoadCustomTranslation");
            }
        }

        private static void CreateTemplateFile()
        {
            var sb = new StringBuilder();
            foreach (var title in translateMaps) sb.Append($"{title.Key}:\n");
            File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/template.dat", sb.ToString());
        }
    }
}