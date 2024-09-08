using ParallelTasks;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Remoting.Messaging;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRage.Noise.Combiners;
using VRageMath;

/// <summary>
/// Транспортный корабль атмосферный (груз-водород)
/// </summary>
namespace BUG_H2_NAV
{
    /// <summary>
    /// Транспортный корабль атмосферный (груз-водород)
    /// </summary>
    public sealed class Program : MyGridProgram
    {
        // v1
        string NameObj = "[BUG_H2_01]";
        static string tag_batterys_duty = "[batterys_duty]";
        static string tag_lightings_warning = "[warning]";
        static string tag_tanks_cargo = "[tanks_cargo]";
        static string tag_tanks_nav = "[tanks_nav]";
        static float GyroMult = 1f;
        static int CriticalMass = 600000;
        static float BaseDistance = 300f;
        static float Conn_Distance_Forw = 20f;
        static float Conn_Distance_Back = 60f;
        static float Pos_Y_Correct = 0.0f;
        static float AlignAccelMult = 0.5f;
        static float ReturnOnCharge = 0.2f;
        static float ReturnOffCharge = 0.9f;
        static float ReturnOnHydrogen = 0.2f;
        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';
        static int clock_main = 0;
        static MyStorage mystorage;
        static LCD lcd_storage;
        static LCD lcd_debug;
        static LCD lcd_nav1;
        static LCD lcd_nav2;
        static LCD lcd_nav3;
        static Batterys bats;
        static Connector connector_forw;
        static Connector connector_back;
        static ReflectorsLight reflectors_light;
        static Lightings lightings;
        static Gyros gyros;
        static Thrusts thrusts;
        static Cockpit cockpit;
        static LandingGear landing_gears;
        static HydrogenTanks hydrogen_tanks_cargo, hydrogen_tanks_nav;
        static Navigation navigation;
        static Program _scr;
        public class PText
        {
            static public string GetPersent(double perse) { return " - " + Math.Round((perse * 100), 1) + "%"; }
            static public string GetScalePersent(double perse, int scale) { string prog = "["; for (int i = 0; i < Math.Round((perse * scale), 0); i++) { prog += "|"; } for (int i = 0; i < scale - Math.Round((perse * scale), 0); i++) { prog += "'"; } prog += "]" + GetPersent(perse); return prog; }
            static public string GetCurrentOfMax(float cur, float max, string units) { return "[ " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]"; }
            static public string GetThrust(float value) { return Math.Round(value / 1000000, 1) + "МН"; }
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
        public class BaseController
        {
            public IMyShipController obj;
            private double current_height = 0;
            public double CurrentHeight { get { return this.current_height; } }
            public Matrix GetCockpitMatrix() { Matrix CockpitMatrix = new MatrixD(); this.obj.Orientation.GetMatrix(out CockpitMatrix); return CockpitMatrix; }
            public BaseController(string name) { obj = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyShipController; _scr.Echo("base_controller:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block"))); }
            public void Dampeners(bool on) { this.obj.DampenersOverride = on; }
            public void OutText(StringBuilder values, int num_lcd) { if (this.obj is IMyTextSurfaceProvider) { IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider; if (num_lcd > ipp.SurfaceCount) return; IMyTextSurface ts = ipp.GetSurface(num_lcd); if (ts != null) { ts.WriteText(values, false); } } }
            public void OutText(string text, bool append, int num_lcd) { if (this.obj is IMyTextSurfaceProvider) { IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider; if (num_lcd > ipp.SurfaceCount) return; IMyTextSurface ts = ipp.GetSurface(num_lcd); if (ts != null) { ts.WriteText(text, append); } } }
            public StringBuilder GetText(int num_lcd) { StringBuilder values = new StringBuilder(); if (this.obj is IMyTextSurfaceProvider) { IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider; if (num_lcd > ipp.SurfaceCount) return null; IMyTextSurface ts = ipp.GetSurface(num_lcd); if (ts != null) { ts.ReadText(values); } } return values; }
            public double GetCurrentHeight() { double cur_h = 0; this.obj.TryGetPlanetElevation(MyPlanetElevation.Surface, out cur_h); return cur_h; }
            public void Logic(string argument, UpdateType updateSource) { switch (argument) { default: break; } if (updateSource == UpdateType.Update10) { current_height = GetCurrentHeight(); } }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            lcd_nav1 = new LCD(NameObj + "-LCD-NAV1");
            lcd_nav2 = new LCD(NameObj + "-LCD-NAV2");
            lcd_nav3 = new LCD(NameObj + "-LCD-NAV3");
            cockpit = new Cockpit(NameObj + "-Cocpit [LCD]");
            bats = new Batterys(NameObj);
            connector_forw = new Connector(NameObj + "-Коннектор [forw] Locked");
            connector_back = new Connector(NameObj + "-Коннектор [back] Locked");
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            gyros = new Gyros(NameObj);
            thrusts = new Thrusts(NameObj);
            landing_gears = new LandingGear(NameObj);
            hydrogen_tanks_cargo = new HydrogenTanks(NameObj, tag_tanks_cargo);
            hydrogen_tanks_nav = new HydrogenTanks(NameObj, tag_tanks_nav);
            lightings = new Lightings(NameObj, tag_lightings_warning);
            lightings.Off();
            navigation = new Navigation();
            mystorage = new MyStorage();
            mystorage.LoadFromStorage();
            navigation.FindPlanetCenter();
        }
        public void Save() { }
        public void Main(string argument, UpdateType updateSource)
        {

            bats.Logic(argument, updateSource);
            //cargo_components.Logic(argument, updateSource);
            navigation.Logic(argument, updateSource);
            switch (argument)
            {
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                StringBuilder values_nav1 = new StringBuilder();
                values_nav1.Append(navigation.TextPointsDock());
                lcd_nav1.OutText(values_nav1);
                cockpit.OutText(values_nav1, 0);

                StringBuilder values_nav3 = new StringBuilder();
                values_nav3.Append(navigation.TextProgramm());
                lcd_nav3.OutText(values_nav3);
                cockpit.OutText(values_nav3, 1);

                StringBuilder values_cockpit0 = new StringBuilder();
                values_cockpit0.Append(bats.TextInfo());
                values_cockpit0.Append(hydrogen_tanks_cargo.TextInfo("H2-CARGO  "));
                values_cockpit0.Append(hydrogen_tanks_nav.TextInfo("H2-THRUSTS"));
                values_cockpit0.Append(connector_forw.TextInfo("К-Forw"));
                values_cockpit0.Append(connector_back.TextInfo("К-Back"));
                values_cockpit0.Append(landing_gears.TextInfo());
                values_cockpit0.Append(navigation.TextCritical());
                cockpit.OutText(values_cockpit0, 2);

                if (clock_main >= 10)
                {
                    clock_main = 0;
                }
                clock_main++;
            }
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
            public int count_work_batterys { get { return list_obj.Where(n => !((IMyTerminalBlock)n).CustomName.Contains(tag_batterys_duty)).Count(); } }
            public Batterys(string name_obj) : base(name_obj) { base.On(); }
            public Batterys(string name_obj, string tag) : base(name_obj, tag) { base.On(); }
            public float MaxPower() { return base.list_obj.Select(b => b.MaxStoredPower).Sum(); }
            public float CurrentPower() { return base.list_obj.Select(b => b.CurrentStoredPower).Sum(); }
            public float CurrentPersent() { return base.list_obj.Select(b => b.CurrentStoredPower).Sum() / base.list_obj.Select(b => b.MaxStoredPower).Sum(); }
            public int CountCharger() { return base.list_obj.Select(b => b.ChargeMode == ChargeMode.Recharge).Count(); }
            public int CountAuto() { return base.list_obj.Select(b => b.ChargeMode == ChargeMode.Auto).Count(); }
            public bool IsCharger() { int count_charger = CountCharger(); return count_work_batterys > 0 && count_charger > 0 && count_work_batterys == count_charger ? true : false; }
            public bool IsAuto() { int count_auto = CountAuto(); return Count > 0 && count_auto > 0 && Count == count_auto ? true : false; }
            public void Charger() { foreach (IMyBatteryBlock obj in base.list_obj) { if (!obj.CustomName.Contains(tag_batterys_duty)) { obj.ChargeMode = ChargeMode.Recharge; } } }
            public void Auto() { foreach (IMyBatteryBlock obj in base.list_obj) { obj.ChargeMode = ChargeMode.Auto; } }
            public void Logic(string argument, UpdateType updateSource) { switch (argument) { default: break; } if (updateSource == UpdateType.Update10) { } }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("БАТАРЕЯ: [" + Count + "] [А-" + CountAuto() + " З-" + CountCharger() + "]" + PText.GetCurrentOfMax(CurrentPower(), MaxPower(), "MW") + "\n");
                values.Append("|- ЗАР:  " + PText.GetScalePersent(CurrentPower() / MaxPower(), 20) + "\n");
                return values.ToString();
            }
        }
        public class Connector : BaseTerminalBlock<IMyShipConnector>
        {
            public MyShipConnectorStatus Status { get { return base.obj.Status; } }
            public bool Connected { get { return base.obj.Status == MyShipConnectorStatus.Connected ? true : false; } }
            public bool Unconnected { get { return base.obj.Status == MyShipConnectorStatus.Unconnected ? true : false; } }
            public bool Connectable { get { return base.obj.Status == MyShipConnectorStatus.Connectable ? true : false; } }
            public Connector(string name) : base(name) { if (base.obj != null) { } }
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append((name != null ? name : "КОННЕКТОР") + " : " + (Connected ? igreen.ToString() : (Connectable ? iyellow.ToString() : ired.ToString())));
                return values.ToString();
            }
            public string TextStatus()
            {
                StringBuilder values = new StringBuilder();
                values.Append(Connected ? igreen.ToString() : (Connectable ? iyellow.ToString() : ired.ToString()));
                return values.ToString();
            }
            public long? getEntityIdRemoteConnector() { List<IMyShipConnector> list_conn = new List<IMyShipConnector>(); _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn); foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList()) { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 3) return conn.EntityId; } return null; }
            public IMyShipConnector getRemoteConnector() { List<IMyShipConnector> list_conn = new List<IMyShipConnector>(); _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn); foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList()) { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 3) return conn; } return null; }
        }
        public class ReflectorsLight : BaseListTerminalBlock<IMyReflectorLight>
        {
            public ReflectorsLight(string name_obj) : base(name_obj) { }
            public ReflectorsLight(string name_obj, string tag) : base(name_obj, tag) { }
        }
        public class Lightings : BaseListTerminalBlock<IMyInteriorLight> { public Lightings(string name_obj, string tag) : base(name_obj) { if (!String.IsNullOrWhiteSpace(tag)) { list_obj = list_obj.Where(n => n.CustomName.Contains(tag)).ToList(); } _scr.Echo("Найдено Lighting:[" + tag + "]: " + list_obj.Count()); } }
        public class Gyros : BaseListTerminalBlock<IMyGyro>
        {
            public Gyros(string name_obj) : base(name_obj) { }
            public Gyros(string name_obj, string tag) : base(name_obj, tag) { }
            public void SetOverride(bool OverrideOnOff, Vector3 settings, float Power = 1) { foreach (IMyGyro gyro in base.list_obj) { if ((!gyro.GyroOverride) && OverrideOnOff) gyro.ApplyAction("Override"); gyro.GyroPower = Power; gyro.Yaw = settings.GetDim(0); gyro.Pitch = settings.GetDim(1); gyro.Roll = settings.GetDim(2); } }
            public void SetOverride(bool OverrideOnOff = true, float OverrideValue = 0, float Power = 1) { foreach (IMyGyro gyro in base.list_obj) { if (((!gyro.GyroOverride) && OverrideOnOff) || ((gyro.GyroOverride) && !OverrideOnOff)) gyro.ApplyAction("Override"); gyro.GyroPower = Power; gyro.Yaw = OverrideValue; gyro.Pitch = OverrideValue; gyro.Roll = OverrideValue; } }
            public string TextDebug() { StringBuilder values = new StringBuilder(); values.Append("Yaw :" + base.list_obj.Select(g => g.Yaw).Average() + "\n"); values.Append("Pitch :" + base.list_obj.Select(g => g.Pitch).Average() + "\n"); values.Append("Roll :" + base.list_obj.Select(g => g.Roll).Average() + "\n"); return values.ToString(); }
        }
        public class Thrusts : BaseListTerminalBlock<IMyThrust>
        {
            private BaseController remote_control;
            public float Value;
            public string axis;
            //------------------------------------------------
            public List<IMyThrust> UpThrusters = new List<IMyThrust>();
            public List<IMyThrust> DownThrusters = new List<IMyThrust>();
            public List<IMyThrust> LeftThrusters = new List<IMyThrust>();
            public List<IMyThrust> RightThrusters = new List<IMyThrust>();
            public List<IMyThrust> ForwardThrusters = new List<IMyThrust>();
            public List<IMyThrust> BackwardThrusters = new List<IMyThrust>();
            public double UpThrMax { get { return UpThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double DownThrMax { get { return DownThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double LeftThrMax { get { return LeftThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double RightThrMax { get { return RightThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double ForwardThrMax { get { return ForwardThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double BackwardThrMax { get { return BackwardThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public Thrusts(string name_obj) : base(name_obj)
            {
            }
            public Thrusts(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public void InitThrusts(BaseController remote_control)
            {
                this.remote_control = remote_control;
                MatrixD OrientationCocpit = this.remote_control.GetCockpitMatrix();
                // Список трастеров
                UpThrusters.Clear();
                DownThrusters.Clear();
                LeftThrusters.Clear();
                RightThrusters.Clear();
                ForwardThrusters.Clear();
                BackwardThrusters.Clear();
                // Орентация трастеров
                Matrix ThrusterMatrix = new MatrixD();
                foreach (IMyThrust thrust in this.list_obj)
                {
                    thrust.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == OrientationCocpit.Up)
                    {
                        UpThrusters.Add(thrust);
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Down)
                    {
                        DownThrusters.Add(thrust);
                    }
                    //X
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Left)
                    {
                        LeftThrusters.Add(thrust);
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Right)
                    {
                        RightThrusters.Add(thrust);
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Forward)
                    {
                        ForwardThrusters.Add(thrust);
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Backward)
                    {
                        BackwardThrusters.Add(thrust);
                    }
                }
            }
            public void ClearThrustOverridePersent()
            {
                SetOverridePercent(UpThrusters, 0f);
                SetOverridePercent(DownThrusters, 0f);
                SetOverridePercent(LeftThrusters, 0f);
                SetOverridePercent(RightThrusters, 0f);
                SetOverridePercent(ForwardThrusters, 0f);
                SetOverridePercent(BackwardThrusters, 0f);
            }
            public void SetOverridePercent(List<IMyThrust> Thrusts, float persent)
            {
                foreach (IMyThrust tr in Thrusts)
                {
                    tr.ThrustOverridePercentage = persent;
                }
            }
            public void SetOverridePercent(string axis, float persentValue)
            {
                if (axis == "U")
                {
                    SetOverridePercent(DownThrusters, persentValue);
                }
                else if (axis == "D")
                {
                    SetOverridePercent(UpThrusters, persentValue);
                }
                else if (axis == "L")
                {
                    SetOverridePercent(RightThrusters, persentValue);
                }
                else if (axis == "R")
                {
                    SetOverridePercent(LeftThrusters, persentValue);
                }
                else if (axis == "F")
                {
                    SetOverridePercent(BackwardThrusters, persentValue);
                }
                else if (axis == "B")
                {
                    SetOverridePercent(ForwardThrusters, persentValue);
                }
            }
            public void SetOverrideN(string axis, float OverrideValue)
            {
                double MaxThrust = 0;
                Value = 0;
                this.axis = axis;
                if (axis == "D") { MaxThrust = UpThrMax; SetOverridePercent("U", 0f); }
                else if (axis == "U") { MaxThrust = DownThrMax; SetOverridePercent("D", 0f); }
                else if (axis == "F") { MaxThrust = BackwardThrMax; SetOverridePercent("B", 0f); }
                else if (axis == "B") { MaxThrust = ForwardThrMax; SetOverridePercent("F", 0f); }
                else if (axis == "R") { MaxThrust = LeftThrMax; SetOverridePercent("L", 0f); }
                else if (axis == "L") { MaxThrust = RightThrMax; SetOverridePercent("R", 0f); }
                if (OverrideValue == 0)
                {
                    Value = 0;
                }
                else
                {
                    Value = (float)Math.Max(OverrideValue / MaxThrust, 0.1f);
                }
                SetOverridePercent(axis, Value);
            }
            public void SetOverrideAccel(string axis, float OverrideValue)
            {
                switch (axis)
                {
                    case "U":
                        if (OverrideValue < 0)
                        {
                            axis = "D";
                            OverrideValue = -OverrideValue;
                        }
                        else
                        {
                            OverrideValue += (float)this.remote_control.obj.GetNaturalGravity().Length();
                        }
                        break;
                    case "D":
                        if (OverrideValue < 0)
                        {
                            axis = "U";
                            OverrideValue = -OverrideValue;
                        }
                        else
                        {
                            //OverrideValue -= (float)this.remote_control.obj.GetNaturalGravity().Length();
                            OverrideValue += (float)this.remote_control.obj.GetNaturalGravity().Length();
                        }
                        break;
                    case "L":
                        if (OverrideValue < 0)
                        {
                            axis = "R";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "R":
                        if (OverrideValue < 0)
                        {
                            axis = "L";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "F":
                        if (OverrideValue < 0)
                        {
                            axis = "B";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "B":
                        if (OverrideValue < 0)
                        {
                            axis = "F";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                }
                SetOverrideN(axis, OverrideValue * this.remote_control.obj.CalculateShipMass().PhysicalMass);
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("PhysicalMass : " + Math.Round(this.remote_control.obj.CalculateShipMass().PhysicalMass) + "\n");
                values.Append("TotalMass : " + Math.Round(this.remote_control.obj.CalculateShipMass().TotalMass) + "\n");
                values.Append("Grav         : " + Math.Round(this.remote_control.obj.GetNaturalGravity().Length()) + "\n");
                values.Append("axis         : " + axis + " , Value : " + Value + "\n");
                values.Append("------------------------------------------\n");
                values.Append("UP MAX       : " + PText.GetThrust((float)UpThrMax) + "\n");
                values.Append("DOWN MAX     : " + PText.GetThrust((float)DownThrMax) + "\n");
                values.Append("Forward MAX  : " + PText.GetThrust((float)ForwardThrMax) + "\n");
                values.Append("Backward MAX : " + PText.GetThrust((float)BackwardThrMax) + "\n");
                values.Append("Left MAX     : " + PText.GetThrust((float)LeftThrMax) + "\n");
                values.Append("Right MAX    : " + PText.GetThrust((float)RightThrMax) + "\n");
                return values.ToString();
            }
        }
        public class Cockpit : BaseController { public Cockpit(string name) : base(name) { } }
        public class LandingGear : BaseListTerminalBlock<IMyLandingGear>
        {
            public LandingGear(string name_obj) : base(name_obj)
            {
            }
            public LandingGear(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public bool IsLocked() { foreach (IMyLandingGear obj in base.list_obj) { if (obj.IsLocked) return true; } return false; }
            public void Lock() { foreach (IMyLandingGear obj in base.list_obj) { obj.Lock(); } }
            public void Unlock() { foreach (IMyLandingGear obj in base.list_obj) { obj.Unlock(); } }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("ШАССИ: " + (IsLocked() ? igreen.ToString() : ired.ToString()) + "\n");
                return values.ToString();
            }
        }
        public class HydrogenTanks : BaseListTerminalBlock<IMyGasTank>
        {
            public HydrogenTanks(string name_obj) : base(name_obj) { AutoRefillBottles(true); }
            public HydrogenTanks(string name_obj, string tag) : base(name_obj, tag) { AutoRefillBottles(true); }
            public float MaxCapacity() { return base.list_obj.Select(b => b.Capacity).Sum(); }
            public double AverageFilledRatio { get { return base.list_obj.Average(t => t.FilledRatio); } }
            public double CountAutoRefillBottles { get { return base.list_obj.Count(t => t.AutoRefillBottles); } }
            public double CountStockpile { get { return base.list_obj.Count(t => t.Stockpile); } }
            public double Capacity { get { return base.list_obj.Sum(t => t.Capacity); } }
            public void AutoRefillBottles(bool on) { foreach (IMyGasTank obj in base.list_obj) { obj.AutoRefillBottles = on; } }
            public void Stockpile(bool on) { foreach (IMyGasTank obj in base.list_obj) { obj.Stockpile = on; } }
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append((name != null ? name : "БАКИ") + " : [" + base.list_obj.Count() + "] [А-" + CountAutoRefillBottles + " З-" + CountStockpile + "]" + PText.GetCurrentOfMax((float)(Capacity * AverageFilledRatio) / 1000000, (float)Capacity / 1000000, "МЛ") + "\n");
                values.Append("|- ЗАП:  " + PText.GetScalePersent(AverageFilledRatio, 20) + "\n");
                return values.ToString();
            }
        }
        public class Navigation
        {
            public int clock = 0;
            public bool m_forw { get; set; } = false;       // Направление расстыковки-стыковка
            public bool m_back { get; set; } = false;       // Направление расстыковки-стыковка
            public bool m_up_gears { get; set; } = false;   // Направление расстыковки-стыковка
            public bool gravity { get; set; } = false;
            public bool horizont { get; private set; } = false;
            public Vector3D? TackVector { get; set; } = null;
            public enum programm : int { none = 0, fly_bp_bp = 1, carriage = 2, };
            public static string[] name_programm = { "", "Б:ПЛАНЕТА->Б:ПЛАНЕТА", "ПЕРЕВОЗКА ГРУЗА" };
            public programm curent_programm = programm.none;
            public enum mode : int
            {
                none = 0,
                base_operation = 1,
                un_dock = 2,
                to_base = 3,
                dock = 4,
            };
            public static string[] name_mode = { "", "БАЗА", "РАСТЫКОВКА", "К БАЗЕ", "СТЫКОВКА" };
            public mode curent_mode = mode.none;
            //------------------------------
            public Vector3D MyPos { get; private set; }
            public Vector3D MyPrevPos { get; private set; }
            public Vector3D VelocityVector { get; private set; }
            public Vector3D UpVelocityVector { get; private set; }
            public Vector3D ForwVelocityVector { get; private set; }
            public Vector3D LeftVelocityVector { get; private set; }
            public Vector3D GravVector { get; private set; }
            public float PhysicalMass { get; private set; } // Физическая масса
            public float TotalMass { get; private set; } // Физическая масса
            public MatrixD WMCocpit { get; private set; } //
            public MatrixD OrientationCocpit { get; private set; } //
            public float XMaxA { get; private set; }
            public float YMaxA { get; private set; }
            public float ZMaxA { get; private set; }
            //---------------------------------------------------
            public float Distance { get; private set; }

            public Vector3D PlanetCenter = new Vector3D(0.50, 0.50, 0.50);
            public class PointDock
            {
                public bool back { get; set; } = false;
                public MatrixD DockMatrix { get; set; }
                public Vector3D BaseDockPoint { get; set; } = new Vector3D(0, 0, -BaseDistance);
                public Vector3D ConnectorPoint { get; set; } = new Vector3D(0, 0, Conn_Distance_Forw);
                public Vector3D PlanetCenter { get; set; } = new Vector3D(0.50, 0.50, 0.50);
                public double FlyHeight { get; set; } = 0;
                public long EntityId { get; set; } = 0;
                public string Name { get; set; } = null;
                public bool Home { get; set; } = false;
                public bool Loading { get; set; } = false;
            }
            public PointDock[] PointsDock = new PointDock[2];
            public int CurrDockPoint { get; set; } = 0;
            public bool CriticalMassReached { get; private set; }// Признак критической массы
            public bool EmergencyReturn = false;
            public bool go_home = false; // вернутся домой и остатся
            public bool paused = false;
            public string Message { get; set; } = "";
            public Navigation()
            {
                thrusts.InitThrusts(cockpit);
                for (int i = 0; i < 2; i++) { PointsDock[i] = new PointDock(); }
            }
            public MatrixD GetNormTransMatrixFromMyPos()
            {
                MatrixD mRot;
                Vector3D V3Dcenter = MyPos;
                Vector3D V3Dup = WMCocpit.Up;
                if (gravity) V3Dup = -Vector3D.Normalize(GravVector);
                Vector3D V3Dleft = Vector3D.Normalize(Vector3D.Reject(WMCocpit.Left, V3Dup));
                Vector3D V3Dfow = Vector3D.Normalize(Vector3D.Cross(V3Dleft, V3Dup));
                mRot = new MatrixD(V3Dleft.GetDim(0), V3Dleft.GetDim(1), V3Dleft.GetDim(2), 0, V3Dup.GetDim(0), V3Dup.GetDim(1), V3Dup.GetDim(2), 0, V3Dfow.GetDim(0), V3Dfow.GetDim(1), V3Dfow.GetDim(2), 0, 0, 0, 0, 1);
                mRot = MatrixD.Invert(mRot);
                return new MatrixD(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -V3Dcenter.GetDim(0), -V3Dcenter.GetDim(1), -V3Dcenter.GetDim(2), 1) * mRot;
            }
            public int? GetCurrConnectorDock(Connector connector)
            {
                Message = "";
                if (connector.Connected)
                {
                    IMyShipConnector con_base = connector.getRemoteConnector();
                    if (con_base != null)
                    {
                        int index = Array.FindIndex(PointsDock, element => element.EntityId == con_base.EntityId);
                        return index;
                    }
                    else { Message = String.Format("Коннектор базы не определен!"); }
                }
                return null;
            }
            public void SetDockMatrix(Connector connector, bool back)
            {
                if (connector.Connected)
                {
                    IMyShipConnector con_base = connector.getRemoteConnector();
                    if (con_base != null)
                    {
                        int index = Array.FindIndex(PointsDock, element => element.EntityId == con_base.EntityId);
                        if (index >= 0) { CurrDockPoint = index; }
                        PointsDock[CurrDockPoint].back = back;
                        PointsDock[CurrDockPoint].DockMatrix = GetNormTransMatrixFromMyPos();
                        PointsDock[CurrDockPoint].BaseDockPoint = new Vector3D(0, 0, back ? BaseDistance : -BaseDistance);
                        PointsDock[CurrDockPoint].ConnectorPoint = new Vector3D(0, 0, back ? -Conn_Distance_Back : Conn_Distance_Forw);
                        PointsDock[CurrDockPoint].EntityId = con_base.EntityId;
                        PointsDock[CurrDockPoint].Name = con_base.DisplayNameText;
                        mystorage.SaveToStorage();
                    }
                    else
                    {
                        Message = String.Format("Коннектор базы не определен!");
                    }
                }
            }
            public void SetFlyHeight()
            {
                cockpit.obj.TryGetPlanetPosition(out PlanetCenter);
                PointsDock[CurrDockPoint].PlanetCenter = PlanetCenter;
                PointsDock[CurrDockPoint].FlyHeight = (MyPos - PlanetCenter).Length();
                mystorage.SaveToStorage();
            }
            public void SetHomeDock()
            {
                PointsDock[CurrDockPoint].Home = true;
                mystorage.SaveToStorage();
            }
            public void SetLoadingDock()
            {
                PointsDock[CurrDockPoint].Loading = true;
                mystorage.SaveToStorage();
            }
            public void FindPlanetCenter()
            {
                if (cockpit.obj.TryGetPlanetPosition(out PlanetCenter)) { mystorage.SaveToStorage(); }
            }
            //---------------------------------------------
            public Vector3D GetNavAngles(Vector3D Target, MatrixD InvMatrix)
            {
                Vector3D V3Dcenter = cockpit.obj.GetPosition();
                Vector3D V3Dfow = cockpit.obj.WorldMatrix.Forward + V3Dcenter;
                Vector3D V3Dup = cockpit.obj.WorldMatrix.Up + V3Dcenter;
                Vector3D V3Dleft = cockpit.obj.WorldMatrix.Left + V3Dcenter;
                V3Dcenter = Vector3D.Transform(V3Dcenter, InvMatrix);
                V3Dfow = (Vector3D.Transform(V3Dfow, InvMatrix)) - V3Dcenter;
                V3Dup = (Vector3D.Transform(V3Dup, InvMatrix)) - V3Dcenter;
                V3Dleft = (Vector3D.Transform(V3Dleft, InvMatrix)) - V3Dcenter;

                Vector3D GravNorm = Vector3D.Normalize(new Vector3D(-0, -1, -0));
                Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter);
                if (gravity)
                {
                    GravNorm = Vector3D.Normalize(GravVector);
                    GravNorm = Vector3D.Normalize(Vector3D.Transform(GravNorm + cockpit.obj.GetPosition(), InvMatrix) - V3Dcenter);
                }
                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(V3Dfow);
                double gL = GravNorm.Dot(V3Dleft);
                double gU = GravNorm.Dot(V3Dup);
                //Получаем сигналы по тангажу и крены операцией atan2
                double TargetRoll = (float)Math.Atan2(gL, -gU); // крен
                double TargetPitch = -(float)Math.Atan2(gF, -gU); // тангаж
                //Рысканием прицеливаемся на точку Target.
                double tF = TargetNorm.Dot(V3Dfow);
                double tL = TargetNorm.Dot(V3Dleft);
                double TargetYaw = -(float)Math.Atan2(tL, tF);
                if (double.IsNaN(TargetYaw)) TargetYaw = 0;
                if (double.IsNaN(TargetPitch)) TargetPitch = 0;
                if (double.IsNaN(TargetRoll)) TargetRoll = 0;
                return new Vector3D(TargetYaw, TargetPitch, TargetRoll);
            }
            public Vector3D GetNavAngles(Vector3D? Vector)
            {
                Vector3D GravNorm = Vector3D.Normalize(GravVector);
                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(cockpit.obj.WorldMatrix.Forward);
                double gL = GravNorm.Dot(cockpit.obj.WorldMatrix.Left);
                double gU = GravNorm.Dot(cockpit.obj.WorldMatrix.Up);
                //Получаем сигналы по тангажу и крены операцией atan2
                double TargetRoll = (float)Math.Atan2(gL, -gU); // крен
                double TargetPitch = -(float)Math.Atan2(gF, -gU); // тангаж
                double TargetYaw = 0;
                if (Vector != null)
                {
                    Vector3D TargetNorm = Vector3D.Normalize((Vector3D)Vector);
                    //Рысканием прицеливаемся на точку Target.
                    double tF = TargetNorm.Dot(cockpit.obj.WorldMatrix.Forward);
                    double tL = TargetNorm.Dot(cockpit.obj.WorldMatrix.Left);
                    TargetYaw = -(float)Math.Atan2(tL, tF);
                }
                if (double.IsNaN(TargetYaw)) TargetYaw = 0;
                if (double.IsNaN(TargetPitch)) TargetPitch = 0;
                if (double.IsNaN(TargetRoll)) TargetRoll = 0;
                return new Vector3D(TargetYaw, TargetPitch, TargetRoll);
            }
            //----------------------------------------------
            public void Horizon()
            {
                Vector3D gyrAng = GetNavAngles(TackVector);
                if (TackVector == null)
                {
                    if (cockpit.obj.IsUnderControl)
                    {
                        gyrAng.SetDim(0, cockpit.obj.RotationIndicator.Y);
                    }
                }
                gyros.SetOverride(true, gyrAng * GyroMult, 1);
            }
            //-----------------------------------------------
            public void Fly_BP_BP()
            {
                Message = "";
                if (IsCorrectBasePoints(CurrDockPoint) && IsCorrectBasePoints(CurrDockPoint == 0 ? 1 : 0))
                {
                    if (curent_mode == mode.none) { curent_mode = mode.un_dock; mystorage.SaveToStorage(); }
                    if (curent_mode == mode.un_dock && UnDock()) { curent_mode = mode.to_base; mystorage.SaveToStorage(); }
                    if (curent_mode == mode.to_base && ToBase()) { curent_mode = mode.dock; mystorage.SaveToStorage(); }
                    if (curent_mode == mode.dock && Dock()) { Clear(); curent_programm = programm.none; mystorage.SaveToStorage(); }
                }
                else
                {
                    Message = String.Format("Точка {0}->{1} - неопределена!", CurrDockPoint, CurrDockPoint == 0 ? 1 : 0);
                    curent_programm = programm.none;
                    mystorage.SaveToStorage();
                }
            }
            public void Carriage()
            {
                Message = "";
                if (IsCorrectBasePoints(CurrDockPoint) && IsCorrectBasePoints(CurrDockPoint == 0 ? 1 : 0))
                {
                    if (curent_mode == mode.none)
                    {
                        go_home = false;
                        if (connector_forw.Connected)
                        {
                            curent_mode = mode.base_operation;
                            mystorage.SaveToStorage();
                        }
                        else
                        {
                            if (landing_gears.IsLocked())
                            {
                                curent_mode = mode.un_dock;
                                mystorage.SaveToStorage();
                            }
                            else
                            {
                                // Проверка на вес и отправка на нужную базу
                                curent_mode = mode.to_base;
                            }
                        }
                    }
                    else
                    {
                        if (go_home && (PointsDock[CurrDockPoint].Home || PointsDock[(CurrDockPoint == 0 ? 1 : 0)].Home))
                        {
                            if (!PointsDock[(CurrDockPoint == 0 ? 1 : 0)].Home)
                            {
                                if (curent_mode == mode.to_base || curent_mode == mode.dock || curent_mode == mode.un_dock)
                                {
                                    CurrDockPoint = (CurrDockPoint == 0 ? 1 : 0);
                                    curent_mode = mode.to_base;
                                    mystorage.SaveToStorage();
                                }
                            }
                            if (!PointsDock[CurrDockPoint].Home)
                            {
                                if (curent_mode == mode.base_operation)
                                {
                                    curent_mode = mode.un_dock;
                                    mystorage.SaveToStorage();
                                }
                            }
                        }
                        else
                        {
                            if (curent_mode == mode.un_dock && UnDock()) { curent_mode = mode.to_base; mystorage.SaveToStorage(); }
                            if (curent_mode == mode.to_base && ToBase()) { curent_mode = mode.dock; mystorage.SaveToStorage(); }
                            if (curent_mode == mode.dock && Dock()) { curent_mode = mode.base_operation; mystorage.SaveToStorage(); }
                            if (curent_mode == mode.base_operation && BaseOperation()) { curent_mode = mode.un_dock; mystorage.SaveToStorage(); }
                        }
                    }

                }
                else
                {
                    Message = String.Format("Точка {0}->{1} - неопределена!", CurrDockPoint, CurrDockPoint == 0 ? 1 : 0);
                    curent_programm = programm.none;
                    mystorage.SaveToStorage();
                }
            }
            //-----------------------------------------------
            public void UpdateCalc()
            {
                MyPrevPos = MyPos;
                MyPos = cockpit.obj.GetPosition();
                GravVector = cockpit.obj.GetNaturalGravity();
                gravity = GravVector.LengthSquared() > 0.2f;
                PhysicalMass = cockpit.obj.CalculateShipMass().PhysicalMass;
                TotalMass = cockpit.obj.CalculateShipMass().TotalMass;
                WMCocpit = cockpit.obj.WorldMatrix;
                VelocityVector = (MyPos - MyPrevPos) * 6;
                UpVelocityVector = WMCocpit.Up * Vector3D.Dot(VelocityVector, WMCocpit.Up);
                ForwVelocityVector = WMCocpit.Forward * Vector3D.Dot(VelocityVector, WMCocpit.Forward);
                LeftVelocityVector = WMCocpit.Left * Vector3D.Dot(VelocityVector, WMCocpit.Left);
                OrientationCocpit = cockpit.GetCockpitMatrix();
                YMaxA = Math.Abs((float)Math.Min(thrusts.UpThrMax / PhysicalMass - GravVector.Length(), thrusts.DownThrMax / PhysicalMass + GravVector.Length()));
                ZMaxA = (float)Math.Min(thrusts.ForwardThrMax, thrusts.BackwardThrMax) / PhysicalMass;
                XMaxA = (float)Math.Min(thrusts.RightThrMax, thrusts.LeftThrMax) / PhysicalMass;
                if (PhysicalMass > CriticalMass) { CriticalMassReached = true; } else { CriticalMassReached = false; }
                EmergencyReturn = bats.CurrentPersent() <= ReturnOnCharge || PhysicalMass >= CriticalMass || hydrogen_tanks_cargo.AverageFilledRatio < ReturnOnHydrogen;
            }
            public void Pause(bool enable)
            {
                if (enable)
                {
                    thrusts.ClearThrustOverridePersent();
                    gyros.SetOverride(false, 1);
                    reflectors_light.Off();
                    paused = true;
                }
                else { paused = false; }
                mystorage.SaveToStorage();
            }
            public void ClearThrust()
            {
                thrusts.ClearThrustOverridePersent();
                gyros.SetOverride(false, 1);
                mystorage.SaveToStorage();
            }
            public void Clear()
            {
                ClearThrust();
                curent_mode = mode.none;
                mystorage.SaveToStorage();
            }
            public void Stop()
            {
                thrusts.ClearThrustOverridePersent();
                gyros.SetOverride(false, 1);
                curent_mode = mode.none;
                curent_programm = programm.none;
                go_home = false;
                paused = false;
                reflectors_light.Off();
                mystorage.SaveToStorage();
            }
            public bool ToBase()
            {
                bool Complete = false;
                Message = "";
                if (IsCorrectBasePoints(CurrDockPoint == 0 ? 1 : 0))
                {
                    thrusts.On();
                    float MaxUSpeed, MaxFSpeed;
                    Vector3D gyrAng = GetNavAngles(PointsDock[CurrDockPoint == 0 ? 1 : 0].BaseDockPoint, PointsDock[CurrDockPoint == 0 ? 1 : 0].DockMatrix);
                    Vector3D MyPosCon = Vector3D.Transform(MyPos, PointsDock[CurrDockPoint == 0 ? 1 : 0].DockMatrix);
                    Distance = (float)(PointsDock[CurrDockPoint == 0 ? 1 : 0].BaseDockPoint - new Vector3D(MyPosCon.GetDim(0), 0, MyPosCon.GetDim(2))).Length();
                    MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(PointsDock[CurrDockPoint == 0 ? 1 : 0].FlyHeight - (MyPos - PlanetCenter).Length()) * YMaxA) / 1.2f;
                    MaxFSpeed = (float)Math.Sqrt(2 * Distance * ZMaxA) / 2f;
                    gyros.SetOverride(true, gyrAng * GyroMult, 1);
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                    if (UpVelocityVector.Length() < MaxUSpeed)
                        thrusts.SetOverrideAccel("U", (float)((PointsDock[CurrDockPoint == 0 ? 1 : 0].FlyHeight - (MyPos - PlanetCenter).Length()) * AlignAccelMult));
                    else
                    {
                        thrusts.SetOverridePercent("U", 0);
                        thrusts.SetOverridePercent("D", 0);
                    }
                    if (Distance > BaseDistance + 100f)
                    {
                        if (ForwVelocityVector.Length() < MaxFSpeed)
                        {
                            thrusts.SetOverrideAccel("F", (float)(Distance * AlignAccelMult));
                            thrusts.SetOverridePercent("B", 0);
                        }
                        else
                        {
                            thrusts.SetOverridePercent("F", 0);
                            thrusts.SetOverridePercent("B", 0);
                        }
                    }
                    else
                    {
                        thrusts.ClearThrustOverridePersent();
                        gyros.SetOverride(false, 1);
                        if (cockpit.obj.GetShipSpeed() < 0.1f)
                        {
                            curent_mode = mode.none;
                            Complete = true;
                        }

                    }
                    OutStatusMode(MaxFSpeed, MaxUSpeed, 0);
                }
                else { Message = String.Format("Точка {0} - неопределена!", CurrDockPoint == 0 ? 1 : 0); }
                return Complete;
            }
            public bool Dock()
            {
                bool Complete = false;

                Message = "";
                if (IsCorrectBasePoints(CurrDockPoint == 0 ? 1 : 0))
                {
                    thrusts.On();
                    float MaxUSpeed, MaxLSpeed, MaxFSpeed;
                    Vector3D MyPosCon = Vector3D.Transform(MyPos, PointsDock[CurrDockPoint == 0 ? 1 : 0].DockMatrix);
                    Vector3D gyrAng = new Vector3D();
                    bool back = PointsDock[CurrDockPoint == 0 ? 1 : 0].back;
                    if (back)
                    {
                        // back
                        gyrAng = GetNavAngles(MyPosCon * 2 - PointsDock[CurrDockPoint == 0 ? 1 : 0].ConnectorPoint, PointsDock[CurrDockPoint == 0 ? 1 : 0].DockMatrix);
                    }
                    else
                    {
                        gyrAng = GetNavAngles(PointsDock[CurrDockPoint == 0 ? 1 : 0].ConnectorPoint, PointsDock[CurrDockPoint == 0 ? 1 : 0].DockMatrix);
                    }
                    Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, PointsDock[CurrDockPoint == 0 ? 1 : 0].DockMatrix)))).Length() + PointsDock[CurrDockPoint == 0 ? 1 : 0].ConnectorPoint.Length());
                    MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(0)) * XMaxA) / 3;
                    MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(1)) * YMaxA) / 2;
                    MaxFSpeed = (float)Math.Sqrt(2 * Distance * ZMaxA) / 3;
                    if (Distance < (back ? Conn_Distance_Back : Conn_Distance_Forw) + 30)
                        MaxFSpeed = MaxFSpeed / 5;
                    if (Math.Abs((MyPosCon.GetDim(1) - Pos_Y_Correct)) < 1f)
                        MaxUSpeed = 0.1f;
                    gyros.SetOverride(true, gyrAng * GyroMult, 1);
                    if (LeftVelocityVector.Length() < MaxLSpeed)
                        thrusts.SetOverrideAccel("R", (float)(MyPosCon.GetDim(0) * AlignAccelMult));
                    else
                    {
                        thrusts.SetOverridePercent("R", 0);
                        thrusts.SetOverridePercent("L", 0);
                    }
                    float UpAccel = -(float)((MyPosCon.GetDim(1) - Pos_Y_Correct) * AlignAccelMult);
                    float minUpAccel = 0.3f;
                    if ((UpAccel < 0) && (UpAccel > -minUpAccel))
                        UpAccel = -minUpAccel;
                    if ((UpAccel > 0) && (UpAccel < minUpAccel))
                        UpAccel = minUpAccel;
                    if (UpVelocityVector.Length() < MaxUSpeed)
                        thrusts.SetOverrideAccel("U", UpAccel);
                    else
                    {
                        thrusts.SetOverridePercent("D", 0);
                        thrusts.SetOverridePercent("U", 0);

                    }
                    if (((Distance > BaseDistance) || ((Math.Abs(MyPosCon.GetDim(0)) < (Distance / 10 + 0.2f)) && (Math.Abs((MyPosCon.GetDim(1) - Pos_Y_Correct)) < (Distance / 10 + 0.2f)))) && (ForwVelocityVector.Length() < MaxFSpeed))
                    {
                        thrusts.SetOverrideAccel((back ? "B" : "F"), (float)(Distance * AlignAccelMult));
                        thrusts.SetOverridePercent((back ? "F" : "B"), 0);
                    }
                    else
                    {
                        thrusts.SetOverridePercent("F", 0);
                        thrusts.SetOverridePercent("B", 0);
                    }
                    if (Distance < (back ? Conn_Distance_Back : Conn_Distance_Forw) + 10)
                    {
                        if (!back && connector_forw.Status == MyShipConnectorStatus.Connectable)
                        {
                            connector_forw.obj.Connect();
                        }
                        if (back && connector_back.Status == MyShipConnectorStatus.Connectable)
                        {
                            connector_back.obj.Connect();
                        }
                        if ((!back && connector_forw.Status == MyShipConnectorStatus.Connected) || (back && connector_back.Status == MyShipConnectorStatus.Connected))
                        {
                            thrusts.ClearThrustOverridePersent();
                            gyros.SetOverride(false, 1);
                            curent_mode = mode.none;
                            m_forw = false;
                            m_back = false;
                            m_up_gears = false;
                            Complete = true;
                        }
                    }
                    OutStatusMode(MaxFSpeed, MaxUSpeed, MaxLSpeed);
                }
                else { Message = String.Format("Точка {0} - неопределена!", CurrDockPoint == 0 ? 1 : 0); }
                return Complete;
            }
            public bool UnDock()
            {
                bool Complete = false;
                Message = "";
                if (IsCorrectBasePoints(CurrDockPoint) || landing_gears.IsLocked())
                {
                    Vector3D MyPosCon = Vector3D.Transform(MyPos, PointsDock[CurrDockPoint].DockMatrix);
                    Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, PointsDock[CurrDockPoint].DockMatrix)))).Length() + PointsDock[CurrDockPoint].ConnectorPoint.Length());
                    if ((!m_forw && !connector_forw.Connected && !m_back && !connector_back.Connected && !m_up_gears && !landing_gears.IsLocked()) ||
                        (m_forw && Distance > BaseDistance / 2 && !connector_forw.Connected && !connector_back.Connected) ||
                        (m_back && Distance > BaseDistance / 2 && !connector_back.Connected && !connector_forw.Connected) ||
                        (m_up_gears && cockpit.CurrentHeight > 50f && !connector_forw.Connected && !connector_back.Connected && !landing_gears.IsLocked()))
                    {
                        thrusts.ClearThrustOverridePersent();
                        gyros.SetOverride(false, 1);
                        if (cockpit.obj.GetShipSpeed() < 0.1f)
                        {
                            curent_mode = mode.none;
                            m_forw = false;
                            m_back = false;
                            m_up_gears = false;
                            Complete = true;
                        }
                    }
                    else
                    {
                        if (connector_forw.Connected)
                        {
                            connector_forw.obj.Disconnect();
                            m_forw = true;
                        }
                        else if (connector_back.Connected)
                        {
                            connector_back.obj.Disconnect();
                            m_back = true;
                        }
                        else
                        {
                            if (landing_gears.IsLocked())
                            {
                                landing_gears.Unlock();
                                m_up_gears = true;
                            }
                        }
                        if (!connector_forw.Connected && m_forw)
                        {
                            Vector3D gyrAng = GetNavAngles(PointsDock[CurrDockPoint].ConnectorPoint, PointsDock[CurrDockPoint].DockMatrix);
                            gyros.SetOverride(true, gyrAng * GyroMult, 1);
                            thrusts.SetOverridePercent("U", 0);
                            thrusts.SetOverridePercent("D", 0);
                            thrusts.SetOverridePercent("R", 0);
                            thrusts.SetOverridePercent("L", 0);
                            thrusts.SetOverridePercent("F", 0);
                            thrusts.SetOverrideAccel("B", 10);
                        }
                        else if (!connector_back.Connected && m_back)
                        {
                            Vector3D gyrAng = GetNavAngles(MyPosCon * 2 - PointsDock[CurrDockPoint == 0 ? 1 : 0].ConnectorPoint, PointsDock[CurrDockPoint == 0 ? 1 : 0].DockMatrix);
                            //Vector3D gyrAng = GetNavAngles(PointsDock[CurrDockPoint].ConnectorPoint, PointsDock[CurrDockPoint].DockMatrix);
                            gyros.SetOverride(true, gyrAng * GyroMult, 1);
                            thrusts.SetOverridePercent("U", 0);
                            thrusts.SetOverridePercent("D", 0);
                            thrusts.SetOverridePercent("R", 0);
                            thrusts.SetOverridePercent("L", 0);
                            thrusts.SetOverridePercent("B", 0);
                            thrusts.SetOverrideAccel("F", 10);
                        }
                        else
                        {
                            if (m_up_gears && !landing_gears.IsLocked())
                            {
                                if (cockpit.CurrentHeight < 50f)
                                {
                                    thrusts.SetOverrideAccel("U", 1000);
                                }
                            }
                        }
                    }
                    OutStatusMode(0, 0, 0);
                }
                else { Message = String.Format("Точка {0} - неопределена!", CurrDockPoint); }
                return Complete;
            }
            public bool BaseOperation()
            {
                bool Complete = false;
                bats.Charger();
                thrusts.Off();
                hydrogen_tanks_cargo.Stockpile(PointsDock[CurrDockPoint].Loading);
                if (go_home && PointsDock[CurrDockPoint].Home)
                {
                    Stop();
                }
                else
                {
                    if ((PointsDock[CurrDockPoint].Loading && hydrogen_tanks_cargo.AverageFilledRatio == 1.0f) || (!PointsDock[CurrDockPoint].Loading && hydrogen_tanks_cargo.AverageFilledRatio == 0.0f))
                    {
                        if (bats.CurrentPersent() >= ReturnOffCharge)
                        {
                            bats.Auto();
                            //thrusts.On();
                            Complete = true;
                        }
                    }
                }
                return Complete;
            }
            //-------------------------------------------------
            public void OutStatusMode(float MaxFSpeed, float MaxUSpeed, float MaxLSpeed)
            {
                StringBuilder values = new StringBuilder();
                values.Append(" STATUS\n");
                //Vector3D MyPosPoint = Vector3D.Transform(MyPos, WorkMatrix) - WorkPoint;
                Vector3D MyPosPoint = Vector3D.Transform(MyPos, PointsDock[CurrDockPoint == 0 ? 1 : 0].DockMatrix);
                values.Append("My_Length   : " + Math.Round(MyPosPoint.Length(), 2) + "\n");
                values.Append("MyPosDrill[0]   : " + Math.Round(MyPosPoint.GetDim(0), 2) + "\n");
                values.Append("MyPosDrill[1]   : " + Math.Round(MyPosPoint.GetDim(1), 2) + "\n");
                values.Append("MyPosDrill[2]   : " + Math.Round(MyPosPoint.GetDim(2), 2) + "\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("DeltaHeight: " + Math.Round(PointsDock[CurrDockPoint == 0 ? 1 : 0].FlyHeight - (MyPos - PlanetCenter).Length()).ToString() + "\n");
                values.Append("Distance: " + Math.Round(Distance).ToString() + "\n");
                values.Append("------------------------------------------\n");
                values.Append("ZMaxA (F-B) : " + Math.Round(ZMaxA, 2).ToString() + "MaxFSpeed: " + Math.Round(MaxFSpeed, 2).ToString() + "\n");
                values.Append("YMaxA (U-D) : " + Math.Round(YMaxA, 2).ToString() + "MaxUSpeed: " + Math.Round(MaxUSpeed, 2).ToString() + "\n");
                values.Append("XMaxA (L-R) : " + Math.Round(XMaxA, 2).ToString() + "MaxLSpeed: " + Math.Round(MaxLSpeed, 2).ToString() + "\n");
                values.Append("------------------------------------------\n");
                values.Append("UpVelocityVector   : " + Math.Round(UpVelocityVector.Length(), 2) + "\n");
                values.Append("MaxUSpeed   : " + Math.Round(MaxUSpeed, 2) + "\n");
                values.Append(thrusts.TextInfo());
                //lcd_debug.OutText(values);
            }
            public string TextCritical()
            {
                StringBuilder values = new StringBuilder();
                //values.Append("ВЫСОТА (Цен.план.): " + Math.Round((MyPos - PlanetCenter).Length()).ToString() + " / " + Math.Round(PointsDock[CurrDockPoint].FlyHeight).ToString() + "\n");
                //values.Append("ДИСТАНЦИЯ         : " + Math.Round(Distance).ToString() + "\n");
                values.Append("--------------------------------------\n");
                values.Append("АВАРИЙНЫЙ ВОЗВРАТ   : " + (EmergencyReturn ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("|-ФИЗ./КРИТ.(МАССА) : " + Math.Round(PhysicalMass).ToString() + " / " + CriticalMass + " " + (CriticalMassReached ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("|-БАТАРЕЯ %         : " + PText.GetPersent(bats.CurrentPersent()) + " " + (bats.CurrentPersent() <= ReturnOnCharge ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("|-ТОПЛИВО H2 %      : " + PText.GetPersent(hydrogen_tanks_cargo.AverageFilledRatio) + " " + (hydrogen_tanks_cargo.AverageFilledRatio < ReturnOnHydrogen ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("--------------------------------------\n");
                values.Append("ERROR: " + Message + "\n");
                return values.ToString();
            }
            public bool IsCorrectBasePoints(int num)
            {
                bool vdm = IsDockMatrix(PointsDock[num].DockMatrix);
                bool vf = PointsDock[num].FlyHeight > 0;
                return vdm && vf;
            }
            public bool IsDockMatrix(MatrixD matrix)
            {
                double res = matrix.M11 + matrix.M12 + matrix.M13 + matrix.M14 + matrix.M21 + matrix.M22 + matrix.M23 + matrix.M24 + matrix.M31 + matrix.M32 + matrix.M33 + matrix.M34 + matrix.M41 + matrix.M42 + matrix.M43 + matrix.M44;
                return (int)Math.Round(res) == 0 ? false : true;
            }
            public string TextPointsDock()
            {
                StringBuilder values = new StringBuilder();
                bool vdm = IsDockMatrix(PointsDock[CurrDockPoint].DockMatrix);
                bool vf = PointsDock[CurrDockPoint].FlyHeight > 0;
                values.Append("==[ СПИСОК БАЗ ]==================================\n");
                values.Append((IsCorrectBasePoints(CurrDockPoint) ? igreen.ToString() : ired.ToString()) + "БАЗА #: " + CurrDockPoint.ToString() + " Id:" + PointsDock[CurrDockPoint].EntityId + "\n");
                values.Append("БАЗА ОСНОВНАЯ " + (PointsDock[CurrDockPoint].Home ? igreen.ToString() : ired.ToString()) + ", БАЗА ЗАГРУЗКИ " + (PointsDock[CurrDockPoint].Loading ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append(PointsDock[CurrDockPoint].Name + "\n");
                values.Append("--------------------------------------------------\n");
                values.Append((!vdm ? ired.ToString() : igreen.ToString()) + PText.GetGPSMatrixD("DockMatrix:", PointsDock[CurrDockPoint].DockMatrix) + "\n");
                values.Append((PointsDock[CurrDockPoint].BaseDockPoint.IsZero() ? ired.ToString() : igreen.ToString()) + PText.GetGPS("DockPoint:", PointsDock[CurrDockPoint].BaseDockPoint) + "\n");
                values.Append((PointsDock[CurrDockPoint].ConnectorPoint.IsZero() ? ired.ToString() : igreen.ToString()) + PText.GetGPS("ConnectorPoint:", PointsDock[CurrDockPoint].ConnectorPoint) + "\n");
                values.Append((!vf ? ired.ToString() : igreen.ToString()) + "БЕЗОП. ВЫСОТА : " + Math.Round(PointsDock[CurrDockPoint].FlyHeight).ToString() + "\n");
                values.Append("--------------------------------------------------\n");
                values.Append("==================================================\n");
                return values.ToString();
            }
            public string TextProgramm()
            {
                Vector3D conn = connector_forw.GetPosition() - cockpit.obj.GetPosition();
                StringBuilder values = new StringBuilder();
                values.Append("==[ МАРШРУТЫ ]=================================\n");
                values.Append((IsCorrectBasePoints(CurrDockPoint) ? igreen.ToString() : ired.ToString()) + "БАЗА ТЕКУЩАЯ#    : " + CurrDockPoint.ToString() + "\n");
                values.Append((IsCorrectBasePoints(CurrDockPoint == 0 ? 1 : 0) ? igreen.ToString() : ired.ToString()) + "БАЗА НАЗНАЧЕНИЯ# : " + (CurrDockPoint == 0 ? 1 : 0).ToString() + "\n");
                values.Append("-----------------------------------------------\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("ПАУЗА       : " + (paused ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("ДОМОЙ       : " + (go_home ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("-----------------------------------------------\n");
                values.Append("ГРАВИТАЦИЯ          : " + Math.Round(GravVector.Length(), 2) + "\n");
                values.Append("СКОРОСТЬ            : " + Math.Round(cockpit.obj.GetShipSpeed(), 2) + "\n");
                values.Append("ВЫСОТА (над.пл)     : " + Math.Round(cockpit.CurrentHeight, 2) + "\n");
                values.Append("ВЫСОТА (от цен.пл.) : " + Math.Round((MyPos - PlanetCenter).Length()).ToString() + " / " + Math.Round(PointsDock[CurrDockPoint == 0 ? 1 : 0].FlyHeight).ToString() + "\n");
                values.Append("ДИСТАНЦИЯ           : " + Math.Round(Distance).ToString() + "\n");
                values.Append("----------------------------------------------\n");
                values.Append("ФИЗ./КРИТ.(МАССА) : " + Math.Round(PhysicalMass).ToString() + " / " + CriticalMass + " " + (CriticalMassReached ? ired.ToString() : igreen.ToString()) + "\n");
                //values.Append("Conn_Distance: " + Math.Round((conn).Length()).ToString() + "\n");
                //values.Append("Conn_Distance[0] X : " + Math.Round(conn.GetDim(0), 2) + "\n");
                //values.Append("Conn_Distance[1] Y : " + Math.Round(conn.GetDim(1), 2) + "\n");
                //values.Append("Conn_Distance[2] Z : " + Math.Round(conn.GetDim(2), 2) + "\n");
                //values.Append("ERROR: " + Message + "\n");
                values.Append("==============================================\n");
                return values.ToString();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "horizont": if (curent_programm == programm.none) { if (horizont) { horizont = false; } else { horizont = true; } } break;
                    case "load": mystorage.LoadFromStorage(); break;
                    case "save": mystorage.SaveToStorage(); break;
                    case "pause": Pause(!paused); break;
                    case "stop": Stop(); break;
                    case "clear": Clear(); curent_programm = programm.none; mystorage.SaveToStorage(); break;
                    case "save_base":
                        if (connector_forw.Connected)
                        {
                            SetDockMatrix(connector_forw, false);
                        }
                        else if (connector_back.Connected)
                        {
                            SetDockMatrix(connector_back, true);
                        }
                        break;
                    case "save_height": SetFlyHeight(); break;
                    case "save_home": SetHomeDock(); break;
                    case "save_loading": SetLoadingDock(); break;
                    case "fly_bp_bp": curent_programm = programm.fly_bp_bp; mystorage.SaveToStorage(); break;
                    case "carriage": curent_programm = programm.carriage; mystorage.SaveToStorage(); break;
                    case "go_home": { go_home = true; break; }
                    case "to_base": curent_mode = mode.to_base; mystorage.SaveToStorage(); break;
                    case "dock": curent_mode = mode.dock; mystorage.SaveToStorage(); break;
                    case "un_dock": curent_mode = mode.un_dock; mystorage.SaveToStorage(); break;
                    case "point+": if (!connector_forw.Connected && !connector_back.Connected) { CurrDockPoint++; if (CurrDockPoint > 1) CurrDockPoint = 0; } break;
                    case "point-": if (!connector_forw.Connected && !connector_back.Connected) { CurrDockPoint--; if (CurrDockPoint < 0) CurrDockPoint = 1; } break;
                    case "reverse_point": if (!connector_forw.Connected && !connector_back.Connected) { CurrDockPoint = (CurrDockPoint == 0 ? 1 : 0); } break;
                    case "clear_point": { PointsDock[0] = new PointDock();PointsDock[1] = new PointDock(); break; break;};
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    cockpit.Logic(argument, updateSource);
                    if (!connector_forw.Connected && !connector_back.Connected && !landing_gears.IsLocked())
                    {
                        if ((gravity && cockpit.CurrentHeight > 8.0f) || (!gravity && (connector_forw.Connectable || connector_back.Connectable)))
                        {
                            hydrogen_tanks_nav.Stockpile(false);
                            bats.Auto();
                            thrusts.On();
                        }
                    }
                    else
                    {
                        // Припаркован
                        int? index = null;
                        if (connector_forw.Connected)
                        {
                            index = GetCurrConnectorDock(connector_forw);
                        }
                        else if (connector_back.Connected)
                        {
                            index = GetCurrConnectorDock(connector_back);
                        }
                        if (index == -1) { index = Array.FindIndex(PointsDock, element => element.EntityId == 0); }
                        if (index >= 0)
                        {
                            CurrDockPoint = (int)index;
                        }
                        hydrogen_tanks_nav.Stockpile(true);
                        reflectors_light.Off();
                        bats.Charger();
                        thrusts.Off();
                    }
                    //if (clock >= 10)
                    //{
                    //    clock = 0;
                    //}
                    //clock++;
                    UpdateCalc();
                    if (EmergencyReturn) lightings.On(); else lightings.Off();
                    if (curent_programm == programm.none)
                    {
                        if (horizont) { Horizon(); } else { gyros.SetOverride(false, 1); }
                        if (curent_mode == mode.un_dock && !paused)
                        {
                            if (UnDock() && curent_programm == programm.none)
                            {
                                curent_mode = mode.none;
                            }
                        }
                        if (curent_mode == mode.to_base && !paused)
                        {
                            if (ToBase() && curent_programm == programm.none)
                            {
                                curent_mode = mode.none;
                            }
                        }
                        if (curent_mode == mode.dock && !paused)
                        {
                            if (Dock() && curent_programm == programm.none)
                            {
                                curent_mode = mode.none;
                            }
                        }
                    }
                    if (curent_programm == programm.fly_bp_bp && !paused) { Fly_BP_BP(); }
                    if (curent_programm == programm.carriage && !paused) { Carriage(); }
                }
            }
        }
        public class MyStorage
        {
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                navigation.curent_programm = (Navigation.programm)GetValInt("curent_programm", str.ToString());
                navigation.curent_mode = (Navigation.mode)GetValInt("curent_mode", str.ToString());
                navigation.paused = GetValBool("pause", str.ToString());
                navigation.go_home = GetValBool("go_home", str.ToString());
                navigation.EmergencyReturn = GetValBool("EmergencyReturn", str.ToString());
                navigation.CurrDockPoint = GetValInt("CurrDockPoint", str.ToString());
                for (int i = 0; i < 2; i++)
                {
                    navigation.PointsDock[i].back = GetValBool("PD_back_" + i, str.ToString());
                    navigation.PointsDock[i].DockMatrix = new MatrixD(GetValDouble("PD_DM" + i + "_11", str.ToString()), GetValDouble("PD_DM" + i + "_12", str.ToString()), GetValDouble("PD_DM" + i + "_13", str.ToString()), GetValDouble("PD_DM" + i + "_14", str.ToString()),
                        GetValDouble("PD_DM" + i + "_21", str.ToString()), GetValDouble("PD_DM" + i + "_22", str.ToString()), GetValDouble("PD_DM" + i + "_23", str.ToString()), GetValDouble("PD_DM" + i + "_24", str.ToString()),
                        GetValDouble("PD_DM" + i + "_31", str.ToString()), GetValDouble("PD_DM" + i + "_32", str.ToString()), GetValDouble("PD_DM" + i + "_33", str.ToString()), GetValDouble("PD_DM" + i + "_34", str.ToString()),
                        GetValDouble("PD_DM" + i + "_41", str.ToString()), GetValDouble("PD_DM" + i + "_42", str.ToString()), GetValDouble("PD_DM" + i + "_43", str.ToString()), GetValDouble("PD_DM" + i + "_44", str.ToString()));
                    navigation.PointsDock[i].BaseDockPoint = new Vector3D(GetValDouble("PD_BDP_" + i + "_X", str.ToString()), GetValDouble("PD_BDP_" + i + "_Y", str.ToString()), GetValDouble("PD_BDP_" + i + "_Z", str.ToString()));
                    navigation.PointsDock[i].ConnectorPoint = new Vector3D(GetValDouble("PD_CP_" + i + "_X", str.ToString()), GetValDouble("PD_CP_" + i + "_Y", str.ToString()), GetValDouble("PD_CP_" + i + "_Z", str.ToString()));
                    navigation.PointsDock[i].FlyHeight = GetValDouble("PD_FlyHeight_" + i, str.ToString());
                    navigation.PointsDock[i].EntityId = GetValInt64("PD_EntityId_" + i, str.ToString());
                    navigation.PointsDock[i].Name = GetValString("PD_Name_" + i, str.ToString());
                    navigation.PointsDock[i].Home = GetValBool("PD_Home_" + i, str.ToString());
                    navigation.PointsDock[i].Loading = GetValBool("PD_Loading_" + i, str.ToString());
                    navigation.PointsDock[i].PlanetCenter = new Vector3D(GetValDouble("PX_" + i, str.ToString()), GetValDouble("PY_" + i, str.ToString()), GetValDouble("PZ_" + i, str.ToString()));
                }
                //navigation.PlanetCenter = new Vector3D(GetValDouble("PX", str.ToString()), GetValDouble("PY", str.ToString()), GetValDouble("PZ", str.ToString()));
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                values.Append("curent_programm: " + ((int)navigation.curent_programm).ToString() + ";\n");
                values.Append("curent_mode: " + ((int)navigation.curent_mode).ToString() + ";\n");
                values.Append("pause: " + navigation.paused.ToString() + ";\n");
                values.Append("go_home: " + navigation.go_home.ToString() + ";\n");
                values.Append("EmergencyReturn: " + navigation.EmergencyReturn.ToString() + ";\n");
                values.Append("CurrDockPoint: " + navigation.CurrDockPoint.ToString() + ";\n");
                for (int i = 0; i < 2; i++)
                {
                    values.Append("PD_back_" + i + ": " + navigation.PointsDock[i].back.ToString() + ";\n");
                    values.Append(navigation.PointsDock[i].DockMatrix.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "PD_DM" + i + "_"));
                    values.Append(navigation.PointsDock[i].BaseDockPoint.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PD_BDP_" + i + "_X").Replace("Y", "PD_BDP_" + i + "_Y").Replace("Z", "PD_BDP_" + i + "_Z") + ";\n");
                    values.Append(navigation.PointsDock[i].ConnectorPoint.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PD_CP_" + i + "_X").Replace("Y", "PD_CP_" + i + "_Y").Replace("Z", "PD_CP_" + i + "_Z") + ";\n");
                    values.Append("PD_FlyHeight_" + i + ": " + Math.Round(navigation.PointsDock[i].FlyHeight, 0) + ";\n");
                    values.Append("PD_EntityId_" + i + ": " + navigation.PointsDock[i].EntityId.ToString() + ";\n");
                    values.Append("PD_Name_" + i + ": " + navigation.PointsDock[i].Name + ";\n");
                    values.Append("PD_Home_" + i + ": " + navigation.PointsDock[i].Home.ToString() + ";\n");
                    values.Append("PD_Loading_" + i + ": " + navigation.PointsDock[i].Loading.ToString() + ";\n");
                    values.Append(navigation.PointsDock[i].PlanetCenter.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PX_" + i).Replace("Y", "PY_" + i).Replace("Z", "PZ_" + i) + ";\n");

                }
                //values.Append(navigation.PlanetCenter.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PX").Replace("Y", "PY").Replace("Z", "PZ") + ";\n");
                lcd_storage.OutText(values);
            }
            public MyStorage() { }
            private string GetVal(string Key, string str, string val) { string pattern = @"(" + Key + "):([^:^;]+);"; System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern); if (match.Success) { val = match.Groups[2].Value; } return val; }
            public string GetValString(string Key, string str) { return GetVal(Key, str, ""); }
            public double GetValDouble(string Key, string str) { return Convert.ToDouble(GetVal(Key, str, "0")); }
            public int GetValInt(string Key, string str) { return Convert.ToInt32(GetVal(Key, str, "0")); }
            public long GetValInt64(string Key, string str) { return Convert.ToInt64(GetVal(Key, str, "0")); }
            public bool GetValBool(string Key, string str) { return Convert.ToBoolean(GetVal(Key, str, "False")); }
        }
    }
}
