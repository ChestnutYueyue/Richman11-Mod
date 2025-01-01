using BepInEx;
using BepInEx.Logging;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using System.Text;
using UnityEngine;
using System.Linq;
using System;

namespace MonopolyCardModifier;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class CardTexPlugin : BaseUnityPlugin
{
    // 配置项
    private ConfigEntry<KeyCode> toggleKey;

    // 窗口相关
    private Rect rect = new(50, 50, 500, 650);
    private Vector2 scrollViewVector;
    private bool showWindow = false;

    // 标志变量
    private static bool buttonFlag = false;
    private static bool moneyButtonFlag = false;
    private static bool assistantButtonFlag = false;

    // 字符串存储
    public static string cardIds = "7,1,35,40,43,45,42,26";
    public static string moneyValue = "999999";
    private StringBuilder stringBuilder = new();

    // 初始设置
    void Start()
    {
        toggleKey = Config.Bind("General", "Toggle Key", KeyCode.F1, "切换窗口的键");
        Harmony.CreateAndPatchAll(typeof(CardTexPlugin));  // 初始化 Harmony 补丁
    }

    void Update()
    {
        // 切换窗口显示/隐藏
        if (Input.GetKeyDown(toggleKey.Value))
        {
            showWindow = !showWindow;
        }

        // 获取卡牌信息
        if (uLoading.Instance.iShowStep >= 10 && string.IsNullOrEmpty(stringBuilder.ToString()))
        {
            GetCardTex();
        }
    }

    // OnGUI渲染
    private void OnGUI()
    {
        if (!showWindow) return;

        GUI.skin.window.fontSize = 16;
        rect = GUILayout.Window(1, rect, MyWindow, "大富翁修改器MOD by栗悦棠");
    }

    // 自定义窗口内容
    public void MyWindow(int windowID)
    {
        GUI.skin.label.fontSize = 16;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.skin.textField.fontSize = 16;

        GUILayout.Label("提示: F1 切换菜单显示\n当前已经支持了挑战模式修改。", GUI.skin.box);

        // 渲染各个功能
        RenderCardInput();
        RenderMoneyInput();
        RenderFreeConsumptionToggle();
        RenderCardInfoDisplay();

        GUI.DragWindow();
    }

    // 渲染卡牌ID输入框
    private void RenderCardInput()
    {
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("输入卡牌ID:");
        cardIds = GUILayout.TextField(cardIds);

        if (GUILayout.Button(buttonFlag ? "解锁" : "锁定"))
        {
            buttonFlag = !buttonFlag;
        }
        GUILayout.EndHorizontal();
    }

    // 渲染玩家金钱输入框
    private void RenderMoneyInput()
    {
        moneyValue = moneyValue.Length > 9 ? moneyValue.Substring(0, 9) : moneyValue;

        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("玩家金钱:");
        moneyValue = GUILayout.TextField(moneyValue);

        if (GUILayout.Button(moneyButtonFlag ? "取消" : "修改"))
        {
            moneyButtonFlag = !moneyButtonFlag;
        }
        GUILayout.EndHorizontal();
    }

    // 渲染消费免费开关
    private void RenderFreeConsumptionToggle()
    {
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("不花钱免费");
        GUILayout.TextField("开启后一切消费都免费");

        if (GUILayout.Button(assistantButtonFlag ? "关闭" : "开启"))
        {
            assistantButtonFlag = !assistantButtonFlag;
        }
        GUILayout.EndHorizontal();
    }

    // 渲染卡牌信息显示
    private void RenderCardInfoDisplay()
    {
        GUILayout.Label("卡牌信息显示 (ID | 名称 | 说明)", GUI.skin.label);
        scrollViewVector = GUILayout.BeginScrollView(scrollViewVector);
        GUI.skin.box.alignment = TextAnchor.UpperLeft;

        StringBuilder formattedCards = new StringBuilder("ID    名称             说明\n");

        foreach (var card in stringBuilder.ToString().Split('\n'))
        {
            var fields = card.Split('\t');
            if (fields.Length == 3)
            {
                formattedCards.AppendLine(
                    $"{fields[0].PadRight(10)}{fields[1].PadRight(20)}{fields[2].PadRight(20)}");
            }
        }

        GUILayout.Label(formattedCards.ToString(), GUI.skin.box);
        GUILayout.EndScrollView();
    }

    // 获取卡牌信息
    public void GetCardTex()
    {
        var cardTex = Traverse.Create(typeof(DataManager)).Field("baseCard").GetValue<BaseCardMgr>();
        if (cardTex != null)
        {
            foreach (var key in cardTex.card.Keys)
            {
                stringBuilder.AppendLine($"{key}\t{cardTex.card[key].GetName()}\t{cardTex.card[key].GetNote(0)}");
            }
        }
    }

    // 锁定卡片功能
    [HarmonyPrefix, HarmonyPatch(typeof(BattleBaseInfo), "DelCard")]
    public static bool BattleBaseInfoDelCard(ref BattleBaseInfo __instance)
    {
        var cardIdsArray = cardIds.Split(new[] { ' ', ',', '，', '|', '；', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Take(9)
                                   .ToArray();

        if (IsPlayerCardLocked(__instance))
        {
            __instance.pNowCardBagList.Clear();
            foreach (var cardId in cardIdsArray)
            {
                if (int.TryParse(cardId, out int key))
                {
                    __instance.pNowCardBagList.Add(key);
                }
            }
            return false; // 阻止原有的卡片删除操作
        }

        return true; // 允许默认的卡片删除操作
    }

    // 判断是否是玩家卡片锁定
    private static bool IsPlayerCardLocked(BattleBaseInfo __instance)
    {
        return __instance.GetName() == "我" && buttonFlag && (__instance.iOrder == 0 || __instance.iOrder == 1);
    }

    // 修改金钱时的前置处理，防止银行操作中错误扣除金钱
    [HarmonyPrefix, HarmonyPatch(typeof(BattleBaseInfo), "ChangeMoney2")]
    public static bool BattleBaseInfoChangeMoney2Prefix(BattleBaseInfo __instance, int money, BattleMgr.eMoneyType type, int value = 0, int staffpos = -1, float ttime = 0f)
    {
        if (money == 0 || !__instance.IsActive(false) || !IsPlayerFreeConsumption(__instance) || money >= 0)
        {
            return true;
        }

        Debug.Log($"玩家 '{__instance.GetName()}' 被禁用了扣除金钱操作（money = {money}）");
        return false; // 阻止金钱扣除
    }

    // 判断玩家是否开启了免费消费功能
    private static bool IsPlayerFreeConsumption(BattleBaseInfo __instance)
    {
        return __instance.GetName() == "我" && assistantButtonFlag && (__instance.iOrder == 0 || __instance.iOrder == 1);
    }

    // 修改金钱时的后置处理
    [HarmonyPostfix, HarmonyPatch(typeof(BattleBaseInfo), "AddEffectMoney")]
    public static void BattleBaseInfoAddEffectMoneyPostfix(ref BattleBaseInfo __instance)
    {
        if (IsPlayerMoneyModified(__instance) && int.TryParse(moneyValue, out int newMoney))
        {
            __instance.iNowMoney = newMoney;
            Debug.Log($"玩家 '{__instance.GetName()}' 的金钱被修改为: {newMoney}");
            moneyButtonFlag = false; // 取消修改标记
        }
        else
        {
            Debug.LogWarning("无效的金钱值，未修改金钱");
        }
    }

    // 判断是否需要修改玩家金钱
    private static bool IsPlayerMoneyModified(BattleBaseInfo __instance)
    {
        return __instance.GetName() == "我" && moneyButtonFlag && (__instance.iOrder == 0 || __instance.iOrder == 1);
    }

    // 拦截银行金钱操作的前置补丁，防止金钱错误扣除
    [HarmonyPrefix, HarmonyPatch(typeof(BattleBaseInfo), "ChangeBank2")]
    public static bool ChangeBank2Prefix(ref BattleBaseInfo __instance, int money, BattleMgr.eMoneyType type, int value = 0, int staffpos = -1, float ttime = 0f)
    {
        if (money == 0 || !__instance.IsActive(false) || !IsPlayerFreeConsumption(__instance) || money >= 0)
        {
            return true;
        }

        Debug.Log($"玩家 '{__instance.GetName()}' 启用了免费消费，阻止了银行扣款（金额：{money}）");
        return false; // 阻止扣除金钱
    }
}
