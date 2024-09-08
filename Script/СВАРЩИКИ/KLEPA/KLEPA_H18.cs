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
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRage.Noise.Combiners;
using VRage.Scripting;
using VRageMath;

namespace KLEPA_H18
{
    /// <summary>
    /// Сварщик (не атмасферный) водородный на 18 сварщиков
    /// </summary>
    public sealed class Program : MyGridProgram
    {
        // v3
        string NameObj = "[KLEPA_H18-01]";
        static string tag_batterys_duty = "[batterys_duty]";    // дежурная батарея
        static string tag_lightings_warning = "[warning]";      // сигнализация предуприждения
        static float BaseDistance = 200f;
        static float Conn_Distance = 25f;
        static float Pos_Y_Correct = 0.0f;
        static float GyroMult = 1f;
        static int CriticalMass = 200000;       // Критическая масса
        static float TargetSize = 100;
        static float AlignAccelMult = 0.6f;
        static float ReturnOnCharge = 0.2f;     // Процент заряда
        //static float ReturnOffCharge = 0.9f;    // Процент заряда
        static float ReturnHydrogen = 0.2f;     // Возрат по водороду

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static int clock_main = 0;
        static MyStorage mystorage;
        static LCD lcd_storage;
        static LCD lcd_info;
        static LCD lcd_debug;
        static Batterys bats;
        static Connector connector;
        static ShipWelders welders;
        static ReflectorsLight reflectors_light;
        static Lightings lightings;
        static Gyros gyros;
        static Thrusts thrusts;
        static Cockpit cockpit;
        static LandingGears landing_gears;
        static HydrogenTanks hydrogen_tanks;
        static CargoComponents cargo_components;
        static Navigation navigation;
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
            public BaseListTerminalBlock(string name_obj)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "]: " + list_obj.Count());
            }
            public BaseListTerminalBlock(string name_obj, string tag)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    list_obj = list_obj.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag)).ToList();
                }
                _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "],[" + tag + "]: " + list_obj.Count());
            }
            private void Off(List<T> list)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    obj.ApplyAction("OnOff_Off");
                }
            }
            public void Off()
            {
                Off(list_obj);
            }
            private void OffOfTag(List<T> list, string tag)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(tag))
                    {
                        obj.ApplyAction("OnOff_Off");
                    }
                }
            }
            public void OffOfTag(string tag)
            {
                OffOfTag(list_obj, tag);
            }
            private void On(List<T> list)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    obj.ApplyAction("OnOff_On");
                }
            }
            public void On()
            {
                On(list_obj);
            }
            private void OnOfTag(List<T> list, string tag)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(tag))
                    {
                        obj.ApplyAction("OnOff_On");
                    }
                }
            }
            public void OnOfTag(string tag)
            {
                OnOfTag(list_obj, tag);
            }
            public bool Enabled(string tag)
            {
                foreach (IMyTerminalBlock obj in list_obj)
                {
                    if (obj.CustomName.Contains(tag) && !((IMyFunctionalBlock)obj).Enabled)
                    {
                        return false;
                    }
                }
                return true;
            }
            public bool Enabled()
            {
                foreach (IMyTerminalBlock obj in list_obj)
                {
                    if (!((IMyFunctionalBlock)obj).Enabled)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        public class BaseTerminalBlock<T> where T : class
        {
            public T obj;
            public string CustomName { get { return ((IMyTerminalBlock)this.obj).CustomName; } set { ((IMyTerminalBlock)this.obj).CustomName = value; } }
            public BaseTerminalBlock(string name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as T;
                _scr.Echo("block:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            public BaseTerminalBlock(T myobj)
            {
                obj = myobj;
                _scr.Echo("block:[" + obj.ToString() + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            public Vector3D GetPosition()
            {
                return ((IMyEntity)obj).GetPosition();
            }
            public void Off()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_Off");
            }
            public void On()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_On");
            }
        }
        public class BaseController
        {
            public IMyShipController obj;
            private double current_height = 0;
            public double CurrentHeight { get { return this.current_height; } }
            public Matrix GetCockpitMatrix()
            {
                Matrix CockpitMatrix = new MatrixD();
                this.obj.Orientation.GetMatrix(out CockpitMatrix);
                return CockpitMatrix;
            }
            public BaseController(string name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyShipController;
                _scr.Echo("base_controller:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            public void Dampeners(bool on)
            {
                this.obj.DampenersOverride = on;
            }
            public void OutText(StringBuilder values, int num_lcd)
            {
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.WriteText(values, false);
                    }
                }
            }
            public void OutText(string text, bool append, int num_lcd)
            {
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.WriteText(text, append);
                    }
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
                    if (ts != null)
                    {
                        ts.ReadText(values);
                    }
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
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_info = new LCD(NameObj + "-LCD-INFO");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            cockpit = new Cockpit(NameObj + "-Cocpit [LCD] Locked");
            bats = new Batterys(NameObj);
            connector = new Connector(NameObj + "-Коннектор Locked");
            welders = new ShipWelders(NameObj);
            welders.Off();
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            gyros = new Gyros(NameObj);
            thrusts = new Thrusts(NameObj);
            landing_gears = new LandingGears(NameObj);
            hydrogen_tanks = new HydrogenTanks(NameObj);
            cargo_components = new CargoComponents(NameObj);
            lightings = new Lightings(NameObj, tag_lightings_warning);
            lightings.Off();
            navigation = new Navigation();
            mystorage = new MyStorage();
            mystorage.LoadFromStorage();
            navigation.FindPlanetCenter();
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder values_info = new StringBuilder();
            bats.Logic(argument, updateSource);
            cargo_components.Logic(argument, updateSource);
            navigation.Logic(argument, updateSource);
            switch (argument)
            {
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                values_info.Append(bats.TextInfo());
                values_info.Append(connector.TextInfo() + " " + landing_gears.TextInfo());
                values_info.Append(welders.TextInfo());
                //values_info.Append(special_inventory.TextInfo());
                values_info.Append(navigation.TextInfo1());
                cockpit.OutText(values_info, 0);
                StringBuilder test_info = new StringBuilder();
                lcd_info.OutText(test_info);
                StringBuilder values_info1 = new StringBuilder();
                values_info1.Append(navigation.TextCritical());
                cockpit.OutText(values_info1, 1);
                if (clock_main >= 10)
                {
                    clock_main = 0;
                    StringBuilder values_info2 = new StringBuilder();
                    values_info2.Append(cargo_components.TextInfoCurr());
                    cockpit.OutText(values_info2, 2);
                }
                clock_main++;
            }
        }
        public class LCD : BaseTerminalBlock<IMyTextPanel>
        {
            public LCD(string name) : base(name)
            {
                if (base.obj != null)
                {
                    base.obj.SetValue("Content", (Int64)1);
                }
            }
            public void OutText(StringBuilder values)
            {
                if (base.obj != null)
                {
                    base.obj.WriteText(values, false);
                }
            }
            public void OutText(string text, bool append)
            {
                if (base.obj != null)
                {
                    base.obj.WriteText(text, append);
                }
            }
            public StringBuilder GetText()
            {
                StringBuilder values = new StringBuilder();
                if (base.obj != null)
                {
                    base.obj.ReadText(values);
                }
                return values;
            }
        }
        public class Batterys : BaseListTerminalBlock<IMyBatteryBlock>
        {
            public int count_work_batterys { get { return list_obj.Where(n => !((IMyTerminalBlock)n).CustomName.Contains(tag_batterys_duty)).Count(); } }
            public bool charger = false;
            public Batterys(string name_obj) : base(name_obj)
            {
                Init();
            }
            public Batterys(string name_obj, string tag) : base(name_obj, tag)
            {
                Init();
            }
            public void Init()
            {
                base.On();
                charger = IsCharger();
            }
            public float MaxPower()
            {
                return base.list_obj.Select(b => b.MaxStoredPower).Sum();
            }
            public float CurrentPower()
            {
                return base.list_obj.Select(b => b.CurrentStoredPower).Sum();
            }
            public float CurrentPersent()
            {
                return base.list_obj.Select(b => b.CurrentStoredPower).Sum() / base.list_obj.Select(b => b.MaxStoredPower).Sum();
            }
            public int CountCharger()
            {
                List<IMyBatteryBlock> res = base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Recharge).ToList();
                return res.Count();
            }
            public int CountAuto()
            {
                List<IMyBatteryBlock> res = base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Auto).ToList();
                return res.Count();
            }
            public bool IsCharger()
            {
                int count_charger = CountCharger();
                return count_work_batterys > 0 && count_charger > 0 && count_work_batterys == count_charger ? true : false;
            }
            public bool IsAuto()
            {
                int count_auto = CountAuto();
                return Count > 0 && count_auto > 0 && Count == count_auto ? true : false;
            }
            public void Charger()
            {
                foreach (IMyBatteryBlock obj in base.list_obj)
                {
                    // проверка батарея дежурного режима
                    if (!obj.CustomName.Contains(tag_batterys_duty))
                    {
                        obj.ChargeMode = ChargeMode.Recharge;
                    }
                }
                charger = IsCharger();
            }
            public void Auto()
            {
                foreach (IMyBatteryBlock obj in base.list_obj)
                {
                    obj.ChargeMode = ChargeMode.Auto;
                }
                charger = IsCharger();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "bat_charger":
                        Charger();
                        break;
                    case "bat_auto":
                        Auto();
                        break;
                    case "bat_toggle":
                        if (charger) { Auto(); } else { Charger(); }
                        break;
                    default:
                        break;
                }

                if (updateSource == UpdateType.Update10)
                {

                }

            }
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
            public long? getRemoteConnector()
            {
                List<IMyShipConnector> list_conn = new List<IMyShipConnector>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn);
                foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList())
                { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 2) return conn.EntityId; }
                return null;
            }
        }
        public class ShipWelders : BaseListTerminalBlock<IMyShipWelder>
        {
            public ShipWelders(string name_obj) : base(name_obj)
            {
            }
            public ShipWelders(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("СВАРЩИКИ: " + (base.Enabled() ? igreen.ToString() : ired.ToString()) + "\n");
                return values.ToString();
            }
        }
        public class ReflectorsLight : BaseListTerminalBlock<IMyReflectorLight>
        {
            public ReflectorsLight(string name_obj) : base(name_obj)
            {
            }
            public ReflectorsLight(string name_obj, string tag) : base(name_obj, tag)
            {

            }
        }
        public class Lightings : BaseListTerminalBlock<IMyInteriorLight>
        {
            public Lightings(string name_obj, string tag) : base(name_obj)
            {
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    list_obj = list_obj.Where(n => n.CustomName.Contains(tag)).ToList();
                }
                _scr.Echo("Найдено Lighting:[" + tag + "]: " + list_obj.Count());
            }
        }
        public class Gyros : BaseListTerminalBlock<IMyGyro>
        {
            public Gyros(string name_obj) : base(name_obj) { }
            public Gyros(string name_obj, string tag) : base(name_obj, tag) { }
            public void SetOverride(bool OverrideOnOff, Vector3 settings, float Power = 1)
            {
                foreach (IMyGyro gyro in base.list_obj)
                {
                    if ((!gyro.GyroOverride) && OverrideOnOff)
                        gyro.ApplyAction("Override");
                    gyro.GyroPower = Power;
                    gyro.Yaw = settings.GetDim(0);
                    gyro.Pitch = settings.GetDim(1);
                    gyro.Roll = settings.GetDim(2);
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
                values.Append("Yaw :" + base.list_obj.Select(g => g.Yaw).Average() + "\n");
                values.Append("Pitch :" + base.list_obj.Select(g => g.Pitch).Average() + "\n");
                values.Append("Roll :" + base.list_obj.Select(g => g.Roll).Average() + "\n");
                return values.ToString();
            }
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
        public class LandingGears : BaseListTerminalBlock<IMyLandingGear>
        {
            public LandingGears(string name_obj) : base(name_obj)
            {
            }
            public LandingGears(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public bool IsLocked()
            {
                foreach (IMyLandingGear obj in list_obj)
                {
                    if (obj.IsLocked)
                    {
                        return true;
                    }
                }
                return false;
            }
            public void AutoLock(bool on)
            {
                foreach (IMyLandingGear obj in list_obj)
                {
                    obj.AutoLock = on;
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("ШАССИ: " + (IsLocked() ? igreen.ToString() : ired.ToString()) + "\n");
                return values.ToString();
            }

        }
        public class CargoComponents
        {
            int clock = 0;
            public enum Component : int
            {
                Construction = 0,
                Girder = 1,
                MetalGrid = 2,
                InteriorPlate = 3,
                SteelPlate = 4,
                SmallTube = 5,
                LargeTube = 6,
                Motor = 7,
                Display = 8,
                BulletproofGlass = 9,
                Computer = 10,
                Reactor = 11,
                Thrust = 12,
                GravityGenerator = 13,
                Medical = 14,
                RadioCommunication = 15,
                Detector = 16,
                SolarCell = 17,
                PowerCell = 18,
                Superconductor = 19,
            };
            public class LocationCargos
            {
                public Component? component { get; set; }
                public MyItemType myItemType { get; set; }
                public MyFixedPoint Amount { get; set; }
                public IMyInventory myInventory { get; set; }
                public int num_item { get; set; }

            }

            public static string[] name_component = {
                "Строительный ком.",
                "Балка",
                "Мет. сетка",
                "Внут. пластина",
                "Ст. пластина",
                "Мал. трубка",
                "Бол. трубка",
                "Двигатель",
                "Экран",
                "Пуленепр. стекло",
                "Компьютер",
                "Ком. реактор",
                "Ком. трастер",
                "Ком. ген. гравитации",
                "Медицинский ком.",
                "Ком. радиосвязи",
                "Ком. детектора",
                "Солнечная батарея",
                "Силовая ячейка",
                "Сверхпроводник" };
            public int[] Amounts = new int[20];

            public int[] AmountsAll = new int[20] { 4000, 2000, 2000, 4000, 10000, 2000, 500, 2000, 2000, 2000, 2000, 500, 500, 500, 10, 500, 500, 2000, 2000, 2000 };
            public int[] AmountsBase = new int[20] { 5000, 500, 500, 5000, 10000, 2000, 200, 2000, 500, 0, 2000, 0, 0, 0, 0, 0, 0, 0, 1000, 0 };
            public int[] AmountsBaseOs = new int[20] { 22000, 1200, 4000, 11000, 40000, 11000, 1600, 3000, 500, 2000, 3700, 300, 100, 10, 10, 400, 300, 2000, 500, 600 };
            public List<LocationCargos> local_cargos = new List<LocationCargos>();
            public List<LocationCargos> base_cargos = new List<LocationCargos>();

            public enum cargo_mode : int
            {
                none = 0,
                all = 1,
                bases = 2,
                bases_os = 3,
            };
            public static string[] name_mode = { "ПУСТО", "ВСЕ", "БАЗОВЫЙ", "ОС-БАЗА"};
            public cargo_mode curent_mode = cargo_mode.none;

            public List<IMyTerminalBlock> cargos_local = new List<IMyTerminalBlock>();
            public List<IMyTerminalBlock> cargos_base = new List<IMyTerminalBlock>();
            public List<IMyTerminalBlock> cargos_base_os = new List<IMyTerminalBlock>();
            public string name_obj;
            public int MaxVolume { get; set; } = 0;
            public int CurrentVolume { get; set; } = 0;
            public int CurrentMass { get; set; } = 0;
            public int MaxVolumeBase { get; set; } = 0;
            public int CurrentVolumeBase { get; set; } = 0;
            public int CurrentMassBase { get; set; } = 0;
            public CargoComponents(string name_obj)
            {
                this.name_obj = name_obj;
                List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
                _scr.GridTerminalSystem.GetBlocksOfType(list, r => r.CustomName.Contains(name_obj));
                foreach (IMyTerminalBlock cargo in list)
                {
                    if (cargo.HasInventory)
                    //if ((cargo is IMyCargoContainer) || (cargo is IMyShipConnector))
                    {
                        MaxVolume += (int)cargo.GetInventory(0).MaxVolume;
                        CurrentVolume += (int)(cargo.GetInventory(0).CurrentVolume * 1000);
                        CurrentMass += (int)cargo.GetInventory(0).CurrentMass;
                        cargos_local.Add(cargo);
                    }
                }
                _scr.Echo("Найдено Cargos : " + cargos_local.Count());
            }
            public void UpdateCargos(List<IMyTerminalBlock> list_cargos, ref List<LocationCargos> cargos, ref int CurrentVolume, ref int CurrentMass)
            {
                CurrentVolume = 0;
                CurrentMass = 0;
                cargos.Clear();
                foreach (IMyTerminalBlock cargo in list_cargos)
                {
                    if (cargo != null)
                    {
                        CurrentVolume += (int)cargo.GetInventory(0).CurrentVolume;
                        CurrentMass += (int)cargo.GetInventory(0).CurrentMass;
                        var crateItems = new List<MyInventoryItem>();
                        cargo.GetInventory(0).GetItems(crateItems);
                        for (int j = crateItems.Count - 1; j >= 0; j--)
                        {
                            LocationCargos location_cargos = new LocationCargos()
                            {
                                component = null,
                                myItemType = crateItems[j].Type,
                                //SubtypeId = crateItems[j].Type.SubtypeId,
                                //Amount = (int)crateItems[j].Amount,
                                Amount = crateItems[j].Amount,
                                myInventory = cargo.GetInventory(0),
                                num_item = j
                            };
                            foreach (Component comp in Enum.GetValues(typeof(Component)))
                            {
                                if (crateItems[j].Type.SubtypeId == comp.ToString())
                                {
                                    location_cargos.component = comp;
                                }
                            }
                            if (location_cargos.Amount != MyFixedPoint.Zero)
                            {
                                cargos.Add(location_cargos);
                            }
                        }
                    }
                }
            }
            public void UpdateLocal()
            {
                int Volume = 0;
                int Mass = 0;
                UpdateCargos(cargos_local, ref local_cargos, ref Volume, ref Mass);
                this.CurrentVolume = Volume;
                this.CurrentMass = Mass;
            }
            public void UpdateBase()
            {
                int Volume = 0;
                int Mass = 0;
                List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
                _scr.GridTerminalSystem.GetBlocksOfType(list, r => r.CustomName.Contains(this.name_obj) != true);
                cargos_base.Clear();
                foreach (IMyTerminalBlock cargo in list)
                {
                    if (cargo.HasInventory)
                    //if ((cargo is IMyCargoContainer) || (cargo is IMyShipConnector))
                    {
                        MaxVolumeBase += (int)cargo.GetInventory(0).MaxVolume;
                        CurrentVolumeBase += (int)(cargo.GetInventory(0).CurrentVolume * 1000);
                        CurrentMassBase += (int)cargo.GetInventory(0).CurrentMass;
                        cargos_base.Add(cargo);
                    }
                }
                _scr.Echo("Найдено Cargos : " + cargos_base.Count());
                UpdateCargos(cargos_base, ref base_cargos, ref Volume, ref Mass);
                this.CurrentVolumeBase = Volume;
                this.CurrentMassBase = Mass;
            }
            public void Load(int[] list)
            {
                //lcd_debug.OutText("Start" + "\n", false);
                List<LocationCargos> comp_null = local_cargos.Where(l => l.component == null).ToList();
                if (comp_null != null && comp_null.Count() > 0)
                {
                    //lcd_debug.OutText("comp_null :" + comp_null.Count() + "\n", true);
                    // Уберем лишнее
                    foreach (LocationCargos lc in comp_null)
                    {
                        MyFixedPoint del_amount = lc.Amount;
                        foreach (IMyTerminalBlock bc in cargos_base)
                        {
                            if (del_amount == 0) break;
                            for (int i = 0; i < bc.InventoryCount; i++)
                            {
                                if (del_amount == 0) break;
                                IMyInventory base_inv = bc.GetInventory(i);
                                if (base_inv != null)
                                {
                                    // Проверим поместится? если нет следующий
                                    bool beadded = base_inv.CanItemsBeAdded(del_amount, comp_null[0].myItemType);
                                    //lcd_debug.OutText("Поместится :" + beadded + "\n", true);
                                    if (!beadded) continue;
                                    //lcd_debug.OutText("lc.Amount_null -> base\n", true);
                                    bool transf = lc.myInventory.TransferItemTo(base_inv, lc.num_item, null, true, null);
                                    //lcd_debug.OutText("Перенес :" + transf + "\n", true);
                                    if (transf) del_amount = 0;
                                }

                            }
                        }
                    }
                }
                for (int a = list.Length - 1; a >= 0; a--)
                {
                    Component comp = (Component)Enum.GetValues(typeof(Component)).GetValue(a);
                    //lcd_debug.OutText("Component :" + (comp != null ? comp.ToString() : "null") + "\n", true);
                    int local_amouts = local_cargos.Where(l => l.component == comp).Sum(s => (int)s.Amount);
                    //lcd_debug.OutText("local_amouts :" + local_amouts + "\n", true);
                    int base_amouts = base_cargos.Where(l => l.component == comp).Sum(s => (int)s.Amount);
                    //lcd_debug.OutText("base_amouts :" + base_amouts + "\n", true);
                    List<LocationCargos> comp_cargos = base_cargos.Where(l => l.component == comp).OrderByDescending(c => (int)c.Amount).ToList();
                    List<LocationCargos> comp_local = local_cargos.Where(l => l.component == comp).ToList();

                    if (list[a] > 0 && local_amouts < list[a])
                    {
                        // Добаввить
                        MyFixedPoint add_amount = list[a] - local_amouts;
                        //lcd_debug.OutText("add_amount :" + add_amount + "\n", true);
                        foreach (IMyTerminalBlock bc in cargos_local)
                        {
                            if (add_amount == 0) break;
                            for (int i = 0; i < bc.InventoryCount; i++)
                            {
                                if (add_amount == 0) break;
                                IMyInventory local_inv = bc.GetInventory(i);
                                if (local_inv != null)
                                {
                                    //lcd_debug.OutText("comp_cargos :" + comp_cargos.Count() + "\n", true);
                                    if (comp_cargos != null && comp_cargos.Count > 0)
                                    {
                                        // Проверим поместится? если нет следующий
                                        bool beadded = local_inv.CanItemsBeAdded(add_amount, comp_cargos[0].myItemType);
                                        //lcd_debug.OutText("Поместится :" + beadded + "\n", true);
                                        if (!beadded) continue;
                                        foreach (LocationCargos lc in comp_cargos)
                                        {
                                            if (add_amount == 0) break;
                                            if (lc.Amount >= add_amount)
                                            {
                                                //lcd_debug.OutText("lc.Amount >= add_amount\n", true);
                                                bool transf = lc.myInventory.TransferItemTo(local_inv, lc.num_item, null, true, add_amount);
                                                //lcd_debug.OutText("Перенес :" + transf + "\n", true);
                                                if (transf) add_amount = 0;
                                            }
                                            else
                                            {

                                                //lcd_debug.OutText("lc.Amount < add_amount = " + (add_amount - lc.Amount) + "\n", true);
                                                if (lc.myInventory.TransferItemTo(local_inv, lc.num_item, null, true, lc.Amount)) add_amount -= lc.Amount;
                                            }

                                        }
                                    }

                                }
                            }
                        }
                    }
                    else if ((list[a] > 0 && local_amouts > list[a]) ||
                        (list[a] == 0 && local_amouts > list[a]))
                    {
                        // Убрать
                        MyFixedPoint del_amount = local_amouts - list[a];
                        //lcd_debug.OutText("del_amount :" + del_amount + "\n", true);
                        foreach (IMyTerminalBlock bc in cargos_base)
                        {
                            if (del_amount == 0) break;
                            for (int i = 0; i < bc.InventoryCount; i++)
                            {
                                if (del_amount == 0) break;
                                IMyInventory base_inv = bc.GetInventory(i);
                                if (base_inv != null)
                                {
                                    //lcd_debug.OutText("comp_local :" + comp_local.Count() + "\n", true);
                                    if (comp_local != null && comp_local.Count > 0)
                                    {
                                        // Проверим поместится? если нет следующий
                                        bool beadded = base_inv.CanItemsBeAdded(del_amount, comp_local[0].myItemType);
                                        //lcd_debug.OutText("Поместится :" + beadded + "\n", true);
                                        if (!beadded) continue;
                                        foreach (LocationCargos lc in comp_local)
                                        {
                                            if (del_amount == 0) break;
                                            //lcd_debug.OutText("lc.Amount >= add_amount\n", true);
                                            bool transf = lc.myInventory.TransferItemTo(base_inv, lc.num_item, null, true, del_amount);
                                            //lcd_debug.OutText("Перенес :" + transf + "\n", true);
                                            if (transf) del_amount = 0;
                                        }
                                    }

                                }

                            }
                        }
                    }

                }
                UpdateLocal();
                UpdateBase();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "cargo_clear":
                        curent_mode = cargo_mode.none;
                        mystorage.SaveToStorage();
                        break;
                    case "cargo_all":
                        curent_mode = cargo_mode.all;
                        mystorage.SaveToStorage();
                        break;
                    case "cargo_base":
                        curent_mode = cargo_mode.bases;
                        mystorage.SaveToStorage();
                        break;
                    case "cargo_base_os":
                        curent_mode = cargo_mode.bases_os;
                        mystorage.SaveToStorage();
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    if (clock >= 10)
                    {
                        UpdateLocal();
                        clock = 0;
                        if (connector.Connected)
                        {
                            UpdateBase();
                            if (curent_mode == cargo_mode.none)
                            {
                                Load(Amounts);
                            }
                            else if (curent_mode == cargo_mode.all)
                            {
                                Load(AmountsAll);
                            }
                            else if (curent_mode == cargo_mode.bases)
                            {
                                Load(AmountsBase);
                            }
                            else if (curent_mode == cargo_mode.bases_os)
                            {
                                Load(AmountsBaseOs);
                            }
                        }
                    }
                    clock++;
                }
            }
            public string TextInfoCurr()
            {
                StringBuilder values = new StringBuilder();
                values.Append("ТЕК.МАССА: " + CurrentMass.ToString() + "\n");
                values.Append("|- ЗАГРУЖ:  " + PText.GetScalePersent(((float)CurrentVolume / (float)MaxVolume), 20) + "\n");
                values.Append("|- КОМПОНЕНТЫ:  " + name_mode[(int)curent_mode] + "\n");
                List<IGrouping<string, LocationCargos>> group_lc = local_cargos.GroupBy(c => c.myItemType.SubtypeId).ToList();
                foreach (IGrouping<string, LocationCargos> gr_lc in group_lc)
                {
                    values.Append(gr_lc.Key + " : " + gr_lc.Sum(c => (int)c.Amount) + "\n");
                }
                return values.ToString();
            }
            public string TextInfoBase()
            {
                StringBuilder values = new StringBuilder();
                values.Append("ТЕК.МАССА: " + CurrentMassBase.ToString() + "\n");
                values.Append("|- ЗАГРУЖ:  " + PText.GetScalePersent((float)CurrentVolumeBase / (float)MaxVolumeBase, 20) + "\n");
                values.Append("|- КОМПОНЕНТЫ:  " + name_mode[(int)curent_mode] + "\n");
                List<IGrouping<string, LocationCargos>> group_lc = base_cargos.GroupBy(c => c.myItemType.SubtypeId).ToList();
                foreach (IGrouping<string, LocationCargos> gr_lc in group_lc)
                {
                    values.Append(gr_lc.Key + " : " + gr_lc.Sum(c => (int)c.Amount) + "\n");
                }
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
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("БАКИ : [" + base.list_obj.Count() + "] [А-" + CountAutoRefillBottles + " З-" + CountStockpile + "]" + PText.GetCurrentOfMax((float)(Capacity * AverageFilledRatio) / 1000000, (float)Capacity / 1000000, "МЛ") + "\n");
                values.Append("|- ЗАП:  " + PText.GetScalePersent(AverageFilledRatio, 20) + "\n");
                return values.ToString();
            }
        }
        public class Navigation
        {
            public bool gravity = false;
            public bool horizont { get; set; } = false;  // держим горизонтальное направление
            public Vector3D? TackVector { get; set; } = null;
            public enum programm : int
            {
                none = 0,
                fly_connect_base = 1,   // лететь на базу
                fly_place_work = 2,     // лететь к месту работы
            };
            public static string[] name_programm = { "", "ПОЛЕТ НА БАЗУ", "ПОЛЕТ К МЕСТУ РАБОТЫ" };
            public programm curent_programm = programm.none;
            public enum mode : int
            {
                none = 0,
                base_operation = 1,
                un_dock = 2,
                to_base = 3,
                dock = 4,
                to_work = 5,
                align_work = 6,
            };
            public static string[] name_mode = { "", "БАЗА", "РАСТЫКОВКА", "К БАЗЕ", "СТЫКОВКА", "К РАБОТЕ", "ВЫРАВНЯТЬ" };
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
            public Vector3D BaseDockPoint = new Vector3D(0, 0, 200);
            public Vector3D ConnectorPoint = new Vector3D(0, 0, -25);
            public Vector3D WorkPoint = new Vector3D(0, 0, 0);
            public MatrixD DockMatrix { get; set; }
            public MatrixD WorkMatrix { get; set; }

            public double FlyHeight;
            public bool CriticalMassReached { get; private set; }// Признак критической массы
            public bool EmergencyReturn = false;
            public bool go_home = false; // вернутся домой и остатся
            public bool paused = false;
            public Navigation()
            {
                thrusts.InitThrusts(cockpit); // Привяжем трастеры к контроллеру
            }
            //-------------------------------------
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
            public void SetDockMatrix()
            {
                if (connector.Connected)
                {
                    DockMatrix = GetNormTransMatrixFromMyPos();
                    mystorage.SaveToStorage();
                }
            }
            public void SetWorkMatrix()
            {
                WorkMatrix = GetNormTransMatrixFromMyPos();
                WorkPoint = new Vector3D(0, 0, 0);
                mystorage.SaveToStorage();
            }
            public void SetFlyHeight()
            {
                FlyHeight = (MyPos - PlanetCenter).Length();
                BaseDockPoint = new Vector3D(0, 0, 200);
                mystorage.SaveToStorage();
            }
            public void FindPlanetCenter()
            {
                if (cockpit.obj.TryGetPlanetPosition(out PlanetCenter))
                {
                    mystorage.SaveToStorage();
                }
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
            //-----------------------------------------------
            public void FlyConnectBase()
            {
                if (curent_mode == mode.none)
                {
                    SetWorkMatrix();
                    curent_mode = mode.to_base;
                    mystorage.SaveToStorage();
                }
                if (curent_mode == mode.to_base && ToBase())
                {
                    curent_mode = mode.dock;
                    mystorage.SaveToStorage();
                }
                if (curent_mode == mode.dock && Dock())
                {
                    Clear();
                    curent_programm = programm.none;
                    mystorage.SaveToStorage();
                }
            }
            public void FlyPlaceWork()
            {
                if (curent_mode == mode.none)
                {
                    if (connector.Connected)
                    {
                        curent_mode = mode.un_dock;
                        mystorage.SaveToStorage();
                    }
                    else
                    {
                        curent_mode = mode.to_work;
                        mystorage.SaveToStorage();
                    }
                }
                if (curent_mode == mode.un_dock && UnDock())
                {
                    curent_mode = mode.to_work;
                    mystorage.SaveToStorage();
                }
                if (curent_mode == mode.to_work && ToWorkPoint())
                {

                    curent_mode = mode.align_work;
                    mystorage.SaveToStorage();
                }
                if (curent_mode == mode.align_work && WorkAlign())
                {
                    Clear();
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
                EmergencyReturn = bats.CurrentPersent() <= ReturnOnCharge || PhysicalMass >= CriticalMass || hydrogen_tanks.AverageFilledRatio < ReturnHydrogen;
            }
            public void Pause(bool enable)
            {
                if (enable)
                {
                    thrusts.ClearThrustOverridePersent();
                    gyros.SetOverride(false, 1);
                    welders.Off();
                    reflectors_light.Off();
                    paused = true;
                }
                else { paused = false; }
                mystorage.SaveToStorage();
            }
            public void Clear()
            {
                thrusts.ClearThrustOverridePersent();
                gyros.SetOverride(false, 1);
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
                welders.Off();
                reflectors_light.Off();
                mystorage.SaveToStorage();
            }
            public bool ToBase()
            {
                bool Complete = false;
                welders.Off();
                float MaxUSpeed, MaxFSpeed;
                Vector3D gyrAng = GetNavAngles(BaseDockPoint, DockMatrix);
                Vector3D MyPosCon = Vector3D.Transform(MyPos, DockMatrix);
                Distance = (float)(BaseDockPoint - new Vector3D(MyPosCon.GetDim(0), 0, MyPosCon.GetDim(2))).Length();
                MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(FlyHeight - (MyPos - PlanetCenter).Length()) * YMaxA) / 1.2f;
                MaxFSpeed = (float)Math.Sqrt(2 * Distance * ZMaxA) / 1.2f;
                gyros.SetOverride(true, gyrAng * GyroMult, 1);
                thrusts.SetOverridePercent("R", 0);
                thrusts.SetOverridePercent("L", 0);
                if (UpVelocityVector.Length() < MaxUSpeed)
                    thrusts.SetOverrideAccel("U", (float)((FlyHeight - (MyPos - PlanetCenter).Length()) * AlignAccelMult));
                else
                {
                    thrusts.SetOverridePercent("U", 0);
                    thrusts.SetOverridePercent("D", 0);
                }
                if (Distance > TargetSize)
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
                    curent_mode = mode.none;
                    Complete = true;
                }
                OutStatusMode(MaxFSpeed, MaxUSpeed, 0);
                return Complete;
            }
            public bool Dock()
            {
                bool Complete = false;
                float MaxUSpeed, MaxLSpeed, MaxFSpeed;
                Vector3D MyPosCon = Vector3D.Transform(MyPos, DockMatrix);
                Vector3D gyrAng = GetNavAngles(MyPosCon * 2 - ConnectorPoint, DockMatrix);
                //Vector3D gyrAng = GetNavAngles(ConnectorPoint, DockMatrix);
                Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, DockMatrix)))).Length() + ConnectorPoint.Length());
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
                        Complete = true;
                    }
                }
                OutStatusMode(MaxFSpeed, MaxUSpeed, MaxLSpeed);
                return Complete;
            }
            public bool UnDock()
            {
                bool Complete = false;
                Distance = 0;
                connector.obj.Disconnect();
                if (!connector.Connected)
                {
                    Vector3D MyPosCon = Vector3D.Transform(MyPos, DockMatrix);
                    //Vector3D gyrAng = GetNavAngles(ConnectorPoint, DockMatrix);
                    Vector3D gyrAng = GetNavAngles(MyPosCon * 2 - ConnectorPoint, DockMatrix);
                    Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, DockMatrix)))).Length() + ConnectorPoint.Length());
                    gyros.SetOverride(true, gyrAng * GyroMult, 1);
                    thrusts.SetOverridePercent("U", 0);
                    thrusts.SetOverridePercent("D", 0);
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                    thrusts.SetOverrideAccel("F", 10);
                    thrusts.SetOverridePercent("B", 0);
                    if (Distance > 50)
                    {
                        thrusts.SetOverrideAccel("F", 0);
                        Complete = true;
                    }
                }
                OutStatusMode(0, 0, 0);
                return Complete;
            }
            public bool ToWorkPoint()
            {
                bool Complete = false;
                welders.Off();
                float MaxUSpeed, MaxFSpeed;
                Vector3D gyrAng = GetNavAngles(new Vector3D(0, 0, 0), WorkMatrix);
                Vector3D MyPosWork = Vector3D.Transform(MyPos, WorkMatrix);
                Distance = (float)(WorkPoint - new Vector3D(MyPosWork.GetDim(0), 0, MyPosWork.GetDim(2))).Length();
                MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(FlyHeight - (MyPos - PlanetCenter).Length()) * YMaxA) / 1.2f;
                MaxFSpeed = (float)Math.Sqrt(2 * Distance * ZMaxA) / 1.2f;
                gyros.SetOverride(true, gyrAng * GyroMult, 1);
                thrusts.SetOverridePercent("R", 0);
                thrusts.SetOverridePercent("L", 0);
                if (UpVelocityVector.Length() < MaxUSpeed)
                    thrusts.SetOverrideAccel("U", (float)((FlyHeight - (MyPos - PlanetCenter).Length()) * AlignAccelMult));
                else
                {
                    thrusts.SetOverridePercent("U", 0);
                    thrusts.SetOverridePercent("D", 0);
                }
                if (Distance > TargetSize)
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
                    Complete = true;
                }
                OutStatusMode(MaxFSpeed, MaxUSpeed, 0);
                return Complete;
            }
            public bool WorkAlign()
            {
                bool Complete = false;
                welders.Off();
                float MaxUSpeed, MaxLSpeed, MaxFSpeed;
                float UpAccel = 0;
                Vector3D MyPosDrill = Vector3D.Transform(MyPos, WorkMatrix) - WorkPoint;
                Vector3D gyrAng = GetNavAngles(MyPosDrill + WorkPoint + new Vector3D(0, 0, 1), WorkMatrix);
                gyros.SetOverride(true, gyrAng * GyroMult, 1);
                MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosDrill.GetDim(0)) * XMaxA) / 2;
                MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosDrill.GetDim(1)) * YMaxA) / 2;
                MaxFSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosDrill.GetDim(2)) * ZMaxA) / 2;
                if (double.IsNaN(MaxUSpeed)) MaxUSpeed = 0.1f;
                if (Math.Abs(MyPosDrill.GetDim(1)) < 1)
                    MaxUSpeed = 0.1f;
                if (LeftVelocityVector.Length() < MaxLSpeed)
                    thrusts.SetOverrideAccel("R", (float)(MyPosDrill.GetDim(0) * AlignAccelMult));
                else
                {
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                }
                if (ForwVelocityVector.Length() < MaxFSpeed)
                    thrusts.SetOverrideAccel("B", (float)(MyPosDrill.GetDim(2) * AlignAccelMult));
                else
                {
                    thrusts.SetOverridePercent("F", 0);
                    thrusts.SetOverridePercent("B", 0);
                }
                if (UpVelocityVector.Length() < MaxUSpeed)
                {
                    UpAccel = -(float)(MyPosDrill.GetDim(1) * AlignAccelMult);
                    float minUpAccel = 0.1f;
                    if ((UpAccel < 0) && (UpAccel > -minUpAccel))
                        UpAccel = -minUpAccel;
                    if ((UpAccel > 0) && (UpAccel < minUpAccel))
                        UpAccel = minUpAccel;
                    thrusts.SetOverrideAccel("U", UpAccel);
                }
                else
                {
                    thrusts.SetOverridePercent("U", 0);
                    thrusts.SetOverridePercent("D", 0);
                }
                if (MyPosDrill.Length() < 0.5)
                {
                    Complete = true;
                }
                OutStatusMode(MaxFSpeed, MaxUSpeed, MaxLSpeed);
                return Complete;
            }
            //-------------------------------------------------
            public void OutStatusMode(float MaxFSpeed, float MaxUSpeed, float MaxLSpeed)
            {
                StringBuilder values = new StringBuilder();
                values.Append(" STATUS\n");
                //Vector3D MyPosPoint = Vector3D.Transform(MyPos, WorkMatrix) - WorkPoint;
                Vector3D MyPosPoint = Vector3D.Transform(MyPos, DockMatrix);
                values.Append("My_Length   : " + Math.Round(MyPosPoint.Length(), 2) + "\n");
                values.Append("MyPosDrill[0]   : " + Math.Round(MyPosPoint.GetDim(0), 2) + "\n");
                values.Append("MyPosDrill[1]   : " + Math.Round(MyPosPoint.GetDim(1), 2) + "\n");
                values.Append("MyPosDrill[2]   : " + Math.Round(MyPosPoint.GetDim(2), 2) + "\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("DeltaHeight: " + Math.Round(FlyHeight - (MyPos - PlanetCenter).Length()).ToString() + "\n");
                values.Append("Distance: " + Math.Round(Distance).ToString() + "\n");
                values.Append("------------------------------------------\n");
                values.Append("ZMaxA (F-B) : " + Math.Round(ZMaxA, 2).ToString() + "MaxFSpeed: " + Math.Round(MaxFSpeed, 2).ToString() + "\n");
                values.Append("YMaxA (U-D) : " + Math.Round(YMaxA, 2).ToString() + "MaxUSpeed: " + Math.Round(MaxUSpeed, 2).ToString() + "\n");
                values.Append("XMaxA (L-R) : " + Math.Round(XMaxA, 2).ToString() + "MaxLSpeed: " + Math.Round(MaxLSpeed, 2).ToString() + "\n");
                //values.Append(thrusts.TextInfo());
                lcd_debug.OutText(values);
            }
            public string TextInfo1()
            {
                StringBuilder values = new StringBuilder();
                values.Append("СКОРОСТЬ    : " + Math.Round(cockpit.obj.GetShipSpeed(), 2) + "\n");
                values.Append("ВЫСОТА    : " + Math.Round(cockpit.CurrentHeight, 2) + "\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("ПАУЗА : " + (paused ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("ДОМОЙ : " + (go_home ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("ГОРИЗОНТ    : " + (horizont ? igreen.ToString() : ired.ToString()) + "\n");
                return values.ToString();
            }
            public string TextCritical()
            {
                StringBuilder values = new StringBuilder();
                //values.Append("ВЫСОТА (Цен.план.): " + Math.Round((MyPos - PlanetCenter).Length()).ToString() + " / " + Math.Round(FlyHeight).ToString() + "\n");
                //values.Append("ДИСТАНЦИЯ         : " + Math.Round(Distance).ToString() + "\n");
                values.Append("--------------------------------------\n");
                values.Append("АВАРИЙНЫЙ ВОЗВРАТ   : " + (EmergencyReturn ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("|-ФИЗ./КРИТ.(МАССА) : " + Math.Round(PhysicalMass).ToString() + " / " + CriticalMass + " " + (CriticalMassReached ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("|-БАТАРЕЯ %         : " + PText.GetPersent(bats.CurrentPersent()) + " " + (bats.CurrentPersent() <= ReturnOnCharge ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("|-БАКИ H2 %           : " + PText.GetPersent(hydrogen_tanks.AverageFilledRatio) + " " + (hydrogen_tanks.AverageFilledRatio < ReturnHydrogen ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("--------------------------------------\n");

                return values.ToString();
            }
            public string TextTEST()
            {
                StringBuilder values = new StringBuilder();
                return values.ToString();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "horizont": if (curent_programm == programm.none) { if (horizont) { horizont = false; } else { horizont = true; } } mystorage.SaveToStorage(); break;
                    case "load": mystorage.LoadFromStorage(); break;
                    case "save":
                        mystorage.SaveToStorage();
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
                        break;
                    case "save_height":
                        SetFlyHeight();
                        break;
                    case "save_base":
                        SetDockMatrix();
                        break;
                    case "save_work":
                        SetWorkMatrix();
                        break;
                    case "fly_base":
                        curent_programm = programm.fly_connect_base;
                        mystorage.SaveToStorage();
                        break;
                    case "fly_work":
                        curent_programm = programm.fly_place_work;
                        mystorage.SaveToStorage();
                        break;
                    case "go_home":
                        {
                            go_home = true;
                            break;
                        }
                    case "to_base":
                        curent_mode = mode.to_base;
                        mystorage.SaveToStorage();
                        break;
                    case "dock":
                        curent_mode = mode.dock;
                        mystorage.SaveToStorage();
                        break;
                    case "un_dock":
                        curent_mode = mode.un_dock;
                        mystorage.SaveToStorage();
                        break;
                    case "to_work":
                        curent_mode = mode.to_work;
                        mystorage.SaveToStorage();
                        break;
                    case "align_work":
                        curent_mode = mode.align_work;
                        mystorage.SaveToStorage();
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    cockpit.Logic(argument, updateSource);
                    landing_gears.AutoLock(cockpit.CurrentHeight < 2.2f);
                    if (!connector.Connected && !landing_gears.IsLocked())
                    {
                        if (gravity || (!gravity && connector.Connectable))
                        {
                            hydrogen_tanks.Stockpile(false);
                            bats.Auto();
                            thrusts.On();

                        }
                    }
                    else
                    {
                        // Припаркован
                        welders.Off();
                        reflectors_light.Off();
                        bats.Charger();
                        thrusts.Off();
                        hydrogen_tanks.Stockpile(true);
                    }
                    // Обновим состояние навигации
                    UpdateCalc();
                    if (EmergencyReturn) lightings.On(); else lightings.Off();
                    if (curent_programm == programm.none)
                    {
                        if (horizont) { Horizon(); } else { gyros.SetOverride(false, 1); }
                        if (welders.Enabled()) { reflectors_light.On(); }
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
                        if (curent_mode == mode.to_work && !paused)
                        {
                            if (ToWorkPoint() && curent_programm == programm.none)
                            {
                                curent_mode = mode.none;
                            }
                        }
                        if (curent_mode == mode.align_work && !paused)
                        {
                            if (WorkAlign() && curent_programm == programm.none)
                            {
                                curent_mode = mode.none;
                            }
                        }
                    }
                    if (curent_programm == programm.fly_connect_base && !paused)
                    {
                        FlyConnectBase();
                    }
                    if (curent_programm == programm.fly_place_work && !paused)
                    {
                        FlyPlaceWork();
                    }

                }
            }
        }
        public class MyStorage
        {
            public MyStorage() { }
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                navigation.curent_programm = (Navigation.programm)GetValInt("curent_programm", str.ToString());
                navigation.curent_mode = (Navigation.mode)GetValInt("curent_mode", str.ToString());
                cargo_components.curent_mode = (CargoComponents.cargo_mode)GetValInt("cargo_mode", str.ToString());
                navigation.paused = GetValBool("pause", str.ToString());
                navigation.go_home = GetValBool("go_home", str.ToString());
                navigation.FlyHeight = GetValDouble("FlyHeight", str.ToString());
                navigation.EmergencyReturn = GetValBool("EmergencyReturn", str.ToString());
                navigation.DockMatrix = new MatrixD(GetValDouble("DM11", str.ToString()), GetValDouble("DM12", str.ToString()), GetValDouble("DM13", str.ToString()), GetValDouble("DM14", str.ToString()),
                GetValDouble("DM21", str.ToString()), GetValDouble("DM22", str.ToString()), GetValDouble("DM23", str.ToString()), GetValDouble("DM24", str.ToString()),
                GetValDouble("DM31", str.ToString()), GetValDouble("DM32", str.ToString()), GetValDouble("DM33", str.ToString()), GetValDouble("DM34", str.ToString()),
                GetValDouble("DM41", str.ToString()), GetValDouble("DM42", str.ToString()), GetValDouble("DM43", str.ToString()), GetValDouble("DM44", str.ToString()));
                navigation.WorkMatrix = new MatrixD(GetValDouble("WM11", str.ToString()), GetValDouble("WM12", str.ToString()), GetValDouble("WM13", str.ToString()), GetValDouble("WM14", str.ToString()),
                GetValDouble("WM21", str.ToString()), GetValDouble("WM22", str.ToString()), GetValDouble("WM23", str.ToString()), GetValDouble("WM24", str.ToString()),
                GetValDouble("WM31", str.ToString()), GetValDouble("WM32", str.ToString()), GetValDouble("WM33", str.ToString()), GetValDouble("WM34", str.ToString()),
                GetValDouble("WM41", str.ToString()), GetValDouble("WM42", str.ToString()), GetValDouble("WM43", str.ToString()), GetValDouble("WM44", str.ToString()));
                navigation.PlanetCenter = new Vector3D(GetValDouble("PX", str.ToString()), GetValDouble("PY", str.ToString()), GetValDouble("PZ", str.ToString()));
                navigation.BaseDockPoint = new Vector3D(0, 0, 200);
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                values.Append("curent_programm: " + ((int)navigation.curent_programm).ToString() + ";\n");
                values.Append("curent_mode: " + ((int)navigation.curent_mode).ToString() + ";\n");
                values.Append("cargo_mode: " + ((int)cargo_components.curent_mode).ToString() + ";\n");
                values.Append("pause: " + navigation.paused.ToString() + ";\n");
                values.Append("go_home: " + navigation.go_home.ToString() + ";\n");
                values.Append("FlyHeight: " + Math.Round(navigation.FlyHeight, 0) + ";\n");
                values.Append("EmergencyReturn: " + navigation.EmergencyReturn.ToString() + ";\n");
                values.Append(navigation.DockMatrix.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "DM"));
                values.Append(navigation.WorkMatrix.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "WM"));
                values.Append(navigation.PlanetCenter.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PX").Replace("Y", "PY").Replace("Z", "PZ") + ";\n");
                lcd_storage.OutText(values);
            }
            private string GetVal(string Key, string str, string val) { string pattern = @"(" + Key + "):([^:^;]+);"; System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern); if (match.Success) { val = match.Groups[2].Value; } return val; }
            public string GetValString(string Key, string str) { return GetVal(Key, str, ""); }
            public double GetValDouble(string Key, string str) { return Convert.ToDouble(GetVal(Key, str, "0")); }
            public int GetValInt(string Key, string str) { return Convert.ToInt32(GetVal(Key, str, "0")); }
            public long GetValInt64(string Key, string str) { return Convert.ToInt64(GetVal(Key, str, "0")); }
            public bool GetValBool(string Key, string str) { return Convert.ToBoolean(GetVal(Key, str, "False")); }
        }
    }
}
