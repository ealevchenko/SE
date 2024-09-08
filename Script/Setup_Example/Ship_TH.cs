using Newtonsoft.Json.Linq;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRage.Network;
using VRage.Noise.Combiners;
using VRage.Scripting;
using VRageMath;
using static MINER_A9.Program;

/* Скрипт тестирования на корабле классов обмен сообщений и навигационного блока
        tag_antena = "[antena]" - на базе пометить программный блок
        tag_nav = "[nav]";    - на корабле пометить кокпит ....
        type_ship = 1; - указать тип коробля
        type_thruster = "H";  - указать тип ьрастеров
 */
namespace Ship_TH
{
    public sealed class Program : MyGridProgram
    {
        string NameObj = "[SHIP-T]";
        static string tag_antena = "[antena]";
        static string tag_nav = "[nav]";
        static int type_ship = 1; // бур
        static string type_thruster = "H";

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static LCD lcd_storage;
        static LCD lcd_info, lcd_debug;
        static LCD lcd_lstr;

        static Batterys bats;
        static Connectors connectors;
        static Navigation nav;
        static MyStorage strg;
        static Program _scr;

        public class PText
        {
            static public string GetPersent(double perse) { return " - " + Math.Round((perse * 100), 1) + "%"; }
            static public string GetScalePersent(double perse, int scale) { string prog = "["; for (int i = 0; i < Math.Round((perse * scale), 0); i++) { prog += "|"; } for (int i = 0; i < scale - Math.Round((perse * scale), 0); i++) { prog += "'"; } prog += "]" + GetPersent(perse); return prog; }
            static public string GetCurrentOfMax(float cur, float max, string units) { return "[ " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]"; }
            static public string GetCurrentOfMinMax(float min, float cur, float max, string units) { return "[ " + Math.Round(min, 1) + units + " / " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]"; }
            static public string GetThrust(float value) { return Math.Round(value / 1000000, 1) + "МН"; }
            static public string GetFarm(float value) { return Math.Round(value, 1) + "L"; }
            static public string GetGPS(string name, Vector3D target) { return "GPS:" + name + ":" + target.GetDim(0) + ":" + target.GetDim(1) + ":" + target.GetDim(2) + ":\n"; }
            static public string GetGPSMatrixD(string name, MatrixD target) { return "MatrixD:" + name + "\n" + "M11:" + target.M11 + "M12:" + target.M12 + "M13:" + target.M13 + "M14:" + target.M14 + ":\n" + "M21:" + target.M21 + "M22:" + target.M22 + "M23:" + target.M23 + "M24:" + target.M24 + ":\n" + "M31:" + target.M31 + "M32:" + target.M32 + "M33:" + target.M33 + "M34:" + target.M34 + ":\n" + "M41:" + target.M41 + "M42:" + target.M42 + "M43:" + target.M43 + "M44:" + target.M44 + ":\n"; }
        }
        public class BaseListTerminalBlock<T> where T : class
        {
            public List<T> list_obj = new List<T>();
            public int Count { get { return list_obj.Count(); } }
            public BaseListTerminalBlock(string name_obj) { _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj)); _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "]: " + list_obj.Count()); }
            public BaseListTerminalBlock(string name_obj, string tag) { _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj)); if (!String.IsNullOrWhiteSpace(tag)) { list_obj = list_obj.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag)).ToList(); } _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "],[" + tag + "]: " + list_obj.Count()); }
            public T Get(long EntityId) { return list_obj.Where(c => ((IMyTerminalBlock)c).EntityId == EntityId).FirstOrDefault(); }
            private void Off(List<T> list) { foreach (IMyTerminalBlock obj in list) { obj.ApplyAction("OnOff_Off"); } }
            public void Off() { Off(list_obj); }
            private void OffOfTag(List<T> list, string tag) { foreach (IMyTerminalBlock obj in list) { if (obj.CustomName.Contains(tag)) { obj.ApplyAction("OnOff_Off"); } } }
            public void OffOfTag(string tag) { OffOfTag(list_obj, tag); }
            private void On(List<T> list) { foreach (IMyTerminalBlock obj in list) { obj.ApplyAction("OnOff_On"); } }
            public void On() { On(list_obj); }
            private void OnOfTag(List<T> list, string tag) { foreach (IMyTerminalBlock obj in list) { if (obj.CustomName.Contains(tag)) { obj.ApplyAction("OnOff_On"); } } }
            public void OnOfTag(string tag) { OnOfTag(list_obj, tag); }
            public bool Enabled(string tag) { foreach (IMyTerminalBlock obj in list_obj) { if (obj.CustomName.Contains(tag) && !((IMyFunctionalBlock)obj).Enabled) { return false; } } return true; }
            public bool Enabled() { foreach (IMyTerminalBlock obj in list_obj) { if (!((IMyFunctionalBlock)obj).Enabled) { return false; } } return true; }
        }
        public class BaseTerminalBlock<T> where T : class
        {
            public T obj;
            public string CustomName { get { return ((IMyTerminalBlock)this.obj).CustomName; } set { ((IMyTerminalBlock)this.obj).CustomName = value; } }
            public BaseTerminalBlock(string name) { obj = _scr.GridTerminalSystem.GetBlockWithName(name) as T; _scr.Echo("block:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block"))); }
            public BaseTerminalBlock(string name_obj, string tag)
            {
                List<T> list_obj = new List<T>();
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "]: " + list_obj.Count());
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    obj = list_obj.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag)).FirstOrDefault();
                }
                _scr.Echo("Выбран " + typeof(T).Name + ((obj != null) ? (((IMyTerminalBlock)obj).CustomName + " - Ок") : ("not Block")));
            }
            public BaseTerminalBlock(T myobj) { obj = myobj; _scr.Echo("block:[" + obj.ToString() + "]: " + ((obj != null) ? ("Ок") : ("not Block"))); }
            public Vector3D GetPosition() { return ((IMyEntity)obj).GetPosition(); }
            public void Off() { if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_Off"); }
            public void On() { if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_On"); }
        }
        public class Batterys : BaseListTerminalBlock<IMyBatteryBlock>
        {
            public float MaxPower { get { return base.list_obj.Select(b => b.MaxStoredPower).Sum(); } }
            public float CurrentPower { get { return base.list_obj.Select(b => b.CurrentStoredPower).Sum(); } }
            public float CurrentPersent { get { return base.list_obj.Select(b => b.CurrentStoredPower).Sum() / base.list_obj.Select(b => b.MaxStoredPower).Sum(); } }
            public float CountCharger { get { return base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Recharge).ToList().Count(); } }
            public float CountAuto { get { return base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Auto).ToList().Count(); } }
            public bool IsCharger { get { return base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Recharge).ToList().Count() > 0; } }
            public bool IsAuto { get { return base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Auto).ToList().Count() > 0; } }
            public Batterys(string name_obj) : base(name_obj) { }
            public Batterys(string name_obj, string tag) : base(name_obj, tag) { }
            public void Charger() { foreach (IMyBatteryBlock obj in base.list_obj) { obj.ChargeMode = ChargeMode.Recharge; } }
            public void Auto() { foreach (IMyBatteryBlock obj in base.list_obj) { obj.ChargeMode = ChargeMode.Auto; } }
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append((!String.IsNullOrWhiteSpace(name) ? name : "БАТАРЕИ") + ": [" + Count + "] [А-" + CountAuto + " З-" + CountCharger + "]" + PText.GetCurrentOfMax(CurrentPower, MaxPower, "MW") + "\n");
                values.Append("|- ЗАР:  " + PText.GetScalePersent(CurrentPower / MaxPower, 20) + "\n");
                return values.ToString();
            }
        }
        public class Connectors : BaseListTerminalBlock<IMyShipConnector>
        {
            public Connectors(string name_obj) : base(name_obj) { }
            public Connectors(string name_obj, string tag) : base(name_obj, tag) { }
            public bool Connected(long EntityId) { IMyShipConnector con = Get(EntityId); return con != null && con.Status == MyShipConnectorStatus.Connected ? true : false; }
            public bool Unconnected(long EntityId) { IMyShipConnector con = Get(EntityId); return con != null && con.Status == MyShipConnectorStatus.Unconnected ? true : false; }
            public bool Connectable(long EntityId) { IMyShipConnector con = Get(EntityId); return con != null && con.Status == MyShipConnectorStatus.Connectable ? true : false; }
            public void Connect(long EntityId) { IMyShipConnector con = base.list_obj.Where(c => c.EntityId == EntityId).FirstOrDefault(); if (con != null) con.Connect(); }
            public void Disconnect(long EntityId) { IMyShipConnector con = base.list_obj.Where(c => c.EntityId == EntityId).FirstOrDefault(); if (con != null) con.Disconnect(); }
            public string GetInfoStatus(long EntityId)
            {
                IMyShipConnector con = Get(EntityId);
                switch (con.Status)
                {
                    case MyShipConnectorStatus.Connected: { return "ПОДКЛЮЧЕН"; }
                    case MyShipConnectorStatus.Connectable: { return "ГОТОВ"; }
                    case MyShipConnectorStatus.Unconnected: { return "НЕПОДКЛЮЧЕН"; }
                    default: { return ""; }
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                foreach (IMyShipConnector con in base.list_obj)
                {
                    values.Append("КОН(" + con.EntityId + "): " + (con.Status == MyShipConnectorStatus.Connected ? igreen.ToString() : (con.Status == MyShipConnectorStatus.Connectable ? iyellow.ToString() : ired.ToString())) + "\n");
                }
                return values.ToString();
            }
        }
        public class MyStorage
        {
            public MyStorage() { }
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                //navigation.curent_programm = (Navigation.programm)GetValInt("curent_programm", str.ToString());
                nav.curent_mode = (Nav.mode)GetValInt("curent_mode", str.ToString());
                //navigation.paused = GetValBool("pause", str.ToString());
                //navigation.EmergencySetpoint = GetValBool("EmergencySetpoint", str.ToString());
                nav.FlyHeight = GetValDouble("FlyHeight", str.ToString());
                nav.DockMatrix = GetValMatrixD("DM", str.ToString());
                nav.PlanetCenter = GetValVector3D("PC", str.ToString());
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                //values.Append("curent_programm: " + ((int)navigation.curent_programm).ToString() + ";\n");
                values.Append("curent_mode: " + ((int)nav.curent_mode).ToString() + ";\n");
                //values.Append("pause: " + navigation.paused.ToString() + ";\n");
                //values.Append("EmergencySetpoint: " + navigation.EmergencySetpoint.ToString() + ";\n");
                values.Append("FlyHeight: " + Math.Round(nav.FlyHeight, 0) + ";\n");
                values.Append(SetValMatrixD("DM", nav.DockMatrix) + ";\n");
                values.Append(SetValVector3D("PC", nav.PlanetCenter) + ";\n");
                lcd_storage.OutText(values);
            }
            private string GetVal(string Key, string str, string val) { string pattern = @"(" + Key + "):([^:^;]+);"; System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern); if (match.Success) { val = match.Groups[2].Value; } return val; }
            public string GetValString(string Key, string str) { return GetVal(Key, str, ""); }
            public double GetValDouble(string Key, string str) { return Convert.ToDouble(GetVal(Key, str, "0")); }
            public int GetValInt(string Key, string str) { return Convert.ToInt32(GetVal(Key, str, "0")); }
            public long GetValInt64(string Key, string str) { return Convert.ToInt64(GetVal(Key, str, "0")); }
            public bool GetValBool(string Key, string str) { return Convert.ToBoolean(GetVal(Key, str, "False")); }
            public MatrixD GetValMatrixD(string Key, string str)
            {
                return new MatrixD(GetValDouble(Key + "11", str.ToString()), GetValDouble(Key + "12", str.ToString()), GetValDouble(Key + "13", str.ToString()), GetValDouble(Key + "14", str.ToString()),
                GetValDouble(Key + "21", str.ToString()), GetValDouble(Key + "22", str.ToString()), GetValDouble(Key + "23", str.ToString()), GetValDouble(Key + "24", str.ToString()),
                GetValDouble(Key + "31", str.ToString()), GetValDouble(Key + "32", str.ToString()), GetValDouble(Key + "33", str.ToString()), GetValDouble(Key + "34", str.ToString()),
                GetValDouble(Key + "41", str.ToString()), GetValDouble(Key + "42", str.ToString()), GetValDouble(Key + "43", str.ToString()), GetValDouble(Key + "44", str.ToString()));
            }
            public Vector3D GetValVector3D(string Key, string str) { return new Vector3D(GetValDouble(Key + "X", str.ToString()), GetValDouble(Key + "Y", str.ToString()), GetValDouble(Key + "Z", str.ToString())); }
            public string SetValVector3D(string Key, Vector3D val) { return val.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", Key + "X").Replace("Y", Key + "Y").Replace("Z", Key + "Z"); }
            public string SetValMatrixD(string Key, MatrixD val) { return val.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", Key); }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_info = new LCD(NameObj + "-LCD-INFO");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            lcd_lstr = new LCD(NameObj + "-LCD-Listener");
            bats = new Batterys(NameObj);
            connectors = new Connectors(NameObj, "[port]");
            //connector_forw = new Connector(NameObj + "-Коннектор [forw]");
            //connector_back = new Connector(NameObj + "-Коннектор [back]");
            //connector_down = new Connector(NameObj + "-Коннектор [down]");
            nav = new Navigation(NameObj);
            strg = new MyStorage();
            strg.LoadFromStorage();
        }
        void Main(string argument, UpdateType updateSource)
        {
            //nav.Logic(argument, updateSource);// обработаем навигацию
        }
        public class LCD : BaseTerminalBlock<IMyTextPanel>
        {
            public LCD(string name) : base(name) { if (base.obj != null) { base.obj.SetValue("Content", (Int64)1); } }
            public void OutText(StringBuilder values) { if (base.obj != null) { base.obj.WriteText(values, false); } }
            public void OutText(string text, bool append) { if (base.obj != null) { base.obj.WriteText(text, append); } }
            public StringBuilder GetText() { StringBuilder values = new StringBuilder(); if (base.obj != null) { base.obj.ReadText(values); } return values; }
        }
        public class Navigation
        {
            public Navigation(string NameObj)
            {

            }
        }
    }
}
