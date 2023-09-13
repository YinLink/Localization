using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : SingletonSpawningMonoBehaviour<LocalizationManager>
{
    public class TextContent
    {
        public string content;
    }

    public enum LanguageEnum
    {
        scn,
        tcn,
        en
    }

    public LanguageEnum _language;

    public string Language
    {
        private set;
        get;
    }

    public string TestKey
    {
        get;
        set;
    }

    protected Dictionary<string, TextContent> _languageStrings;
    protected Dictionary<string, string> _cnStrings;

    protected override void Awake()
    {
        base.Awake();
        TestKey = null;
    }

    
    public void Init()
    {
        var language = PlayerPrefs.GetString("Language");
        if (string.IsNullOrEmpty(language))
            language = _language.ToString();

        Language = language;

        _languageStrings = LitJsonDoFile<Dictionary<string, TextContent>>(language);
        if (_languageStrings == null)
            _languageStrings = new Dictionary<string, TextContent>();

        _cnStrings = new Dictionary<string, string>();
        var scn = LitJsonDoFile<Dictionary<string, TextContent>>("scn");
        if (scn == null)
            scn = new Dictionary<string, TextContent>();
        foreach (var item in scn)
        {
            _cnStrings[item.Value.content] = item.Key;
        }

        //patch
        var patch = ResourceManager.Load<TextAsset>("Json/i18nPatch", language);
        if (null != patch)
        {
            var patchDic = JsonMapper.ToObject<Dictionary<string, TextContent>>(patch.text);
            foreach (var item in patchDic)
            {
                _languageStrings[item.Key] = item.Value;
            }
        }

        patch = ResourceManager.Load<TextAsset>("Json/i18nPatch", "scn");
        if (null != patch)
        {
            var patchDic = JsonMapper.ToObject<Dictionary<string, TextContent>>(patch.text);
            foreach (var item in patchDic)
            {
                _cnStrings[item.Value.content] = item.Key;
            }
        }
    }

    public void ChangeLanguage(string language)
    {
        PlayerPrefs.SetString("Language", language);
        Init();
    }

    public string GetStringByKey(string key)
    {
#if I18N_TEST
        return "tttest";
#endif

        if (null != TestKey)
            return TestKey;

        if (null == _languageStrings)
            return key;

        TextContent result = null;
        if (_languageStrings.TryGetValue(key, out result))
        {
            return result.content;
        }

        return key;
    }
    
    public string GetStringByCN(string cn)
    {
#if I18N_TEST
        return "tttest";
#endif

        if (null != TestKey)
            return TestKey;

        if (null == _cnStrings)
            return cn;

        if (Language == "scn")
            return cn;

        string key = "";
        if(_cnStrings.TryGetValue(cn, out key))
        {
            return GetStringByKey(key);
        }

        return cn;
    }

    protected T LitJsonDoFile<T>(string file)
    {
        TextAsset jsonFile = ResourceManager.Load<TextAsset>("Json/i18n", file);
        if (null == jsonFile)
        {
            Debug.LogError("Fail to load " + file);
            return default(T);
        }

        return JsonMapper.ToObject<T>(jsonFile.text);
    }
}
