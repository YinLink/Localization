using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using LitJson;
using UnityEditor;
#endif

public class TextScript: MonoBehaviour
{
    private Text _textControl;

    private Text TextControl
    {
        get
        {
            if (_textControl == null)
                _textControl = GetComponent<Text>();
            return _textControl;
        }
    }

    public string _textKey;
    public bool _typewriter;
    public float _delayPrint = 0.05f;

    private void Awake()
    {
        _textKey = "";
    }

    void Start()
    {

    }

    protected void ReshowText()
    {
        if(string.IsNullOrEmpty(_textKey))
            _textKey = TextControl.text;

        ShowText(_textKey, false);
    }

    private void OnEnable()
    {
        ReshowText();
        EventController.Register(EnumEvent.OnLocalizationChange, ReshowText);

        if(_typewriter)
        {
            StartCoroutine(DoTypeWriter());
            EventController.Register(EnumEvent.OnTextScriptTyperEnd, OnTextScriptTyperEnd);
        }
    }

    private void OnDisable()
    {
        EventController.Dispose(EnumEvent.OnLocalizationChange, ReshowText);
        if (_typewriter)
        {
            EventController.Dispose(EnumEvent.OnTextScriptTyperEnd, OnTextScriptTyperEnd);
        }
    }

#if UNITY_EDITOR

#endif

    public void ShowText(string cn, bool check = true)
    {
        if (check && _textKey == cn)
            return;

        _textKey = cn;
        TextControl.text = LocalizationManager.Instance.GetStringByCN(cn);
    }

    protected void OnTextScriptTyperEnd()
    {
        StopAllCoroutines();
        TextControl.text = LocalizationManager.Instance.GetStringByCN(_textKey);
    }

    protected IEnumerator DoTypeWriter()
    {
        var text = TextControl.text;
        TextControl.text = "";
        var tempText = "";

        bool replaceColor = false;
        var delayPrint = new WaitForSeconds(_delayPrint);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '[')
            {
                replaceColor = true;
            }
            else if (c == ']')
            {
                replaceColor = false;
            }
            else
            {
                if (replaceColor)
                    tempText += "<color=#FF8686>" + c + "</color>";
                else
                    tempText += c;
            }
            yield return delayPrint;

            TextControl.text = tempText;
        }
    }
}
