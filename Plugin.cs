using BepInEx;
using BepInEx.Logging;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using System.Text;
using UnityEngine;
using DeJson;
using System;

namespace MonopolyCardModifier;
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class CardTexPlugin : BaseUnityPlugin
{
    ConfigEntry<KeyCode> toggleKey;
    public StringBuilder stringBuilder = new();
    private Rect rect = new(50, 50, 500, 650);
    private Vector2 scrollViewVector;
    bool Cardflag = false;
    bool showWindow = false;
    static bool buttonflag = false;
    static bool Moneybuttonflag = false;
    static bool Assistantbuttonflag = false;
    public static string stringArray = "7,13";
    public static string MoneystringArray = "0";
    void Start()
    {
        toggleKey = Config.Bind("General", "Toggle Key", KeyCode.F1, "切换窗口的键");
        Harmony.CreateAndPatchAll(typeof(CardTexPlugin));
    }
    void Update()
    {
        if (Input.GetKeyDown(toggleKey.Value))
        {
            showWindow = !showWindow;
        }
        if (uLoading.Instance.iShowStep >= 10 && Cardflag == false)
        {
            GetCardTex();
            Cardflag = true;
        }

    }
    private void OnGUI()
    {
        if (!showWindow)
        {
            GUI.skin.window.fontSize = 16;
            rect = GUILayout.Window(1, rect, MyWindow, "大富翁修改器MOD by栗悦棠");
        }
    }
    public void MyWindow(int windowID)
    {
        GUI.skin.label.fontSize = 16;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.skin.textField.fontSize = 16;
        GUILayout.Label("提示:F1切换菜单隐藏还是显示");
        GUILayout.Label("当前已经支持了挑战模式修改。");
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("输入卡牌ID:");
        stringArray = GUILayout.TextField(stringArray);
        if (GUILayout.Button(buttonflag ? "解锁" : "锁定"))
        {
            buttonflag = !buttonflag;
        }
        GUILayout.EndHorizontal();
        // 设置玩家 金钱 限制GUILayout.TextField输入不能超过9个字符
        if (MoneystringArray.Length > 9)
        {
            MoneystringArray = MoneystringArray.Substring(0, 9);
        }
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("玩家金钱:");
        MoneystringArray = GUILayout.TextField(MoneystringArray);
        if (GUILayout.Button(Moneybuttonflag ? "取消" : "修改"))
        {
            Moneybuttonflag = !Moneybuttonflag;
        }
        GUILayout.EndHorizontal();
        // 设置消费 金钱
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("不花钱免费");
        GUILayout.TextField("开启后一切消费都免费");
        if (GUILayout.Button(Assistantbuttonflag ? "关闭" : "开启"))
        {
            Assistantbuttonflag = !Assistantbuttonflag;
        }
        GUILayout.EndHorizontal();
        // 显示 卡牌信息
        GUILayout.Label("卡牌信息显示ID|名称|说明");
        scrollViewVector = GUILayout.BeginScrollView(scrollViewVector);
        GUI.skin.box.alignment = TextAnchor.UpperLeft;
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(stringBuilder.ToString(), GUI.skin.box);
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        GUI.DragWindow();
    }
    public void GetCardTex()
    {
        var cardTex = Traverse.Create(typeof(DataManager)).Field("baseCard").GetValue<BaseCardMgr>();
        if (cardTex != null && Cardflag == false)
        {
            foreach (KeyValuePair<int, BaseCardData> key in cardTex.card)
            {
                stringBuilder.AppendLine($"{key.Key}\t{key.Value.GetName()}\t{key.Value.GetNote(0)}");
            }
        }
    }
    [HarmonyPrefix, HarmonyPatch(typeof(BattleBaseInfo), "DelCard", [typeof(int)])]
    public static bool BattleBaseInfoDelCard(ref BattleBaseInfo __instance)
    {
        // 锁定卡片
        string[] array = stringArray.Split(
        [
                ' ',
                ',',
                '，',
                '|',
                '；',
                ';'
        ]);
        if (__instance.GetName() == "我" && __instance.iOrder == 0 && buttonflag == true)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (int.TryParse(array[i], out int key))
                {
                    __instance.pNowCardBagList[i] = key;
                }
            }
            return false;
        }
        else if (__instance.iOrder == 1 && buttonflag == true)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (int.TryParse(array[i], out int key))
                {
                    __instance.pNowCardBagList[i] = key;
                }
            }
            return false;
        }
        return true;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(BattleBaseInfo), "ChangeMoney2")]
    public static bool BattleBaseInfo_ChangeMoney2(ref BattleBaseInfo __instance, ref int money)
    {
        if (__instance.GetName() == "我" && __instance.iOrder == 0)
        {
            if (Moneybuttonflag == true)
            {
                // 锁定金钱
                if (int.TryParse(MoneystringArray, out int Moneys))
                {
                    __instance.iNowMoney = Moneys;
                }
                Moneybuttonflag = false;
            }
            else if (Assistantbuttonflag == true)
            {
                money = 0;
            }
            return false;
        }
        else if (__instance.iOrder == 1)
        {
            if (Moneybuttonflag == true)
            {
                // 锁定金钱
                if (int.TryParse(MoneystringArray, out int Moneys))
                {
                    __instance.iNowMoney = Moneys;
                }
                Moneybuttonflag = false;
            }
            else if (Assistantbuttonflag == true)
            {
                money = 0;
            }
            return false;
        }
        return true;
    }
}
