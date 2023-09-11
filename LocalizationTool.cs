using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System.Reflection;
using System;
using EncodeMy;
using System.Text.RegularExpressions;
using LitJson;
using Microsoft.VisualBasic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;

public class LocalizationTool
{
    public static List<string> uiPathes = new List<string>() { "UI", "UICommon", "SmallGame", "Tutorial", "Apartment", "Agent", "ApartmentArea", "ApartmentRoom", "UIMall", "UIPrefab" };
    public static List<string> wrongTCN = new List<string>() {"\\\\n", "老板", "猛犸", "癥", "外賣", "游園會", "防御", "登錄", "服務器", "斗", "日志", "愿", "鉆", "圣", "余", "家具", "土豆", "手撕面包", "面包", "面粉", "方便面", "夸", "云", "墙", "柜", "休閑", "游", "联系", "聯系", "体", "么", "后", "开" };
    public static List<string> rightTCN = new List<string>() { "\\n", "老闆", "猛獁", "症", "外送", "園遊會", "防禦", "登入", "伺服器", "鬥", "日誌", "願", "鑽", "聖", "餘", "傢俱", "馬鈴薯", "手撕吐司", "麵包", "麵粉", "速食麵", "誇", "雲", "牆", "櫃", "休閒", "遊", "聯繫", "聯繫", "體", "麼", "後", "開" };

    [MenuItem("Tools/Localization/Check")]
    public static void LocalizationCheck()
    {
        LocalizationCheck(false);
    }

    [MenuItem("Tools/Localization/Check Repeat")]
    public static void LocalizationCheckRepeat()
    {
        var prefix = Path.Combine(Application.dataPath, ResourceManager.EDITOR_RESOURCE_PATH);
        foreach (var up in uiPathes)
        {
            var files = Directory.GetFiles(Path.Combine(prefix, up));
            foreach (var file in files)
            {
                //Debug.Log(file.Remove(0, prefix.Length));
                if (file.EndsWith(".prefab"))
                {
                    var ui = AssetDatabase.LoadAssetAtPath<GameObject>(file.Replace(Application.dataPath, "Assets/"));

                    var references = new List<Text>();
                    var referencesMesh = new List<TextMesh>();
                    foreach (var com in ui.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if(null == com)
                        {
                            continue;
                        }    

                        var ts = com.GetComponents<TextScript>();
                        if (ts.Length > 1)
                        {
                            Debug.LogError(com.gameObject.name + " : " + ts.Length);
                            //for (int i = ts.Length - 1; i >= 1; i--)
                            //{
                            //    Component.DestroyImmediate(ts[i]);
                            //}
                            for (int i = 1; i < ts.Length; i++)
                            {
                                Component.DestroyImmediate(com.GetComponent<TextScript>(), true);
                            }
                        }
                    }
                }
            }
        }

        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Localization/Check And Reset")]
    public static void LocalizationCheckAndReset()
    {
        if(EditorUtility.DisplayDialog("Warning", "已经实装翻译文本后不建议全量捕获文本，建议使用Check命令增量更新。是否依然要进行Check&Reset操作？", "非常确定", "不了不了"))
        {
            if (EditorUtility.DisplayDialog("Warning", "再次确定要改？", "非常确定", "不了不了"))
                LocalizationCheck(true);
        }
    }

    protected static void LocalizationCheck(bool reset)
    {
        var csv = Path.Combine(Application.dataPath, "i18n_ui.csv");

        var csvLines = new List<string>();
        var keyStrings = new List<string>();

        //增量式: 先读取现有文本项
        if (!reset)
        {
            foreach (var line in File.ReadAllLines(csv))
            {
                csvLines.Add(line);
                var content = line.Split(new[] { ",\"" }, StringSplitOptions.None);

                if (content.Length > 1)
                {
                    //Debug.Log(content[1].Substring(0, content[1].Length - 1));
                    keyStrings.Add(content[1].Substring(0, content[1].Length - 1));
                }
            }
        }

        //UI和场景
        var textType = typeof(Text);
        var textsType = typeof(Text[]);
        var textArrayType = typeof(List<Text>);
        var textMeshType = typeof(TextMesh);
        var strType = typeof(string);
        var strList = typeof(List<string>);
        var strArray = typeof(string[]);
        var prefix = Path.Combine(Application.dataPath, ResourceManager.EDITOR_RESOURCE_PATH);
        int num = 0;
        foreach (var up in uiPathes)
        {
            var files = Directory.GetFiles(Path.Combine(prefix, up), "*", SearchOption.AllDirectories);
            var ignores = new List<string>() {"UICheating", "UIClerkRoomExchange", "UILodgerMaterial", "UIMessageBoard", "UISmallGame", "UIJxwArea", "UIWorldFrag", "UILodgerMaterialViewAll",
            "UIGuideContent", "UIExpressReceive", "UILodgerMaterialView"};
            foreach (var file in files)
            {
                //Debug.Log(file.Remove(0, prefix.Length));
                if (file.EndsWith(".prefab"))
                {
                    var ui = AssetDatabase.LoadAssetAtPath<GameObject>(file.Replace(Application.dataPath, "Assets/"));
                    if (ignores.Contains(ui.name))
                        continue;

                    var references = new List<Text>();
                    var referencesMesh = new List<TextMesh>();
                    foreach (var com in ui.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (null != com)
                        {
                            foreach (var field in com.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            {
                                if (field.FieldType == textType)
                                {
                                    var value = field.GetValue(com);
                                    if (null != value)
                                    {
                                        references.Add(value as Text);
                                    }
                                }
                                else if (field.FieldType == textsType)
                                {
                                    var value = field.GetValue(com);
                                    if (null != value)
                                    {
                                        foreach(var item in value as Text[])
                                            references.Add(item);
                                    }
                                }
                                else if (field.FieldType == textArrayType)
                                {
                                    var value = field.GetValue(com);
                                    if (null != value)
                                    {
                                        foreach (var item in value as List<Text>)
                                            references.Add(item);
                                    }
                                }
                                else if (field.FieldType == textMeshType)
                                {
                                    var value = field.GetValue(com);
                                    if (null != value)
                                    {
                                        referencesMesh.Add(value as TextMesh);
                                    }
                                }
                                else if (field.FieldType == strType && com.gameObject.GetComponent<Text>() == null && com.gameObject.GetComponent<InputField>() == null)
                                {
                                    var str = field.GetValue(com) as string;
                                    if (!string.IsNullOrEmpty(str))
                                    {
                                        foreach (var character in str)
                                        {
                                            if ((int)character > 127)
                                            {
                                                if (!keyStrings.Contains(str))
                                                {
                                                    Debug.Log("\"" + ui.name + "_" + com.gameObject.name + "_" + com.gameObject.GetHashCode() + "_" + field.Name + "\",\"" + str + "\"");
                                                    csvLines.Add("\"" + ui.name + "_" + com.gameObject.name + "_" + com.gameObject.GetHashCode() + "_" + field.Name + "\",\"" + str + "\"");
                                                    keyStrings.Add(str);
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                                else if (field.FieldType == strArray || field.FieldType == strList)
                                {
                                    List<string> strings = field.FieldType == strList ? field.GetValue(com) as List<string> : new List<string>(field.GetValue(com) as string[]);
                                    int strCounter = 0;
                                    if (null == strings)
                                    {
                                        //Debug.LogError("empty strings");
                                    }
                                    else
                                    {
                                        foreach (var str in strings)
                                        {
                                            strCounter++;
                                            foreach (var character in str)
                                            {
                                                if ((int)character > 127)
                                                {
                                                    if (!keyStrings.Contains(str))
                                                    {
                                                        Debug.Log("\"" + ui.name + "_" + com.gameObject.name + "_" + com.gameObject.GetHashCode() + "_" + field.Name + "_" + strCounter + "\",\"" + str + "\"");
                                                        csvLines.Add("\"" + ui.name + "_" + com.gameObject.name + "_" + com.gameObject.GetHashCode() + "_" + field.Name + "_" + strCounter + "\",\"" + str + "\"");
                                                        keyStrings.Add(str);
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    foreach (var text in ui.GetComponentsInChildren<Text>(true))
                    {
                        if (string.IsNullOrEmpty(text.text))
                            continue;
                        if (text.text == "?")
                            continue;
                        if (text.text == "+")
                            continue;
                        if (text.text == "-")
                            continue;
                        if (text.text == "ID")
                            continue;
                        if (int.TryParse(text.text, out num))
                            continue;

                        if (references.Contains(text))
                        {
                            //Debug.LogError(text.name + ":" + text.text);
                            continue;
                        }

                        foreach (var character in text.text)
                        {
                            if ((int)character > 127)
                            {
                                if (!keyStrings.Contains(text.text))
                                {
                                    Debug.Log("\"" + ui.name + "_" + text.name + "_" + text.GetHashCode() + "\",\"" + text.text + "\"");
                                    csvLines.Add("\"" + ui.name + "_" + text.name + "_" + text.GetHashCode() + "\",\"" + text.text + "\"");
                                    keyStrings.Add(text.text);
                                }
                            }
                        }
                        if (null == text.GetComponent<TextScript>())
                        {
                            if (text.transform.parent.GetComponent<Dropdown>() != null)
                                continue;
                            text.gameObject.AddComponent<TextScript>();
                        }
                    }
                    foreach (var text in ui.GetComponentsInChildren<TextMesh>(true))
                    {
                        if (string.IsNullOrEmpty(text.text))
                            continue;
                        if (text.text == "?")
                            continue;
                        if (text.text == "+")
                            continue;
                        if (text.text == "-")
                            continue;
                        if (text.text == "ID")
                            continue;
                        if (int.TryParse(text.text, out num))
                            continue;

                        if (referencesMesh.Contains(text))
                        {
                            //Debug.LogError(text.name + ":" + text.text);
                            continue;
                        }

                        foreach (var character in text.text)
                        {
                            if ((int)character > 127)
                            {
                                if (!keyStrings.Contains(text.text))
                                {
                                    Debug.Log("\"" + ui.name + "_" + text.name + "_" + text.GetHashCode() + "\",\"" + text.text + "\"");
                                    csvLines.Add("\"" + ui.name + "_" + text.name + "_" + text.GetHashCode() + "\",\"" + text.text + "\"");
                                    keyStrings.Add(text.text);
                                }
                            }
                        }
                        if (null == text.GetComponent<TextMeshScript>())
                            text.gameObject.AddComponent<TextMeshScript>();
                    }
                }
            }
        }


        //脚本
        prefix = Path.Combine(Application.dataPath, "Scripts");
        var scriptPath = new List<string>() { "Game", "UI", "Network" , "SmallGame", "3rd/Common" };
        var scriptIgnore = new List<string>() { "UICheating" };

        foreach(var path in scriptPath)
        {
            var dir = (path.Contains("/") || path.Contains("\\")) ? Path.Combine(Application.dataPath, path) : Path.Combine(prefix, path);
            var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            foreach(var file in files)
            {
                if(file.EndsWith(".cs"))
                {
                    int lineCounter = 0;
#if UNITY_ANDROID
                    var scriptName = file.Substring(file.LastIndexOf('\\'));
#elif UNITY_IOS
                    var scriptName = file.Substring(file.LastIndexOf('/'));
#endif
                    scriptName = scriptName.Substring(1, scriptName.Length - 4);

                    if (scriptIgnore.Contains(scriptName))
                        continue;

                    foreach (var line in File.ReadAllLines(file))
                    {
                        lineCounter++;

                        int index = line.IndexOf("/");
                        if (index >= 0 && index + 1 < line.Length && line[index + 1] == '/')
                            continue;

                        if (line.Contains(".GetStringByCN("))
                        {
                            var content = line.Split('"');
                            for (int i = 0; i < content.Length; i++)
                            {
                                if (content[i].EndsWith(".GetStringByCN("))
                                {
                                    if (i + 1 < content.Length)
                                    {
                                        if (!keyStrings.Contains(content[i + 1]))
                                        {
                                            csvLines.Add("\"" + scriptName + "_" + lineCounter + "\",\"" + content[i + 1] + "\"");
                                            keyStrings.Add(content[i + 1]);
                                        }
                                    }
                                    break;
                                }
                            }
                            continue;
                        }

                        if (line.Contains("StringExtends.Format("))
                        {
                            var content = line.Split('"');
                            for (int i = 0; i < content.Length; i++)
                            {
                                if (content[i].EndsWith("Extends.Format("))
                                {
                                    if (i + 1 < content.Length)
                                    {
                                        if(!keyStrings.Contains(content[i + 1]))
                                        {
                                            csvLines.Add("\"" + scriptName + "_" + lineCounter + "\",\"" + content[i + 1] + "\"");
                                            keyStrings.Add(content[i + 1]);
                                        }
                                    }
                                    break;
                                }
                            }
                            continue;
                        }

                        if (line.Contains(".ShowText("))
                        {
                            foreach (var character in line)
                            {
                                if ((int)character > 127)
                                {
                                    var content = line.Split('"');
                                    for (int i = 0; i < content.Length; i++)
                                    {
                                        if (content[i].EndsWith(".ShowText("))
                                        {
                                            if (i + 1 < content.Length)
                                            {
                                                if (!keyStrings.Contains(content[i + 1]))
                                                {
                                                    csvLines.Add("\"" + scriptName + "_" + lineCounter + "\",\"" + content[i + 1] + "\"");
                                                    keyStrings.Add(content[i + 1]);
                                                }
                                            }
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                            continue;
                        }

                        if (line.Contains("UIManager.Instance.ShowHint("))
                        {
                            var content = line.Split('"');
                            for (int i = 0; i < content.Length; i++)
                            {
                                if (content[i].EndsWith(".ShowHint("))
                                {
                                    if (i + 1 < content.Length)
                                    {
                                        if (!keyStrings.Contains(content[i + 1]))
                                        {
                                            csvLines.Add("\"" + scriptName + "_" + lineCounter + "\",\"" + content[i + 1] + "\"");
                                            keyStrings.Add(content[i + 1]);
                                        }
                                    }
                                    break;
                                }
                            }
                            continue;
                        }

                        if (line.Contains("UIManager.Instance.ShowUnlockHint(") || line.Contains("UIManager.Instance.ShowHintSp("))
                        {
                            int wordsCounter = 0;
                            foreach (var words in GetPatternText(line, false))
                            {
                                wordsCounter++;
                                foreach (var character in words)
                                {
                                    if ((int)character > 127)
                                    {
                                        if (!keyStrings.Contains(words))
                                        {
                                            Debug.Log("\"" + scriptName + "_" + lineCounter + "_" + wordsCounter + "\",\"" + words + "\"");
                                            csvLines.Add("\"" + scriptName + "_" + lineCounter + "_" + wordsCounter + "\",\"" + words + "\"");
                                            keyStrings.Add(words);
                                        }
                                    }
                                }
                            }
                            continue;
                        }

                        if (line.Contains("UIManager.Instance.ShowMessage("))
                        {
                            var content = line.Split('"');
                            for (int i = 0; i < content.Length; i++)
                            {
                                if (content[i].EndsWith(".ShowMessage("))
                                {
                                    if (i + 1 < content.Length)
                                    {
                                        if (!keyStrings.Contains(content[i + 1]))
                                        {
                                            csvLines.Add("\"" + scriptName + "_" + lineCounter + "\",\"" + content[i + 1] + "\"");
                                            keyStrings.Add(content[i + 1]);
                                        }
                                    }
                                    break;
                                }
                            }
                            continue;
                        }

                        if (line.Contains("UIManager.Instance.ShowMessageSmall("))
                        {
                            var content = line.Split('"');
                            for (int i = 0; i < content.Length; i++)
                            {
                                if (content[i].EndsWith(".ShowMessageSmall("))
                                {
                                    if (i + 1 < content.Length)
                                    {
                                        if (!keyStrings.Contains(content[i + 1]))
                                        {
                                            csvLines.Add("\"" + scriptName + "_" + lineCounter + "\",\"" + content[i + 1] + "\"");
                                            keyStrings.Add(content[i + 1]);
                                        }
                                    }
                                    break;
                                }
                            }
                            continue;
                        }

                        if (line.Contains("new UIDialog.DialogData(") || line.Contains("UIManager.Instance._bubblePool.ShowBubble("))
                        {
                            int wordsCounter = 0;
                            foreach(var words in GetPatternText(line, false))
                            {
                                wordsCounter++;
                                foreach (var character in words)
                                {
                                    if ((int)character > 127)
                                    {
                                        if (!keyStrings.Contains(words))
                                        {
                                            Debug.Log("\"" + scriptName + "_" + lineCounter + "_" + wordsCounter + "\",\"" + words + "\"");
                                            csvLines.Add("\"" + scriptName + "_" + lineCounter + "_" + wordsCounter + "\",\"" + words + "\"");
                                            keyStrings.Add(words);
                                        }
                                    }
                                }
                            }
                            continue;
                        }

                        if (line.Contains(".text = "))
                        {
                            foreach (var character in line)
                            {
                                if ((int)character > 127)
                                {
                                    Debug.LogError(scriptName + "_" + lineCounter + ":" + line + "\n" + file);

                                    var content = line.Split('"');
                                    for (int i = 0; i < content.Length; i++)
                                    {
                                        if (content[i].EndsWith(".text = "))
                                        {
                                            if (i + 1 < content.Length)
                                            {
                                                if (!keyStrings.Contains(content[i + 1]))
                                                {
                                                    csvLines.Add("\"" + scriptName + "_" + lineCounter + "\",\"" + content[i + 1] + "\"");
                                                    keyStrings.Add(content[i + 1]);
                                                }
                                            }
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                        }

                        if (line.Contains(".Append(\""))
                        {
                            int wordsCounter = 0;
                            foreach (var words in GetPatternText(line, false))
                            {
                                wordsCounter++;
                                foreach (var character in words)
                                {
                                    if ((int)character > 127)
                                    {
                                        if (!keyStrings.Contains(words))
                                        {
                                            Debug.Log("\"" + scriptName + "_" + lineCounter + "_" + wordsCounter + "\",\"" + words + "\"");
                                            csvLines.Add("\"" + scriptName + "_" + lineCounter + "_" + wordsCounter + "\",\"" + words + "\"");
                                            keyStrings.Add(words);
                                        }
                                    }
                                }
                            }
                            continue;
                        }

                        if (line.Contains("Debug.Log") || line.Contains("DebugManager.Debug") || line.Contains("DebugManager.Log") || line.Contains("[Header(") || line.Contains("[Obsolete("))
                            continue;

                        if(line.Contains("\""))
                        {
                            {
                                foreach (var character in line)
                                {
                                    if ((int)character > 127)
                                    {
                                        Debug.LogError("unexpect words : " + scriptName + "_" + lineCounter + ":" + line + "\n" + file);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        Debug.Log("done.");

        if (File.Exists(csv))
            File.Delete(csv);

        File.WriteAllLines(csv, csvLines);

        AssetDatabase.Refresh();
    }

    //[MenuItem("Tools/Localization/Test")]
    public static void Test()
    {
        //GetPatternText("\"dsfsdfsdf\",\"902738467835029835\"khjkhdakjfh902384hjkdhfkjhsakfhskdjhfsdfas");
        //public string
        var prefix = Path.Combine(Application.dataPath, "Scripts");
        var scriptPath = new List<string>() { "Game/LuaManager.cs", "Game/Config" };
        var counter = 0;
        foreach (var path in scriptPath)
        {
            string[] files;
            if (path.Contains(".cs"))
            {
                files = new string[] { Path.Combine(prefix, path) };
            }
            else
            {
                files = Directory.GetFiles(Path.Combine(prefix, path), "*", SearchOption.AllDirectories);
            }

            foreach (var file in files)
            {
                if (file.EndsWith(".cs"))
                {
                    counter = 0;
                    var lines = new List<string>(File.ReadAllLines(file));
                    foreach (var line in lines)
                    {
                        if (line.Contains("//public string "))
                        {
                            counter ++;
                            //Debug.Log(file + "  " + counter + "  " + line);
                        }
                    }

                    while(counter > 0)
                    {
                        for (int i = 0; i < lines.Count; i++)
                        {
                            if(lines[i].Contains("//public string "))
                            {
                                lines[i] = lines[i].Replace("//public", "public");
                                lines.Insert(i + 1, "#endif");
                                lines.Insert(i, "#if UNITY_EDITOR && I18N_DISABLE");
                                counter--;
                                break;
                            }
                        }
                    }
                    File.WriteAllLines(file, lines);
                }
            }
        }
    }

    [MenuItem("Tools/Localization/Remove Text Script")]
    public static void RemoveTextScript()
    {
        var ui = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/" + ResourceManager.EDITOR_RESOURCE_PATH + "/UI/UICheating");
        if (null != ui)
        {
            TextScript[] cmps = ui.GetComponentsInChildren<TextScript>(true);
            List<SerializedObject> modifiedGos = new List<SerializedObject>(cmps.Length);
            foreach (var cmp in cmps)
            {
                SerializedObject obj = new SerializedObject(cmp.gameObject);
                SerializedProperty prop = obj.FindProperty("m_Component");
                // 这里再次找组件，只是为了找到目标组件在GameObject上挂载的位置
                Component[] allCmps = cmp.gameObject.GetComponents<Component>();
                for (int i = 0; i < allCmps.Length; ++i)
                {
                    if (allCmps[i] == cmp)
                    {
                        prop.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
                modifiedGos.Add(obj);
            }
            foreach (SerializedObject so in modifiedGos)
            {
                // Apply之后cmps里的所有组件都会被销毁，导致后面的清理无法执行，
                // 所以将SO对象缓存，最后一起清理。
                so.ApplyModifiedProperties();
            }
        }
    }

    //[MenuItem("Tools/Localization/Modify Config")]
    public static void LocalizationModifyConfig()
    {
        var prefix = Path.Combine(Application.dataPath, "Scripts");

        var files = Directory.GetFiles(Path.Combine(prefix, "Game/Config"), "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            if (file.EndsWith(".cs"))
            {
                List<string> strings = new List<string>();
                foreach (var line in File.ReadAllLines(file))
                {
                    if (line.Contains("class Cfg") && !line.Contains(":"))
                    {
                        strings.Add(line + " : ConfigData");
                    }
                    else
                    {
                        strings.Add(line);
                    }
                }

                File.WriteAllLines(file, strings);
            }
        }

        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Localization/SC to TC")]
    public static void LocalizationScToTc()
    {
        var files = new List<string>() { "i18n.csv", "i18n_ui.csv" };

        var scContent = "{";
        var tcContent = "{";
        var fileSc = Path.Combine(Application.dataPath, "LocalResources/Json/I18n/scn.json");
        var fileTc = Path.Combine(Application.dataPath, "LocalResources/Json/I18n/tcn.json");

        foreach (var item in files)
        {
            var file = Path.Combine(Application.dataPath, item);
            if (!File.Exists(file))
            {
                Debug.LogError("未找到本地化文本文档" + item + "，请检查。");
                continue;
            }

            var lines = File.ReadAllLines(file);
            for(int i = 0; i < lines.Length; i ++)
            {
                var content = lines[i].Split(new[] { ",\"" }, StringSplitOptions.None);
                if (content.Length > 0)
                {
                    scContent += content[0] + ":";
                    tcContent += content[0] + ":";

                    if (content.Length > 1)
                    {
                        scContent += "\"" + content[1];
                        tcContent += "\"" + ConvertChinTrad(content[1]);

                        //判断后续行文本是否是同一组
                        for(int j = i + 1; j < lines.Length; j ++)
                        {
                            if(lines[j].Length == 0 )
                            {
                                scContent += "\\n\\n";
                                tcContent += "\\n\\n";
                                i++;
                            }
                            else if (lines[j].Length > 0 && !lines[j].StartsWith("\""))
                            {
                                scContent += "\\n" + lines[j];
                                tcContent += "\\n" + ConvertChinTrad(lines[j]);
                                i++;
                            }
                            else
                                break;
                        }

                        scContent += ",";
                        tcContent += ",";
                    }
                    else
                    {
                        scContent += ",";
                        tcContent += ",";
                    }
                }
            }
        }

        if (scContent.Length > 1)
            scContent = scContent.Substring(0, scContent.Length - 1);
        if (tcContent.Length > 1)
            tcContent = tcContent.Substring(0, tcContent.Length - 1);

        scContent += "}";
        tcContent += "}";

        if (File.Exists(fileSc))
            File.Delete(fileSc);

        if (File.Exists(fileTc))
            File.Delete(fileTc);

        File.WriteAllText(fileSc, scContent);
        File.WriteAllText(fileTc, tcContent);

        AssetDatabase.Refresh();
        Debug.Log("done");
    }

    [MenuItem("Tools/Localization/Mix SC&TC")]
    public static void LocalizationMixScTc()
    {
        var folder = EditorUtility.OpenFolderPanel("选择导出目录", Application.dataPath, "");

        var tcn = LitJsonDoFile<Dictionary<string, LocalizationManager.TextContent>>("tcn");
        var scn = LitJsonDoFile<Dictionary<string, LocalizationManager.TextContent>>("scn");

        var outFile = "文本.csv" ;

        var output = Path.Combine(folder, outFile);
        if (File.Exists(output))
            File.Delete(output);

        List<string> lines = new List<string>();
        foreach (var item in scn)
        {
            var line = "\"" + item.Key + "\",\"";
            line += item.Value.content + "\"";
            LocalizationManager.TextContent tLine ;
            if(tcn.TryGetValue(item.Key, out tLine))
            {
                line += ",\"" + tLine.content + "\"";
            }
            lines.Add(line);
        }

        var data = new List<string>(lines);

        File.WriteAllLines(output, data, System.Text.Encoding.UTF8);

        AssetDatabase.Refresh();
        Debug.Log("done.");
    }

    [MenuItem("Tools/Localization/Merge")]
    public static void LocalizationMerge()
    {
        var tcn = LitJsonDoFile<Dictionary<string, LocalizationManager.TextContent>>("tcn");
        var scn = LitJsonDoFile<Dictionary<string, LocalizationManager.TextContent>>("scn");
        var fileSc = Path.Combine(Application.dataPath, "LocalResources/Json/I18n/scn.json");
        var fileTc = Path.Combine(Application.dataPath, "LocalResources/Json/I18n/tcn.json");

        var files = new List<string>() { "i18n.csv", "i18n_ui.csv" };
        foreach (var item in files)
        {
            var file = Path.Combine(Application.dataPath, item);
            if (!File.Exists(file))
            {
                Debug.LogError("未找到本地化文本文档" + item + "，请检查。");
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                var content = lines[i].Split(new[] { ",\"" }, StringSplitOptions.None);
                if (content.Length > 0)
                {
                    if (content.Length > 1 && content[0].Length > 2)
                    {
                        var key = content[0].Substring(1, content[0].Length - 2);
                        //新id的
                        if (!tcn.ContainsKey(key) || !scn.ContainsKey(key) || (content[1].Length > 1 && scn[key].content != (content[1].Substring(0, content[1].Length - 1))))
                        {
                            //Debug.Log(!tcn.ContainsKey(key));
                            //Debug.Log(!scn.ContainsKey(key));
                            //Debug.Log(content[1].Length > 1 && !scn[key].content.StartsWith(content[1].Substring(0, content[1].Length - 1)));

                            var scContent = content[1];
                            var tcContent = ConvertChinTrad(content[1]);

                            //判断后续行文本是否是同一组
                            for (int j = i + 1; j < lines.Length; j++)
                            {
                                if (lines[j].Length == 0)
                                {
                                    scContent += "\\n\\n";
                                    tcContent += "\\n\\n";
                                    i++;
                                }
                                else if (lines[j].Length > 0 && !lines[j].StartsWith("\""))
                                {
                                    scContent += "\\n" + lines[j];
                                    tcContent += "\\n" + ConvertChinTrad(lines[j]);
                                    i++;
                                }
                                else
                                    break;
                            }

                            scn[key] = new LocalizationManager.TextContent();
                            tcn[key] = new LocalizationManager.TextContent();
                            scn[key].content = scContent.Substring(0, scContent.Length - 1);
                            tcn[key].content = CheckTcnWrongWords(tcContent.Substring(0, tcContent.Length - 1));
                            Debug.Log(key);
                            Debug.Log(scContent);
                            Debug.Log(tcContent);
                        }
                        ////TODO 同id不同文本的
                        //else if(content[1].Length > 1 && !scn[key].content.StartsWith(content[1].Substring(0, content[1].Length - 1)))
                        //{
                        //    //Debug.Log(key);
                        //    //Debug.Log(scn[key].content);
                        //    //Debug.Log(content[1].Substring(0, content[1].Length - 1));
                        //}
                    }
                }
            }
        }

        File.WriteAllText(fileSc, JsonMapper.ToJson(scn).Replace("\\\\n", "\\n"));
        File.WriteAllText(fileTc, JsonMapper.ToJson(tcn).Replace("\\\\n", "\\n"));

        AssetDatabase.Refresh();

        Debug.Log("done");
    }

    [MenuItem("Tools/Localization/Patch")]
    public static void LocalizationPatch()
    {
        var tcn = LitJsonDoFile<Dictionary<string, LocalizationManager.TextContent>>("tcn");
        var scn = LitJsonDoFile<Dictionary<string, LocalizationManager.TextContent>>("scn");
        var fileSc = Path.Combine(Application.dataPath, "LocalResources/Json/I18nPatch/scn.json");
        var fileTc = Path.Combine(Application.dataPath, "LocalResources/Json/I18nPatch/tcn.json");

        var scnPatch = new Dictionary<string, LocalizationManager.TextContent>();
        var tcnPatch = LitJsonDoFile<Dictionary<string, LocalizationManager.TextContent>>("tcn", "Json/i18nPatch");

        var files = new List<string>() { "i18n.csv", "i18n_ui.csv" };
        foreach (var item in files)
        {
            var file = Path.Combine(Application.dataPath, item);
            if (!File.Exists(file))
            {
                Debug.LogError("未找到本地化文本文档" + item + "，请检查。");
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                var content = lines[i].Split(new[] { ",\"" }, StringSplitOptions.None);
                if (content.Length > 0)
                {
                    if (content.Length > 1 && content[0].Length > 2)
                    {
                        var key = content[0].Substring(1, content[0].Length - 2);
                        //新id的
                        if (!tcn.ContainsKey(key) || !scn.ContainsKey(key) || (content[1].Length > 1 && !scn[key].content.StartsWith(content[1].Substring(0, content[1].Length - 1))))
                        {
                            //Debug.Log(!tcn.ContainsKey(key));
                            //Debug.Log(!scn.ContainsKey(key));
                            //Debug.Log(content[1].Length > 1 && !scn[key].content.StartsWith(content[1].Substring(0, content[1].Length - 1)));

                            var scContent = content[1];
                            var tcContent = ConvertChinTrad(content[1]);

                            //判断后续行文本是否是同一组
                            for (int j = i + 1; j < lines.Length; j++)
                            {
                                if (lines[j].Length == 0)
                                {
                                    scContent += "\\n\\n";
                                    tcContent += "\\n\\n";
                                    i++;
                                }
                                else if (lines[j].Length > 0 && !lines[j].StartsWith("\""))
                                {
                                    scContent += "\\n" + lines[j];
                                    tcContent += "\\n" + ConvertChinTrad(lines[j]);
                                    i++;
                                }
                                else
                                    break;
                            }

                            scnPatch[key] = new LocalizationManager.TextContent();
                            tcnPatch[key] = new LocalizationManager.TextContent();
                            scnPatch[key].content = scContent.Substring(0, scContent.Length - 1);
                            tcnPatch[key].content = CheckTcnWrongWords(tcContent.Substring(0, tcContent.Length - 1));
                            Debug.Log(key);
                            Debug.Log(scContent);
                            Debug.Log(tcContent);
                        }
                        ////TODO 同id不同文本的
                        //else if(content[1].Length > 1 && !scn[key].content.StartsWith(content[1].Substring(0, content[1].Length - 1)))
                        //{
                        //    //Debug.Log(key);
                        //    //Debug.Log(scn[key].content);
                        //    //Debug.Log(content[1].Substring(0, content[1].Length - 1));
                        //}
                    }
                }
            }
        }

        File.WriteAllText(fileSc, JsonMapper.ToJson(scnPatch).Replace("\\\\n", "\\n"));
        File.WriteAllText(fileTc, JsonMapper.ToJson(tcnPatch).Replace("\\\\n", "\\n"));

        AssetDatabase.Refresh();

        Debug.Log("done");
    }

    /// <summary>
    /// 实现简体到繁体的互转
    /// </summary>
    /// <param name="strInput"></param>
    /// <param name="flag"></param>
    /// <returns>转换的后的字符串</returns>
    public static string ConvertChinTrad(string strInput)
    {
        //EncodeRobert edControl = new EncodeRobert();
        //string strResult = "";
        //if (strInput == null)
        //    return strResult;
        //if (strInput.ToString().Length >= 1)
        //    strResult = edControl.SCTCConvert(ConvertType.Simplified, ConvertType.Traditional, strInput);
        //else
        //    strResult = strInput;
        //return strResult;
        return ChineseStringUtility.ToTraditional(strInput);   //   简体转繁体
    }

    /// <summary>
    /// 实现繁体到简体的互转
    /// </summary>
    /// <param name="strInput"></param>
    /// <param name="flag"></param>
    /// <returns>转换的后的字符串</returns>
    public static string ConvertChinSim(string strInput)
    {
        EncodeRobert edControl = new EncodeRobert();
        string strResult = "";
        if (strInput == null)
            return strResult;
        if (strInput.ToString().Length >= 1)
            strResult = edControl.SCTCConvert(ConvertType.Traditional, ConvertType.Simplified, strInput);
        else
            strResult = strInput;
        return strResult;
    }

    public static List<string> GetPatternText(string text, bool quote = true)
    {
        var result = new List<string>();
        foreach (Match match in Regex.Matches(text, "\"([^\"]*)\""))
        {
            //Debug.Log(match.Value);
            if(quote)
                result.Add(match.Value);
            else
                result.Add(match.Value.Substring(1, match.Value.Length - 2));
        }

        return result;
    }
    protected static T LitJsonDoFile<T>(string file, string path = "Json/i18n")
    {
        TextAsset jsonFile = ResourceManager.Load<TextAsset>(path, file);
        if (null == jsonFile)
        {
            Debug.LogError("Fail to load " + file);
            return default(T);
        }

        return JsonMapper.ToObject<T>(jsonFile.text);
    }



    protected static string CheckTcnWrongWords(string tcn)
    {
        for (int i = 0; i < wrongTCN.Count; i++)
        {
            if(tcn.Contains(wrongTCN[i]))
            {
                tcn = tcn.Replace(wrongTCN[i], rightTCN[i]);
            }
        }

        return tcn;
    }
#if UNITY_ANDROID
    public static class ChineseStringUtility
    {
        internal const int LOCALE_SYSTEM_DEFAULT = 0x0800;
        internal const int LCMAP_SIMPLIFIED_CHINESE = 0x02000000;
        internal const int LCMAP_TRADITIONAL_CHINESE = 0x04000000;

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int LCMapString(int Locale, int dwMapFlags, string lpSrcStr, int cchSrc, [Out] string lpDestStr, int cchDest);

        public static string ToSimplified(string source)
        {
            String target = new String(' ', source.Length);
            int ret = LCMapString(LOCALE_SYSTEM_DEFAULT, LCMAP_SIMPLIFIED_CHINESE, source, source.Length, target, source.Length);
            return target;
        }

        public static string ToTraditional(string source)
        {
            String target = new String(' ', source.Length);
            int ret = LCMapString(LOCALE_SYSTEM_DEFAULT, LCMAP_TRADITIONAL_CHINESE, source, source.Length, target, source.Length);
            return target;
        }
    }
#elif UNITY_IOS

    public static class ChineseStringUtility
    {
        public static string ToSimplified(string source)
        {
            return ChineseConverter.Convert(source, ChineseConversionDirection.TraditionalToSimplified);
        }

        public static string ToTraditional(string source)
        {
            return ChineseConverter.Convert(source, ChineseConversionDirection.SimplifiedToTraditional);
        }
    }

#endif
}
