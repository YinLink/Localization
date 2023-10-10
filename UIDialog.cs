using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

public class UIDialog : UIBaseFullRect
{
    public enum DialogType
    {
        Input,
        Chat
    }

    [Serializable]
    public class DialogData
    {
        public AgentType _type;
        public string _id;
        public string _words;
        public int _pos;
        public List<string> _selection;

        public DialogData(AgentType type, string id, string words, int pos = 2, params string[] selection)
        {
            _type = type;
            _id = id;
            _words = LocalizationManager.Instance.GetStringByCN(words);
            _pos = pos;
            _selection = new List<string>(selection);
        }
    }

    public Image _nameBg;
    public Color _nameBgColor, _nameTxtColor;
    //public RectTransform _dialogAgentRect, _dialogTxtRect;
    public UIAgentInfo _dialogAgent;
    public Text _dialogTxt;
    public GameObject _waitForNext;
    public float _interval = 0.05f;
    public float _waitTime = 0.1f;

    public GameObject _selectionPanel;
    public List<Button> _selections;

    public GameObject _imagePanel;
    public Image _image;

    public int SelectionIndex
    {
        get
        {
            return _selectionIndex;
        }
    }

    protected DialogType _dialogType;
    protected List<DialogData> _dialogs;
    protected List<LuaManager.CfgChatContent> _chatDialogs;
    protected LuaManager.CfgChatContent _curChatDialog;
    protected int _storyID, _dialogIndex, _selectionIndex;
    protected UnityAction _dialogCallback;
    protected string _dialogLodger;
    protected bool _wordsDone;

    public override void Open(params object[] param)
    {
        base.Open(param);

        if (param.Length > 2)
            _dialogLodger = param[2].ToString();

        if (param[0] is string)
        {
            _dialogType = DialogType.Chat;
            _dialogIndex = 1;

            ShowAgent(AgentType.Lodger, "", 0);
            _chatDialogs = LuaManager.Instance.GetChatContent(param[0].ToString());
        }
        else
        {
            _dialogType = DialogType.Input;
            _dialogIndex = 0;

            _dialogs = param[0] as List<DialogData>;
        }

        if (param.Length > 1)
            _dialogCallback = param[1] as UnityAction;
        else
            _dialogCallback = null;

        //Utility.SetActive(_dialogAgent.gameObject, false);

        _selectionIndex = 0;
        ShowDialog();
    }

    protected void EndDialog()
    {
        if (null != _dialogCallback)
        {
            _dialogCallback();
        }

        UIManager.Instance.Command(UIManager.UIOperate.Hide, UIManager.UIType.StoryDialog, UIManager.CanvasType.CanvasMessage);
    }

    protected void ShowDialog()
    {
        AnyKeyDown = false;

        if (_dialogType == DialogType.Input)
        {
            if (_dialogIndex < _dialogs.Count)
            {
                ShowAgent(_dialogs[_dialogIndex]._type, _dialogs[_dialogIndex]._id, _dialogs[_dialogIndex]._pos);

                StartCoroutine(ShowDialog(_dialogs[_dialogIndex]._words));

                if (_dialogs[_dialogIndex]._type == AgentType.Lodger)
                {
                    AudioManager.Instance.LodgerTalk(_dialogs[_dialogIndex]._id, _dialogs[_dialogIndex]._words.Length);
                }
            }
            else
            {
                EndDialog();
            }
        }
        else if (_dialogType == DialogType.Chat)
        {
            GetDialog();
            if (_curChatDialog != null)
            {
                //StartCoroutine(ShowDialog(LuaManager.Instance.GetDialogString(_curDialog.Str)));
                StartCoroutine(ShowDialog(_curChatDialog.Content));

                AudioManager.Instance.LodgerTalk(_dialogLodger, _curChatDialog.Content.Length);
            }
            else
            {
                EndDialog();
            }
        }
    }

    protected void ShowAgent(AgentType type, string id, int pos)
    {
        if (string.IsNullOrEmpty(id))
        {
            if (type == AgentType.Lodger)
                id = _dialogLodger;
            else
                type = AgentType.Zuzhang;
        }

        _nameBg.color = _nameBgColor;
        _dialogAgent._name.color = _nameTxtColor;
        if (type == AgentType.Lodger)
        {
            var cfg = LuaManager.Instance.GetCfgLodger(id);
            if (null != cfg)
            {
                if (cfg.dialogBgColor.a > 0)
                    _nameBg.color = cfg.dialogBgColor;
                if (cfg.dialogTxtColor.a > 0)
                    _dialogAgent._name.color = cfg.dialogTxtColor;
            }
        }

        //if (pos == 2)
        //{
        //    _dialogAgentRight.Show(type, id, true);
        //    Utility.SetActive(_dialogAgentRight.gameObject, true);
        //    Utility.SetActive(_dialogAgentMid.gameObject, false);
        //    Utility.SetActive(_dialogAgentLeft.gameObject, false);

        //    if (_dialogAgentRight._spine != null && type == AgentType.Lodger)
        //    {
        //        var cfglodger = LuaManager.Instance.GetCfgLodger(id);
        //        if (null != cfglodger)
        //        {
        //            _dialogAgentRight._spine.AnimationState.SetAnimation(1, "jichu/shuohua" + id, false);
        //            _dialogAgentRight._spine.AnimationState.GetCurrent(1).Complete -= OnRightSpeakEnd;
        //            _dialogAgentRight._spine.AnimationState.GetCurrent(1).Complete += OnRightSpeakEnd;
        //        }
        //    }

        //    //_dialogAgentRight._agent.color = Color.white;
        //    //if (_dialogAgentLeft.gameObject.activeSelf)
        //    //    _dialogAgentLeft._agent.color = new Color(1, 1, 1, 0.25f);
        //    //if (_dialogAgentMid.gameObject.activeSelf)
        //    //    _dialogAgentMid._agent.color = new Color(1, 1, 1, 0.25f);
        //}
        //else if(pos == 3)
        //{
        //    _dialogAgentMid.Show(type, id, true);
        //    Utility.SetActive(_dialogAgentMid.gameObject, true);
        //    Utility.SetActive(_dialogAgentRight.gameObject, false);
        //    Utility.SetActive(_dialogAgentLeft.gameObject, false);

        //    //_dialogAgentMid._agent.color = Color.white;
        //    //if (_dialogAgentRight.gameObject.activeSelf)
        //    //    _dialogAgentRight._agent.color = new Color(1, 1, 1, 0.25f);
        //    //if (_dialogAgentLeft.gameObject.activeSelf)
        //    //    _dialogAgentLeft._agent.color = new Color(1, 1, 1, 0.25f);
        //}
        //else
        //{
        //    _dialogAgentLeft.Show(type, id, true);
        //    Utility.SetActive(_dialogAgentLeft.gameObject, true);
        //    Utility.SetActive(_dialogAgentMid.gameObject, false);
        //    Utility.SetActive(_dialogAgentRight.gameObject, false);

        //    if (_dialogAgentLeft._spine != null && type == AgentType.Lodger)
        //    {
        //        var cfglodger = LuaManager.Instance.GetCfgLodger(id);
        //        if (null != cfglodger)
        //        {
        //            _dialogAgentLeft._spine.AnimationState.SetAnimation(1, "jichu/shuohua" + id, false);
        //            _dialogAgentLeft._spine.AnimationState.GetCurrent(1).Complete -= OnLeftSpeakEnd;
        //            _dialogAgentLeft._spine.AnimationState.GetCurrent(1).Complete += OnLeftSpeakEnd;
        //        }
        //    }

        //    //_dialogAgentLeft._agent.color = Color.white;
        //    //if (_dialogAgentRight.gameObject.activeSelf)
        //    //    _dialogAgentRight._agent.color = new Color(1, 1, 1, 0.25f);
        //    //if (_dialogAgentMid.gameObject.activeSelf)
        //    //    _dialogAgentMid._agent.color = new Color(1, 1, 1, 0.25f);
        //}

        _dialogAgent.Show(type, id);
    }

    protected Button ShowSelection(int index, string str)
    {
        while (index >= _selections.Count)
        {
            var selection = Instantiate(_selections[0]);
            Utility.SetParent(selection.transform, _selections[0].transform.parent);
            _selections.Add(selection);
        }

        _selections[index].transform.Find("Text").GetComponent<Text>().text = str;
        _selections[index].gameObject.name = index.ToString();
        _selections[index].gameObject.SetActive(true);
        return _selections[index];
    }

    protected void ShowImage()
    {
        //_image.sprite = ResourceManager.LoadByPath<Sprite>(_curDialog.Rsrc);
        //_image.SetNativeSize();
    }

    protected void CountIndex()
    {
        if (_dialogType == DialogType.Input)
            _dialogIndex++;
        else if (_dialogType == DialogType.Chat)
        {
            if (_curChatDialog.next.Length > 0)
            {
                _dialogIndex = _curChatDialog.next[0];
                if (_dialogIndex == 0)
                    _dialogIndex = -1;
            }
            else
            {
                _dialogIndex = -1;
            }
        }
    }

    protected void GetDialog()
    {
        _curChatDialog = null;

        foreach (var item in _chatDialogs)
        {
            if (item.step == _dialogIndex)
            {
                _curChatDialog = item;
                break;
            }
        }
    }

    protected IEnumerator _check;
    
    IEnumerator ShowDialog(string text)
    {
        _dialogTxt.text = "";
        Utility.SetActive(_waitForNext, false);
        _wordsDone = false;

        yield return null; //等一帧输入

        bool finishWords = false;

        _check = CheckInpout(() => { finishWords = true; });
        StartCoroutine(_check);

        text = Utility.GetPatternText(text);

        bool replaceColor = false;
        //if (audio == null)
        {
            var tempText = "";

            for (int i = 0; i < text.Length; i++)
            {
                if (finishWords)
                {
                    _dialogTxt.text = text.Replace("[", "<color=red>").Replace("]", "</color>");
                    yield return null;
                    break;
                }

                char c = text[i];
                if (c == '[')
                    replaceColor = true;
                else if (c == ']')
                    replaceColor = false;
                else if (replaceColor)
                    tempText += "<color=red>" + c + "</color>";
                else
                    tempText += c;

                yield return new WaitForSeconds(_interval);

                _dialogTxt.text = tempText;
                yield return null;
                yield return null;
            }
        }

        _wordsDone = true;

        StopCoroutine(_check);

        yield return new WaitForSeconds(_waitTime);

        Utility.SetActive(_waitForNext, true);
        //ResetTextRect();

        while (!AnyKeyDown)
        {
            yield return null;
        }

        AnyKeyDown = false;
        //Utility.SetActive(_waitForNext, false);

        if (_dialogType == DialogType.Chat)
        {
            if (_curChatDialog.next.Length > 1)
            {
                int counter = 0;
                foreach (var select in _curChatDialog.next)
                {
                    var data = GetChatContent(select);
                    if (null != data)
                    {
                        ShowSelection(counter, data.Select).gameObject.name = select.ToString();
                        counter++;
                    }
                }

                for (int i = counter; i < _selections.Count; i++)
                {
                    Utility.SetActive(_selections[i].gameObject, false);
                }

                Utility.SetActive(_selectionPanel, true);
                yield break;
            }
            else if (_curChatDialog.next.Length == 1)
            {
                var next = GetChatContent(_curChatDialog.next[0]);
                if (null != next && !string.IsNullOrEmpty(next.Select))
                {
                    ShowSelection(0, next.Select).gameObject.name = _curChatDialog.next[0].ToString();
                    for (int i = 1; i < _selections.Count; i++)
                    {
                        Utility.SetActive(_selections[i].gameObject, false);
                    }

                    Utility.SetActive(_selectionPanel, true);
                    yield break;
                }
            }
        }
        else
        {
            if (_dialogs[_dialogIndex]._selection.Count > 0)
            {
                int counter = 0;
                for (int i = 0; i < _dialogs[_dialogIndex]._selection.Count; i++)
                {
                    ShowSelection(counter, _dialogs[_dialogIndex]._selection[i]).gameObject.name = i.ToString();
                    counter++;
                }

                for (int i = counter; i < _selections.Count; i++)
                {
                    Utility.SetActive(_selections[i].gameObject, false);
                }

                Utility.SetActive(_selectionPanel, true);
                yield break;
            }
        }

        CountIndex();

        ShowDialog();
    }

    IEnumerator CheckInpout(System.Action Do)
    {
        while (!AnyKeyDown)
        {
            yield return null;
        }

        AnyKeyDown = false;
        Do();
    }

    protected bool _clicked;
    protected bool AnyKeyDown
    {
        get
        {
            //return Input.anyKeyDown;
            return _clicked;
        }
        set
        {
            _clicked = value;
        }
    }

    protected void OnRightSpeakEnd(Spine.TrackEntry entry)
    {
        //if(!_wordsDone)
        //    _dialogAgentRight._spine.AnimationState.SetAnimation(1, "jichu/shuohua" + _dialogAgentRight.ID, false);
    }

    protected void OnLeftSpeakEnd(Spine.TrackEntry entry)
    {
        //if (!_wordsDone)
        //    _dialogAgentLeft._spine.AnimationState.SetAnimation(1, "jichu/shuohua" + _dialogAgentLeft.ID, false);
    }

    private LuaManager.CfgChatContent GetChatContent(int step)
    {
        foreach (var item in _chatDialogs)
        {
            if (item.step == step)
                return item;
        }

        return null;
    }

    public void OnDialogClick()
    {
        AnyKeyDown = true;
    }

    public void OnImageClick()
    {
        Utility.SetActive(_imagePanel, false);
        CountIndex();
        ShowDialog();
    }

    
    public void OnSelectionClick(GameObject select)
    {
        int index = 0;
        int.TryParse(select.name, out index);

        _selectionIndex = index;

        Utility.SetActive(_selectionPanel, false);

        if (_dialogType == DialogType.Chat)
        {
            var cfg = GetChatContent(index);
            if (!string.IsNullOrEmpty(cfg.mission))
            {
                foreach (var lodger in GameManager.Instance.game.Apartment._lodgers)
                {
                    if (lodger.Value.purposeData != null && lodger.Value.lodgerPurpose != null && lodger.Value.lodgerPurpose.id == cfg.mission)
                    {
                        GameManager.Instance._network.MissionOccurSelect(lodger.Value.purposeData.key);
                        if (!string.IsNullOrEmpty(lodger.Value.lodgerPurpose.selectGameId))
                        {
                            GameManager.Instance._network._missionCurrent = lodger.Value.purposeData.key;
                            var cfgGame = LuaManager.Instance.GetCfgLodgerGame(lodger.Value.lodgerPurpose.selectGameId);
                            if (null != cfgGame)
                            {
                                GameManager.Instance._smallGameLodger = lodger.Value.template;
                                GameManager.Instance.PlaySmallGame(cfgGame.path, false, cfgGame.difficulty, lodger.Value.template);
                            }
                        }
                        break;
                    }
                }
            }
            if (cfg.next?.Length > 0)
                _dialogIndex = cfg.next[0];
            else
            {
                EndDialog();
                return;
            }
        }
        else
        {
            CountIndex();
        }

        ShowDialog();
    }
}
