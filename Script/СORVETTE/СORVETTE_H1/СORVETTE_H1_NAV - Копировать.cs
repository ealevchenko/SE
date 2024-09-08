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
/// Корвет водородный
/// </summary>
namespace СORVETTE_H1_NAV_Copy
{
    /// <summary>
    /// Корвет водородный
    /// </summary>
    public sealed class Program : MyGridProgram
    {
        // v1
        string NameObj = "[СORVETTE-H1]";
        static string tag_batterys_duty = "[batterys_duty]";
        static string tag_lightings_warning = "[warning]";
        static float GyroMult = 1f;
        static int CriticalMass = 2400000;
        static float BaseDistance = 300f;
        static float Conn_Distance = 60f;
        static float Pos_Y_Correct = 0.6f;
        static float AlignAccelMult = 0.3f;
        static float ReturnOnCharge = 0.2f;
        static float ReturnOffCharge = 0.9f;
        static float MinHeight = 1000f;
        static float MinDistance = 500f;
        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';
        static int clock_main = 0;
        static MyStorage mystorage;
        static LCD lcd_storage;
        static LCD lcd_nav1;
        static LCD lcd_nav3;
        static LCD lcd_info;
        static LCD lcd_debug;
        static Batterys bats;
        static Connector connector;
        static Connector connector_down;
        static ReflectorsLight reflectors_light;
        static Lightings lightings;
        static Gyros gyros;
        static Thrusts thrusts;
        static Cockpit cockpit;
        static LandingGear landing_gears;
        static Camera camera_course;
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
            mystorage = new MyStorage();
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_nav1 = new LCD(NameObj + "-LCD-NAV1");
            lcd_nav3 = new LCD(NameObj + "-LCD-NAV3");
            lcd_info = new LCD(NameObj + "-LCD-INFO");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            cockpit = new Cockpit(NameObj + "-Cocpit [LCD]");
            bats = new Batterys(NameObj);
            connector = new Connector(NameObj + "-Connector parking Locked");
            connector_down = new Connector(NameObj + "-Connector parking [down] Locked");
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            gyros = new Gyros(NameObj);
            thrusts = new Thrusts(NameObj);
            landing_gears = new LandingGear(NameObj);
            lightings = new Lightings(NameObj, tag_lightings_warning);
            lightings.Off();
            camera_course = new Camera(NameObj + "-Камера [curse]");
            navigation = new Navigation();
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
                //values_nav1.Append(thrusts.TextInfo());
                values_nav1.Append(navigation.TextPanel());
                lcd_nav1.OutText(values_nav1);

                StringBuilder values_nav3 = new StringBuilder();
                values_nav3.Append(bats.TextInfo());
                values_nav3.Append(connector.TextInfo());
                values_nav3.Append(connector_down.TextInfo());
                values_nav3.Append(landing_gears.TextInfo());
                values_nav3.Append(navigation.TextInfo1());
                lcd_nav3.OutText(values_nav3);

                //StringBuilder values_info = new StringBuilder();
                //values_info.Append(bats.TextInfo());
                //values_info.Append(connector.TextInfo());
                //values_info.Append(connector_down.TextInfo());
                //values_info.Append(landing_gears.TextInfo());
                ////values_info.Append(special_inventory.TextInfo());
                //values_info.Append(navigation.TextInfo1());
                //cockpit.OutText(values_info, 0);
                //StringBuilder test_info = new StringBuilder();
                //lcd_info.OutText(test_info);
                StringBuilder values_info1 = new StringBuilder();
                values_info1.Append(navigation.TextInfo2());
                cockpit.OutText(values_info1, 0);
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
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("КОННЕКТОР: " + (Connected ? igreen.ToString() : (Connectable ? iyellow.ToString() : ired.ToString())) + "\n");
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
                            OverrideValue -= (float)this.remote_control.obj.GetNaturalGravity().Length();
                            //OverrideValue += (float)this.remote_control.obj.GetNaturalGravity().Length();
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
        public class Camera : BaseTerminalBlock<IMyCameraBlock>
        {
            public Camera(string name) : base(name) { base.obj.EnableRaycast = true; }
            public MyDetectedEntityInfo? Raycast(double dist_scan, float pitch_scan, float yaw_scan)
            {
                MyDetectedEntityInfo? result = null;
                if (base.obj.CanScan(dist_scan))
                {
                    result = base.obj.Raycast(dist_scan, pitch_scan, yaw_scan);
                }
                return result;
            }
            public string TextInfo() { StringBuilder values = new StringBuilder(); return values.ToString(); }
            public string GetTextDetectedEntityInfo(MyDetectedEntityInfo? info)
            {
                StringBuilder values = new StringBuilder();
                if (info != null)
                {
                    Vector3D? HitPosition = ((MyDetectedEntityInfo)info).HitPosition;
                    values.Append("РАССТОЯНИЕ   : " + (HitPosition != null ? Math.Round(((Vector3D)((Vector3D)HitPosition) - base.obj.GetPosition()).Length(), 2).ToString() : "") + "\n");
                    values.Append("Name         : " + ((MyDetectedEntityInfo)info).Name + "\n");
                    values.Append("Type         : " + ((MyDetectedEntityInfo)info).Type + "\n");
                    values.Append("HitPosition  : " + HitPosition + "\n");
                    values.Append("Orientation  : " + ((MyDetectedEntityInfo)info).Orientation + "\n");
                    values.Append("Velocity     : " + ((MyDetectedEntityInfo)info).Velocity + "\n");
                    values.Append("Relationship : " + ((MyDetectedEntityInfo)info).Relationship + "\n");
                    values.Append("BoundingBox  : " + ((MyDetectedEntityInfo)info).BoundingBox + "\n");
                }
                else { values.Append("РАССТОЯНИЕ   : \n"); values.Append("Name         : \n"); values.Append("Type         : \n"); values.Append("HitPosition  : \n"); values.Append("Orientation  : \n"); values.Append("Velocity     : \n"); values.Append("Relationship : \n"); values.Append("BoundingBox  : \n"); };
                return values.ToString();
            }
        }
        public class CollisionProtection : BaseListTerminalBlock<IMyCameraBlock>
        {
            private BaseController remote_control;

            float pitch_scan = 0f;
            float yaw_scan = 0f;
            //------------------------------------------------
            public List<IMyCameraBlock> UpCamera = new List<IMyCameraBlock>();
            public List<IMyCameraBlock> DownCamera = new List<IMyCameraBlock>();
            public List<IMyCameraBlock> LeftCamera = new List<IMyCameraBlock>();
            public List<IMyCameraBlock> RightCamera = new List<IMyCameraBlock>();
            public List<IMyCameraBlock> ForwardCamera = new List<IMyCameraBlock>();
            public List<IMyCameraBlock> BackwardCamera = new List<IMyCameraBlock>();
            public CollisionProtection(string name_obj) : base(name_obj) { }
            public CollisionProtection(string name_obj, string tag) : base(name_obj, tag) { }
            public void InitProtection(BaseController remote_control)
            {
                this.remote_control = remote_control;
                MatrixD OrientationCocpit = this.remote_control.GetCockpitMatrix();
                UpCamera.Clear();
                DownCamera.Clear();
                LeftCamera.Clear();
                RightCamera.Clear();
                ForwardCamera.Clear();
                BackwardCamera.Clear();
                Matrix CameraMatrix = new MatrixD();
                foreach (IMyCameraBlock cam in this.list_obj)
                {
                    cam.Orientation.GetMatrix(out CameraMatrix);
                    //Y
                    if (CameraMatrix.Forward == OrientationCocpit.Up)
                    {
                        UpCamera.Add(cam);
                    }
                    else if (CameraMatrix.Forward == OrientationCocpit.Down)
                    {
                        DownCamera.Add(cam);
                    }
                    //X
                    else if (CameraMatrix.Forward == OrientationCocpit.Left)
                    {
                        LeftCamera.Add(cam);
                    }
                    else if (CameraMatrix.Forward == OrientationCocpit.Right)
                    {
                        RightCamera.Add(cam);
                    }
                    //Z
                    else if (CameraMatrix.Forward == OrientationCocpit.Forward)
                    {
                        ForwardCamera.Add(cam);
                    }
                    else if (CameraMatrix.Forward == OrientationCocpit.Backward)
                    {
                        BackwardCamera.Add(cam);
                    }
                }
            }
            public MyDetectedEntityInfo? Raycast(IMyCameraBlock cam, double dist_scan, float pitch_scan, float yaw_scan)
            {
                MyDetectedEntityInfo? result = null;
                cam.EnableRaycast = true;
                if (cam.CanScan(dist_scan))
                {
                    result = cam.Raycast(dist_scan, pitch_scan, yaw_scan);
                }
                return result;
            }
            public List<MyDetectedEntityInfo> Raycast(List<IMyCameraBlock> cams, double dist_scan, float pitch_scan, float yaw_scan)
            {
                List<MyDetectedEntityInfo> result = new List<MyDetectedEntityInfo>();
                foreach (IMyCameraBlock cam in cams)
                {
                    MyDetectedEntityInfo? info = Raycast(cam, dist_scan, pitch_scan, yaw_scan);
                    if (info != null) result.Add((MyDetectedEntityInfo)info);
                }
                return result;
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
        public class Navigation
        {
            public int clock = 0;
            public bool m_forw { get; set; } = false;       // Направление расстыковки-стыковка
            public bool m_up { get; set; } = false;         // Направление расстыковки-стыковка
            public bool m_up_gears { get; set; } = false;   // Направление расстыковки-стыковка
            public bool gravity { get; set; } = false;
            public bool horizont { get; private set; } = false;
            public bool scaning { get; private set; } = false;
            public double dist_scan { get; set; } = 1000;
            public double? dist_curse { get; set; } = null;
            public bool curse { get; private set; } = false;
            public MyDetectedEntityInfo? info_scan { get; set; }
            public Vector3D? TackVector { get; set; } = null;
            public enum programm : int
            {
                none = 0,
                fly_bp_bs = 1,      // перелет база планета -> база космос 
                fly_bs_bp = 2,      // перелет база космос -> база планете
                fly_bp_up = 3,      // перелет 
                fly_down_bp = 4,      // перелет 
                fly_bp_bp = 5,      // перелет 
            };
            public static string[] name_programm = { "", "Б:ПЛАНЕТА->Б:КОСМОС", "Б:КОСМОС->Б:ПЛАНЕТА", "Б:ПЛАНЕТА->КОСМОС", "КОСМОС->Б:ПЛАНЕТА", "Б:ПЛАНЕТА->Б:ПЛАНЕТА" };
            programm curent_programm = programm.none;
            public enum mode : int
            {
                none = 0,
                base_operation = 1,
                un_dock = 2,
                to_base = 3,
                dock = 4,
                planet_up = 5,
                planet_down = 6,
                to_work = 7,
                align_work = 8,
            };
            public static string[] name_mode = { "", "БАЗА", "РАСТЫКОВКА", "К БАЗЕ", "СТЫКОВКА", "ВЗЛЕТ", "ПОСАДКА", "К РАБОТЕ", "ВЫРАВНЯТЬ" };
            mode curent_mode = mode.none;
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
            private Vector3D WorkPoint = new Vector3D(0, 0, 0);
            public class PointDock
            {
                public MatrixD DockMatrix { get; set; }
                public Vector3D BaseDockPoint { get; set; } = new Vector3D(0, 0, 200);
                public Vector3D ConnectorPoint { get; set; } = new Vector3D(0, 0, -60);
                public double FlyHeight { get; set; } = 0;
                public long EntityId { get; set; } = 0;
                public string Name { get; set; } = null;
                public bool gravity { get; set; } = false;
            }
            public PointDock[] PointsDock = new PointDock[10];
            public int CurrDockPoint { get; set; } = 0;
            public int NextDockPoint { get; set; } = 0;
            public int panel { get; set; } = 0;
            public int route { get; set; } = 0;
            public MatrixD WorkMatrix { get; private set; }
            public bool CriticalMassReached { get; private set; }// Признак критической массы
            public bool EmergencyReturn = false;
            public bool go_home = false; // вернутся домой и остатся
            public bool paused = false;
            public string Message { get; set; } = "";
            public double S { get; set; } = 0;
            public Navigation()
            {
                thrusts.InitThrusts(cockpit);
                for (int i = 0; i < 10; i++){PointsDock[i] = new PointDock();}
                LoadFromStorage();
                FindPlanetCenter();
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
            public void SetDockMatrix(Connector connector)
            {
                if (connector.Connected)
                {
                    IMyShipConnector con_base = connector.getRemoteConnector();
                    if (con_base != null)
                    {
                        int index = Array.FindIndex(PointsDock, element => element.EntityId == con_base.EntityId);
                        if (index >= 0) { CurrDockPoint = index; }
                        PointsDock[CurrDockPoint].DockMatrix = GetNormTransMatrixFromMyPos();
                        PointsDock[CurrDockPoint].ConnectorPoint = new Vector3D(0, 0, -60);
                        PointsDock[CurrDockPoint].EntityId = con_base.EntityId;
                        PointsDock[CurrDockPoint].Name = con_base.DisplayNameText;
                        PointsDock[CurrDockPoint].gravity = gravity;
                        SaveToStorage();
                    }
                    else
                    {
                        Message = String.Format("Коннектор базы не определен!");
                    }
                }
            }
            public void SetWorkMatrix()
            {
                WorkMatrix = GetNormTransMatrixFromMyPos();
                WorkPoint = new Vector3D(0, 0, 0);
                SaveToStorage();
            }
            public void SetFlyHeight()
            {
                PointsDock[CurrDockPoint].FlyHeight = (MyPos - PlanetCenter).Length();
                PointsDock[CurrDockPoint].BaseDockPoint = new Vector3D(0, 0, 200);
                SaveToStorage();
            }
            public void FindPlanetCenter()
            {
                if (cockpit.obj.TryGetPlanetPosition(out PlanetCenter)) { SaveToStorage(); }
            }
            //---------------------------------------------
            public Vector3D GetNavAngles(Vector3D Target, MatrixD InvMatrix)
            {
                Vector3D V3Dcenter = cockpit.obj.GetPosition();
                Vector3D V3Dfow = cockpit.obj.WorldMatrix.Forward + V3Dcenter;
                Vector3D V3Dup = cockpit.obj.WorldMatrix.Up + V3Dcenter;
                Vector3D V3Dleft = cockpit.obj.WorldMatrix.Left + V3Dcenter;
                //Vector3D GravNorm = Vector3D.Normalize(GravVector) + V3Dcenter;
                V3Dcenter = Vector3D.Transform(V3Dcenter, InvMatrix);
                V3Dfow = (Vector3D.Transform(V3Dfow, InvMatrix)) - V3Dcenter;
                V3Dup = (Vector3D.Transform(V3Dup, InvMatrix)) - V3Dcenter;
                V3Dleft = (Vector3D.Transform(V3Dleft, InvMatrix)) - V3Dcenter;

                //GravNorm = Vector3D.Normalize((Vector3D.Transform(GravNorm, InvMatrix)) - V3Dcenter - new Vector3D(sfiftX, 0, shiftZ));
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
                //Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter);
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
            public double GetBrakingLanding(double max_thrusts) { double a = (max_thrusts / 1000) * (1 / (TotalMass / 1000)); double t = (0 - cockpit.obj.GetShipSpeed()) / -a; S = (cockpit.obj.GetShipSpeed() * t) + ((-a) * Math.Pow(t, 2)) / 2; return S; }
            //-----------------------------------------------
            public void Fly_BP_BS()
            {
                Message = "";
                if (IsCorrectBasePoints(CurrDockPoint) && IsCorrectBasePoints(NextDockPoint))
                {
                    if (curent_mode == mode.none) { curent_mode = mode.un_dock; SaveToStorage(); }
                    if (curent_mode == mode.un_dock && UnDock()) { curent_mode = mode.planet_up; SaveToStorage(); }
                    if (curent_mode == mode.planet_up && PlanetUp()) { curent_mode = mode.to_base; thrusts.On(); SaveToStorage(); }
                    if (curent_mode == mode.to_base && ToBase()) { curent_mode = mode.dock; SaveToStorage(); }
                    if (curent_mode == mode.dock && Dock()) { Clear(); curent_programm = programm.none; SaveToStorage(); }
                }
                else
                {
                    Message = String.Format("Точка {0}->{1} - неопределена!", CurrDockPoint, NextDockPoint);
                    curent_programm = programm.none;
                    SaveToStorage();
                }
            }
            public void Fly_BS_BP()
            {
                Message = "";
                if (IsCorrectBasePoints(CurrDockPoint) && IsCorrectBasePoints(NextDockPoint))
                {
                    if (curent_mode == mode.none)
                    {
                        curent_mode = mode.un_dock;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.un_dock && UnDock())
                    {
                        curent_mode = mode.to_base;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.to_base && ToBase())
                    {
                        curent_mode = mode.planet_down;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.planet_down && PlanetDown())
                    {
                        curent_mode = mode.to_base;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.to_base && ToBase())
                    {
                        curent_mode = mode.dock;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.dock && Dock())
                    {
                        Clear();
                        curent_programm = programm.none;
                        SaveToStorage();
                    }
                }
                else
                {
                    Message = String.Format("Точка {0}->{1} - неопределена!", CurrDockPoint, NextDockPoint);
                    curent_programm = programm.none;
                    SaveToStorage();
                }
            }
            public void Fly_BP_UP()
            {
                Message = "";
                if (IsCorrectBasePoints(CurrDockPoint))
                {
                    if (curent_mode == mode.none)
                    {
                        curent_mode = mode.un_dock;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.un_dock && UnDock())
                    {
                        curent_mode = mode.planet_up;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.planet_up && PlanetUp())
                    {
                        Clear();
                        curent_programm = programm.none;
                        SaveToStorage();
                    }
                }
                else
                {
                    Message = String.Format("Точка {0} - неопределена!", CurrDockPoint);
                    curent_programm = programm.none;
                    SaveToStorage();
                }

            }
            public void Fly_Down_BP()
            {
                Message = "";
                if (IsCorrectBasePoints(CurrDockPoint) && IsCorrectBasePoints(NextDockPoint))
                {
                    if (curent_mode == mode.none)
                    {
                        curent_mode = mode.to_base;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.to_base && ToBase())
                    {
                        curent_mode = mode.planet_down;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.planet_down && PlanetDown())
                    {
                        curent_mode = mode.dock;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.dock && Dock())
                    {
                        Clear();
                        curent_programm = programm.none;
                        SaveToStorage();
                    }
                }
                else
                {
                    Message = String.Format("Точка {0}->{1} - неопределена!", CurrDockPoint, NextDockPoint);
                    curent_programm = programm.none;
                    SaveToStorage();
                }
            }
            public void Fly_BP_BP()
            {
                Message = "";
                if (IsCorrectBasePoints(CurrDockPoint) && IsCorrectBasePoints(NextDockPoint))
                {
                    if (curent_mode == mode.none) { curent_mode = mode.un_dock; SaveToStorage(); }
                    if (curent_mode == mode.un_dock && UnDock()) { curent_mode = mode.to_base; SaveToStorage(); }
                    if (curent_mode == mode.to_base && ToBase()) { curent_mode = mode.dock; SaveToStorage(); }
                    if (curent_mode == mode.dock && Dock()) { Clear(); curent_programm = programm.none; SaveToStorage(); }
                }
                else
                {
                    Message = String.Format("Точка {0}->{1} - неопределена!", CurrDockPoint, NextDockPoint);
                    curent_programm = programm.none;
                    SaveToStorage();
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
                EmergencyReturn = bats.CurrentPersent() <= ReturnOnCharge || PhysicalMass >= CriticalMass;
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
                SaveToStorage();
            }
            public void ClearThrust()
            {
                thrusts.ClearThrustOverridePersent();
                gyros.SetOverride(false, 1);
                SaveToStorage();
            }
            public void Clear()
            {
                ClearThrust();
                curent_mode = mode.none;
                SaveToStorage();
            }
            public void Stop()
            {
                thrusts.ClearThrustOverridePersent();
                gyros.SetOverride(false, 1);
                curent_mode = mode.none;
                curent_programm = programm.none;
                curse = false;
                go_home = false;
                paused = false;
                reflectors_light.Off();
                SaveToStorage();
            }
            public bool ToBase()
            {
                bool Complete = false;
                Message = "";
                if (IsCorrectBasePoints(NextDockPoint))
                {
                    float MaxUSpeed, MaxFSpeed;
                    Vector3D gyrAng = GetNavAngles(PointsDock[NextDockPoint].BaseDockPoint, PointsDock[NextDockPoint].DockMatrix);
                    Vector3D MyPosCon = Vector3D.Transform(MyPos, PointsDock[NextDockPoint].DockMatrix);
                    Distance = (float)(PointsDock[NextDockPoint].BaseDockPoint - new Vector3D(MyPosCon.GetDim(0), 0, MyPosCon.GetDim(2))).Length();
                    MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(PointsDock[NextDockPoint].FlyHeight - (MyPos - PlanetCenter).Length()) * YMaxA) / 1.2f;
                    MaxFSpeed = (float)Math.Sqrt(2 * Distance * ZMaxA) / 1.2f;
                    gyros.SetOverride(true, gyrAng * GyroMult, 1);
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                    if (UpVelocityVector.Length() < MaxUSpeed)
                        thrusts.SetOverrideAccel("U", (float)((PointsDock[NextDockPoint].FlyHeight - (MyPos - PlanetCenter).Length()) * AlignAccelMult));
                    else
                    {
                        thrusts.SetOverridePercent("U", 0);
                        thrusts.SetOverridePercent("D", 0);
                    }
                    if (Distance > BaseDistance)
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
                else { Message = String.Format("Точка {0} - неопределена!", NextDockPoint); }
                return Complete;
            }
            public bool Dock()
            {
                bool Complete = false;
                Message = "";
                if (IsCorrectBasePoints(NextDockPoint))
                {
                    float MaxUSpeed, MaxLSpeed, MaxFSpeed;
                    Vector3D MyPosCon = Vector3D.Transform(MyPos, PointsDock[NextDockPoint].DockMatrix);
                    Vector3D gyrAng = GetNavAngles(MyPosCon * 2 - PointsDock[NextDockPoint].ConnectorPoint, PointsDock[NextDockPoint].DockMatrix);
                    //Vector3D gyrAng = GetNavAngles(ConnectorPoint, DockMatrix);
                    Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, PointsDock[NextDockPoint].DockMatrix)))).Length() + PointsDock[NextDockPoint].ConnectorPoint.Length());
                    MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(0)) * XMaxA) / 3;
                    MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(1)) * YMaxA) / 2;
                    MaxFSpeed = (float)Math.Sqrt(2 * Distance * ZMaxA) / 3;
                    if (Distance < Conn_Distance + 40)
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
                        thrusts.SetOverrideAccel("B", (float)(Distance * AlignAccelMult));
                        thrusts.SetOverridePercent("F", 0);
                    }
                    else
                    {
                        thrusts.SetOverridePercent("F", 0);
                        thrusts.SetOverridePercent("B", 0);
                    }
                    if (Distance < Conn_Distance + 20)
                    {
                        if (connector.Status == MyShipConnectorStatus.Connectable)
                        {
                            connector.obj.Connect();
                        }
                        if (connector.Status == MyShipConnectorStatus.Connected)
                        {
                            thrusts.ClearThrustOverridePersent();
                            gyros.SetOverride(false, 1);
                            curent_mode = mode.none;
                            m_forw = false;
                            m_up = false;
                            m_up_gears = false;
                            Complete = true;
                        }
                    }
                    OutStatusMode(MaxFSpeed, MaxUSpeed, MaxLSpeed);
                }
                else { Message = String.Format("Точка {0} - неопределена!", NextDockPoint); }
                return Complete;
            }
            public bool UnDock()
            {
                bool Complete = false;
                Message = "";
                if (IsCorrectBasePoints(CurrDockPoint) || landing_gears.IsLocked())
                {
                    //Distance = 0;
                    Vector3D MyPosCon = Vector3D.Transform(MyPos, PointsDock[CurrDockPoint].DockMatrix);
                    Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, PointsDock[CurrDockPoint].DockMatrix)))).Length() + PointsDock[CurrDockPoint].ConnectorPoint.Length());
                    if ((Distance > BaseDistance && !connector.Connected && !connector_down.Connected && !landing_gears.IsLocked()) || (m_up_gears && !connector.Connected && !connector_down.Connected && !landing_gears.IsLocked()))
                    {
                        thrusts.ClearThrustOverridePersent();
                        gyros.SetOverride(false, 1);
                        m_forw = false;
                        m_up = false;
                        m_up_gears = false;
                        if (cockpit.obj.GetShipSpeed() < 0.1f)
                        {
                            curent_mode = mode.none;
                            Complete = true;
                        }
                    }
                    if (connector.Connected)
                    {
                        connector.obj.Disconnect();
                        m_forw = true;
                    }
                    else
                    {
                        if (landing_gears.IsLocked())
                        {
                            landing_gears.Unlock();
                            m_up_gears = true;
                        }
                        if (connector_down.Connected)
                        {
                            connector_down.obj.Disconnect();
                            m_up = true;
                        }
                    }
                    if (!connector.Connected && m_forw)
                    {
                        //Vector3D MyPosCon = Vector3D.Transform(MyPos, PointsDock[CurrDockPoint].DockMatrix);
                        //Vector3D gyrAng = GetNavAngles(ConnectorPoint, DockMatrix);
                        Vector3D gyrAng = GetNavAngles(MyPosCon * 2 - PointsDock[CurrDockPoint].ConnectorPoint, PointsDock[CurrDockPoint].DockMatrix);
                        //Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, PointsDock[CurrDockPoint].DockMatrix)))).Length() + PointsDock[CurrDockPoint].ConnectorPoint.Length());
                        gyros.SetOverride(true, gyrAng * GyroMult, 1);
                        thrusts.SetOverridePercent("U", 0);
                        thrusts.SetOverridePercent("D", 0);
                        thrusts.SetOverridePercent("R", 0);
                        thrusts.SetOverridePercent("L", 0);
                        thrusts.SetOverrideAccel("F", 10);
                        thrusts.SetOverridePercent("B", 0);
                        //if (Distance > BaseDistance)
                        //{
                        //    thrusts.SetOverrideAccel("F", 0);
                        //    m_forw = false;
                        //    m_up = false;
                        //    Complete = true;
                        //}
                    }
                    if ((m_up || m_up_gears) && (!landing_gears.IsLocked() && !connector_down.Connected))
                    {

                    }
                    OutStatusMode(0, 0, 0);
                }
                else { Message = String.Format("Точка {0} - неопределена!", CurrDockPoint); }
                return Complete;
            }
            public bool PlanetUp()
            {
                bool Complete = false;

                if (!landing_gears.IsLocked() && !connector.Connected && !connector_down.Connected)
                {

                }

                if (gravity)
                {
                    thrusts.On();
                    Vector3D gyrAng = GetNavAngles(TackVector);
                    gyros.SetOverride(true, gyrAng * GyroMult, 1);
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                    thrusts.SetOverridePercent("F", 0);
                    thrusts.SetOverridePercent("B", 0);
                    if (UpVelocityVector.Length() < 200)
                        thrusts.SetOverridePercent("U", 1.0f);
                    else
                    {
                        thrusts.SetOverrideAccel("U", 0);
                    }
                }
                else
                {
                    ClearThrust();
                    if (cockpit.obj.GetShipSpeed() < 0.1f)
                    {
                        thrusts.Off();
                        curent_mode = mode.none;
                        Complete = true;
                    }
                }
                return Complete;
            }
            public bool PlanetDown()
            {
                bool Complete = false;
                thrusts.SetOverridePercent("R", 0);
                thrusts.SetOverridePercent("L", 0);
                thrusts.SetOverridePercent("F", 0);
                thrusts.SetOverridePercent("B", 0);
                if (gravity)
                {
                    Vector3D gyrAng = GetNavAngles(TackVector);
                    gyros.SetOverride(true, gyrAng * GyroMult, 1);
                    if ((cockpit.CurrentHeight - GetBrakingLanding(thrusts.DownThrMax)) < MinHeight)
                    {
                        thrusts.SetOverridePercent("U", 0);
                        thrusts.SetOverridePercent("D", 0);
                        thrusts.On();
                        ClearThrust();
                        if (cockpit.obj.GetShipSpeed() < 0.1f)
                        {
                            curent_mode = mode.none;
                            Complete = true;
                        }
                    }
                    else
                    {
                        if (UpVelocityVector.Length() < 200)
                        {
                            thrusts.On();
                            thrusts.SetOverridePercent("D", 1.0f);
                        }

                        else
                        {
                            thrusts.SetOverridePercent("D", 0f);
                            thrusts.Off();
                        }
                    }
                }
                else
                {
                    if (UpVelocityVector.Length() < 200)
                    {
                        thrusts.On();
                        thrusts.SetOverridePercent("D", 1.0f);
                    }
                    else
                    {
                        thrusts.SetOverridePercent("D", 0f);
                        thrusts.Off();
                    }
                }
                return Complete;
            }
            public bool Curse()
            {
                bool Complete = false;
                dist_curse = null;
                thrusts.SetOverridePercent("R", 0);
                thrusts.SetOverridePercent("L", 0);
                thrusts.SetOverridePercent("U", 0);
                thrusts.SetOverridePercent("D", 0);
                Vector3D gyrAng = GetNavAngles(TackVector);
                gyros.SetOverride(true, gyrAng * GyroMult, 1);
                double brak_dist = GetBrakingLanding(thrusts.ForwardThrMax) + MinDistance;
                info_scan = camera_course.Raycast(1500f, 0f, 0f);
                if (info_scan != null)
                {
                    Vector3D? HitPosition = ((MyDetectedEntityInfo)info_scan).HitPosition;
                    if (HitPosition != null)
                    {
                        dist_curse = ((Vector3D)HitPosition - camera_course.obj.GetPosition()).Length();
                    }
                }
                if (dist_curse != null && brak_dist >= dist_curse)
                {
                    thrusts.On();
                    thrusts.SetOverridePercent("F", 0);
                    thrusts.SetOverridePercent("B", 0);
                    if (cockpit.obj.GetShipSpeed() < 0.1f)
                    {
                        gyros.SetOverride(false, 1);
                        thrusts.Off();
                        Complete = true;
                    }
                }
                else
                {
                    if (cockpit.obj.GetShipSpeed() < 100f)
                    {
                        thrusts.On();
                        thrusts.SetOverridePercent("F", 1f);
                        thrusts.SetOverridePercent("B", 0f);
                    }
                    else
                    {
                        thrusts.SetOverridePercent("F", 0f);
                        thrusts.SetOverridePercent("B", 0f);
                        thrusts.Off();
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
                Vector3D MyPosPoint = Vector3D.Transform(MyPos, PointsDock[CurrDockPoint].DockMatrix);
                values.Append("My_Length   : " + Math.Round(MyPosPoint.Length(), 2) + "\n");
                values.Append("MyPosDrill[0]   : " + Math.Round(MyPosPoint.GetDim(0), 2) + "\n");
                values.Append("MyPosDrill[1]   : " + Math.Round(MyPosPoint.GetDim(1), 2) + "\n");
                values.Append("MyPosDrill[2]   : " + Math.Round(MyPosPoint.GetDim(2), 2) + "\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("DeltaHeight: " + Math.Round(PointsDock[CurrDockPoint].FlyHeight - (MyPos - PlanetCenter).Length()).ToString() + "\n");
                values.Append("Distance: " + Math.Round(Distance).ToString() + "\n");
                values.Append("------------------------------------------\n");
                values.Append("ZMaxA (F-B) : " + Math.Round(ZMaxA, 2).ToString() + "MaxFSpeed: " + Math.Round(MaxFSpeed, 2).ToString() + "\n");
                values.Append("YMaxA (U-D) : " + Math.Round(YMaxA, 2).ToString() + "MaxUSpeed: " + Math.Round(MaxUSpeed, 2).ToString() + "\n");
                values.Append("XMaxA (L-R) : " + Math.Round(XMaxA, 2).ToString() + "MaxLSpeed: " + Math.Round(MaxLSpeed, 2).ToString() + "\n");
                values.Append("------------------------------------------\n");
                values.Append("UpVelocityVector   : " + Math.Round(UpVelocityVector.Length(), 2) + "\n");
                values.Append("MaxUSpeed   : " + Math.Round(MaxUSpeed, 2) + "\n");
                //values.Append(thrusts.TextInfo());
                //lcd_debug.OutText(values);
            }
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                curent_programm = (programm)mystorage.GetValInt("curent_programm", str.ToString());
                curent_mode = (mode)mystorage.GetValInt("curent_mode", str.ToString());
                paused = mystorage.GetValBool("pause", str.ToString());
                go_home = mystorage.GetValBool("go_home", str.ToString());
                EmergencyReturn = mystorage.GetValBool("EmergencyReturn", str.ToString());
                CurrDockPoint = mystorage.GetValInt("CurrDockPoint", str.ToString());
                NextDockPoint = mystorage.GetValInt("NextDockPoint", str.ToString());
                for (int i = 0; i < 10; i++)
                {
                    PointsDock[i].DockMatrix = new MatrixD(mystorage.GetValDouble("PD_DM" + i + "_11", str.ToString()), mystorage.GetValDouble("PD_DM" + i + "_12", str.ToString()), mystorage.GetValDouble("PD_DM" + i + "_13", str.ToString()), mystorage.GetValDouble("PD_DM" + i + "_14", str.ToString()),
                        mystorage.GetValDouble("PD_DM" + i + "_21", str.ToString()), mystorage.GetValDouble("PD_DM" + i + "_22", str.ToString()), mystorage.GetValDouble("PD_DM" + i + "_23", str.ToString()), mystorage.GetValDouble("PD_DM" + i + "_24", str.ToString()),
                        mystorage.GetValDouble("PD_DM" + i + "_31", str.ToString()), mystorage.GetValDouble("PD_DM" + i + "_32", str.ToString()), mystorage.GetValDouble("PD_DM" + i + "_33", str.ToString()), mystorage.GetValDouble("PD_DM" + i + "_34", str.ToString()),
                        mystorage.GetValDouble("PD_DM" + i + "_41", str.ToString()), mystorage.GetValDouble("PD_DM" + i + "_42", str.ToString()), mystorage.GetValDouble("PD_DM" + i + "_43", str.ToString()), mystorage.GetValDouble("PD_DM" + i + "_44", str.ToString()));
                    PointsDock[i].BaseDockPoint = new Vector3D(mystorage.GetValDouble("PD_BDP_" + i + "_X", str.ToString()), mystorage.GetValDouble("PD_BDP_" + i + "_Y", str.ToString()), mystorage.GetValDouble("PD_BDP_" + i + "_Z", str.ToString()));
                    PointsDock[i].ConnectorPoint = new Vector3D(mystorage.GetValDouble("PD_CP_" + i + "_X", str.ToString()), mystorage.GetValDouble("PD_CP_" + i + "_Y", str.ToString()), mystorage.GetValDouble("PD_CP_" + i + "_Z", str.ToString()));
                    PointsDock[i].FlyHeight = mystorage.GetValDouble("PD_FlyHeight_" + i, str.ToString());
                    PointsDock[i].EntityId = mystorage.GetValInt64("PD_EntityId_" + i, str.ToString());
                    PointsDock[i].Name = mystorage.GetValString("PD_Name_" + i, str.ToString());
                    PointsDock[i].gravity = mystorage.GetValBool("PD_gravity_" + i, str.ToString());
                }
                WorkMatrix = new MatrixD(mystorage.GetValDouble("WM11", str.ToString()), mystorage.GetValDouble("WM12", str.ToString()), mystorage.GetValDouble("WM13", str.ToString()), mystorage.GetValDouble("WM14", str.ToString()),
                mystorage.GetValDouble("WM21", str.ToString()), mystorage.GetValDouble("WM22", str.ToString()), mystorage.GetValDouble("WM23", str.ToString()), mystorage.GetValDouble("WM24", str.ToString()),
                mystorage.GetValDouble("WM31", str.ToString()), mystorage.GetValDouble("WM32", str.ToString()), mystorage.GetValDouble("WM33", str.ToString()), mystorage.GetValDouble("WM34", str.ToString()),
                mystorage.GetValDouble("WM41", str.ToString()), mystorage.GetValDouble("WM42", str.ToString()), mystorage.GetValDouble("WM43", str.ToString()), mystorage.GetValDouble("WM44", str.ToString()));
                PlanetCenter = new Vector3D(mystorage.GetValDouble("PX", str.ToString()), mystorage.GetValDouble("PY", str.ToString()), mystorage.GetValDouble("PZ", str.ToString()));
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                values.Append("curent_programm: " + ((int)curent_programm).ToString() + ";\n");
                values.Append("curent_mode: " + ((int)curent_mode).ToString() + ";\n");
                // values.Append("cargo_mode: " + ((int)cargo_components.curent_mode).ToString() + ";\n");
                values.Append("pause: " + paused.ToString() + ";\n");
                values.Append("go_home: " + go_home.ToString() + ";\n");
                //values.Append("FlyHeight: " + Math.Round(FlyHeight, 0) + ";\n");
                values.Append("EmergencyReturn: " + EmergencyReturn.ToString() + ";\n");
                values.Append("CurrDockPoint: " + CurrDockPoint.ToString() + ";\n");
                values.Append("NextDockPoint: " + NextDockPoint.ToString() + ";\n");
                for (int i = 0; i < 10; i++)
                {
                    values.Append(PointsDock[i].DockMatrix.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "PD_DM" + i + "_"));
                    values.Append(PointsDock[i].BaseDockPoint.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PD_BDP_" + i + "_X").Replace("Y", "PD_BDP_" + i + "_Y").Replace("Z", "PD_BDP_" + i + "_Z") + ";\n");
                    values.Append(PointsDock[i].ConnectorPoint.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PD_CP_" + i + "_X").Replace("Y", "PD_CP_" + i + "_Y").Replace("Z", "PD_CP_" + i + "_Z") + ";\n");
                    values.Append("PD_FlyHeight_" + i + ": " + Math.Round(PointsDock[i].FlyHeight, 0) + ";\n");
                    values.Append("PD_EntityId_" + i + ": " + PointsDock[i].EntityId.ToString() + ";\n");
                    values.Append("PD_Name_" + i + ": " + PointsDock[i].Name + ";\n");
                    values.Append("PD_gravity_" + i + ": " + PointsDock[i].gravity.ToString() + ";\n");
                }
                // values.Append(DockMatrix.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "DM"));
                values.Append(WorkMatrix.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "WM"));
                values.Append(PlanetCenter.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PX").Replace("Y", "PY").Replace("Z", "PZ") + ";\n");
                lcd_storage.OutText(values);
            }
            public string TextInfo1()
            {
                StringBuilder values = new StringBuilder();
                values.Append("СКОРОСТЬ    : " + Math.Round(cockpit.obj.GetShipSpeed(), 2) + "\n");
                values.Append("ВЫСОТА    : " + Math.Round(cockpit.CurrentHeight, 2) + ", Sт : " + Math.Round(S, 2) + "\n");
                //values.Append("ВЫСОТА    : " + Math.Round(cockpit.CurrentHeight, 2) + "\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("ПАУЗА : " + (paused ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("ДОМОЙ : " + (go_home ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("ГОРИЗОНТ    : " + (horizont ? igreen.ToString() : ired.ToString()) + "\n");
                return values.ToString();
            }
            public string TextInfo2()
            {
                StringBuilder values = new StringBuilder();
                values.Append("ВЫСОТА (Цен.план.): " + Math.Round((MyPos - PlanetCenter).Length()).ToString() + " / " + Math.Round(PointsDock[CurrDockPoint].FlyHeight).ToString() + "\n");
                values.Append("ДИСТАНЦИЯ         : " + Math.Round(Distance).ToString() + "\n");
                values.Append("--------------------------------------\n");
                values.Append("АВАРИЙНЫЙ ВОЗВРАТ   : " + (EmergencyReturn ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("|-ФИЗ./КРИТ.(МАССА) : " + Math.Round(PhysicalMass).ToString() + " / " + CriticalMass + " " + (CriticalMassReached ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("|-БАТАРЕЯ %         : " + PText.GetPersent(bats.CurrentPersent()) + " " + (bats.CurrentPersent() <= ReturnOnCharge ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("--------------------------------------\n");

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
                values.Append("=[P:" + panel + "]==[ СПИСОК БАЗ ]==============\n");
                values.Append((IsCorrectBasePoints(CurrDockPoint) ? igreen.ToString() : ired.ToString()) + "БАЗА #: " + CurrDockPoint.ToString() + " Id:" + PointsDock[CurrDockPoint].EntityId + "\n");
                values.Append(PointsDock[CurrDockPoint].Name + "\n");
                values.Append("------------------------------------------------\n");
                values.Append((!vdm ? ired.ToString() : igreen.ToString()) + PText.GetGPSMatrixD("DockMatrix:", PointsDock[CurrDockPoint].DockMatrix) + "\n");
                values.Append((PointsDock[CurrDockPoint].BaseDockPoint.IsZero() ? ired.ToString() : igreen.ToString()) + PText.GetGPS("DockPoint:", PointsDock[CurrDockPoint].BaseDockPoint) + "\n");
                values.Append((PointsDock[CurrDockPoint].ConnectorPoint.IsZero() ? ired.ToString() : igreen.ToString()) + PText.GetGPS("ConnectorPoint:", PointsDock[CurrDockPoint].ConnectorPoint) + "\n");
                values.Append((!vf ? ired.ToString() : igreen.ToString()) + "БЕЗОП. ВЫСОТА : " + Math.Round(PointsDock[CurrDockPoint].FlyHeight).ToString() + "\n");
                values.Append("ГРАВИТАЦИЯ: " + (PointsDock[CurrDockPoint].gravity ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("------------------------------------------------\n");
                values.Append("=[1:P+]=[3:M+]=[5:S]=[7:H]=[9:Clear]==============\n");
                return values.ToString();
            }
            public string TextProgramm()
            {
                StringBuilder values = new StringBuilder();
                values.Append("=[P:" + panel + "]==[ МАРШРУТЫ ]==============\n");
                values.Append((IsCorrectBasePoints(CurrDockPoint) ? igreen.ToString() : ired.ToString()) + "БАЗА ТЕКУЩАЯ#    : " + CurrDockPoint.ToString() + "\n");
                values.Append((IsCorrectBasePoints(NextDockPoint) ? igreen.ToString() : ired.ToString()) + "БАЗА НАЗНАЧЕНИЯ# : " + NextDockPoint.ToString() + "\n");
                values.Append("МАРШРУТ: " + name_programm[route] + "\n");
                values.Append("-----------------------------------------------\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("ПАУЗА       : " + (paused ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("-----------------------------------------------\n");
                values.Append("ГРАВИТАЦИЯ          : " + Math.Round(GravVector.Length(), 2) + "\n");
                values.Append("СКОРОСТЬ            : " + Math.Round(cockpit.obj.GetShipSpeed(), 2) + "\n");
                values.Append("ВЫСОТА (над.пл)     : " + Math.Round(cockpit.CurrentHeight, 2) + ", Sт : " + Math.Round(S, 2) + "\n");
                values.Append("ВЫСОТА (от цен.пл.) : " + Math.Round((MyPos - PlanetCenter).Length()).ToString() + " / " + Math.Round(PointsDock[NextDockPoint].FlyHeight).ToString() + "\n");
                values.Append("ДИСТАНЦИЯ           : " + Math.Round(Distance).ToString() + "\n");
                values.Append("----------------------------------------------\n");
                values.Append("ФИЗ./КРИТ.(МАССА) : " + Math.Round(PhysicalMass).ToString() + " / " + CriticalMass + " " + (CriticalMassReached ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("ERROR: " + Message + "\n");
                values.Append("=[1:P+]=[3:N+]=[5:P+]=[7:Start]=[9:Stop]======\n");
                return values.ToString();
            }
            public string TextScaner()
            {
                StringBuilder values = new StringBuilder();
                values.Append("=[P:" + panel + "]==[ СКАНЕР ]========================\n");
                values.Append("СКАНИРОВАТЬ   : " + (scaning ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("ДИСТАНЦИЯ СК. : " + Math.Round(dist_scan).ToString() + "\n");
                values.Append("------------------------------------------------------\n");
                values.Append(camera_course.GetTextDetectedEntityInfo(info_scan).ToString() + "\n");
                values.Append("------------------------------------------------------\n");
                values.Append("ERROR: " + Message + "\n");
                values.Append("=[1:P+]=[3:Д+]=[5:Д-]=[7:Start]=[9:Stop]==============\n");
                return values.ToString();
            }
            public string TextCurse()
            {
                StringBuilder values = new StringBuilder();
                values.Append("=[P:" + panel + "]==[ ПОЛЕТЫ ПО КУРСУ ]===============\n");
                values.Append("ПОЛЕТ     : " + (curse ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append(TackVector != null ? PText.GetGPS("КУРС      : ", (Vector3D)TackVector) : "КУРС      : " + "\n");//
                values.Append("Name         : " + (info_scan != null ? ((MyDetectedEntityInfo)info_scan).Name : "") + "\n");
                values.Append("Type         : " + (info_scan != null ? ((MyDetectedEntityInfo)info_scan).Type : MyDetectedEntityType.None) + "\n");
                values.Append("ДИСТАНЦИЯ : " + (dist_curse != null ? Math.Round((double)dist_curse).ToString() : "") + ", Sт : " + Math.Round(S, 2) + "\n");
                values.Append("-----------------------------------------------\n");
                values.Append("ГРАВИТАЦИЯ : " + Math.Round(GravVector.Length(), 2) + "\n");
                values.Append("СКОРОСТЬ   : " + Math.Round(cockpit.obj.GetShipSpeed(), 2) + "\n");
                values.Append("ПАУЗА      : " + (paused ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("------------------------------------------------------\n");
                values.Append("ERROR: " + Message + "\n");
                values.Append("=[1:P+]=[3:Курс]=[5:Камера]=[7:Start]=[9:Stop]==============\n");
                return values.ToString();
            }
            public string TextPanel()
            {
                switch (panel)
                {
                    case 0: { return TextPointsDock(); }
                    case 1: { return TextProgramm(); }
                    case 2: { return TextScaner(); }
                    case 3: { return TextCurse(); }
                    default: return "";
                }
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "horizont": if (curent_programm == programm.none) { if (horizont) { horizont = false; } else { horizont = true; } } break;
                    case "scaning": if (curent_programm == programm.none) { if (scaning) { scaning = false; } else { scaning = true; } } break;
                    case "curse": if (curent_programm == programm.none) { if (curse) { curse = false; } else { curse = true; } } break;
                    case "load":
                        LoadFromStorage();
                        break;
                    case "save":
                        SaveToStorage();
                        break;
                    case "pause":
                        Pause(!paused);
                        break;
                    case "stop":
                        Stop();
                        break;
                    case "clear":
                        Clear();
                        curent_programm = programm.none;
                        SaveToStorage();
                        break;
                    case "save_height":
                        SetFlyHeight();
                        break;
                    case "save_base": SetDockMatrix(connector); break;
                    case "fly_bp_bs": curent_programm = programm.fly_bp_bs; SaveToStorage(); break;
                    case "fly_bs_bp": curent_programm = programm.fly_bs_bp; SaveToStorage(); break;
                    case "fly_bp_up": curent_programm = programm.fly_bp_up; SaveToStorage(); break;
                    case "fly_down_bp": curent_programm = programm.fly_down_bp; SaveToStorage(); break;
                    case "fly_bp_bp": curent_programm = programm.fly_bp_bp; SaveToStorage(); break;
                    case "go_home":
                        {
                            go_home = true;
                            break;
                        }
                    case "to_base": curent_mode = mode.to_base; SaveToStorage(); break;
                    case "dock": curent_mode = mode.dock; SaveToStorage(); break;
                    case "un_dock": curent_mode = mode.un_dock; SaveToStorage(); break;
                    case "planet_up": curent_mode = mode.planet_up; SaveToStorage(); break;
                    case "planet_down": curent_mode = mode.planet_down; SaveToStorage(); break;
                    case "P+": panel++; if (panel > 3) panel = 0; SaveToStorage(); break;
                    case "P-": panel--; if (panel < 0) panel = 3; SaveToStorage(); break;
                    case "1": panel++; if (panel > 3) panel = 0; SaveToStorage(); SaveToStorage(); break;
                    case "3":
                        if (panel == 0) { CurrDockPoint++; if (CurrDockPoint > 9) CurrDockPoint = 0; }
                        if (panel == 1) { if (curent_mode == mode.none && curent_programm == programm.none) { NextDockPoint++; if (NextDockPoint > 9) NextDockPoint = 0; } }
                        if (panel == 2) { dist_scan += 500; if (dist_scan > 10000f) dist_scan = 500f; }
                        if (panel == 3) { TackVector = camera_course.obj.WorldMatrix.Forward; }
                        SaveToStorage();
                        break;
                    case "5":
                        if (panel == 0) { SetDockMatrix(connector); }
                        if (panel == 1) { if (curent_mode == mode.none && curent_programm == programm.none) { route++; if (route > 5) route = 0; } }
                        if (panel == 2) { dist_scan -= 500; if (dist_scan < 500f) dist_scan = 10000f; }
                        //if (panel == 3) { camera_course.obj.ApplyAction("View"); }
                        SaveToStorage();
                        break;
                    case "7":
                        if (panel == 0) { SetFlyHeight(); }
                        if (panel == 1) { curent_programm = (programm)route; }
                        if (panel == 2) { if (curent_programm == programm.none) { { scaning = true; } } break; }
                        if (panel == 3) { if (curent_programm == programm.none && curent_mode == mode.none) { { curse = true; scaning = false; } } break; }
                        SaveToStorage();
                        break;
                    case "9":
                        if (panel == 0) { }
                        if (panel == 1) { Stop(); }
                        if (panel == 2) { scaning = false; ; }
                        if (panel == 3) { thrusts.On(); Stop(); }
                        SaveToStorage();
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    cockpit.Logic(argument, updateSource);
                    if (!connector_down.Connected && !connector.Connected && !landing_gears.IsLocked())
                    {
                        if (gravity && cockpit.CurrentHeight > 15.0f)
                        {
                            bats.Auto();
                            thrusts.On();
                        }
                    }
                    else
                    {
                        // Припаркован
                        int? index = null;
                        if (connector.Connected)
                        {
                            index = GetCurrConnectorDock(connector);
                        }
                        if (connector_down.Connected)
                        {
                            index = GetCurrConnectorDock(connector_down);
                        }
                        if (index == -1) { index = Array.FindIndex(PointsDock, element => element.EntityId == 0); }
                        if (index >= 0)
                        {
                            CurrDockPoint = (int)index;
                        }
                        reflectors_light.Off();
                        bats.Charger();
                        thrusts.Off();
                    }
                    if (clock >= 10)
                    {
                        clock = 0;
                        if (panel == 2 && scaning && curent_programm == programm.none)
                        {
                            info_scan = camera_course.Raycast(dist_scan, 0f, 0f);
                        }
                        else
                        {
                            info_scan = null;
                        }
                    }
                    clock++;
                    UpdateCalc();
                    if (EmergencyReturn) lightings.On(); else lightings.Off();
                    if (curent_programm == programm.none)
                    {
                        if (horizont) { Horizon(); } else { gyros.SetOverride(false, 1); }

                        if (curent_mode == mode.none && curse && TackVector != null && !paused)
                        {
                            if (Curse())
                            {
                                curse = false; TackVector = null;
                            }
                        }

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
                        if (curent_mode == mode.planet_up && !paused)
                        {
                            if (PlanetUp() && curent_programm == programm.none)
                            {
                                curent_mode = mode.none;
                            }
                        }
                        if (curent_mode == mode.planet_down && !paused)
                        {
                            if (PlanetDown() && curent_programm == programm.none)
                            {
                                curent_mode = mode.none;
                            }
                        }
                    }
                    if (curent_programm == programm.fly_bp_bs && !paused) { Fly_BP_BS(); }
                    if (curent_programm == programm.fly_bs_bp && !paused) { Fly_BS_BP(); }
                    if (curent_programm == programm.fly_bp_up && !paused) { Fly_BP_UP(); }
                    if (curent_programm == programm.fly_down_bp && !paused) { Fly_Down_BP(); }
                    if (curent_programm == programm.fly_bp_bp && !paused) { Fly_BP_BP(); }
                }
            }
        }
        public class MyStorage
        {
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
