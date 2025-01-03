using BepInEx;
using BepInEx.Logging;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using System.Text;
using UnityEngine;
using System.Linq;
using System;
using Rich10.Platform.Ddp;

namespace MonopolyCardModifier
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class CardTexPlugin : BaseUnityPlugin
    {
        // 静态实例引用
        public static CardTexPlugin instance;

        // 日志实例
        private static ManualLogSource logger;

        // 配置项
        private ConfigEntry<KeyCode> toggleKey; // 切换窗口
        private ConfigEntry<KeyCode> toggleLogKey; // 切换日志窗口

        // 窗口相关
        private Rect WindowRect = new(50, 50, 500, 650);  // 主窗口位置
        private Rect logWindowRect = new(550, 50, 600, 300); // 日志窗口的实际位置
        private Vector2 scrollViewVector;           // 日志内容滚动位置
        private bool showWindow = false;            // 控制主窗口显示
        private bool showLogWindow = false;         // 控制日志窗口显示


        // 存储日志的列表
        private static List<string> logList = new List<string>(); // 日志列表
        private static StringBuilder logBuilder = new(); // 日志 StringBuilder

        // 标志变量
        private static bool buttonFlag = false;
        private static bool moneyButtonFlag = false;
        private static bool assistantButtonFlag = false;

        // 字符串存储
        public static string cardIds = "7,1,35,40,43,45,42,26";
        public static string moneyValue = "999999";
        public static string playerName; // 新增玩家名称字段
        public static string playerSelf;
        private static StringBuilder stringBuilder = new();

        // 初始设置
        void Start()
        {
            // 设置实例和日志记录器
            instance = this;
            logger = Logger;
            toggleKey = Config.Bind("General", "Toggle Key", KeyCode.F1, "切换窗口的键");
            toggleLogKey = Config.Bind("General", "Toggle Log Key", KeyCode.F2, "切换日志显示的键");
            Harmony.CreateAndPatchAll(typeof(CardTexPlugin));  // 初始化 Harmony 补丁
            playerName = GameEntry.Platform.Instance.GetName();
            LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} Monopoly Card Modifier插件加载成功！");

        }

        void Update()
        {
            // 切换窗口显示/隐藏
            if (Input.GetKeyDown(toggleKey.Value))
            {
                showWindow = !showWindow;
                LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 窗口显示状态切换: {showWindow}");
            }

            // 切换日志窗口显示/隐藏
            if (Input.GetKeyDown(toggleLogKey.Value))
            {
                showLogWindow = !showLogWindow;
                LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 日志窗口显示状态切换: {showLogWindow}");
            }

            // 获取卡牌信息
            if (uLoading.Instance.iShowStep >= 20 && string.IsNullOrEmpty(stringBuilder.ToString()))
            {

                GetCardTex();
            }
            if (string.IsNullOrEmpty(playerSelf))
            {
                playerSelf = DataManager.Instance.GetText(165);
            }
        }

        // OnGUI渲染
        private void OnGUI()
        {
            // 主窗口
            if (showWindow)
            {
                GUI.skin.window.fontSize = 16;
                WindowRect = GUILayout.Window(1, WindowRect, MyWindow, "大富翁11 修改器MOD by栗悦棠");
                logWindowRect = new Rect(WindowRect.x + 500, WindowRect.y, 600, 300); // 使用固定的相对位置

            }

            // 日志窗口
            if (showLogWindow)
            {
                logWindowRect = GUILayout.Window(2, logWindowRect, LogWindow, "日志窗口");
                WindowRect.x = logWindowRect.x - 500; // 使用固定的相对位置
                WindowRect.y = logWindowRect.y;

            }
        }

        // 自定义窗口内容
        public void MyWindow(int windowID)
        {
            // 渲染各个功能
            RenderTitle(); // 渲染标题框
            RenderPlayerName(); // 渲染玩家名称框
            RenderCardInput();       // 渲染卡牌ID输入框
            RenderMoneyInput();      // 渲染玩家金钱输入框
            RenderFreeConsumptionToggle(); // 渲染消费免费开关
            RenderCardInfoDisplay();       // 渲染卡牌信息显示
            RenderLog(); // 渲染日志框
            GUI.DragWindow();
        }
        private void RenderLog()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(showLogWindow ? "隐藏日志" : "显示日志"))
            {
                showLogWindow = !showLogWindow;
            }
            GUILayout.EndHorizontal();
        }
        private void RenderTitle()
        {
            GUI.skin.box.fontSize = 16;
            GUILayout.Label("提示: F1 切换菜单显示\n当前MOD支持了单机、联机、挑战模式修改", GUI.skin.box);
        }

        // 日志窗口拖动
        private void LogWindow(int windowID)
        {

            // 绘制日志内容
            GUILayout.BeginVertical();
            GUILayout.Label("日志内容:", GUI.skin.label);

            // 显示日志
            GUILayout.TextArea(logBuilder.ToString(), GUILayout.Height(200));

            // 清除日志按钮
            if (GUILayout.Button("清除日志"))
            {
                logBuilder.Clear();
                logList.Clear();
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        // 渲染玩家名称输入框
        private void RenderPlayerName()
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"玩家Steam名称: {playerName}");
            GUILayout.Label($"玩家单机名称: {playerSelf}");
            GUILayout.EndHorizontal();
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
                LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 卡牌锁定状态切换: {buttonFlag}");
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
                LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 金钱修改按钮状态切换: {moneyButtonFlag}");
            }
            GUILayout.EndHorizontal();
        }

        // 渲染消费免费开关
        private void RenderFreeConsumptionToggle()
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("免费消费");
            GUILayout.TextField("开启后一切消费都免费");

            if (GUILayout.Button(assistantButtonFlag ? "关闭" : "开启"))
            {
                assistantButtonFlag = !assistantButtonFlag;
                LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 免费消费开关状态切换: {assistantButtonFlag}");
            }
            GUILayout.EndHorizontal();
        }

        // 渲染卡牌信息显示
        private void RenderCardInfoDisplay()
        {
            GUILayout.Label("卡牌信息显示 (ID | 名称 | 说明)", GUI.skin.box);
            scrollViewVector = GUILayout.BeginScrollView(scrollViewVector);
            GUI.skin.box.alignment = TextAnchor.UpperLeft;

            StringBuilder formattedCards = new StringBuilder("ID         名称                   说明\n");

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
            LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 开始获取卡牌信息...");
            var cardTex = Traverse.Create(typeof(DataManager)).Field("baseCard").GetValue<BaseCardMgr>();

            if (cardTex != null)
            {
                foreach (var key in cardTex.card.Keys)
                {
                    stringBuilder.AppendLine($"{key}\t{cardTex.card[key].GetName()}\t{cardTex.card[key].GetNote(0)}");
                }
                LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 卡牌信息获取成功！");
            }
            else
            {
                LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 未能获取到卡牌信息！");
            }
        }
        // 用于将日志写入到显示区域
        public static void LogMessage(string message)
        {
            if (message != null)
            {
                // 输出到调试日志
                logger.LogInfo(message);
                // 添加日志到列表
                logList.Add(message);

                // 如果日志数量超过 20 条，移除最早的日志
                if (logList.Count > 13)
                {
                    logList.RemoveAt(0);
                }

                // 更新 StringBuilder 的内容
                instance.UpdateLogBuilder();

            }
        }
        // 更新 logBuilder 的方法
        private void UpdateLogBuilder()
        {
            logBuilder.Clear();
            foreach (string log in logList)
            {
                logBuilder.AppendLine(log);
            }
        }
        // 例如在某个函数中调用日志记录方法：
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
                // Log the action
                LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 玩家: '{__instance.GetName()}' 的卡片删除操作被拦截，已锁定卡片 ID: {string.Join(", ", cardIdsArray)}");
                return false; // 阻止原有的卡片删除操作
            }
            LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 玩家: '{__instance.GetName()}' 的卡片删除操作未被拦截，继续执行默认操作");
            return true; // 允许默认的卡片删除操作
        }
        // 判断是否是玩家卡片锁定
        private static bool IsPlayerCardLocked(BattleBaseInfo __instance)
        {
            return (__instance.GetName() == playerName || __instance.GetName() == playerSelf) && buttonFlag;
        }

        // 修改金钱时的前置处理，防止银行操作中错误扣除金钱
        [HarmonyPrefix, HarmonyPatch(typeof(BattleBaseInfo), "ChangeMoney2")]
        public static bool BattleBaseInfoChangeMoney2Prefix(BattleBaseInfo __instance, int money, BattleMgr.eMoneyType type, int value = 0, int staffpos = -1, float ttime = 0f)
        {
            if (money == 0 || !__instance.IsActive(false) || !IsPlayerFreeConsumption(__instance) || money >= 0)
            {
                LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 玩家 '{__instance.GetName()}' 未启用了免费消费，（金额：{money}）");
                return true;
            }
            LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 玩家 '{__instance.GetName()}' 启用了免费消费，阻止了扣除金钱操作（金额：{money}）");
            return false; // 阻止金钱扣除
        }
        // 拦截银行金钱操作的前置补丁，防止金钱错误扣除
        [HarmonyPrefix, HarmonyPatch(typeof(BattleBaseInfo), "ChangeBank2")]
        public static bool ChangeBank2Prefix(ref BattleBaseInfo __instance, int money, BattleMgr.eMoneyType type, int value = 0, int staffpos = -1, float ttime = 0f)
        {
            if (money == 0 || !__instance.IsActive(false) || !IsPlayerFreeConsumption(__instance) || money >= 0)
            {
                LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 玩家 '{__instance.GetName()}' 未启用了免费消费，扣除金钱（金额：{money}）");
                return true;
            }
            LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 玩家 '{__instance.GetName()}' 启用了免费消费，阻止了银行扣款（金额：{money}）");
            return false; // 阻止扣除金钱
        }

        // 判断玩家是否开启了免费消费功能
        private static bool IsPlayerFreeConsumption(BattleBaseInfo __instance)
        {
            return (__instance.GetName() == playerName || __instance.GetName() == playerSelf) && assistantButtonFlag;
        }


        // 修改金钱时的后置处理
        [HarmonyPostfix, HarmonyPatch(typeof(BattleBaseInfo), "AddEffectMoney")]
        public static void BattleBaseInfoAddEffectMoneyPostfix(ref BattleBaseInfo __instance)
        {
            if (IsPlayerMoneyModified(__instance) && int.TryParse(moneyValue, out int newMoney))
            {
                __instance.iNowMoney = newMoney;
                LogMessage($"当前时间:{DateTime.Now.ToString("HH:mm:ss")} 玩家 '{__instance.GetName()}' 启用了修改金钱功能，玩家的金钱被修改为: {newMoney}");
                moneyButtonFlag = false; // 取消修改标记
            }
        }

        // 判断是否需要修改玩家金钱
        private static bool IsPlayerMoneyModified(BattleBaseInfo __instance)
        {
            return (__instance.GetName() == playerName || __instance.GetName() == playerSelf) && moneyButtonFlag;
        }

    }
}
