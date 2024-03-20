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
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRage.Noise.Combiners;
using VRageMath;

namespace BULL_H
{
    /// <summary>
    /// Ракетоноситель вывода на орбиту объектов, с возвратом на планету (v4.0)
    /// </summary>
    public sealed class Program : MyGridProgram
    {
        // v4
        string NameObj = "[BULL-H-01]";
        static float BaseDistance = 200f;
        static float Vh_con_cocpit = 20f;       // Растояние от коннектора стыковки к корпиту

        static float GyroMult = 1f;
        static int CriticalMass = 3400000;       // Критическая масса
        static float MaxSpeedUD = 250.0f;       // мах скорость подъема и посадки 
        static float MinBrDistance = 1000f;     // минимальное растояние от земли, начинаем тормозить
        static float AlignAccelMult = 0.6f;
        static float CriticalOnCharge = 0.2f;     // Критический процент заряда
        static float CriticalOnH2 = 0.3f;         // Критический процент топлива

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static int clock_main = 0;
        static bool mem_thr = false;
        static MyStorage mystorage;
        static LCD lcd_storage;
        static LCD lcd_info;
        static LCD lcd_debug;
        //static LCD lcd_debug1;
        //static LCD lcd_debug2;
        static Batterys bats;
        static Connector connector_base;
        static Connector connector_cargo;
        static ShipMergeBlock mergeblock_cargo1, mergeblock_cargo2; // 
        static Gyros gyros;
        static Thrusts thrusts;
        static IMyThrust thrust_ext_dor;
        static Lightings lightings_warning;
        static Lightings lightings_cabins;
        static Cockpit cockpit;
        static HydrogenTanks hydrogen_tanks_nav;
        static Gateway gateway;
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
            //lcd_debug1 = new LCD(NameObj + "-LCD-DEBUG-1");
            //lcd_debug2 = new LCD(NameObj + "-LCD-DEBUG-2");
            cockpit = new Cockpit(NameObj + "-Cocpit [LCD] Locked");
            bats = new Batterys(NameObj);
            connector_base = new Connector(NameObj + "-Коннектор парковка");
            connector_cargo = new Connector(NameObj + "-Коннектор груз");
            mergeblock_cargo1 = new ShipMergeBlock(NameObj + "-Соединитель груз1");
            mergeblock_cargo1.On();
            mergeblock_cargo2 = new ShipMergeBlock(NameObj + "-Соединитель груз2");
            mergeblock_cargo2.On();
            gyros = new Gyros(NameObj);
            thrusts = new Thrusts(NameObj);
            thrust_ext_dor = _scr.GridTerminalSystem.GetBlockWithName(NameObj + "-H_Thrust [door]") as IMyThrust;
            lightings_warning = new Lightings(NameObj, "[warning]");
            lightings_warning.Off();
            lightings_cabins = new Lightings(NameObj, "[cabins]");
            lightings_cabins.Off();
            hydrogen_tanks_nav = new HydrogenTanks(NameObj);
            gateway = new Gateway(NameObj);
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

            navigation.Logic(argument, updateSource);
            switch (argument)
            {
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                gateway.Logic();
                if (gateway.ActiveSNExternal) {
                    mem_thr = thrust_ext_dor.IsWorking;
                    thrust_ext_dor.ApplyAction("OnOff_Off"); 
                } else { 
                    if (mem_thr) thrust_ext_dor.ApplyAction("OnOff_On"); 
                }
                if (gateway.count_internal > 0) { lightings_cabins.On(); } else { lightings_cabins.Off(); }
                values_info.Append(bats.TextInfo());
                values_info.Append(hydrogen_tanks_nav.TextInfo("H2-НОСИТЕЛЯ"));
                values_info.Append(connector_base.TextInfo("К:Base") + "\n");
                values_info.Append(connector_cargo.TextInfo("К:Cargo") + "\n");
                values_info.Append(mergeblock_cargo1.TextInfo("СОЕД-ГРУЗ-1"));
                values_info.Append(mergeblock_cargo2.TextInfo("СОЕД-ГРУЗ-2"));
                cockpit.OutText(values_info, 1);
                StringBuilder values_info1 = new StringBuilder();
                values_info1.Append(navigation.TextCritical());
                cockpit.OutText(values_info1, 0);
                //StringBuilder values_info2 = new StringBuilder();
                //values_info2.Append(thrusts.TextInfo());
                //lcd_debug2.OutText(values_info2);
                StringBuilder values_lcd_info = new StringBuilder();
                values_lcd_info.Append(bats.TextInfo());
                values_lcd_info.Append(hydrogen_tanks_nav.TextInfo("H2-НОСИТЕЛЯ"));
                values_lcd_info.Append(connector_base.TextInfo("К:Base") + "\n");
                values_lcd_info.Append(connector_cargo.TextInfo("К:Cargo") + "\n");
                values_lcd_info.Append(mergeblock_cargo1.TextInfo("СОЕД-ГРУЗ-1"));
                values_lcd_info.Append(mergeblock_cargo2.TextInfo("СОЕД-ГРУЗ-2"));
                values_lcd_info.Append(navigation.TextInfo1());
                lcd_info.OutText(values_lcd_info);
                if (clock_main >= 10)
                {
                    clock_main = 0;
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
                return count_charger > 0;
                //return count_work_batterys > 0 && count_charger > 0 && count_work_batterys == count_charger ? true : false;
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
                    //if (!obj.CustomName.Contains(tag_batterys_duty))
                    //{
                    //    obj.ChargeMode = ChargeMode.Recharge;
                    //}
                    obj.ChargeMode = ChargeMode.Recharge;
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
            public string TextStatus() { StringBuilder values = new StringBuilder(); values.Append(Connected ? igreen.ToString() : (Connectable ? iyellow.ToString() : ired.ToString())); return values.ToString(); }
            public string TextInfo(string name) { StringBuilder values = new StringBuilder(); values.Append((name != null ? name : "КОННЕКТОР") + " : " + TextStatus()); return values.ToString(); }
            public void Connect() { obj.Connect(); }
            public void Disconnect() { obj.Disconnect(); }
            public long? getEntityIdRemoteConnector() { List<IMyShipConnector> list_conn = new List<IMyShipConnector>(); _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn); foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList()) { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 3) return conn.EntityId; } return null; }
            public IMyShipConnector getRemoteConnector() { List<IMyShipConnector> list_conn = new List<IMyShipConnector>(); _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn); foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList()) { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 3) return conn; } return null; }
        }
        public class ShipMergeBlock : BaseTerminalBlock<IMyShipMergeBlock>
        {
            public MergeState State { get { return base.obj.State; } }
            public bool Unset { get { return base.obj.State == MergeState.Unset ? true : false; } }
            public bool None { get { return base.obj.State == MergeState.None ? true : false; } }
            public bool Locked { get { return base.obj.State == MergeState.Locked ? true : false; } }
            public bool Constrained { get { return base.obj.State == MergeState.Constrained ? true : false; } }
            public bool Working { get { return base.obj.State == MergeState.Working ? true : false; } }
            public ShipMergeBlock(string name) : base(name)
            {
                if (base.obj != null)
                {
                }
            }
            public string GetInfoStatus()
            {
                switch (base.obj.State)
                {
                    case MergeState.Unset:
                        {
                            return "НЕ УСТАНОВЛЕНО";
                        }
                    case MergeState.Working:
                        {
                            return "РАБОТАЕТ";
                        }
                    case MergeState.Constrained:
                        {
                            return "ДЕРЖИТ";
                        }
                    case MergeState.Locked:
                        {
                            return "ЗАБЛОКИРОВАННО";
                        }
                    default: return "-";
                }
            }
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append(name + ": " + GetInfoStatus() + "\n");
                return values.ToString();
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
            public float OverrideValue;
            public float pers;
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
                this.Value = 0;
                this.axis = axis;
                this.OverrideValue = OverrideValue;

                if (axis == "D") { MaxThrust = UpThrMax; SetOverridePercent("U", 0f); }
                else if (axis == "U") { MaxThrust = DownThrMax; SetOverridePercent("D", 0f); }
                else if (axis == "F") { MaxThrust = BackwardThrMax; SetOverridePercent("B", 0f); }
                else if (axis == "B") { MaxThrust = ForwardThrMax; SetOverridePercent("F", 0f); }
                else if (axis == "R") { MaxThrust = LeftThrMax; SetOverridePercent("L", 0f); }
                else if (axis == "L") { MaxThrust = RightThrMax; SetOverridePercent("R", 0f); }
                this.pers = (float)(OverrideValue / MaxThrust);
                if (OverrideValue == 0)
                {
                    this.Value = 0;
                }
                else
                {
                    this.Value = (float)Math.Max(OverrideValue / MaxThrust, 0.001f);
                }
                SetOverridePercent(axis, this.Value);
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
                values.Append("Grav         : " + Math.Round(this.remote_control.obj.GetNaturalGravity().Length()) + "\n");
                values.Append("axis         : " + this.axis + " , Value : " + this.Value + "\n");
                values.Append("OverrideValue         : " + this.OverrideValue + " , pers : " + this.pers + "\n");
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
        public class Gateway
        {
            private List<IMyDoor> doors = new List<IMyDoor>();
            private List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            public int count_external = 0;
            public int count_internal = 0;
            IMySensorBlock sn_external;
            IMySensorBlock sn_internal;
            IMyDoor door_external;
            IMyDoor door_internal;
            bool sn1_active = false;    // датчик входа
            bool sn2_active = false;   // датчик выхода
            public bool ActiveSNExternal { get { return sn_external.IsActive; } }
            public Gateway(string name_obj)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors, r => r.CustomName.Contains(name_obj));
                _scr.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, r => r.CustomName.Contains(name_obj));
                sn_external = sensors.Where(r => r.CustomName.Contains("[external]")).FirstOrDefault();
                sn_internal = sensors.Where(r => r.CustomName.Contains("[internal]")).FirstOrDefault();
                door_external = doors.Where(r => r.CustomName.Contains("[external]")).FirstOrDefault();
                door_internal = doors.Where(r => r.CustomName.Contains("[internal]")).FirstOrDefault();
                this.door_external.ApplyAction("OnOff_On");
                this.door_internal.ApplyAction("OnOff_On");
                this.door_external.CloseDoor();
                this.door_internal.CloseDoor();
            }
            public void Logic()
            {
                if (!sn_external.IsActive && door_external.Status == DoorStatus.Open)
                {
                    // Игрок не найден возле внутр двери
                    door_external.CloseDoor();
                }
                if (sn_external.IsActive && door_external.Status == DoorStatus.Closed && door_internal.Status == DoorStatus.Closed)
                {
                    // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                    door_external.OpenDoor();
                }
                if (!sn_internal.IsActive && door_internal.Status == DoorStatus.Open)
                {
                    // Игрок не найден возле внутр двери
                    door_internal.CloseDoor();
                }
                if (sn_internal.IsActive && door_internal.Status == DoorStatus.Closed && door_external.Status == DoorStatus.Closed)
                {
                    // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                    door_internal.OpenDoor();
                }
                // Логика направдения движения
                if (sn1_active && !sn2_active && sn_internal.IsActive)
                {
                    // Выход
                    sn2_active = true;
                    count_external--;
                    count_internal++;
                }
                if (sn2_active && !sn1_active && sn_external.IsActive)
                {
                    // Вход
                    sn1_active = true;
                    count_external++;
                    count_internal--;
                }
                if (sn2_active && sn1_active && !sn_internal.IsActive && !sn_external.IsActive)
                {
                    // Вход
                    sn1_active = false;
                    sn2_active = false;
                }

                if (!sn1_active && !sn2_active)
                {
                    // Выход
                    sn1_active = sn_external.IsActive;
                    sn2_active = sn_internal.IsActive;
                }
                if (count_external < 0) count_external = 0;
                if (count_internal < 0) count_internal = 0;
            }
        }
        public class Navigation
        {
            static int clock = 0;
            public bool gravity = false;
            public bool horizont { get; set; } = false;  // держим горизонтальное направление
            public Vector3D? TackVector { get; set; } = null;
            public enum programm : int
            {
                none = 0,
                fly_base = 1,   // лететь на базу
                fly_orbit = 2,  // лететь на орбиту
                fly_auto = 3,   // авто-полет
            };
            public static string[] name_programm = { "", "ВОЗВРАТ НА БАЗУ", "ПОЛЕТ К ОРБИТЕ", "АВТО-ПОЛЕТ" };
            public programm curent_programm = programm.none;
            public enum mode : int
            {
                none = 0,
                base_operation = 1,
                un_dock = 2,
                to_orbit = 3,
                un_dock_cargo = 4,
                to_base = 5,
                dock = 6,
            };
            public static string[] name_mode = { "", "БАЗА", "РАСТЫКОВКА", "НА ОРБИТУ", "СБРОС ГРУЗА", "НА БАЗУ", "СТЫКОВКА" };
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
            public float UpThrust { get; private set; }
            //---------------------------------------------------
            public float HDistance { get; private set; } // Дистанция по горизонтали (плоскость X,Z)
            public float VDistance { get; private set; } // Дистанция по вертикали (плоскость Y)

            public double DownBrDistance { get; set; } = 0;

            public Vector3D PlanetCenter = new Vector3D(0.50, 0.50, 0.50);
            public Vector3D ConnectorPoint = new Vector3D(0, 0, -100);
            public Vector3D WorkPoint = new Vector3D(0, 0, 0);
            public MatrixD DockMatrix { get; set; }
            Vector3D glp { get; set; }
            public bool CriticalMassReached { get; private set; }// Признак критической массы
            public bool CriticalBatteryCharge { get; private set; }// Признак критического заряда
            public bool CriticalHydrogenSupply { get; private set; }// Признак критического запаса водорода

            public bool EmergencySetpoint = false;
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
                if (connector_base.Connected)
                {
                    DockMatrix = GetNormTransMatrixFromMyPos();
                    mystorage.SaveToStorage();
                }
            }
            //public void SetFlyHeight()
            //{
            //    FlyHeight = (MyPos - PlanetCenter).Length();
            //    BaseDockPoint = new Vector3D(0, 0, 200);
            //    mystorage.SaveToStorage();
            //}
            public void FindPlanetCenter()
            {
                if (cockpit.obj.TryGetPlanetPosition(out PlanetCenter))
                {
                    mystorage.SaveToStorage();
                }
            }
            //---------------------------------------------
            //public Vector3D GetNavAngles(Vector3D Target, MatrixD InvMatrix)
            //{
            //    Vector3D V3Dcenter = cockpit.obj.GetPosition();
            //    Vector3D V3Dfow = cockpit.obj.WorldMatrix.Forward + V3Dcenter;
            //    Vector3D V3Dup = cockpit.obj.WorldMatrix.Up + V3Dcenter;
            //    Vector3D V3Dleft = cockpit.obj.WorldMatrix.Left + V3Dcenter;
            //    //Vector3D GravNorm = Vector3D.Normalize(GravVector) + V3Dcenter;
            //    V3Dcenter = Vector3D.Transform(V3Dcenter, InvMatrix);
            //    V3Dfow = (Vector3D.Transform(V3Dfow, InvMatrix)) - V3Dcenter;
            //    V3Dup = (Vector3D.Transform(V3Dup, InvMatrix)) - V3Dcenter;
            //    V3Dleft = (Vector3D.Transform(V3Dleft, InvMatrix)) - V3Dcenter;

            //    //GravNorm = Vector3D.Normalize((Vector3D.Transform(GravNorm, InvMatrix)) - V3Dcenter - new Vector3D(sfiftX, 0, shiftZ));
            //    Vector3D GravNorm = Vector3D.Normalize(new Vector3D(-0, -1, -0));
            //    Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter);

            //    if (gravity)
            //    {
            //        GravNorm = Vector3D.Normalize(GravVector);
            //        GravNorm = Vector3D.Normalize(Vector3D.Transform(GravNorm + cockpit.obj.GetPosition(), InvMatrix) - V3Dcenter);
            //    }
            //    //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
            //    double gF = GravNorm.Dot(V3Dfow);
            //    double gL = GravNorm.Dot(V3Dleft);
            //    double gU = GravNorm.Dot(V3Dup);
            //    //Получаем сигналы по тангажу и крены операцией atan2
            //    double TargetRoll = (float)Math.Atan2(gL, -gU); // крен
            //    double TargetPitch = -(float)Math.Atan2(gF, -gU); // тангаж
            //    //Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter);
            //    //Рысканием прицеливаемся на точку Target.
            //    double tF = TargetNorm.Dot(V3Dfow);
            //    double tL = TargetNorm.Dot(V3Dleft);
            //    double TargetYaw = -(float)Math.Atan2(tL, tF);

            //    if (double.IsNaN(TargetYaw)) TargetYaw = 0;
            //    if (double.IsNaN(TargetPitch)) TargetPitch = 0;
            //    if (double.IsNaN(TargetRoll)) TargetRoll = 0;
            //    return new Vector3D(TargetYaw, TargetPitch, TargetRoll);
            //}
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

                Vector3D GravNorm = Vector3D.Normalize(new Vector3D(-0, -1, -0));
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
                //glp = Vector3D.Transform(Target, cockpit.obj.WorldMatrix); //test
                Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter) * -1; // развернул
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
            public bool FlyBase()
            {
                bool Complete = false;
                if (!gravity && curent_mode == mode.none)
                {
                    if (!connector_cargo.Unconnected)
                    {
                        curent_mode = mode.un_dock_cargo;
                    }
                    else
                    {
                        curent_mode = mode.to_base;
                    }
                    mystorage.SaveToStorage();
                }
                if (curent_mode == mode.un_dock_cargo && UnDockCargo())
                {
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
                    Complete = true;
                }
                return Complete;
            }
            public bool FlyOrbit()
            {
                bool Complete = false;
                if (curent_mode == mode.none && connector_base.Connected &&
                    mergeblock_cargo1.Locked && mergeblock_cargo2.Locked)
                {
                    curent_mode = mode.un_dock;
                    mystorage.SaveToStorage();
                }
                if (curent_mode == mode.un_dock && UnDock())
                {
                    curent_mode = mode.to_orbit;
                    mystorage.SaveToStorage();
                }
                if (curent_mode == mode.to_orbit && ToOrbit())
                {
                    Complete = true;
                }
                return Complete;
            }
            public bool FlyAuto()
            {
                bool Complete = false;
                if (curent_mode == mode.none)
                {
                    FlyOrbit();
                    if (curent_mode == mode.none) { Complete = true; }
                }
                if ((curent_mode == mode.un_dock || curent_mode == mode.to_orbit) && FlyOrbit())
                {
                    curent_mode = mode.none;
                    FlyBase();
                    if (curent_mode == mode.none) { Complete = true; }
                }
                if ((curent_mode == mode.un_dock_cargo || curent_mode == mode.to_base || curent_mode == mode.dock) && FlyBase())
                {
                    Complete = true;
                }
                return Complete;

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
                UpThrust = (float)(GravVector * PhysicalMass).Dot(WMCocpit.Up);
                VelocityVector = (MyPos - MyPrevPos) * 6;
                UpVelocityVector = WMCocpit.Up * Vector3D.Dot(VelocityVector, WMCocpit.Up);
                ForwVelocityVector = WMCocpit.Forward * Vector3D.Dot(VelocityVector, WMCocpit.Forward);
                LeftVelocityVector = WMCocpit.Left * Vector3D.Dot(VelocityVector, WMCocpit.Left);
                OrientationCocpit = cockpit.GetCockpitMatrix();
                YMaxA = Math.Abs((float)Math.Min(thrusts.UpThrMax / PhysicalMass - GravVector.Length(), thrusts.DownThrMax / PhysicalMass + GravVector.Length()));
                ZMaxA = (float)Math.Min(thrusts.ForwardThrMax, thrusts.BackwardThrMax) / PhysicalMass;
                XMaxA = (float)Math.Min(thrusts.RightThrMax, thrusts.LeftThrMax) / PhysicalMass;
                // расчтет пути торможения (посадка Down-внизу)
                double a = (thrusts.DownThrMax / 1000) * (1 / (PhysicalMass / 1000));
                double t = (0 - UpVelocityVector.Length()) / -a; //t = (V - V[0]) / a
                DownBrDistance = (UpVelocityVector.Length() * t) + ((-a) * Math.Pow(t, 2)) / 2; //S = V[0] * t + ( a * t^2 ) / 2
                // Критические уставки
                CriticalMassReached = (PhysicalMass > CriticalMass);
                CriticalBatteryCharge = connector_base.Connected ? bats.CurrentPersent() < 1.0f : bats.CurrentPersent() <= CriticalOnCharge;
                CriticalHydrogenSupply = connector_base.Connected ? hydrogen_tanks_nav.AverageFilledRatio < 1.0f : hydrogen_tanks_nav.AverageFilledRatio <= CriticalOnH2;
                EmergencySetpoint = CriticalMassReached || CriticalBatteryCharge || CriticalHydrogenSupply;
            }
            public Vector3D GetLocalPosCon(Vector3D ConnectorPoint, MatrixD DockMatrix)
            {
                Vector3D MyPosCon = Vector3D.Transform(MyPos, DockMatrix);
                Vector3D gyrAng = GetNavAngles(ConnectorPoint, DockMatrix);
                // расчет дистанций
                HDistance = (float)(new Vector3D(MyPosCon.GetDim(0), 0, MyPosCon.GetDim(2))).Length();
                Vector3D b = Vector3D.Transform(PlanetCenter, DockMatrix);
                VDistance = (float)(Vector3D.ProjectOnVector(ref MyPosCon, ref b).Length());
                gyros.SetOverride(true, gyrAng * GyroMult, 1);
                return MyPosCon;
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
                paused = false;
                mystorage.SaveToStorage();
            }
            public bool ToBase()
            {
                bool Complete = false;

                float MaxFSpeed, MaxLSpeed;
                Vector3D MyPosCon = GetLocalPosCon(ConnectorPoint, DockMatrix);
                MaxFSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(2)) * XMaxA) / 4;
                MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(0)) * XMaxA) / 4;
                if (HDistance > 2.0f)
                {
                    // Подгоним по X и Z
                    thrusts.SetOverridePercent("U", 0);
                    thrusts.SetOverridePercent("D", 0);
                    thrusts.On();
                    if (ForwVelocityVector.Length() < MaxFSpeed)
                    {
                        thrusts.SetOverrideAccel("B", (float)(MyPosCon.GetDim(2) * AlignAccelMult));
                    }
                    else
                    {
                        thrusts.SetOverridePercent("F", 0); thrusts.SetOverridePercent("B", 0);

                    }
                    if (LeftVelocityVector.Length() < MaxLSpeed)
                        thrusts.SetOverrideAccel("R", (float)(MyPosCon.GetDim(0) * AlignAccelMult));
                    else
                    {
                        thrusts.SetOverridePercent("R", 0); thrusts.SetOverridePercent("L", 0);
                    }
                }
                else
                {
                    // Выполним посадку
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                    thrusts.SetOverridePercent("F", 0);
                    thrusts.SetOverridePercent("B", 0);
                    thrusts.SetOverridePercent("U", 0);
                    if ((VDistance - DownBrDistance) < MinBrDistance)
                    {
                        // Тормозим
                        thrusts.On();
                        thrusts.ClearThrustOverridePersent();
                        if (cockpit.obj.GetShipSpeed() < 0.1f)
                        {
                            gyros.SetOverride(false, 1);
                            Complete = true;
                        }
                    }
                    else
                    {
                        if (UpVelocityVector.Length() < MaxSpeedUD)
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
                OutStatusMode(MaxFSpeed, 0, MaxLSpeed);
                return Complete;
            }
            public bool Dock()
            {
                bool Complete = false;
                float MaxUSpeed, MaxLSpeed, MaxFSpeed;
                Vector3D MyPosCon = GetLocalPosCon(ConnectorPoint, DockMatrix);
                MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(0)) * XMaxA) / 4;
                MaxUSpeed = (float)Math.Sqrt(2 * VDistance * YMaxA) / 4;
                MaxFSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(2)) * YMaxA) / 4;
                if (VDistance < Vh_con_cocpit + 40) MaxUSpeed = MaxUSpeed / 5;
                if (Math.Abs((MyPosCon.GetDim(2))) < 1f) MaxFSpeed = 0.1f;
                if (Math.Abs((MyPosCon.GetDim(0))) < 1f) MaxLSpeed = 0.1f;

                if (ForwVelocityVector.Length() < MaxFSpeed)
                    thrusts.SetOverrideAccel("B", (float)(MyPosCon.GetDim(2) * AlignAccelMult));
                else
                {
                    thrusts.SetOverridePercent("B", 0); thrusts.SetOverridePercent("F", 0);
                }
                if (LeftVelocityVector.Length() < MaxLSpeed)
                    thrusts.SetOverrideAccel("R", (float)(MyPosCon.GetDim(0) * AlignAccelMult));
                else
                {
                    thrusts.SetOverridePercent("R", 0); thrusts.SetOverridePercent("L", 0);
                }
                if ((VDistance > MinBrDistance) ||
                    ((Math.Abs(MyPosCon.GetDim(1)) < VDistance) && (UpVelocityVector.Length() < MaxUSpeed) &&
                        (Math.Abs(MyPosCon.GetDim(0)) < 1.0f) && (Math.Abs(MyPosCon.GetDim(2)) < 1.0f)))
                {
                    float UpAccel = -(float)((MyPosCon.GetDim(1)) * AlignAccelMult);
                    float minUpAccel = 0.1f;
                    if ((UpAccel < 0) && (UpAccel > -minUpAccel))
                        UpAccel = -minUpAccel;
                    if ((UpAccel > 0) && (UpAccel < minUpAccel))
                        UpAccel = minUpAccel;
                    thrusts.SetOverrideAccel("U", UpAccel);
                }
                else
                {
                    thrusts.SetOverridePercent("U", 0); thrusts.SetOverridePercent("D", 0);
                }
                if (VDistance < Vh_con_cocpit + 20)
                {
                    if (connector_base.Status == MyShipConnectorStatus.Connectable)
                    {
                        connector_base.obj.Connect();
                    }
                    if (connector_base.Status == MyShipConnectorStatus.Connected)
                    {
                        thrusts.ClearThrustOverridePersent();
                        gyros.SetOverride(false, 1);
                        Complete = true;
                    }
                }
                OutStatusMode(MaxFSpeed, MaxUSpeed, MaxLSpeed);
                return Complete;
            }
            public bool UnDock()
            {
                bool Complete = false;
                hydrogen_tanks_nav.Stockpile(false);
                connector_base.obj.Disconnect();
                if (!connector_base.Connected)
                {
                    Vector3D MyPosCon = GetLocalPosCon(ConnectorPoint, DockMatrix);
                    thrusts.SetOverridePercent("U", 1.0f);
                    thrusts.SetOverridePercent("D", 0);
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                    thrusts.SetOverridePercent("F", 0);
                    thrusts.SetOverridePercent("B", 0);
                    if (VDistance > BaseDistance)
                    {
                        thrusts.SetOverridePercent("U", 0);
                        Complete = true;
                    }
                }
                OutStatusMode(0, 0, 0);
                return Complete;
            }
            public bool ToOrbit()
            {
                bool Complete = false;
                Vector3D MyPosCon = GetLocalPosCon(ConnectorPoint, DockMatrix);
                if (gravity)
                {
                    thrusts.On();
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                    thrusts.SetOverridePercent("F", 0);
                    thrusts.SetOverridePercent("B", 0);
                    if (UpVelocityVector.Length() < MaxSpeedUD)
                        thrusts.SetOverridePercent("U", 1.0f);
                    else
                    {
                        thrusts.SetOverrideN("U", -UpThrust);
                    }
                }
                else
                {
                    thrusts.ClearThrustOverridePersent();
                    gyros.SetOverride(false, 1);
                    if (cockpit.obj.GetShipSpeed() < 0.1f)
                    {
                        thrusts.Off();
                        Complete = true;
                    }
                }
                OutStatusMode(0, 0, 0);
                return Complete;
            }
            public bool UnDockCargo()
            {
                bool Complete = false;
                connector_cargo.obj.Disconnect();
                mergeblock_cargo1.Off();
                mergeblock_cargo2.Off();
                if (!connector_cargo.Connected && !mergeblock_cargo1.Locked && !mergeblock_cargo2.Locked)
                {
                    thrusts.On();
                    thrusts.SetOverridePercent("D", 1.0f);
                    clock++;
                    if (connector_cargo.Unconnected && clock > 6)
                    {

                        // Тормозим
                        thrusts.ClearThrustOverridePersent();
                        if (cockpit.obj.GetShipSpeed() < 0.1f)
                        {
                            thrusts.Off();
                            clock = 0;
                            gyros.SetOverride(false, 1);
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
                //values.Append(" STATUS\n");
                Vector3D MyPosPoint = Vector3D.Transform(MyPos, DockMatrix);
                values.Append("My_Length   : " + Math.Round(MyPosPoint.Length(), 2) + "\n");
                values.Append("MyPosDrill[0]   : " + Math.Round(MyPosPoint.GetDim(0), 2) + "\n");
                values.Append("MyPosDrill[1]   : " + Math.Round(MyPosPoint.GetDim(1), 2) + "\n");
                values.Append("MyPosDrill[2]   : " + Math.Round(MyPosPoint.GetDim(2), 2) + "\n");
                values.Append("------------------------------------------\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("Dist(вертикаль): " + Math.Round(VDistance).ToString() + "\n");
                values.Append("Dist(горизонталь): " + Math.Round(HDistance).ToString() + "\n");
                values.Append("------------------------------------------\n");
                values.Append("ZMaxA (F-B) : " + Math.Round(ZMaxA, 2).ToString() + ", MaxFSpeed: " + Math.Round(MaxFSpeed, 2).ToString() + "\n");
                values.Append("YMaxA (U-D) : " + Math.Round(YMaxA, 2).ToString() + ", MaxUSpeed: " + Math.Round(MaxUSpeed, 2).ToString() + "\n");
                values.Append("XMaxA (L-R) : " + Math.Round(XMaxA, 2).ToString() + ", MaxLSpeed: " + Math.Round(MaxLSpeed, 2).ToString() + "\n");
                values.Append(thrusts.TextInfo());
                lcd_debug.OutText(values);
            }
            public string TextInfo1()
            {
                StringBuilder values = new StringBuilder();
                values.Append("СКОРОСТЬ      : " + Math.Round(cockpit.obj.GetShipSpeed(), 2) + "\n");
                values.Append("ВЫСОТА        : " + Math.Round(cockpit.CurrentHeight, 2) + "\n");
                values.Append("ВЫСОТА (вер.) : " + Math.Round(VDistance).ToString() + "\n");
                values.Append("ВЫСОТА (гор.) : " + Math.Round(HDistance).ToString() + "\n");
                values.Append("--------------------------------------\n");
                values.Append("ГОРИЗОНТ : " + (horizont ? igreen.ToString() : ired.ToString()) + ", ");
                values.Append("ПАУЗА : " + (paused ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("--------------------------------------\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                return values.ToString();
            }
            public string TextCritical()
            {
                StringBuilder values = new StringBuilder();
                values.Append("--------------------------------------\n");
                values.Append("АВАРИЙНЫЕ УСТАВКИ   : " + (EmergencySetpoint ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("|-ФИЗ./КРИТ.(МАССА) : " + Math.Round(PhysicalMass).ToString() + " / " + CriticalMass + " " + (CriticalMassReached ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("|-БАТАРЕЯ %         : " + PText.GetPersent(bats.CurrentPersent()) + " " + (CriticalBatteryCharge ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("|-ТОПЛИВО H2 %      : " + PText.GetPersent(hydrogen_tanks_nav.AverageFilledRatio) + " " + (CriticalHydrogenSupply ? ired.ToString() : igreen.ToString()) + "\n");
                values.Append("--------------------------------------\n");
                values.Append("ПРОГРАММА : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП      : " + name_mode[(int)curent_mode] + "\n");
                return values.ToString();
            }
            public string TextTEST()
            {
                StringBuilder values = new StringBuilder();
                values.Append(PText.GetGPS("glp", glp) + "\n");
                Vector3D MyPosPoint = Vector3D.Transform(MyPos, DockMatrix);
                values.Append("My_Length   : " + Math.Round(MyPosPoint.Length(), 2) + "\n");
                values.Append("MyPosDrill[0]   : " + Math.Round(MyPosPoint.GetDim(0), 2) + "\n");
                values.Append("MyPosDrill[1]   : " + Math.Round(MyPosPoint.GetDim(1), 2) + "\n");
                values.Append("MyPosDrill[2]   : " + Math.Round(MyPosPoint.GetDim(2), 2) + "\n");
                values.Append("------------------------------------------\n");
                values.Append("СКОРОСТЬ     : " + Math.Round(cockpit.obj.GetShipSpeed(), 2) + "\n");
                //values.Append("a     : " + Math.Round(a, 2) + "t     : " + Math.Round(t, 2) + "\n");
                values.Append("UpVelocity   : " + Math.Round(UpVelocityVector.Length(), 2).ToString() + ", DownBrDistance : " + Math.Round(DownBrDistance, 2).ToString() + "\n");
                values.Append("UpDownThrust : " + PText.GetThrust((float)UpThrust) + ", DownThrMax : " + PText.GetThrust((float)thrusts.DownThrMax) + "\n");
                values.Append("------------------------------------------\n");
                values.Append("ZMaxA (F-B) : " + Math.Round(ZMaxA, 2).ToString() + "MaxFSpeed: " + "\n");
                values.Append("YMaxA (U-D) : " + Math.Round(YMaxA, 2).ToString() + "MaxUSpeed: " + "\n");
                values.Append("XMaxA (L-R) : " + Math.Round(XMaxA, 2).ToString() + "MaxLSpeed: " + "\n");
                values.Append(thrusts.TextInfo());
                return values.ToString();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "horizont": if (curent_programm == programm.none) { if (horizont) { horizont = false; } else { horizont = true; } } mystorage.SaveToStorage(); break;
                    case "load": mystorage.LoadFromStorage(); break;
                    case "save": mystorage.SaveToStorage(); break;
                    case "pause": Pause(!paused); break;
                    case "stop": Stop(); break;
                    case "clear": Clear(); curent_programm = programm.none; break;
                    case "save_base": SetDockMatrix(); break;
                    case "fly_base": curent_programm = programm.fly_base; mystorage.SaveToStorage(); break;
                    case "fly_orbit": curent_programm = programm.fly_orbit; mystorage.SaveToStorage(); break;
                    case "fly_auto": curent_programm = programm.fly_auto; mystorage.SaveToStorage(); break;
                    case "un_dock": curent_mode = mode.un_dock; mystorage.SaveToStorage(); break;
                    case "to_orbit": curent_mode = mode.to_orbit; mystorage.SaveToStorage(); break;
                    case "un_dock_cargo": curent_mode = mode.un_dock_cargo; mystorage.SaveToStorage(); break;
                    case "to_base": curent_mode = mode.to_base; mystorage.SaveToStorage(); break;
                    case "dock": curent_mode = mode.dock; mystorage.SaveToStorage(); break;
                    default: break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    cockpit.Logic(argument, updateSource);
                    if (!connector_base.Connected)
                    {
                        if (gravity || (!gravity && connector_base.Connectable))
                        {
                            bats.Auto();
                            thrusts.On();
                        }
                    }
                    else
                    {
                        hydrogen_tanks_nav.Stockpile(true);
                        // Припаркован
                        if (curent_mode == mode.base_operation || curent_mode == mode.none)
                        {
                            // Если сидим в кокпите батарея не заряжается
                            if (cockpit.obj.IsUnderControl) { bats.Auto(); } else { bats.Charger(); }
                        }
                        thrusts.Off();
                    }
                    // Обновим состояние навигации
                    UpdateCalc();
                    if (EmergencySetpoint) lightings_warning.On(); else lightings_warning.Off();
                    if (curent_programm == programm.none)
                    {
                        if (horizont) { Horizon(); } else { gyros.SetOverride(false, 1); }
                        if (curent_mode == mode.un_dock && !paused)
                        {
                            if (UnDock())
                            {
                                curent_mode = mode.none; mystorage.SaveToStorage();

                            }
                        }
                        if (curent_mode == mode.to_orbit && !paused)
                        {
                            if (ToOrbit())
                            {
                                curent_mode = mode.none; mystorage.SaveToStorage();
                            }
                        }
                        if (curent_mode == mode.un_dock_cargo && !paused)
                        {
                            if (UnDockCargo() && curent_programm == programm.none)
                            {
                                curent_mode = mode.none;
                            }
                        }
                        if (curent_mode == mode.to_base && !paused)
                        {
                            if (ToBase())
                            {
                                curent_mode = mode.none; mystorage.SaveToStorage();
                            }
                        }
                        if (curent_mode == mode.dock && !paused)
                        {
                            if (Dock())
                            {
                                curent_mode = mode.none; mystorage.SaveToStorage();
                            }
                        }
                    }
                    if (curent_programm == programm.fly_base && !paused && FlyBase())
                    {
                        curent_mode = mode.none; curent_programm = programm.none; mystorage.SaveToStorage();
                    }
                    if (curent_programm == programm.fly_orbit && !paused && FlyOrbit())
                    {
                        curent_mode = mode.none; curent_programm = programm.none; mystorage.SaveToStorage();

                    }
                    if (curent_programm == programm.fly_auto && !paused && FlyAuto())
                    {
                        curent_mode = mode.none; curent_programm = programm.none; mystorage.SaveToStorage();
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
                navigation.paused = GetValBool("pause", str.ToString());
                navigation.EmergencySetpoint = GetValBool("EmergencySetpoint", str.ToString());
                navigation.DockMatrix = new MatrixD(GetValDouble("DM11", str.ToString()), GetValDouble("DM12", str.ToString()), GetValDouble("DM13", str.ToString()), GetValDouble("DM14", str.ToString()),
                GetValDouble("DM21", str.ToString()), GetValDouble("DM22", str.ToString()), GetValDouble("DM23", str.ToString()), GetValDouble("DM24", str.ToString()),
                GetValDouble("DM31", str.ToString()), GetValDouble("DM32", str.ToString()), GetValDouble("DM33", str.ToString()), GetValDouble("DM34", str.ToString()),
                GetValDouble("DM41", str.ToString()), GetValDouble("DM42", str.ToString()), GetValDouble("DM43", str.ToString()), GetValDouble("DM44", str.ToString()));
                navigation.PlanetCenter = new Vector3D(GetValDouble("PX", str.ToString()), GetValDouble("PY", str.ToString()), GetValDouble("PZ", str.ToString()));
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                values.Append("curent_programm: " + ((int)navigation.curent_programm).ToString() + ";\n");
                values.Append("curent_mode: " + ((int)navigation.curent_mode).ToString() + ";\n");
                values.Append("pause: " + navigation.paused.ToString() + ";\n");
                values.Append("EmergencySetpoint: " + navigation.EmergencySetpoint.ToString() + ";\n");
                values.Append(navigation.DockMatrix.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "DM"));
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
