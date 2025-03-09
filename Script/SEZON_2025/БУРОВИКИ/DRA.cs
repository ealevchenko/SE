using Newtonsoft.Json.Linq;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRage.Noise.Combiners;
using VRage.Scripting;
using VRageMath;

/// <summary>
///  БУРОВАЯ УСТАНОВКА (v1.09-03-2025)
///  Модифицирован под Down и Up ускорители (применим на малой гравитации, можно на ионниках)
/// </summary>
namespace DrillingRig
{
    public sealed class Program : MyGridProgram
    {
        string NameObj = "[DRA-D1-01]";
        static string tag_nav = "[nav]";
        static string tag_ejector = "[out]";
        //static float safe_base_height = 300f;   // безопасная высота
        static float safe_base_distance = 100f; // безопасная дистанция
        static float safe_point_distance = 100f; // безопасная дистанция до точки

        static float GyroMult = 10f;
        static float DrillGyroMult = 10f;
        static float AlignAccelMult = 0.6f;
        static float MinUDSpeed = 0.5f;
        static float MinUDAccel = 0.5f;

        static float MinOnCharge = 0.2f;     // Процент заряда
        static float MaxOffCharge = 0.9f;    // Процент заряда
        static float DrillSpeedLimit = 0.5f;
        static float DrillAccel = 0.5f;
        static int CriticalMass = 150000;       // Критическая масса
        static int StoneDumpOn = 250000;

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';
        static LCD lcd_storage;
        static LCD lcd_debug;
        static LCD lcd_name;
        static LCD lcd_work1, lcd_work2;
        static LCD lcd_test1, lcd_test2, lcd_test3;
        static Batterys bats;
        static Connector connector;
        static ReflectorsLight reflectors_light;
        static BaseShipController cockpit;
        static Gyros gyros;
        static Thrusts thrusts;
        static Cargos cargos;
        static GasGenerator gas_gen;
        static HydrogenEngines h2_engines;
        static MotorStator ms_lsp;
        static MotorStator ms_rsp;
        static Joint jn_lsp;
        static Joint jn_rsp;
        static TurretControlBlock tcb_lsp;
        static TurretControlBlock tcb_rsp;
        static Nav nav;
        static Upr upr;
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
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            lcd_name = new LCD(NameObj + "-LCD-Name");
            lcd_work1 = new LCD(NameObj + "-LCD-Work 1");
            lcd_work2 = new LCD(NameObj + "-LCD-Work 2");
            lcd_test1 = new LCD(NameObj + "-test1");
            lcd_test2 = new LCD(NameObj + "-test2");
            lcd_test3 = new LCD(NameObj + "-test3");
            bats = new Batterys(NameObj);
            connector = new Connector(NameObj + "-Коннектор [back]");
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            cockpit = new BaseShipController(NameObj, tag_nav);
            gyros = new Gyros(NameObj);
            thrusts = new Thrusts(NameObj);
            cargos = new Cargos(NameObj);
            gas_gen = new GasGenerator(NameObj); gas_gen.Off();
            h2_engines = new HydrogenEngines(NameObj); h2_engines.Off();
            ms_lsp = new MotorStator(NameObj + "-Ротор [sp-l]");
            ms_rsp = new MotorStator(NameObj + "-Ротор [sp-r]");
            jn_lsp = new Joint(NameObj + "-Шарнир [sp-l]");
            jn_rsp = new Joint(NameObj + "-Шарнир [sp-r]");
            tcb_lsp = new TurretControlBlock(NameObj + "-Кон. турели [sp-l]");
            tcb_rsp = new TurretControlBlock(NameObj + "-Кон. турели [sp-r]");
            upr = new Upr();
            nav = new Nav(NameObj);
            strg = new MyStorage();
            strg.LoadFromStorage();
        }
        public void Save() { }
        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder values_info = new StringBuilder();
            upr.Logic(argument, updateSource);
            nav.Logic(argument, updateSource);
            switch (argument) { default: break; }
            if (updateSource == UpdateType.Update10)
            {
                lcd_name.OutText(NameObj, false);
            }
            values_info.Append(bats.TextInfo(null));
            values_info.Append(connector.TextInfo("К:Base"));
            values_info.Append(nav.TextInfo1());
            values_info.Append(upr.TextInfo1());
            cockpit.OutText(values_info, 0);
            StringBuilder values_info1 = new StringBuilder();
            values_info1.Append(nav.TextCritical());
            cockpit.OutText(values_info1, 1);
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
        public class Connector : BaseTerminalBlock<IMyShipConnector>
        {
            public MyShipConnectorStatus Status { get { return base.obj.Status; } }
            public bool Connected { get { return base.obj.Status == MyShipConnectorStatus.Connected ? true : false; } }
            public bool Unconnected { get { return base.obj.Status == MyShipConnectorStatus.Unconnected ? true : false; } }
            public bool Connectable { get { return base.obj.Status == MyShipConnectorStatus.Connectable ? true : false; } }
            public Connector(string name) : base(name) { if (base.obj != null) { } }
            public string TextStatus() { StringBuilder values = new StringBuilder(); values.Append(Connected ? igreen.ToString() : (Connectable ? iyellow.ToString() : ired.ToString())); return values.ToString(); }
            public string TextInfo(string name) { StringBuilder values = new StringBuilder(); values.Append((name != null ? name : "КОННЕКТОР") + " : " + TextStatus()); return values.ToString(); }
            public void Connect() { obj.Connect(); }
            public void Disconnect() { obj.Disconnect(); }
            public long? getEntityIdRemoteConnector() { List<IMyShipConnector> list_conn = new List<IMyShipConnector>(); _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn); foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList()) { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 3) return conn.EntityId; } return null; }
            public IMyShipConnector getRemoteConnector() { List<IMyShipConnector> list_conn = new List<IMyShipConnector>(); _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn); foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList()) { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 3) return conn; } return null; }
        }
        public class GasGenerator : BaseListTerminalBlock<IMyGasGenerator>
        {
            public GasGenerator(string name_obj) : base(name_obj) { }
            public GasGenerator(string name_obj, string tag) : base(name_obj, tag) { }
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append((!String.IsNullOrWhiteSpace(name) ? name : "ГЕН.H2/O2: "));
                foreach (IMyGasGenerator obj in list_obj)
                {
                    values.Append("[" + (obj.Enabled ? "+" : "-") + "]");
                }
                return values.ToString();
            }
        }
        public class ReflectorsLight : BaseListTerminalBlock<IMyReflectorLight> { public ReflectorsLight(string name_obj) : base(name_obj) { } public ReflectorsLight(string name_obj, string tag) : base(name_obj, tag) { } }
        public class BaseShipController
        {
            public IMyShipController obj;
            private double current_height = 0;
            public Matrix GetCockpitMatrix()
            {
                Matrix CockpitMatrix = new MatrixD();
                this.obj.Orientation.GetMatrix(out CockpitMatrix);
                return CockpitMatrix;
            }
            public BaseShipController(string name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyShipController;
                _scr.Echo("base_controller:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }

            public BaseShipController(string name_obj, string tag)
            {
                List<IMyShipController> list_obj = new List<IMyShipController>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyShipController>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                _scr.Echo("Найдено base_ship_controller : " + list_obj.Count());
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    obj = list_obj.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag)).FirstOrDefault();
                }
                _scr.Echo("Выбран base_ship_controller: " + ((obj != null) ? ("Ок") : ("not Block")));
            }

            public Vector3D GetPlanetCenter()
            {
                Vector3D pc = new Vector3D();
                return cockpit.obj.TryGetPlanetPosition(out pc) ? pc : Vector3D.Zero;
            }
            public void Dampeners(bool on) { this.obj.DampenersOverride = on; }
            public void OutText(StringBuilder values, int num_lcd)
            {
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null) { ts.WriteText(values, false); }
                }
            }
            public void OutText(string text, bool append, int num_lcd)
            {
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null) { ts.WriteText(text, append); }
                }
            }
            public StringBuilder GetText(int num_lcd)
            {
                StringBuilder values = new StringBuilder();
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return null;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null) { ts.ReadText(values); }
                }
                return values;
            }
            public double GetCurrentHeight()
            {
                double cur_h = 0;
                this.obj.TryGetPlanetElevation(MyPlanetElevation.Surface, out cur_h);
                return cur_h;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    // Получить высоту над поверхностью
                    current_height = GetCurrentHeight();
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("Гравитация: " + this.obj.GetNaturalGravity().Length() + "\n");
                values.Append("PhysicalMass: " + this.obj.CalculateShipMass().PhysicalMass + "\n");
                values.Append("Скорость: " + this.obj.GetShipSpeed() + "\n");
                values.Append("Высота: " + current_height + "\n");
                return values.ToString();
            }
        }
        public class Gyros : BaseListTerminalBlock<IMyGyro>
        {
            public Gyros(string name_obj) : base(name_obj) { }
            public Gyros(string name_obj, string tag) : base(name_obj, tag) { }
            public float getPitch() { return base.list_obj.Select(g => g.Pitch).Average(); }
            public float getRoll() { return base.list_obj.Select(g => g.Roll).Average(); }
            public float getYaw() { return base.list_obj.Select(g => g.Yaw).Average(); }
            public void SetOverride(IMyTerminalBlock block, bool OverrideOnOff, Vector3 settings, float Power = 1)
            {
                foreach (IMyGyro gyro in base.list_obj)
                {
                    if ((!gyro.GyroOverride) && OverrideOnOff)
                        gyro.ApplyAction("Override");
                    gyro.GyroPower = Power;
                    Vector3 bforw = block.WorldMatrix.Forward;
                    Vector3 bleft = block.WorldMatrix.Left;
                    Vector3 bup = block.WorldMatrix.Up;
                    Vector3 gforw = gyro.WorldMatrix.Forward;
                    Vector3 gleft = gyro.WorldMatrix.Left;
                    Vector3 gup = gyro.WorldMatrix.Up;

                    // forw
                    if (gforw == bforw) gyro.SetValueFloat("Roll", settings.GetDim(2));
                    else if (gforw == (bforw * -1)) gyro.SetValueFloat("Roll", -settings.GetDim(2));
                    else if (gup == bforw) gyro.SetValueFloat("Yaw", -settings.GetDim(2));
                    else if (gup == (bforw * -1)) gyro.SetValueFloat("Yaw", settings.GetDim(2));
                    else if (gleft == bforw) gyro.SetValueFloat("Pitch", -settings.GetDim(2));
                    else if (gleft == (bforw * -1)) gyro.SetValueFloat("Pitch", settings.GetDim(2));
                    // left
                    if (gleft == bleft) gyro.SetValueFloat("Pitch", -settings.GetDim(1));
                    else if (gleft == (bleft * -1)) gyro.SetValueFloat("Pitch", settings.GetDim(1));
                    else if (gup == bleft) gyro.SetValueFloat("Yaw", -settings.GetDim(1));
                    else if (gup == (bleft * -1)) gyro.SetValueFloat("Yaw", settings.GetDim(1));
                    else if (gforw == bleft) gyro.SetValueFloat("Roll", settings.GetDim(1));
                    else if (gforw == (bleft * -1)) gyro.SetValueFloat("Roll", -settings.GetDim(1));
                    // up
                    if (gup == bup) gyro.SetValueFloat("Yaw", settings.GetDim(0));
                    else if (gup == (bup * -1)) gyro.SetValueFloat("Yaw", -settings.GetDim(0));
                    else if (gleft == bup) gyro.SetValueFloat("Pitch", settings.GetDim(0));
                    else if (gleft == (bup * -1)) gyro.SetValueFloat("Pitch", -settings.GetDim(0));
                    else if (gforw == bup) gyro.SetValueFloat("Roll", -settings.GetDim(0));
                    else if (gforw == (bup * -1)) gyro.SetValueFloat("Roll", settings.GetDim(0));
                }
            }
            public void SetOverride(bool OverrideOnOff = true, float OverrideValue = 0, float Power = 1)
            {
                foreach (IMyGyro gyro in base.list_obj)
                {
                    if (((!gyro.GyroOverride) && OverrideOnOff) || ((gyro.GyroOverride) && !OverrideOnOff))
                        gyro.ApplyAction("Override");
                    gyro.GyroPower = Power;
                    gyro.Yaw = OverrideValue;
                    gyro.Pitch = OverrideValue;
                    gyro.Roll = OverrideValue;
                }
            }
            public string TextDebug()
            {
                StringBuilder values = new StringBuilder();
                values.Append("Yaw :" + this.getYaw() + "\n");
                values.Append("Pitch :" + this.getPitch() + "\n");
                values.Append("Roll :" + this.getRoll() + "\n");
                return values.ToString();
            }
        }
        public class Thrusts : BaseListTerminalBlock<IMyThrust>
        {
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
            public Thrusts(string name_obj) : base(name_obj) { }
            public Thrusts(string name_obj, string tag) : base(name_obj, tag) { }
            public void InitThrusts(IMyTerminalBlock block)
            {
                Matrix OrientationBlock = new MatrixD();
                block.Orientation.GetMatrix(out OrientationBlock);
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
                    if (ThrusterMatrix.Forward == OrientationBlock.Up)
                    {
                        UpThrusters.Add(thrust);
                    }
                    else if (ThrusterMatrix.Forward == OrientationBlock.Down)
                    {
                        DownThrusters.Add(thrust);
                    }
                    //X
                    else if (ThrusterMatrix.Forward == OrientationBlock.Left)
                    {
                        LeftThrusters.Add(thrust);
                    }
                    else if (ThrusterMatrix.Forward == OrientationBlock.Right)
                    {
                        RightThrusters.Add(thrust);
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == OrientationBlock.Forward)
                    {
                        ForwardThrusters.Add(thrust);
                    }
                    else if (ThrusterMatrix.Forward == OrientationBlock.Backward)
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
                foreach (IMyThrust tr in Thrusts) { tr.ThrustOverridePercentage = persent; }
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
                    Value = (float)(OverrideValue / MaxThrust);
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
                            axis = "D"; OverrideValue = -OverrideValue;
                        }
                        else
                        {
                            OverrideValue += (float)cockpit.obj.GetNaturalGravity().Length();
                        }
                        break;
                    case "D":
                        if (OverrideValue < 0)
                        {
                            axis = "U"; OverrideValue = -OverrideValue;
                        }
                        else
                        {
                            OverrideValue += (float)cockpit.obj.GetNaturalGravity().Length();
                        }
                        break;
                    case "L":
                        if (OverrideValue < 0)
                        {
                            axis = "R"; OverrideValue = -OverrideValue;
                        }
                        break;
                    case "R":
                        if (OverrideValue < 0)
                        {
                            axis = "L"; OverrideValue = -OverrideValue;
                        }
                        break;
                    case "F":
                        if (OverrideValue < 0)
                        {
                            axis = "B"; OverrideValue = -OverrideValue;
                        }
                        break;
                    case "B":
                        if (OverrideValue < 0)
                        {
                            axis = "F"; OverrideValue = -OverrideValue;
                        }
                        break;
                }
                SetOverrideN(axis, OverrideValue * cockpit.obj.CalculateShipMass().PhysicalMass);
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("PhysicalMass : " + Math.Round(cockpit.obj.CalculateShipMass().PhysicalMass) + "\n");
                values.Append("Grav         : " + Math.Round(cockpit.obj.GetNaturalGravity().Length()) + "\n");
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
        public class Cargos
        {
            private List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
            public List<IMyTerminalBlock> cargos = new List<IMyTerminalBlock>();
            public string name_obj;
            public int MaxVolume { get; private set; }
            public int CurrentVolume { get; private set; }
            public int CurrentMass { get; private set; }
            //--
            public int FeAmount { get; private set; }
            public int CbAmount { get; private set; }
            public int NiAmount { get; private set; }
            public int MgAmount { get; private set; }
            public int AuAmount { get; private set; }
            public int AgAmount { get; private set; }
            public int PtAmount { get; private set; }
            public int SiAmount { get; private set; }
            public int UAmount { get; private set; }
            public int StoneAmount { get; private set; }
            public int IceAmount { get; private set; }
            public Cargos(string name_obj)
            {
                this.name_obj = name_obj;
                _scr.GridTerminalSystem.GetBlocksOfType(list, r => r.CustomName.Contains(name_obj));
                foreach (IMyTerminalBlock cargo in list)
                {
                    if ((cargo is IMyShipDrill) || (cargo is IMyCargoContainer) || (cargo is IMyShipConnector))
                    {
                        MaxVolume += (int)cargo.GetInventory(0).MaxVolume;
                        CurrentVolume += (int)(cargo.GetInventory(0).CurrentVolume * 1000);
                        CurrentMass += (int)cargo.GetInventory(0).CurrentMass;
                        cargos.Add(cargo);
                    }
                }
                _scr.Echo("Найдено Cargos : " + cargos.Count());
            }
            public void Update()
            {
                CurrentVolume = 0;
                CurrentMass = 0;
                FeAmount = 0;
                CbAmount = 0;
                NiAmount = 0;
                MgAmount = 0;
                AuAmount = 0;
                AgAmount = 0;
                PtAmount = 0;
                SiAmount = 0;
                UAmount = 0;
                StoneAmount = 0;
                IceAmount = 0;
                foreach (IMyTerminalBlock cargo in cargos)
                {
                    if (cargo != null)
                    {
                        CurrentVolume += (int)cargo.GetInventory(0).CurrentVolume;
                        CurrentMass += (int)cargo.GetInventory(0).CurrentMass;
                        var crateItems = new List<MyInventoryItem>();
                        cargo.GetInventory(0).GetItems(crateItems);
                        for (int j = crateItems.Count - 1; j >= 0; j--)
                        {
                            if (crateItems[j].Type.SubtypeId == "Iron")
                                FeAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Cobalt")
                                CbAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Nickel")
                                NiAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Magnesium")
                                MgAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Gold")
                                AuAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Silver")
                                AgAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Platinum")
                                PtAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Silicon")
                                SiAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Uranium")
                                UAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Stone")
                                StoneAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Ice")
                                IceAmount += (int)crateItems[j].Amount;
                        }
                    }
                }
            }
            public void UnLoad()
            {
                List<IMyCargoContainer> base_cargos = new List<IMyCargoContainer>();
                _scr.GridTerminalSystem.GetBlocksOfType(base_cargos, r => r.CustomName.Contains(this.name_obj) != true);
                foreach (IMyCargoContainer bc in base_cargos)
                {
                    var Destination = bc.GetInventory(0);
                    foreach (IMyTerminalBlock cargo in cargos)
                    {
                        var containerInv = cargo.GetInventory(0);
                        var containerItems = new List<MyInventoryItem>();
                        containerInv.GetItems(containerItems);
                        foreach (MyInventoryItem inv in containerItems)
                        {
                            containerInv.TransferItemTo(Destination, 0, null, true, null);
                        }
                    }
                }
                Update();
            }
        }
        public class HydrogenEngines : BaseListTerminalBlock<IMyPowerProducer>
        {
            public HydrogenEngines(string name_obj) : base(name_obj)
            {
                //MyObjectBuilder_HydrogenEngine
                base.list_obj = base.list_obj.Where(o => o.BlockDefinition.TypeIdString == "MyObjectBuilder_HydrogenEngine").ToList();
                _scr.Echo("Найдено HydrogenEngine :" + base.list_obj.Count());
            }
            public HydrogenEngines(string name_obj, string tag) : base(name_obj, tag)
            {
                base.list_obj = base.list_obj.Where(o => o.BlockDefinition.TypeIdString == "MyObjectBuilder_HydrogenEngine").ToList();
                _scr.Echo("Найдено HydrogenEngine :" + base.list_obj.Count());
            }
            public float MaxOutput()
            {
                return base.list_obj.Select(b => b.MaxOutput).Sum();
            }
            public float CurrentOutput()
            {
                return base.list_obj.Select(b => b.CurrentOutput).Sum();
            }
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append((!String.IsNullOrWhiteSpace(name) ? name : "ГЕНЕРАТОР") + ":[" + Count + "]" + "\n");
                values.Append("|- OUT  : [" + Count + "] " + (Count > 0 ? igreen.ToString() : iyellow.ToString()) + " " + PText.GetCurrentOfMax(CurrentOutput(), MaxOutput(), "W") + "\n");
                float max = MaxOutput();
                values.Append("|  " + PText.GetScalePersent(max > 0f ? CurrentOutput() / max : 0f, 40) + "\n");
                return values.ToString();
            }
        }
        public class MotorStator : BaseTerminalBlock<IMyMotorStator>
        {
            public float? task { get; set; } = null;
            private float tolerance = 0.1f;
            private float multiply_speed = 0.1f;
            private float max_speed = 5f;
            public double Degrees { get { return this.obj != null ? (this.obj.Angle * 180 / Math.PI) : 0; } }
            public MotorStator(string name_obj) : base(name_obj)
            {

            }
            public bool SetDegrees(float degrees)
            {
                float speed = 0f;
                double curennt_degrees = this.Degrees;
                double difference = (degrees - curennt_degrees);
                if (Math.Abs(difference) > 180f)
                {
                    difference *= -1;
                }
                if (Math.Abs(difference) > tolerance)
                {
                    speed = (float)(difference * this.multiply_speed);
                    if ((speed < 0f) && (speed < -max_speed)) speed = -max_speed; if ((speed > 0f) && (speed > max_speed)) speed = max_speed;
                    this.obj.TargetVelocityRPM = speed;
                    return false;
                }
                else
                {
                    this.obj.TargetVelocityRPM = speed;
                    return true;
                }
            }
            public string TextInfo()
            {
                if (this.obj == null) return "";
                StringBuilder values = new StringBuilder();
                values.Append("ШАРНИР : " + this.obj.CustomName + "\n");
                values.Append("БЛОК : " + (this.obj.RotorLock ? ired.ToString() : igreen.ToString()) + " НИЗ: " + Math.Round(this.obj.LowerLimitDeg, 2) + " ВЕРХ: " + Math.Round(this.obj.UpperLimitDeg, 2) + "\n");
                values.Append("УГОЛ : " + Math.Round(this.Degrees, 1) + " СКОРОСТЬ : " + Math.Round(this.obj.TargetVelocityRPM, 3) + " ЗАД : " + this.task + "\n");
                return values.ToString();
            }
        }
        public class Joint : BaseTerminalBlock<IMyMotorStator>
        {
            public float? task { get; set; } = null;
            private float tolerance = 0.1f;
            private float multiply_speed = 0.1f;
            private float max_speed = 5f;
            public double Degrees { get { return this.obj != null ? (this.obj.Angle * 180 / Math.PI) : 0; } }
            public Joint(string name_obj) : base(name_obj) { }
            public bool SetDegrees(float degrees)
            {
                float speed = 0f;
                double curennt_degrees = this.Degrees;
                double difference = ((degrees + 90f) - (curennt_degrees + 90f));
                if (Math.Abs(difference) > tolerance)
                {
                    speed = (float)(difference * this.multiply_speed);
                    if ((speed < 0f) && (speed < -5f)) speed = -5f; if ((speed > 0f) && (speed > 5f)) speed = 5f;
                    this.obj.TargetVelocityRPM = speed;
                    return false;
                }
                else
                {
                    this.obj.TargetVelocityRPM = speed;
                    return true;
                }
            }
            public string TextInfo()
            {
                if (this.obj == null) return "";
                StringBuilder values = new StringBuilder();
                values.Append("ШАРНИР : " + this.obj.CustomName + "\n");
                values.Append("БЛОК : " + (this.obj.RotorLock ? ired.ToString() : igreen.ToString()) + " НИЗ: " + Math.Round(this.obj.LowerLimitDeg, 2) + " ВЕРХ: " + Math.Round(this.obj.UpperLimitDeg, 2) + "\n");
                values.Append("УГОЛ : " + Math.Round(this.Degrees, 1) + " СКОРОСТЬ : " + Math.Round(this.obj.TargetVelocityRPM, 3) + " ЗАД : " + this.task + "\n");
                return values.ToString();
            }
        }
        public class TurretControlBlock : BaseTerminalBlock<IMyTurretControlBlock>
        {
            public TurretControlBlock(string name_obj) : base(name_obj) { }
            public string TextInfo()
            {
                if (this.obj == null) return "";
                StringBuilder values = new StringBuilder();
                return values.ToString();
            }
        }
        public class Nav
        {
            public enum programm : int
            {
                none = 0,
                fly_connect_base = 1,   // лететь на базу
                fly_drill = 2,          // лететь к шахте
                start_drill = 3,        // начать бурение
            };
            public static string[] name_programm = { "", "ПОЛЕТ НА БАЗУ", "ПОЛЕТ К ШАХТЕ", "СТАРТ ДОБЫЧИ" };
            public programm curent_programm = programm.none;
            public enum mode : int
            {
                none = 0,
                base_operation = 1,
                un_dock = 2,
                to_base = 3,
                dock = 4,
                to_drill = 5,
                drill_align = 6,
            };
            public static string[] name_mode = { "", "БАЗА", "РАСТЫКОВКА", "К БАЗЕ", "СТЫКОВКА", "К ШАХТЕ", "НА ТОЧКУ БУРЕНИЯ" };
            public mode curent_mode = mode.none;
            public string name_ship { get; set; }
            public Vector3D MyPos { get; private set; }
            public Vector3D MyPrevPos { get; private set; }
            public Vector3D VelocityVector { get; private set; }
            public Vector3D UpVelocityVector { get; private set; }
            public Vector3D ForwVelocityVector { get; private set; }
            public Vector3D LeftVelocityVector { get; private set; }
            public Vector3D GravVector { get; private set; }
            public bool gravity { get; private set; } = false;
            public float PhysicalMass { get; private set; } // Физическая масса
            public MatrixD WMCocpit { get; private set; } //

            public MatrixD OrientationCocpit;
            public float XMaxA { get; private set; }
            public float YMaxA { get; private set; }
            public float ZMaxA { get; private set; }
            //--------------------
            public float HDistance { get; private set; } // Дистанция по горизонтали (плоскость X,Z)
            public float VDistance { get; private set; } // Дистанция по вертикали (плоскость Y)
            //-------------------
            public Vector3D PlanetCenter { get; set; } = new Vector3D(0.50, 0.50, 0.50);
            public Vector3D ConnectorPoint { get; set; } = new Vector3D(0, 0, 5);
            public Vector3D BaseDockPoint { get; set; } = new Vector3D(0, 0, 0);
            public MatrixD DockMatrix { get; set; }
            public Vector3D DrillPoint { get; set; } = new Vector3D(0, 0, 0);
            public MatrixD DrillMatrix { get; set; }
            public double FlyHeight { get; set; }
            public bool CriticalMassReached { get; private set; }// Признак критической массы
            public bool CriticalBatteryCharge { get; private set; }// Признак критического заряда
            public bool CriticalHydrogenSupply { get; private set; }// Признак критического запаса водорода
            public bool EmergencySetpoint { get; private set; } = false;

            public bool EmergencyReturn = false;
            public bool horizont { get; set; } = false;  // держим горизонтальное направление
            public Vector3D? TackVector { get; set; } = null;
            public bool go_home { get; set; } = false; // вернутся домой и остатся
            public bool paused { get; set; } = false;
            public IMyTerminalBlock BlockNav { get; set; }
            public Nav(string name)
            {
                this.name_ship = name;
            }
            public MatrixD GetNormTransMatrixFromMyPos(IMyTerminalBlock block)
            {
                MatrixD mRot;
                Vector3D V3Dcenter = block.GetPosition();
                Vector3D V3Dup = block.WorldMatrix.Up;
                if (gravity) V3Dup = -Vector3D.Normalize(GravVector);
                Vector3D V3Dleft = Vector3D.Normalize(Vector3D.Reject(block.WorldMatrix.Left, V3Dup));
                Vector3D V3Dfow = Vector3D.Normalize(Vector3D.Cross(V3Dleft, V3Dup));
                mRot = new MatrixD(V3Dleft.GetDim(0), V3Dleft.GetDim(1), V3Dleft.GetDim(2), 0, V3Dup.GetDim(0), V3Dup.GetDim(1), V3Dup.GetDim(2), 0, V3Dfow.GetDim(0), V3Dfow.GetDim(1), V3Dfow.GetDim(2), 0, 0, 0, 0, 1);
                mRot = MatrixD.Invert(mRot);
                return new MatrixD(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -V3Dcenter.GetDim(0), -V3Dcenter.GetDim(1), -V3Dcenter.GetDim(2), 1) * mRot;
            }
            public void SetDockMatrix(IMyTerminalBlock block)
            {
                if (connector.Connected)
                {
                    BlockNav = block;
                    thrusts.InitThrusts(block);
                    DockMatrix = GetNormTransMatrixFromMyPos(BlockNav);
                    PlanetCenter = cockpit.GetPlanetCenter();
                    strg.SaveToStorage();
                }
            }
            public void SetFlyHeight()
            {
                FlyHeight = (MyPos - PlanetCenter).Length();
                strg.SaveToStorage();
            }
            public void SetDrillMatrixDepo()
            {
                DrillMatrix = GetNormTransMatrixFromMyPos(cockpit.obj);
                DrillPoint = new Vector3D(0, 0, 0);
                strg.SaveToStorage();
            }
            public void InitBlock(IMyTerminalBlock block)
            {
                BlockNav = block;
                thrusts.InitThrusts(block);
            }
            public void InitMode(mode mode) { if (mode == mode.un_dock || mode == mode.dock || mode == mode.base_operation) InitBlock(connector.obj); else InitBlock(cockpit.obj); curent_mode = mode; strg.SaveToStorage(); }
            public float getAccel(float Accel, float minAccel = 0.1f)
            {
                if ((Accel < 0) && (Accel > -minAccel)) return -minAccel; if ((Accel > 0) && (Accel < minAccel)) return minAccel; return Accel;
            }
            public Vector3D GetNavAngles(Vector3D Target, MatrixD InvMatrix, double sfiftX = 0, double shiftZ = 0)
            {
                Vector3D V3Dcenter = MyPos;
                Vector3D V3Dfow = WMCocpit.Forward + V3Dcenter;
                Vector3D V3Dup = WMCocpit.Up + V3Dcenter;
                Vector3D V3Dleft = WMCocpit.Left + V3Dcenter;
                //Vector3D GravNorm = Vector3D.Normalize(GravVector) + V3Dcenter;
                V3Dcenter = Vector3D.Transform(V3Dcenter, InvMatrix);
                V3Dfow = (Vector3D.Transform(V3Dfow, InvMatrix)) - V3Dcenter;
                V3Dup = (Vector3D.Transform(V3Dup, InvMatrix)) - V3Dcenter;
                V3Dleft = (Vector3D.Transform(V3Dleft, InvMatrix)) - V3Dcenter;
                Vector3D GravNorm = Vector3D.Normalize(new Vector3D(-0, -1, -0));
                if (gravity)
                {
                    GravNorm = Vector3D.Normalize(GravVector);
                    GravNorm = Vector3D.Normalize(Vector3D.Transform(GravNorm + MyPos, InvMatrix) - V3Dcenter - new Vector3D(sfiftX, 0, shiftZ));
                }
                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(V3Dfow);
                double gL = GravNorm.Dot(V3Dleft);
                double gU = GravNorm.Dot(V3Dup);
                //Получаем сигналы по тангажу и крены операцией atan2
                double TargetRoll = (float)Math.Atan2(gL, -gU); // крен
                double TargetPitch = -(float)Math.Atan2(gF, -gU); // тангаж
                Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter);
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
                else
                {
                    if (cockpit.obj.IsUnderControl) TargetYaw = cockpit.obj.RotationIndicator.Y;
                }
                if (double.IsNaN(TargetYaw)) TargetYaw = 0;
                if (double.IsNaN(TargetPitch)) TargetPitch = 0;
                if (double.IsNaN(TargetRoll)) TargetRoll = 0;
                return new Vector3D(TargetYaw, TargetPitch, TargetRoll);
            }
            //----------------------------------------------
            public void Horizon() { Vector3D gyrAng = GetNavAngles(TackVector); gyros.SetOverride(cockpit.obj, true, gyrAng * GyroMult, 1); }
            public void UpdateCalc()
            {
                GravVector = cockpit.obj.GetNaturalGravity();
                gravity = GravVector.LengthSquared() > 0.2f;
                PhysicalMass = cockpit.obj.CalculateShipMass().PhysicalMass;
                if (BlockNav != null)
                {
                    MyPrevPos = MyPos;
                    MyPos = BlockNav.GetPosition();
                    WMCocpit = BlockNav.WorldMatrix;
                    VelocityVector = (MyPos - MyPrevPos) * 6;
                    UpVelocityVector = WMCocpit.Up * Vector3D.Dot(VelocityVector, WMCocpit.Up);
                    ForwVelocityVector = WMCocpit.Forward * Vector3D.Dot(VelocityVector, WMCocpit.Forward);
                    LeftVelocityVector = WMCocpit.Left * Vector3D.Dot(VelocityVector, WMCocpit.Left);
                }
                YMaxA = Math.Abs((float)Math.Min(thrusts.UpThrMax / PhysicalMass - GravVector.Length(), thrusts.DownThrMax / PhysicalMass + GravVector.Length()));
                ZMaxA = (float)Math.Min(thrusts.ForwardThrMax, thrusts.BackwardThrMax) / PhysicalMass;
                XMaxA = (float)Math.Min(thrusts.RightThrMax, thrusts.LeftThrMax) / PhysicalMass;
                cargos.Update();
                if (cargos.IceAmount > 100)
                {
                    gas_gen.On(); h2_engines.On();
                }
                else
                {
                    gas_gen.Off(); h2_engines.Off();
                };
                // Критические уставки
                if (PhysicalMass > CriticalMass) { CriticalMassReached = true; }
                else
                {
                    CriticalMassReached = false;
                    //if (cargos.StoneAmount > StoneDumpOn)
                    //    StoneDumpNeeded = true;
                    //if (cargos.StoneAmount < 100)
                    //    StoneDumpNeeded = false;
                }
                CriticalBatteryCharge = connector.Connected ? bats.CurrentPersent < MaxOffCharge : bats.CurrentPersent <= MinOnCharge;
                EmergencySetpoint = (!connector.Connected ? CriticalMassReached : false) || CriticalBatteryCharge;
                //ТЕСТИРОВАНИЕ
                StringBuilder values1 = new StringBuilder();
                Vector3D MyPosPoint = Vector3D.Transform(MyPos, DockMatrix);
                values1.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values1.Append("BlockNav :" + (BlockNav != null).ToString() + "\n");
                values1.Append("My_Length   : " + Math.Round(MyPosPoint.Length(), 2) + "\n");
                values1.Append("MyPos_X_[0]   : " + Math.Round(MyPosPoint.GetDim(0), 2) + "\n");
                values1.Append("MyPos_Y_[1]   : " + Math.Round(MyPosPoint.GetDim(1), 2) + "\n");
                values1.Append("MyPos_Z_[2]   : " + Math.Round(MyPosPoint.GetDim(2), 2) + "\n");
                lcd_test1.OutText(values1);
                //StringBuilder values2 = new StringBuilder();
                //values2.Append("IceAmount :" + cargos.IceAmount + "\n");
                //lcd_test2.OutText(values2);
                StringBuilder values3 = new StringBuilder();
                Vector3D MyPosDrill = Vector3D.Transform(MyPos, DrillMatrix) - DrillPoint;
                values3.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values3.Append("MyDrill_Length   : " + Math.Round(MyPosDrill.Length(), 2) + "\n");
                values3.Append("MyDrillPos_X_[0]   : " + Math.Round(MyPosDrill.GetDim(0), 2) + "\n");
                values3.Append("MyDrillPos_Y_[1]   : " + Math.Round(MyPosDrill.GetDim(1), 2) + "\n");
                values3.Append("MyDrillPos_Z_[2]   : " + Math.Round(MyPosDrill.GetDim(2), 2) + "\n");
                lcd_test3.OutText(values3);
            }
            //------------------------------------------------
            public void FlyConnectBase()
            {
                if (curent_mode == mode.none) { InitMode(mode.to_base); }
                if (curent_mode == mode.to_base && ToBase()) { InitMode(mode.dock); }
                if (curent_mode == mode.dock && Dock())
                {
                    curent_programm = programm.none;
                    InitMode(mode.none);
                }
            }
            public void FlyDrill()
            {
                if (curent_mode == mode.none) { if (connector.Connected) { InitMode(mode.un_dock); } else { InitMode(mode.to_drill); } }
                if (curent_mode == mode.un_dock && UnDock()) { InitMode(mode.to_drill); }
                if (curent_mode == mode.to_drill && ToDrillPoint())
                {
                    curent_programm = programm.none;
                    InitMode(mode.none);
                }
            }
            //-----------------------------------------------
            public void Stop()
            {
                thrusts.ClearThrustOverridePersent();
                gyros.SetOverride(false, 1);
                curent_mode = mode.none;
                curent_programm = programm.none;
                paused = false;
                go_home = false;
                strg.SaveToStorage();
            }
            public void Pause(bool enable)
            {
                if (enable)
                {
                    thrusts.ClearThrustOverridePersent();
                    gyros.SetOverride(false, 1);
                    paused = true;
                }
                else { paused = false; }
                strg.SaveToStorage();
            }
            public bool UnDock()
            {
                bool Complete = false;
                //hydrogen_tanks_nav.Stockpile(false);
                connector.obj.Disconnect();
                if (!connector.Connected)
                {
                    Vector3D MyPosCon = Vector3D.Transform(MyPos, DockMatrix);
                    HDistance = (float)(new Vector3D(MyPosCon.GetDim(0), 0, MyPosCon.GetDim(2))).Length();
                    Vector3D gyrAng = GetNavAngles(ConnectorPoint, DockMatrix);
                    gyros.SetOverride(BlockNav, true, gyrAng * GyroMult, 1);
                    thrusts.SetOverridePercent("U", 0f);
                    thrusts.SetOverridePercent("D", 0);
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                    thrusts.SetOverridePercent("F", 0);
                    thrusts.SetOverrideAccel("B", 3);
                    if (HDistance > safe_base_distance)
                    {
                        thrusts.SetOverridePercent("B", 0);
                        gyros.SetOverride(false, 1);
                        Complete = true;
                    }
                }
                //OutStatusMode(0, 0, 0);
                return Complete;
            }
            public bool FP(Vector3D Point, MatrixD Matrix, float distance)
            {
                bool Complete = false;
                float MaxUSpeed, MaxFSpeed;
                thrusts.On();
                Vector3D MyPosCon = Vector3D.Transform(MyPos, Matrix);
                Vector3D gyrAng = GetNavAngles(Point, Matrix);
                HDistance = (float)(Point - new Vector3D(MyPosCon.GetDim(0), 0, MyPosCon.GetDim(2))).Length();
                MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(FlyHeight - (MyPos - PlanetCenter).Length()) * YMaxA) / 1.2f;
                MaxFSpeed = (float)Math.Sqrt(2 * HDistance * ZMaxA) / 1.2f;
                gyros.SetOverride(BlockNav, true, gyrAng * GyroMult, 1);
                thrusts.SetOverridePercent("R", 0); thrusts.SetOverridePercent("L", 0);
                if (UpVelocityVector.Length() < MaxUSpeed)
                    thrusts.SetOverrideAccel("U", (float)((FlyHeight - (MyPos - PlanetCenter).Length()) * AlignAccelMult));
                else
                {
                    thrusts.SetOverridePercent("U", 0);
                    thrusts.SetOverridePercent("D", 0);
                }
                if (HDistance > distance)
                {
                    if (ForwVelocityVector.Length() < MaxFSpeed)
                    {
                        thrusts.SetOverrideAccel("F", (float)(HDistance * AlignAccelMult));
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
                    Complete = true;
                }
                //OutStatusMode(MaxFSpeed, MaxUSpeed, 0, 0);
                return Complete;
            }
            public bool ToBase() { return FP(BaseDockPoint, DockMatrix, safe_base_distance); }
            public bool ToDrillPoint() { return FP(DrillPoint, DrillMatrix, safe_point_distance); }
            public bool TP(Vector3D Point, MatrixD Matrix)
            {
                bool Complete = false;
                float MaxUSpeed, MaxLSpeed, MaxFSpeed;
                float UpAccel = 0;
                thrusts.On();
                Vector3D MyPosCcp = Vector3D.Transform(MyPos, Matrix) - Point;
                Vector3D gyrAng = GetNavAngles(MyPosCcp + Point + new Vector3D(0, 0, 5), Matrix);
                gyros.SetOverride(BlockNav, true, gyrAng * GyroMult, 1);
                MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCcp.GetDim(0)) * XMaxA) / 2;
                MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCcp.GetDim(1)) * YMaxA) / 2;
                MaxFSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCcp.GetDim(2)) * ZMaxA) / 2;
                if (double.IsNaN(MaxUSpeed)) MaxUSpeed = MinUDAccel;
                if (Math.Abs(MyPosCcp.GetDim(1)) < 1) MaxUSpeed = MinUDAccel;
                if (LeftVelocityVector.Length() < MaxLSpeed)
                    thrusts.SetOverrideAccel("R", (float)(MyPosCcp.GetDim(0) * AlignAccelMult));
                else { thrusts.SetOverridePercent("R", 0); thrusts.SetOverridePercent("L", 0); }
                if (ForwVelocityVector.Length() < MaxFSpeed) thrusts.SetOverrideAccel("B", (float)(MyPosCcp.GetDim(2) * AlignAccelMult));
                else { thrusts.SetOverridePercent("F", 0); thrusts.SetOverridePercent("B", 0); }
                UpAccel = getAccel(-(float)(MyPosCcp.GetDim(1) * AlignAccelMult), MinUDAccel);
                if (UpVelocityVector.Length() < MaxUSpeed) { thrusts.SetOverrideAccel("U", UpAccel); }
                else { thrusts.SetOverridePercent("U", 0); thrusts.SetOverridePercent("D", 0); }
                if (MyPosCcp.Length() < 0.5f)
                {
                    thrusts.ClearThrustOverridePersent();
                    gyros.SetOverride(false, 1);
                    Complete = true;
                }
                //OutStatusMode(MaxFSpeed, MaxUSpeed, MaxLSpeed, UpAccel);
                return Complete;
            }
            public bool DrillAlign() { return TP(DrillPoint, DrillMatrix); }
            public bool Dock()
            {
                bool Complete = false;
                float MaxUSpeed, MaxLSpeed, MaxFSpeed;
                thrusts.On();
                Vector3D MyPosCon = Vector3D.Transform(MyPos, DockMatrix);
                Vector3D gyrAng = GetNavAngles(ConnectorPoint, DockMatrix);
                HDistance = (float)(new Vector3D(MyPosCon.GetDim(0), 0, MyPosCon.GetDim(2))).Length();
                VDistance = (float)(new Vector3D(0, MyPosCon.GetDim(1), 0)).Length();
                gyros.SetOverride(BlockNav, true, gyrAng * GyroMult, 1);
                MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(0)) * XMaxA) / 2;
                MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(1)) * YMaxA) / 2;
                if (MaxUSpeed < MinUDSpeed) MaxUSpeed = MinUDSpeed;
                MaxFSpeed = (float)Math.Sqrt(2 * HDistance * ZMaxA) / 2;
                if (HDistance < 15) MaxFSpeed = MaxFSpeed / 5;
                if (Math.Abs(MyPosCon.GetDim(1)) < 1) MaxUSpeed = MinUDSpeed;
                if (LeftVelocityVector.Length() < MaxLSpeed) thrusts.SetOverrideAccel("R", (float)(MyPosCon.GetDim(0) * AlignAccelMult));
                else { thrusts.SetOverridePercent("R", 0); thrusts.SetOverridePercent("L", 0); }
                float UpAccel = getAccel(-(float)(MyPosCon.GetDim(1) * AlignAccelMult), MinUDAccel);
                if ((Math.Abs(MyPosCon.GetDim(0)) < 0.8f) && (UpVelocityVector.Length() < MaxUSpeed)) { thrusts.SetOverrideAccel("U", UpAccel); }
                else { thrusts.SetOverridePercent("U", 0); thrusts.SetOverridePercent("D", 0); }
                if (ForwVelocityVector.Length() < MaxFSpeed && MyPosCon.GetDim(2) < 0 && ((HDistance >= safe_base_distance) || (Math.Abs(MyPosCon.GetDim(2)) < safe_base_distance && Math.Abs(MyPosCon.GetDim(0)) < 1.0f && Math.Abs(MyPosCon.GetDim(1)) < 1.0f)))
                { thrusts.SetOverrideAccel("F", (float)(HDistance * AlignAccelMult)); thrusts.SetOverridePercent("B", 0); }
                else { thrusts.SetOverridePercent("F", 0); thrusts.SetOverridePercent("B", 0); }
                if (HDistance < 6)
                {
                    if (connector.Status == MyShipConnectorStatus.Connectable) { connector.Connect(); }
                    if (connector.Status == MyShipConnectorStatus.Connected) { thrusts.ClearThrustOverridePersent(); gyros.SetOverride(false, 1); Complete = true; }
                }
                //OutStatusMode(MaxFSpeed, MaxUSpeed, MaxLSpeed, UpAccel);
                return Complete;
            }
            //-------------------------------------------------
            public void OutStatusMode(float MaxFSpeed, float MaxUSpeed, float MaxLSpeed, float UpAccel)
            {
                //StringBuilder values = new StringBuilder();
                //values.Append(" STATUS\n");
                //Vector3D MyPosPoint = Vector3D.Transform(MyPos, DockMatrix);
                //values.Append("My_Length   : " + Math.Round(MyPosPoint.Length(), 2) + "\n");
                //values.Append("MyPosDrill[0]   : " + Math.Round(MyPosPoint.GetDim(0), 2) + "\n");
                //values.Append("MyPosDrill[1]   : " + Math.Round(MyPosPoint.GetDim(1), 2) + "\n");
                //values.Append("MyPosDrill[2]   : " + Math.Round(MyPosPoint.GetDim(2), 2) + "\n");
                //values.Append("------------------------------------------\n");
                //values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                //values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                ////values.Append("Dist(вертикаль): " + Math.Round(VDistance).ToString() + "\n");
                ////values.Append("Dist(горизонталь): " + Math.Round(HDistance).ToString() + "\n");
                //values.Append("------------------------------------------\n");
                //values.Append("ZMaxA (F-B) : " + Math.Round(ZMaxA, 2).ToString() + ", MaxFSpeed: " + Math.Round(MaxFSpeed, 2).ToString() + "\n");
                //values.Append("YMaxA (U-D) : " + Math.Round(YMaxA, 2).ToString() + ", MaxUSpeed: " + Math.Round(MaxUSpeed, 2).ToString() + "\n");
                //values.Append("XMaxA (L-R) : " + Math.Round(XMaxA, 2).ToString() + ", MaxLSpeed: " + Math.Round(MaxLSpeed, 2).ToString() + "\n");
                //cockpit.OutText(values, 0);
                //values.Append(thrusts.TextInfo());
                //lcd_test1.OutText(values);
            }
            public string TextInfo1()
            {
                StringBuilder values = new StringBuilder();
                values.Append("СКОРОСТЬ             : " + Math.Round(cockpit.obj.GetShipSpeed(), 2) + "\n");
                values.Append("ВЫСОТА    (пов.)     : " + Math.Round(cockpit.GetCurrentHeight(), 2) + "\n");
                values.Append("ВЫСОТА    (точка.)   : " + Math.Round(VDistance).ToString() + "\n");
                values.Append("ДИСТАНЦИЯ (точка.)   : " + Math.Round(HDistance).ToString() + "\n");
                values.Append("--------------------------------------\n");
                values.Append("ГОРИЗОНТ : " + (horizont ? igreen.ToString() : ired.ToString()) + ", ");
                values.Append("ПАУЗА : " + (paused ? igreen.ToString() : ired.ToString()) + ", ");
                values.Append("ДОМОЙ : " + (go_home ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("--------------------------------------\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                return values.ToString();
            }
            public string TextCritical()
            {
                StringBuilder values = new StringBuilder();
                values.Append("--------------------------------------\n");
                values.Append("АВАРИЙНЫЕ УСТАВКИ    : " + (EmergencySetpoint ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("|-ФИЗ./КРИТ.(МАССА)  : " + Math.Round(PhysicalMass).ToString() + " / " + CriticalMass + " " + (CriticalMassReached ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("|-БАТАРЕЯ %          : " + PText.GetPersent(bats.CurrentPersent) + " " + (CriticalBatteryCharge ? ired.ToString() : igreen.ToString()) + "\n");
                //values.Append("|-ТОПЛИВО H2 %      : " + PText.GetPersent(hydrogen_tanks_nav.AverageFilledRatio) + " " + (CriticalHydrogenSupply ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("--------------------------------------\n");
                values.Append("Камень        :" + cargos.StoneAmount + "\n");
                values.Append("--------------------------------------\n");
                values.Append("ПРОГРАММА : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП      : " + name_mode[(int)curent_mode] + "\n");
                values.Append("Я Управ.  : " + cockpit.obj.IsUnderControl.ToString() + "\n");
                return values.ToString();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "save_dock": SetDockMatrix(connector.obj); break;
                    case "save_drill": SetDrillMatrixDepo(); break;
                    case "save_height": SetFlyHeight(); break;
                    case "horizont": if (curent_programm == programm.none && curent_mode == mode.none) { if (horizont) { horizont = false; } else { horizont = true; } } break;
                    case "stop": Stop(); break;
                    case "pause": Pause(!paused); break;
                    case "un_dock": { InitMode(mode.un_dock); break; }
                    case "dock": { InitMode(mode.dock); break; }
                    case "to_base": { InitMode(mode.to_base); break; }
                    case "to_drill": { InitMode(mode.to_drill); break; }
                    case "drill_align": { InitMode(mode.drill_align); break; }
                    case "fly_base": { curent_programm = programm.fly_connect_base; strg.SaveToStorage(); break; }
                    case "fly_drill": { curent_programm = programm.fly_drill; strg.SaveToStorage(); break; }
                    case "start_drill": { curent_programm = programm.start_drill; strg.SaveToStorage(); break; }
                    default: break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    UpdateCalc();
                    if (!connector.Connected)
                    {
                        if (cockpit.GetCurrentHeight() > 5.0f)
                        {
                            bats.Auto();
                            thrusts.On();
                        }
                    }
                    else
                    {
                        // Припаркован
                        reflectors_light.Off();
                        if ((curent_mode == mode.none && curent_programm == programm.none))
                        {
                            // Если сидим в кокпите батарея не заряжается
                            if (cockpit.obj.IsUnderControl) { bats.Auto(); } else { bats.Charger(); }
                        }
                        thrusts.Off();
                    }
                    if (curent_programm == programm.none)
                    {
                        lcd_work1.Off(); lcd_work2.Off(); lcd_debug.Off();
                        if (horizont)
                        {
                            Horizon();
                        }
                        else
                        {
                            gyros.SetOverride(false, 1);
                        }
                        if (curent_mode == mode.un_dock && !paused && UnDock())
                        {
                            curent_mode = mode.none; InitMode(mode.none); //strg.SaveToStorage();
                        }
                        if (curent_mode == mode.dock && !paused && Dock())
                        {
                            curent_mode = mode.none; InitMode(mode.none); //strg.SaveToStorage();
                        }
                        if (curent_mode == mode.to_base && !paused && ToBase())
                        {
                            curent_mode = mode.none; InitMode(mode.none); //strg.SaveToStorage();
                        }
                        if (curent_mode == mode.to_drill && !paused && ToDrillPoint())
                        {
                            curent_mode = mode.none; InitMode(mode.none); //strg.SaveToStorage();
                        }
                        if (curent_mode == mode.drill_align && !paused && DrillAlign())
                        {
                            curent_mode = mode.none; InitMode(mode.none); //strg.SaveToStorage();
                        }
                    }
                    else
                    {
                        lcd_work1.On(); lcd_work2.On(); lcd_debug.On();
                        if (curent_programm == programm.fly_connect_base && !paused)
                        {
                            FlyConnectBase();
                        }
                        if (curent_programm == programm.fly_drill && !paused)
                        {
                            FlyDrill();
                        }
                    }
                }
            }
        }
        public class Upr
        {
            public enum programm : int
            {
                none = 0,
                open_sp = 1,   // лететь на базу
                close_sp = 2
            };
            public static string[] name_programm = { "", "ОТКРЫТЬ СП", "ЗАКРЫТЬ СП" };
            public programm curent_programm = programm.none;
            public enum mode : int
            {
                none = 0,
                sp_open = 1,
                sp_close = 2,
                sp_parking = 3,
                sp_avto = 4,
            };
            public static string[] name_mode = { "", "ВЫДВИНУТЬ СП", "ЗАДВИНУТЬ СП", "СП ПАРКОВКА", "СП РАБОТА" };
            public mode curent_mode = mode.none;

            public void OpenSP()
            {
                if (curent_mode == mode.none) { curent_mode = mode.sp_open; }
                if (curent_mode == mode.sp_open && SPOpen()) { curent_mode = mode.sp_avto; }
                if (curent_mode == mode.sp_avto && SPAvto()) { curent_mode = mode.none; curent_programm = programm.none; }
            }
            public void CloseSP()
            {
                if (curent_mode == mode.none) { curent_mode = mode.sp_parking; }
                if (curent_mode == mode.sp_parking && SPParking()) { curent_mode = mode.sp_close; }
                if (curent_mode == mode.sp_close && SPClose()) { curent_mode = mode.none; curent_programm = programm.none; }
            }
            public bool SPOpen()
            {
                bool rp = jn_rsp.SetDegrees(0);
                bool lp = jn_lsp.SetDegrees(0);
                return lp && rp;
            }
            public bool SPClose()
            {
                if (Math.Abs(ms_lsp.Degrees) < 0.5f && Math.Abs(ms_rsp.Degrees) < 0.5f)
                {
                    bool lp = jn_lsp.SetDegrees(90);
                    bool rp = jn_rsp.SetDegrees(90);
                    return lp && rp;
                } else { return true; }

            }
            public bool SPParking()
            {
                tcb_lsp.Off(); tcb_rsp.Off();
                bool lp = ms_lsp.SetDegrees(0);
                bool rp = ms_rsp.SetDegrees(0);
                return lp && rp;
            }
            public bool SPAvto()
            {
                if (Math.Abs(ms_lsp.Degrees) < 0.5f && Math.Abs(ms_rsp.Degrees) < 0.5f)
                {
                    tcb_lsp.On(); tcb_rsp.On(); return true;
                }
                else
                {
                    tcb_lsp.Off(); tcb_rsp.Off(); return false;
                }
            }
            public string TextInfo1()
            {
                StringBuilder values = new StringBuilder();
                values.Append("--[UPR]----------------------------\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                return values.ToString();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "sp_open": curent_mode = mode.sp_open; break;
                    case "sp_close": curent_mode = mode.sp_close; break;
                    case "sp_parking": curent_mode = mode.sp_parking; break;
                    case "sp_avto": curent_mode = mode.sp_avto; break;
                    case "osp": curent_programm = programm.open_sp; break;
                    case "csp": curent_programm = programm.close_sp; break;

                    default: break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    if (curent_programm == programm.none)
                    {
                        if (curent_mode == mode.sp_open && SPOpen()) { curent_mode = mode.none; }
                        if (curent_mode == mode.sp_close && SPClose()) { curent_mode = mode.none; }
                        if (curent_mode == mode.sp_parking && SPParking()) { curent_mode = mode.none; }
                        if (curent_mode == mode.sp_avto && SPAvto()) { curent_mode = mode.none; }
                    }
                    else
                    {
                        if (curent_programm == programm.open_sp)  { OpenSP(); }
                        if (curent_programm == programm.close_sp) { CloseSP(); }
                    }
                }
                StringBuilder values = new StringBuilder();
                values.Append(ms_lsp.TextInfo());
                values.Append(ms_rsp.TextInfo());
                values.Append("----------------------------");
                values.Append(jn_lsp.TextInfo());
                values.Append(jn_rsp.TextInfo());
                lcd_test2.OutText(values);
            }
        }
        public class MyStorage
        {
            public MyStorage() { }
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                nav.curent_programm = (Nav.programm)GetValInt("curent_programm", str.ToString());
                nav.curent_mode = (Nav.mode)GetValInt("curent_mode", str.ToString());
                nav.horizont = GetValBool("horizont", str.ToString());
                nav.paused = GetValBool("pause", str.ToString());
                nav.go_home = GetValBool("go_home", str.ToString());
                nav.FlyHeight = GetValDouble("FlyHeight", str.ToString());
                nav.EmergencyReturn = GetValBool("EmergencyReturn", str.ToString());
                nav.DockMatrix = GetValMatrixD("DM", str.ToString());
                //nav.BaseDockPoint = GetValVector3D("BDP", str.ToString());
                nav.PlanetCenter = GetValVector3D("PC", str.ToString());
                nav.DrillMatrix = GetValMatrixD("DRM", str.ToString());
                //nav.DrillPoint = ;
                if (nav.curent_mode == Nav.mode.dock || nav.curent_mode == Nav.mode.un_dock || nav.curent_mode == Nav.mode.base_operation)
                {
                    nav.InitBlock(connector.obj);
                }
                else
                {
                    nav.InitBlock(cockpit.obj);
                }
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                values.Append("curent_programm: " + ((int)nav.curent_programm).ToString() + ";\n");
                values.Append("curent_mode: " + ((int)nav.curent_mode).ToString() + ";\n");
                values.Append("horizont: " + nav.horizont.ToString() + ";\n");
                values.Append("pause: " + nav.paused.ToString() + ";\n");
                values.Append("go_home: " + nav.go_home.ToString() + ";\n");
                values.Append("FlyHeight: " + Math.Round(nav.FlyHeight, 0) + ";\n");
                values.Append("EmergencyReturn: " + nav.EmergencyReturn.ToString() + ";\n");
                values.Append(SetValMatrixD("DM", nav.DockMatrix) + ";\n");
                //values.Append(SetValVector3D("BDP", nav.BaseDockPoint) + ";\n");
                values.Append(SetValVector3D("PC", nav.PlanetCenter) + ";\n");
                values.Append(SetValMatrixD("DRM", nav.DrillMatrix) + ";\n");
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
    }
}
/*
    общение с базой
*/