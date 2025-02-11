using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Weapons;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.Scripting;
using VRageMath;


/// <summary>
/// v1.0
/// База (планета земля) V1.07-02-2025
/// </summary>
namespace BASE_EA
{
    public sealed class Program : MyGridProgram
    {
        // v1.
        static string NameObj = "[BEA-01]";
        public enum room : int
        {
            none = 0,
            assembly = 1,
        };
        public static string[] name_room = { "", "СБОРОЧНЫЙ" };
        public static int[] count_room = { 0, 0 };

        public static float speed_piston_wg = 1.0f;       // один оборот в минуту

        public static Color red = new Color(255, 0, 0);
        public static Color yellow = new Color(255, 255, 0);
        public static Color green = new Color(0, 128, 0);

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static LCD lcd_storage;
        static LCD lcd_debug;
        static LCD lcd_info1;
        static Batterys bats;
        static Upr upr;
        static PistonsBase pst;
        static MotorStator mst;

        static MyStorage storage;

        static Program _scr;

        class Help
        {

        }
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
            private void Off(List<T> list) { foreach (IMyTerminalBlock obj in list) { obj.ApplyAction("OnOff_Off"); } }
            public void Off() { Off(list_obj); }
            private void OffOfTag(List<T> list, string tag) { foreach (IMyTerminalBlock obj in list) { if (obj.CustomName.Contains(tag)) { obj.ApplyAction("OnOff_Off"); } } }
            public void OffOfTag(string tag) { OffOfTag(list_obj, tag); }
            private void On(List<T> list) { foreach (IMyTerminalBlock obj in list) { obj.ApplyAction("OnOff_On"); } }
            public void On() { On(list_obj); }
            public void OnOff(bool on_off) { if (on_off) On(); else Off(); }
            private void OnOfTag(List<T> list, string tag)
            { foreach (IMyTerminalBlock obj in list) { if (obj.CustomName.Contains(tag)) { obj.ApplyAction("OnOff_On"); } } }
            public void OnOfTag(string tag) { OnOfTag(list_obj, tag); }
            public bool Enabled(string tag) { foreach (IMyTerminalBlock obj in list_obj) { if (obj.CustomName.Contains(tag) && !((IMyFunctionalBlock)obj).Enabled) { return false; } } return true; }
            public bool Enabled() { foreach (IMyTerminalBlock obj in list_obj) { if (!((IMyFunctionalBlock)obj).Enabled) { return false; } } return true; }
        }
        public class BaseTerminalBlock<T> where T : class
        {
            public T obj;
            public string CustomName { get { return ((IMyTerminalBlock)this.obj).CustomName; } set { ((IMyTerminalBlock)this.obj).CustomName = value; } }
            public BaseTerminalBlock(string name) { obj = _scr.GridTerminalSystem.GetBlockWithName(name) as T; _scr.Echo("block:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block"))); }
            public BaseTerminalBlock(T myobj) { obj = myobj; _scr.Echo("block:[" + obj.ToString() + "]: " + ((obj != null) ? ("Ок") : ("not Block"))); }
            public Vector3D GetPosition() { return ((IMyEntity)obj).GetPosition(); }
            public void Off() { if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_Off"); }
            public void On() { if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_On"); }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            lcd_info1 = new LCD(NameObj + "-LCD-INFO-O2");
            bats = new Batterys(NameObj);
            pst = new PistonsBase(NameObj, "[p-test]");
            mst = new MotorStator(NameObj + "-MotorStator");
            storage = new MyStorage();
            storage.LoadFromStorage();
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument) { default: break; }
            //count_room[(int)room.none] = 0;// В космосе людей не считаем
            upr.Logic(argument, updateSource);// Логика системы контроля питания
            if (updateSource == UpdateType.Update10)
            {

            }
            StringBuilder values = new StringBuilder();
            values.Append(bats.TextInfo(null));
            lcd_info1.OutText(values);
        }
        public class LCD : BaseTerminalBlock<IMyTextPanel>
        {
            public LCD(string name) : base(name) { if (base.obj != null) { base.obj.SetValue("Content", (Int64)1); } }
            public void OutText(StringBuilder values) { if (base.obj != null) { base.obj.WriteText(values, false); } }
            public void OutText(string text, bool append) { if (base.obj != null) { base.obj.WriteText(text, append); } }
            public StringBuilder GetText() { StringBuilder values = new StringBuilder(); if (base.obj != null) { base.obj.ReadText(values); } return values; }
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
        public class PistonsBase : BaseListTerminalBlock<IMyPistonBase>
        {
            public float? task { get; set; } = null;
            private float tolerance = 0.1f;
            private float multiply_speed = 0.5f;
            public float Position { get { return base.list_obj.Select(b => b.CurrentPosition).Sum(); } }
            public float Velocity { get { return base.list_obj.Select(b => b.Velocity).Sum(); } }
            public float LowestPosition { get { return base.list_obj.Select(b => b.LowestPosition).Sum(); } }
            public float HighestPosition { get { return base.list_obj.Select(b => b.HighestPosition).Sum(); } }
            public PistonsBase(string name_obj, string tag) : base(name_obj)
            {
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    list_obj = list_obj.Where(n => n.CustomName.Contains(tag)).ToList();
                }
                _scr.Echo("Найдено PistonBase:[" + tag + "]: " + list_obj.Count());
            }
            public void Open()
            {
                foreach (IMyPistonBase p in base.list_obj)
                {
                    p.Velocity = speed_piston_wg;
                    p.Extend();
                }
            }
            public void Close()
            {
                foreach (IMyPistonBase p in base.list_obj)
                {
                    p.Velocity = speed_piston_wg;
                    p.Retract();
                }
            }
            public void SetVelocity(float speed)
            {
                foreach (IMyPistonBase p in base.list_obj)
                {
                    p.Velocity = speed / base.list_obj.Count();
                }
            }
            public bool SetPosition(float position)
            {
                task = position;
                if (base.list_obj == null || base.list_obj.Count == 0) return false;
                float speed = 0f;
                double curennt_position = this.Position;
                double difference = (position - curennt_position);
                if (Math.Abs(difference) > tolerance)
                {
                    speed = (float)(difference * this.multiply_speed);
                    this.SetVelocity(speed);
                    return false;
                    //if (curennt_position > (position + tolerance))
                    //{
                    //    speed = -(float)(Math.Abs(curennt_position - (float)position) * multiply_speed);
                    //    this.SetVelocity(speed);
                    //    return false;
                    //}
                    //else if (curennt_position < (position - tolerance))
                    //{
                    //    speed = (float)(Math.Abs((float)position - curennt_position) * multiply_speed);
                    //    this.SetVelocity(speed);
                    //    return false;
                    //}
                    //else
                    //{
                    //    this.SetVelocity(speed);
                    //    return true;
                    //    //this.task_position = null;
                    //}
                }
                else
                {
                    this.SetVelocity(speed);
                    return true;
                }
            }
            public string TextInfo(string name)
            {
                if (base.list_obj == null || base.list_obj.Count == 0) return "";
                StringBuilder values = new StringBuilder();
                values.Append("ПОРШЕНЬ  : " + name + " [" + base.list_obj.Count + "] \n");
                values.Append("НИЗ      : " + Math.Round(this.LowestPosition, 1) + " ВЕРХ: " + Math.Round(this.HighestPosition, 1) + "\n");
                values.Append("ПОЛОЖ    : " + Math.Round(this.Position, 1) + " СКОРОСТЬ : " + Math.Round(this.Velocity, 3) + " ЗАД : " + this.task + "\n");
                return values.ToString();
            }
        }
        public class MotorStator : BaseTerminalBlock<IMyMotorStator>
        {
            public float? task { get; set; } = null;
            private float tolerance = 0.1f;
            private float multiply_speed = 0.1f;

            public double Degrees { get { return this.obj != null ? (this.obj.Angle * 180 / Math.PI) : 0; } }
            public MotorStator(string name_obj) : base(name_obj)
            {

            }
            //public double RadToGradus(float rad)
            //{
            //    return rad * 180 / Math.PI;
            //}

            public bool SetDegrees(float degrees) {
                float speed = 0f;
                double curennt_degrees = this.Degrees;
                double difference = (degrees - curennt_degrees);
                if (Math.Abs(difference) > tolerance)
                {
                    speed = (float)(difference * this.multiply_speed);
                    this.obj.TargetVelocityRPM = speed;
                    return false;
                }
                else
                {
                    this.obj.TargetVelocityRPM = speed;
                    return true;
                }
            }
            public void RotateToGradus(float degr)
            {
                if (this.obj == null) return;
                float speed = 0f;
                // Текущее положение
                double curennt_degr = this.Degrees;
                if (curennt_degr > (degr + tolerance))
                {
                    double dist = curennt_degr - degr;
                    if (Math.Abs(dist) <= 180.1f)
                    {
                        speed = -(float)(Math.Abs(dist) * multiply_speed);
                    }
                    else
                    {
                        speed = (float)(Math.Abs(dist) * multiply_speed);
                    }

                    this.obj.TargetVelocityRPM = speed;
                }
                else if (curennt_degr < (degr - tolerance))
                {
                    double dist = (degr - curennt_degr);
                    if (Math.Abs(dist) <= 180.1f)
                    {
                        speed = (float)(Math.Abs(degr - curennt_degr) * multiply_speed);
                    }
                    else
                    {
                        speed = -(float)(Math.Abs(degr - curennt_degr) * multiply_speed);
                    }

                    this.obj.TargetVelocityRPM = speed;
                }
                else
                {
                    this.obj.TargetVelocityRPM = speed;
                    this.task = null;
                }
            }
            //public double GetCurrentGradus()
            //{
            //    if (this.obj == null) return 0;
            //    return RadToGradus(this.obj.Angle);
            //}
            //public void Logic(string argument, UpdateType updateSource)
            //{
            //    switch (argument)
            //    {
            //        default:
            //            break;
            //    }
            //    if (updateSource == UpdateType.Update10)
            //    {
            //        if (task_degr != null)
            //        {
            //            RotateToGradus((float)task_degr);
            //        }
            //    }
            //}
            public string TextInfo()
            {
                if (this.obj == null) return "";
                StringBuilder values = new StringBuilder();
                values.Append("ШАРНИР : " + this.obj.CustomName + "\n");
                values.Append("БЛОК : " + (this.obj.RotorLock ? ired.ToString() : igreen.ToString()) + " НИЗ: " + Math.Round(this.obj.LowerLimitDeg, 1) + " ВЕРХ: " + Math.Round(this.obj.UpperLimitDeg, 1) + "\n");
                values.Append("УГОЛ : " + Math.Round(this.Degrees, 1) + " СКОРОСТЬ : " + Math.Round(this.obj.TargetVelocityRPM, 3) + " ЗАД : " + this.task + "\n");
                return values.ToString();
            }
        }
        public class MyStorage
        {
            public MyStorage() { }
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                //upr.curent_mode = (Nav.mode)GetValInt("curent_mode", str.ToString());
                //upr.horizont = GetValBool("horizont", str.ToString());
                //upr.FlyHeight = GetValDouble("FlyHeight", str.ToString());
                //upr.ShaftN = GetValInt("ShaftN", str.ToString());
                //upr.DockMatrix = GetValMatrixD("DM", str.ToString());
                //upr.PlanetCenter = GetValVector3D("PC", str.ToString());
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                //values.Append("curent_mode: " + ((int)nav.curent_mode).ToString() + ";\n");
                //values.Append("horizont: " + nav.horizont.ToString() + ";\n");
                //values.Append("FlyHeight: " + Math.Round(nav.FlyHeight, 0) + ";\n");
                //values.Append("ShaftN: " + nav.ShaftN.ToString() + ";\n");
                //values.Append(SetValMatrixD("DM", nav.DockMatrix) + ";\n");
                //values.Append(SetValVector3D("PC", nav.PlanetCenter) + ";\n");
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
        public class Upr
        {
            bool pos_15 = false;
            bool pos_5 = false;
            bool pos_20 = false;
            bool pos_0 = false;
            bool ms_0 = false;
            bool ms_45 = false;
            bool ms_m45 = false;

            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "pos_15": pos_15 = true; break;
                    case "pos_5": pos_5 = true; break;
                    case "pos_20": pos_20 = true; break;
                    case "pos_0": pos_0 = true; break;
                    case "ms_0": ms_0 = true; break;
                    case "ms_45": ms_45 = true; break;
                    case "ms_m45": ms_m45 = true; break;
                    default: break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    if (pos_15 && pst.SetPosition(15)) { pos_15 = false; }
                    if (pos_5 && pst.SetPosition(5)) { pos_5 = false; }
                    if (pos_20 && pst.SetPosition(20)) { pos_20 = false; }
                    if (pos_0 && pst.SetPosition(0)) { pos_0 = false; }
                    if (ms_0 && mst.SetDegrees(0)) { ms_0 = false; }
                    if (ms_45 && mst.SetDegrees(45)) { ms_45 = false; }
                    if (ms_m45 && mst.SetDegrees(-45)) { ms_m45 = false; }
                }
                StringBuilder values = new StringBuilder();
                values.Append(pst.TextInfo("[p-test]"));
                values.Append(pst.TextInfo("---------------"));
                values.Append(mst.TextInfo());
                lcd_debug.OutText(values);
            }
        }
    }
}
