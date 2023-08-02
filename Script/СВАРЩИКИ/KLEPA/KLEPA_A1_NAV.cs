using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

/// <summary>
/// + Добавить рен на 45 град
/// 
/// </summary>
namespace KLEPA_A1_NAV
{
    /// <summary>
    /// Сварщик атмосферный -1
    /// </summary>
    public sealed class Program : MyGridProgram
    {
        // v3
        string NameObj = "[KLEPA_A1]";
        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея
        static float GyroMult = 1f;
        static int CriticalMass = 180000;       // Критическая масса
        static float TargetSize = 100;
        static float AlignAccelMult = 0.3f;

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static MyStorage mystorage; 
        static LCD lcd_storage;        
        static LCD lcd_info;
        static LCD lcd_debug;
        static Batterys bats;
        static Connector connector;
        static ShipWelders welders;
        static ReflectorsLight reflectors_light;
        static Gyros gyros;
        static Thrusts thrusts;
        static Cockpit cockpit;
        static LandingGears landing_gears;
        static SpecialInventory special_inventory;
        static Navigation navigation;
        static Program _scr;

        public class PText
        {
            static public string GetPersent(double perse)
            {
                return " - " + Math.Round((perse * 100), 1) + "%";
            }
            static public string GetScalePersent(double perse, int scale)
            {
                string prog = "[";
                for (int i = 0; i < Math.Round((perse * scale), 0); i++)
                {
                    prog += "|";
                }
                for (int i = 0; i < scale - Math.Round((perse * scale), 0); i++)
                {
                    prog += "'";
                }
                prog += "]" + GetPersent(perse);
                return prog;
            }
            static public string GetCurrentOfMax(float cur, float max, string units)
            {
                return "[ " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]";
            }
            static public string GetThrust(float value)
            {
                return Math.Round(value / 1000000, 1) + "МН";
            }
            static public string GetGPS(string name, Vector3D target)
            {
                return "GPS:" + name + ":" + target.GetDim(0) + ":" + target.GetDim(1) + ":" + target.GetDim(2) + ":\n";
            }
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
            mystorage = new MyStorage();
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_info = new LCD(NameObj + "-LCD-INFO");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            cockpit = new Cockpit(NameObj + "-Промышленный кокпит [LCD]");
            bats = new Batterys(NameObj);
            connector = new Connector(NameObj + "-Коннектор парковка");
            welders = new ShipWelders(NameObj);
            welders.Off();
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            gyros = new Gyros(NameObj);
            thrusts = new Thrusts(NameObj);
            landing_gears = new LandingGears(NameObj);
            special_inventory = new SpecialInventory(NameObj, "Special");
            navigation = new Navigation();
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder values_info = new StringBuilder();
            bats.Logic(argument, updateSource);
            special_inventory.Logic(argument, updateSource);

            switch (argument)
            {
                default:
                    break;
            }

            if (updateSource == UpdateType.Update10)
            {

            }
            values_info.Append(bats.TextInfo());
            values_info.Append(connector.TextInfo());
            values_info.Append(welders.TextInfo());
            values_info.Append(special_inventory.TextInfo());
            cockpit.OutText(values_info, 0);
            StringBuilder test_info = new StringBuilder();
            lcd_info.OutText(test_info);

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
                values.Append("СВАРЩИКИ: " + (base.Enabled() ? "ВКЛ" : "ОТК") + "\n");
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
        /// <summary>
        /// v06.07.2023
        /// </summary>
        public class Thrusts : BaseListTerminalBlock<IMyThrust>
        {
            private BaseController remote_control;
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
                float Value = 0;
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

        }
        public class SpecialInventory : BaseListTerminalBlock<IMyCargoContainer>
        {
            public class MyComp
            {
                public Component component { get; set; }
                public int value { get; set; }
            }
            public enum Component : int
            {
                BulletproofGlass = 0,
                Computer = 1,
                Construction = 2,
                Detector = 3,
                Display = 4,
                Girder = 5,
                InteriorPlate = 6,
                LargeTube = 7,
                MetalGrid = 8,
                Motor = 9,
                PowerCell = 10,
                RadioCommunication = 11,
                SmallTube = 12,
                SteelPlate = 13,
                Superconductor = 14,
                GravityGenerator = 15,
                Medical = 16,
                Reactor = 17,
                SolarCell = 18,
                Thrust = 19,
            };

            string current_special = "";

            List<MyComp> list_all = new List<MyComp>() {
                new MyComp() { component = Component.BulletproofGlass, value = 500 },
                new MyComp() { component = Component.SolarCell, value = 500 },
                new MyComp() { component = Component.Computer, value = 500 },
                new MyComp() { component = Component.Construction, value = 5000 },
                new MyComp() { component = Component.Detector, value = 50 },
                new MyComp() { component = Component.Display, value = 500 },
                new MyComp() { component = Component.Girder, value = 500 },
                new MyComp() { component = Component.InteriorPlate, value = 2000 },
                new MyComp() { component = Component.LargeTube, value = 500 },
                new MyComp() { component = Component.MetalGrid, value = 1000 },
                new MyComp() { component = Component.Motor, value = 2000 },
                new MyComp() { component = Component.PowerCell, value = 100 },
                new MyComp() { component = Component.RadioCommunication, value = 50 },
                new MyComp() { component = Component.SmallTube, value = 3000 },
                new MyComp() { component = Component.SteelPlate, value = 5000 }
            };
            List<MyComp> list_base = new List<MyComp>() {
                new MyComp() { component = Component.BulletproofGlass, value = 500 },
                new MyComp() { component = Component.SolarCell, value = 500 },
                new MyComp() { component = Component.Display, value = 500 },
                new MyComp() { component = Component.Motor, value = 2000 },
                new MyComp() { component = Component.Computer, value = 500 },
                new MyComp() { component = Component.Construction, value = 5000 },
                new MyComp() { component = Component.InteriorPlate, value = 2000 },
                new MyComp() { component = Component.LargeTube, value = 500 },
                new MyComp() { component = Component.MetalGrid, value = 200 },
                new MyComp() { component = Component.SmallTube, value = 3000 },
                new MyComp() { component = Component.SteelPlate, value = 5000 }
            };

            public SpecialInventory(string name_obj) : base(name_obj)
            {

            }
            public SpecialInventory(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public string SetListComponent(string list, List<MyComp> components)
            {

                string[] list_st = list.Split('\n');
                // Пройдемся по помещениям и настроим панели
                foreach (Component com in Enum.GetValues(typeof(Component)))
                {
                    int value = 0;
                    MyComp mycom = components.Where(c => c.component == com).FirstOrDefault();
                    if (mycom != null)
                    {
                        value = mycom.value;
                    }
                    int index = Array.FindIndex(list_st, element => element.Contains(com.ToString()));
                    if (index > 0)
                    {
                        int indexOfChar = list_st[index].IndexOf('='); //
                        list_st[index] = list_st[index].Substring(0, indexOfChar + 1) + value.ToString();
                    }
                }
                string result = "";
                foreach (string st in list_st)
                {
                    result += st + "\n";
                }
                return result;
            }
            public void SetComponent_Clear()
            {
                foreach (IMyCargoContainer obj in base.list_obj)
                {
                    obj.CustomData = SetListComponent(obj.CustomData, new List<MyComp>());
                }
                current_special = "Пусто";
            }
            public void SetComponent_All()
            {
                foreach (IMyCargoContainer obj in base.list_obj)
                {
                    obj.CustomData = SetListComponent(obj.CustomData, list_all);
                }
                current_special = "Все";
            }
            public void SetComponent_Base()
            {
                foreach (IMyCargoContainer obj in base.list_obj)
                {
                    obj.CustomData = SetListComponent(obj.CustomData, list_base);
                }
                current_special = "БАЗА";
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "special_clear":
                        SetComponent_Clear();
                        break;
                    case "special_all":
                        SetComponent_All();
                        break;
                    case "special_base":
                        SetComponent_Base();
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
                values.Append("Компоненты: " + current_special + "\n");
                return values.ToString();
            }
        }
        public class Navigation
        {
            public bool gravity = false;
            public bool horizont { get; private set; } = false;  // держим горизонтальное направление
            public Vector3D? TackVector { get; set; } = null;
            public enum programm : int
            {
                none = 0,
                fly_connect_base = 1,   // лететь на базу
                fly_place_work = 2,     // лететь к месту работы
            };
            public static string[] name_programm = { "", "ПОЛЕТ НА БАЗУ", "ПОЛЕТ К МЕСТУ РАБОТЫ"};
            programm curent_programm = programm.none;
            public enum mode : int
            {
                none = 0,
                base_operation = 1,
                un_dock = 2,
                to_base = 3,
                dock = 4,
                to_work= 5,
            };
            public static string[] name_mode = { "", "БАЗА", "РАСТЫКОВКА", "К БАЗЕ", "СТЫКОВКА", "НА РАБОТУ"};
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
            private Vector3D BaseDockPoint = new Vector3D(0, 0, -200);
            private Vector3D ConnectorPoint = new Vector3D(0, 0, 3);
            private Vector3D WorkPoint = new Vector3D(0, 0, 0);
            public MatrixD DockMatrix { get; private set; }
            public MatrixD WorkMatrix { get; private set; }

            public double FlyHeight;
            public bool StoneDumpNeeded { get; private set; } // Признак нужно сбросить груз
            public bool CriticalMassReached { get; private set; }// Признак критической массы
            public bool EmergencyReturn = false;
            public bool go_home = false; // вернутся домой и остатся
            public bool paused = false;
            public Navigation()
            {
                thrusts.InitThrusts(cockpit); // Привяжем трастеры к контроллеру
                LoadFromStorage();
                FindPlanetCenter();
            }
            //-------------------------------------
            public MatrixD GetNormTransMatrixFromMyPos()
            {
                MatrixD mRot;
                Vector3D V3Dcenter = MyPos;
                Vector3D V3Dup = -Vector3D.Normalize(GravVector);
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
                    SaveToStorage();
                }
            }
            public void SetWorkMatrix()
            {
                WorkMatrix = GetNormTransMatrixFromMyPos();
                WorkPoint = new Vector3D(0, 0, 0);
            }
            public void SetFlyHeight()
            {
                FlyHeight = (MyPos - PlanetCenter).Length();
                BaseDockPoint = new Vector3D(0, 0, -200);
                SaveToStorage();
            }
            public void FindPlanetCenter()
            {
                if (cockpit.obj.TryGetPlanetPosition(out PlanetCenter)) { SaveToStorage(); }
            }
            //---------------------------------------------
            public Vector3D GetNavAngles(Vector3D Target, MatrixD InvMatrix, double sfiftX = 0, double shiftZ = 0)
            {
                Vector3D V3Dcenter = cockpit.obj.GetPosition();
                Vector3D V3Dfow = cockpit.obj.WorldMatrix.Forward + V3Dcenter;
                Vector3D V3Dup = cockpit.obj.WorldMatrix.Up + V3Dcenter;
                Vector3D V3Dleft = cockpit.obj.WorldMatrix.Left + V3Dcenter;
                Vector3D GravNorm = Vector3D.Normalize(GravVector) + V3Dcenter;
                V3Dcenter = Vector3D.Transform(V3Dcenter, InvMatrix);
                V3Dfow = (Vector3D.Transform(V3Dfow, InvMatrix)) - V3Dcenter;
                V3Dup = (Vector3D.Transform(V3Dup, InvMatrix)) - V3Dcenter;
                V3Dleft = (Vector3D.Transform(V3Dleft, InvMatrix)) - V3Dcenter;
                GravNorm = Vector3D.Normalize((Vector3D.Transform(GravNorm, InvMatrix)) - V3Dcenter - new Vector3D(sfiftX, 0, shiftZ));
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
            public void FlyPlaceWork()
            {
                if (curent_mode == mode.none)
                {
                    if (connector.Connected)
                    {
                        curent_mode = mode.un_dock;
                        SaveToStorage();
                    }
                    else
                    {
                        curent_mode = mode.to_work;
                        SaveToStorage();
                    }
                }
                if (curent_mode == mode.un_dock && UnDock())
                {
                    curent_mode = mode.to_work;
                    SaveToStorage();
                }
                if (curent_mode == mode.to_work && ToWorkPoint())
                {
                    Clear();
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
                SaveToStorage();
            }
            public void Clear()
            {
                thrusts.ClearThrustOverridePersent();
                gyros.SetOverride(false, 1);
                curent_mode = mode.none;
                SaveToStorage();
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
                SaveToStorage();
            }
            public bool ToBase()
            {
                bool Complete = false;
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
                Vector3D gyrAng = GetNavAngles(ConnectorPoint, DockMatrix);
                Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, DockMatrix)))).Length() + ConnectorPoint.Length());

                MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(0)) * XMaxA) / 2;
                MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(1)) * YMaxA) / 2;
                MaxFSpeed = (float)Math.Sqrt(2 * Distance * ZMaxA) / 2;
                if (Distance < 15)
                    MaxFSpeed = MaxFSpeed / 5;
                if (Math.Abs(MyPosCon.GetDim(1)) < 1)
                    MaxUSpeed = 0.1f;
                gyros.SetOverride(true, gyrAng * GyroMult, 1);
                if (LeftVelocityVector.Length() < MaxLSpeed)
                    thrusts.SetOverrideAccel("R", (float)(MyPosCon.GetDim(0) * AlignAccelMult));
                else
                {
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                }
                float UpAccel = -(float)(MyPosCon.GetDim(1) * AlignAccelMult);
                float minUpAccel = 0.1f;
                if ((UpAccel < 0) && (UpAccel > -minUpAccel))
                    UpAccel = -minUpAccel;
                if ((UpAccel > 0) && (UpAccel < minUpAccel))
                    UpAccel = minUpAccel;
                if (UpVelocityVector.Length() < MaxUSpeed)
                    thrusts.SetOverrideAccel("U", UpAccel);
                else { thrusts.SetOverridePercent("U", 0); }
                if (((Distance > 100) || ((Math.Abs(MyPosCon.GetDim(0)) < (Distance / 10 + 0.2f)) && (Math.Abs(MyPosCon.GetDim(1)) < (Distance / 10 + 0.2f)))) && (ForwVelocityVector.Length() < MaxFSpeed))
                {
                    thrusts.SetOverrideAccel("F", (float)(Distance * AlignAccelMult));
                    thrusts.SetOverridePercent("B", 0);
                }
                else
                {
                    thrusts.SetOverridePercent("F", 0);
                    thrusts.SetOverridePercent("B", 0);
                }
                if (Distance < 6)
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
                    Vector3D gyrAng = GetNavAngles(ConnectorPoint, DockMatrix);
                    Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, DockMatrix)))).Length() + ConnectorPoint.Length());
                    gyros.SetOverride(true, gyrAng * GyroMult, 1);
                    thrusts.SetOverridePercent("U", 0);
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                    thrusts.SetOverridePercent("F", 0);
                    thrusts.SetOverrideAccel("B", 3);
                    if (Distance > 50)
                    {
                        thrusts.SetOverrideAccel("B", 0);
                        Complete = true;
                    }
                }
                OutStatusMode(0, 0, 0);
                return Complete;
            }
            public bool ToWorkPoint()
            {
                bool Complete = false;
                float MaxUSpeed, MaxFSpeed;
                Vector3D gyrAng = GetNavAngles(new Vector3D(0, 0, 0), WorkMatrix);
                Vector3D MyPosDrill = Vector3D.Transform(MyPos, WorkMatrix);
                Distance = (float)(WorkPoint - new Vector3D(MyPosDrill.GetDim(0), 0, MyPosDrill.GetDim(2))).Length();
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
            //-------------------------------------------------
            public void OutStatusMode(float MaxFSpeed, float MaxUSpeed, float MaxLSpeed)
            {
                StringBuilder values = new StringBuilder();
                values.Append(" STATUS\n");
                Vector3D MyPosDrill = Vector3D.Transform(MyPos, WorkMatrix) - WorkPoint;
                values.Append("My_Length   : " + Math.Round(MyPosDrill.Length(), 2) + "\n");
                values.Append("MyPosDrill[0]   : " + Math.Round(MyPosDrill.GetDim(0), 2) + "\n");
                values.Append("MyPosDrill[1]   : " + Math.Round(MyPosDrill.GetDim(1), 2) + "\n");
                values.Append("MyPosDrill[2]   : " + Math.Round(MyPosDrill.GetDim(2), 2) + "\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("DeltaHeight: " + Math.Round(FlyHeight - (MyPos - PlanetCenter).Length()).ToString() + "\n");
                values.Append("Distance: " + Math.Round(Distance).ToString() + "\n");
                values.Append("------------------------------------------\n");
                values.Append("ZMaxA (F-B) : " + Math.Round(ZMaxA, 2).ToString() + "MaxFSpeed: " + Math.Round(MaxFSpeed, 2).ToString() + "\n");
                values.Append("YMaxA (U-D) : " + Math.Round(YMaxA, 2).ToString() + "MaxUSpeed: " + Math.Round(MaxUSpeed, 2).ToString() + "\n");
                values.Append("XMaxA (L-R) : " + Math.Round(XMaxA, 2).ToString() + "MaxLSpeed: " + Math.Round(MaxLSpeed, 2).ToString() + "\n");
                values.Append(thrusts.TextInfo());
                lcd_debug.OutText(values);
            }
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                curent_programm = (programm)mystorage.GetValInt("curent_programm", str.ToString());
                curent_mode = (mode)mystorage.GetValInt("curent_mode", str.ToString());
                paused = mystorage.GetValBool("pause", str.ToString());
                go_home = mystorage.GetValBool("go_home", str.ToString());
                FlyHeight = mystorage.GetVal("FlyHeight", str.ToString());
                EmergencyReturn = mystorage.GetValBool("EmergencyReturn", str.ToString());
                DockMatrix = new MatrixD(mystorage.GetVal("MC11", str.ToString()), mystorage.GetVal("MC12", str.ToString()), mystorage.GetVal("MC13", str.ToString()), mystorage.GetVal("MC14", str.ToString()),
                mystorage.GetVal("MC21", str.ToString()), mystorage.GetVal("MC22", str.ToString()), mystorage.GetVal("MC23", str.ToString()), mystorage.GetVal("MC24", str.ToString()),
                mystorage.GetVal("MC31", str.ToString()), mystorage.GetVal("MC32", str.ToString()), mystorage.GetVal("MC33", str.ToString()), mystorage.GetVal("MC34", str.ToString()),
                mystorage.GetVal("MC41", str.ToString()), mystorage.GetVal("MC42", str.ToString()), mystorage.GetVal("MC43", str.ToString()), mystorage.GetVal("MC44", str.ToString()));
                PlanetCenter = new Vector3D(mystorage.GetVal("PX", str.ToString()), mystorage.GetVal("PY", str.ToString()), mystorage.GetVal("PZ", str.ToString()));
                BaseDockPoint = new Vector3D(0, 0, -200);
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                values.Append("curent_programm: " + ((int)curent_programm).ToString() + ";\n");
                values.Append("curent_mode: " + ((int)curent_mode).ToString() + ";\n");
                values.Append("pause: " + paused.ToString() + ";\n");
                values.Append("go_home: " + go_home.ToString() + ";\n");
                values.Append("FlyHeight: " + Math.Round(FlyHeight, 0) + ";\n");
                values.Append("EmergencyReturn: " + EmergencyReturn.ToString() + ";\n");
                values.Append(DockMatrix.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "MC"));
                values.Append(PlanetCenter.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PX").Replace("Y", "PY").Replace("Z", "PZ") + ";\n");
                lcd_storage.OutText(values);
            }
            public string TextInfo1()
            {
                StringBuilder values = new StringBuilder();
                values.Append("СКОРОСТЬ    : " + Math.Round(cockpit.obj.GetShipSpeed(), 2) + "\n");
                //values.Append("ВЫСОТА    : " + Math.Round(cockpit.CurrentHeight, 2) + ", Sт : " + Math.Round(S, 2) + "\n");
                //values.Append("ГОРИЗОНТ    : " + (current_vector_axis != null ? igreen.ToString() : ired.ToString()) + ",  Vector : " + (TackVector != null ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("ПАУЗА : " + (paused ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("ДОМОЙ : " + (go_home ? igreen.ToString() : ired.ToString()) + "\n");
                return values.ToString();
            }
            public string TextInfo2()
            {
                StringBuilder values = new StringBuilder();
                values.Append("Height            : " + Math.Round((MyPos - PlanetCenter).Length()).ToString() + " / " + Math.Round(FlyHeight).ToString() + "\n");
                values.Append("Distance          : " + Math.Round(Distance).ToString() + "\n");
                //values.Append("Phys./Crit.(Mass) : " + Math.Round(PhysicalMass).ToString() + " / " + CriticalMass + " " + (CriticalMassReached ? red.ToString() : green.ToString()) + "\n");
               // values.Append("Volume/Mass       : " + cargos.CurrentVolume + " / " + cargos.CurrentMass + "\n");
               // values.Append("Батарея %         : " + PText.GetPersent(bats.CurrentPersent()) + " " + (bats.CurrentPersent() <= ReturnOnCharge ? red.ToString() : green.ToString()) + "\n");
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
                    case "horizont":
                        if (curent_programm == programm.none)
                        {
                            if (horizont)
                            {
                                horizont = false;
                            }
                            else
                            {
                                horizont = true;
                            }
                        }
                        break;
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
                        SaveToStorage();
                        break;
                    case "fly_work":
                        curent_programm = programm.fly_place_work;
                        SaveToStorage();
                        break;

                    case "go_home":
                        {
                            go_home = true;
                            break;
                        }
                    case "to_base":
                        curent_mode = mode.to_base;
                        SaveToStorage();
                        break;
                    case "dock":
                        curent_mode = mode.dock;
                        SaveToStorage();
                        break;
                    case "un_dock":
                        curent_mode = mode.un_dock;
                        SaveToStorage();
                        break;
                    case "to_work":
                        curent_mode = mode.to_work;
                        SaveToStorage();
                        break;
                     default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    cockpit.Logic(argument, updateSource);
                    if (!connector.Connected)
                    {
                        if (cockpit.CurrentHeight > 5.0f)
                        {
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
                    }
                    // Обновим состояние навигации
                    UpdateCalc();
                    if (curent_programm == programm.none)
                    {
                        if (horizont)
                        {
                            Horizon();
                        }
                        else
                        {
                            gyros.SetOverride(false, 1);
                        }
                        if (welders.Enabled())
                        {
                            reflectors_light.On();
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

                }
            }
        }
        public class MyStorage
        {
            public Vector3D PlanetCenter = new Vector3D(0.50, 0.50, 0.50);
            public MyStorage()
            {

            }
            public double GetVal(string Key, string str)
            {
                string val = "0";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToDouble(val);
            }
            public int GetValInt(string Key, string str)
            {
                string val = "0";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToInt32(val);
            }
            public long GetValInt64(string Key, string str)
            {
                string val = "0";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToInt64(val);
            }
            public bool GetValBool(string Key, string str)
            {
                string val = "False";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToBoolean(val);
            }
        }

    }
}