using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

/// <summary>
/// v1.0
/// Навигация коробля
/// </summary>
namespace OSS_BM_UPR
{
    public sealed class Program : MyGridProgram
    {
        // v1.0
        string NameObj = "[OSS]-"; // [OSS]-[BM]-
        string NameCockpit = "[OSS]-[BM]-Кресло пилота [LCD]"; // основное
        string NameCockpit1 = "[OSS]-[BM]-Кресло оператора 1 [LCD]";
        string NameCockpit2 = "[OSS]-[BM]-Кресло оператора 2 [LCD]";
        string NameCameraCourse = "[OSS]-[BM]-Камера course";
        string NameConnector1 = "[OSS]-[BM]-Коннектор парковка-1";
        string NameConnector2 = "[OSS]-[BM]-Коннектор парковка-2";
        string NameProjector = "[OSS]-[BM]-Проектор МС";
        string NameLCDInfo = "[OSS]-[BM]-LCD-INFO";
        string NameLCDInfo_Upr = "[OSS]-[BM]-LCD-INFO-UPR";
        string NameLCDInfo_Debug = "[OSS]-[BM]-LCD-INFO-DEBUG";
        string NameLCDInfo_Solar = "[OSS]-[BM]-LCD-INFO-Solar";
        string NameLSolarPanel = "[OSS]-[EML]-";
        string NameRSolarPanel = "[OSS]-[EMR]-";
        string NameLSPMotorStator = "[OSS]-[EML]-Ротор СП";
        string NameRSPMotorStator = "[OSS]-[EMR]-Ротор СП";

        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея
        string tag_mechanical_connectior = "[mechanical_connectior]";

        public enum or_mtr : int
        {
            not = 0, up = 1, down = 2, left = 3, right = 4, forward = 5, backward = 6
        };

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static LCD lcd_info;
        static LCD lcd_info_upr;
        static LCD lcd_info_solar;
        static LCD lcd_info_debug;

        static Batterys bats;
        static Connector connector1;
        static Connector connector2;
        static MechanicalConnectior mechanical_connectior;
        static ReflectorsLight reflectors_light;
        static Gyros gyros;
        static GasTanks gastanks;
        static Cockpit cockpit;
        static Cockpit cockpit_operator1;
        static Cockpit cockpit_operator2;
        static Camera camera_course;
        static Projector projector_ls;
        static SolarPanel solar_panel_left;
        static SolarPanel solar_panel_right;
        static SolarPower solar_power;
        static MotorStator motor_stator_spl;
        static MotorStator motor_stator_spr;
        static Program _scr;

        bool ship_connect1 = false;
        bool ship_connect2 = false;
        public class PText
        {
            static public string GetVector3D(Vector3D vector)
            {
                return "X: " + Math.Round(vector.GetDim(0), 2).ToString() + "Y: " + Math.Round(vector.GetDim(1), 2).ToString() + "Z: " + Math.Round(vector.GetDim(2), 2).ToString();
            }
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
            static public string GetSign(int x, int y)
            {
                if (x == y) { return "="; }
                if (x > y) { return ">"; }
                if (x < y) { return "<"; }
                return "";
            }
            static public string GetCountObj(int count, int count_on)
            {
                return count_on + GetSign(count_on, count) + count;
            }
            //------------------------------------------------------------------------
            static public string GetCapacityTanks(double perse, float capacity)
            {
                return "[ " + Math.Round(((perse * capacity) / 1000000), 1) + "МЛ / " + Math.Round((capacity / 1000000), 1) + "МЛ ]";
            }
            static public string GetThrust(float value)
            {
                return Math.Round(value / 1000000, 1) + "МН";
            }
            static public string GetCurrentThrust(float to, float cur, float max)
            {
                return "[ " + GetThrust(to) + " / " + GetThrust(cur) + " / " + GetThrust(max) + " ]";
            }
            static public string GetCurrentThrust(float cur, float max)
            {
                return "[ " + GetThrust(cur) + " / " + GetThrust(max) + " ]";
            }
            static public string GetCountDetaliThrust(int count_a, int count_h, int count_i, int count_on_a, int count_on_h, int count_on_i)
            {
                string result = "[";
                if (count_a > 0)
                {
                    result += "{А-" + count_on_a + "|" + count_a + "}";
                }
                if (count_h > 0)
                {
                    result += "{В-" + count_on_h + "|" + count_h + "}";
                }
                if (count_i > 0)
                {
                    result += "{И-" + count_on_i + "|" + count_i + "}";
                }
                result += "]";
                return result;
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

            // Команды включения\выключения
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

            //public T _obj { get { return obj; } }

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

            // Команды включения\выключения
            public void Off()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_Off");
            }
            public void On()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_On");
            }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            _scr = this;
            lcd_info = new LCD(NameLCDInfo);
            lcd_info_upr = new LCD(NameLCDInfo_Upr);
            lcd_info_debug = new LCD(NameLCDInfo_Debug);
            lcd_info_solar = new LCD(NameLCDInfo_Solar);
            cockpit = new Cockpit(NameCockpit);
            cockpit_operator1 = new Cockpit(NameCockpit1);
            cockpit_operator1.SetControl(false); // Откл. упр.
            cockpit_operator2 = new Cockpit(NameCockpit2);
            cockpit_operator2.SetControl(false); // Откл. упр.
            bats = new Batterys(NameObj);
            connector1 = new Connector(NameConnector1);
            connector2 = new Connector(NameConnector2);
            mechanical_connectior = new MechanicalConnectior(NameObj, tag_mechanical_connectior);
            mechanical_connectior.AttachDetach(mechanical_connectior.IsAttached());
            ship_connect1 = connector1.Connected;
            ship_connect2 = connector2.Connected;
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            gyros = new Gyros(NameObj);
            gastanks = new GasTanks(NameObj);
            camera_course = new Camera(NameCameraCourse);
            projector_ls = new Projector(NameProjector);
            solar_panel_left = new SolarPanel(NameLSolarPanel);
            solar_panel_right = new SolarPanel(NameRSolarPanel);
            motor_stator_spl = new MotorStator(NameLSPMotorStator);
            motor_stator_spr = new MotorStator(NameRSPMotorStator);

            solar_power = new SolarPower();
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder values_info = new StringBuilder();
            bats.Logic(argument, updateSource);
            cockpit.Logic(argument, updateSource);
            projector_ls.Logic(argument, updateSource);
            solar_power.Logic(argument, updateSource);
            switch (argument)
            {
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                // Проверим корабль не припаркован к первому коннектору
                if (!connector1.Connected) { } else { }
                // Проверим корабль не припаркован к второму коннектору
                if (!connector2.Connected) { } else { }
            }
            //values_info.Append(bats.TextInfo());
            //values_info.Append(gastanks.TextInfo());
            values_info.Append(connector1.TextInfo());
            values_info.Append(connector2.TextInfo());
            values_info.Append(cockpit.TextInfo());
            //cockpit.OutText(values_info, 0);
            lcd_info_upr.OutText(values_info);
            lcd_info_solar.OutText(solar_power.TextInfo(), false);
            ship_connect1 = connector1.Connected; // сохраним состояние
            ship_connect2 = connector2.Connected; // сохраним состояние
            //StringBuilder test_info = new StringBuilder();
            //lcd_info.OutText(test_info);
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
                //БАТАРЕЯ: [10 - 10] [0.0MW / 0.0MW]
                //|- ЗАР:  [''''''''''''''''''''''''']-0%
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
            public Connector(string name) : base(name)
            {
                if (base.obj != null)
                {

                }
            }
            public string GetInfoStatus()
            {
                switch (base.obj.Status)
                {
                    case MyShipConnectorStatus.Connected:
                        {
                            return "ПОДКЛЮЧЕН";
                        }
                    case MyShipConnectorStatus.Connectable:
                        {
                            return "ГОТОВ";
                        }
                    case MyShipConnectorStatus.Unconnected:
                        {
                            return "НЕПОДКЛЮЧЕН";
                        }
                    default:
                        {
                            return "";
                        }
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("КОННЕКТОР: [" + GetInfoStatus() + "]" + "\n");
                return values.ToString();
            }
            public void Connect()
            {
                obj.Connect();
            }
            public void Disconnect()
            {
                obj.Disconnect();
            }
        }
        public class MechanicalConnectior : BaseListTerminalBlock<IMyMechanicalConnectionBlock>
        {
            public MechanicalConnectior(string name_obj) : base(name_obj)
            {

            }
            public MechanicalConnectior(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public bool IsAttached()
            {
                bool result = false;
                foreach (IMyMechanicalConnectionBlock obj in base.list_obj)
                {
                    if (obj.IsAttached) return true;
                }
                return result;
            }
            public void Attach()
            {
                foreach (IMyMechanicalConnectionBlock obj in base.list_obj)
                {
                    obj.Attach();
                }
            }
            public void Detach()
            {
                foreach (IMyMechanicalConnectionBlock obj in base.list_obj)
                {
                    obj.Detach();
                }
            }
            public void AttachDetach(bool on)
            {
                foreach (IMyMechanicalConnectionBlock obj in base.list_obj)
                {
                    if (on)
                    {
                        obj.Attach();
                    }
                    else
                    {
                        obj.Detach();
                    }

                }
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
        public class GasTanks : BaseListTerminalBlock<IMyGasTank>
        {
            public GasTanks(string name_obj) : base(name_obj)
            {
                AutoRefillBottles(true);
            }
            public GasTanks(string name_obj, string tag) : base(name_obj, tag)
            {
                AutoRefillBottles(true);
            }
            public void AutoRefillBottles(bool on)
            {
                foreach (IMyGasTank obj in base.list_obj)
                {
                    obj.AutoRefillBottles = on;
                }
            }
            public void Stockpile(bool on)
            {
                foreach (IMyGasTank obj in base.list_obj)
                {
                    obj.Stockpile = on;
                }
            }
            public float MaxCapacity()
            {
                return base.list_obj.Select(b => b.Capacity).Sum();
            }
            public double FilledRatio()
            {
                return base.list_obj.Select(b => b.FilledRatio).Average();
            }
            public int CountAutoRefillBottles()
            {
                return base.list_obj.Where(b => b.AutoRefillBottles == true).Select(t => t.AutoRefillBottles).Count();
            }
            public int CountStockpiles()
            {
                return base.list_obj.Where(b => b.Stockpile == true).Select(t => t.Stockpile).Count();
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                //БАТАРЕЯ: [10 - 10] [0.0MW / 0.0MW]
                //|- ЗАР:  [''''''''''''''''''''''''']-0%
                values.Append("БАКИ H2: [" + Count + "] [А-" + CountAutoRefillBottles() + " З-" + CountStockpiles() + "]" + PText.GetCapacityTanks(FilledRatio(), MaxCapacity()) + "\n");
                values.Append("|- НАП:  " + PText.GetScalePersent(FilledRatio(), 20) + "\n");
                return values.ToString();
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
        public class Cockpit : BaseTerminalBlock<IMyShipController>
        {
            // V2.0
            private double current_height = 0;
            public float BaseMass { get { return base.obj.CalculateShipMass().BaseMass; } }
            public float TotalMass { get { return base.obj.CalculateShipMass().TotalMass; } }
            public float PhysicalMass { get { return base.obj.CalculateShipMass().PhysicalMass; } }
            public double CurrentHeight { get { return this.current_height; } }
            public double ShipSpeed { get { return base.obj.GetShipSpeed(); } }
            public MatrixD WorldMatrix { get { return base.obj.WorldMatrix; } }
            public IMyShipController _obj { get { return obj; } }
            public bool IsUnderControl { get { return obj.IsUnderControl; } }
            public Vector3D GetNaturalGravity { get { return obj.GetNaturalGravity(); } }
            public Matrix GetCockpitMatrix()
            {
                Matrix CockpitMatrix = new MatrixD();
                base.obj.Orientation.GetMatrix(out CockpitMatrix);
                return CockpitMatrix;
            }
            public Cockpit(string name) : base(name)
            {

            }
            public void Dampeners(bool on)
            {
                obj.DampenersOverride = on;
            }
            public void SetControl(bool on)
            {
                obj.ControlThrusters = on;
                obj.ControlWheels = on;
                obj.HandBrake = on;
                obj.ShowHorizonIndicator = on;
            }
            public double GetDistance(Vector3D target)
            {
                return (target - obj.GetPosition()).Length();
            }
            public void OutText(StringBuilder values, int num_lcd)
            {
                if (obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = obj as IMyTextSurfaceProvider;
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
                if (obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.WriteText(text, append);
                    }
                }
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

                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("Гравитация: " + base.obj.GetNaturalGravity().Length() + "\n");
                values.Append("BaseMass: " + this.BaseMass + "\n");
                values.Append("TotalMass: " + this.TotalMass + "\n");
                values.Append("Скорость: " + base.obj.GetShipSpeed() + "\n");
                values.Append("Высота: " + current_height + "\n");
                //values.Append("LinearVelocity: " + base.obj.GetShipVelocities().LinearVelocity + "\n");
                //values.Append("LinearVelocity: " + base.obj.GetShipVelocities().LinearVelocity.Length() + "\n");
                //values.Append("AngularVelocity: " + base.obj.GetShipVelocities().AngularVelocity + "\n");
                //values.Append("AngularVelocity: " + base.obj.GetShipVelocities().AngularVelocity.Length() + "\n");
                return values.ToString();
            }
        }
        public class Camera : BaseTerminalBlock<IMyCameraBlock>
        {
            public MyBlockOrientation Orientation { get { return base.obj.Orientation; } }
            public Camera(string name) : base(name)
            {
                base.obj.EnableRaycast = true;

            }
            public bool CanScan(double distance)
            {

                return base.obj.CanScan(distance);
            }
            public MyDetectedEntityInfo? Raycast(double dist_scan, float pitch_scan, float yaw_scan)
            {
                MyDetectedEntityInfo? result = null;
                //base.obj.EnableRaycast = true;
                if (this.CanScan(dist_scan))
                {
                    result = base.obj.Raycast(dist_scan, pitch_scan, yaw_scan);
                }
                //base.obj.EnableRaycast = false;
                return result;
            }
            public Vector3D GetVectorForward()
            {
                return obj.WorldMatrix.Forward;
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();

                return values.ToString();
            }
            public string GetTextDetectedEntityInfo(MyDetectedEntityInfo? info)
            {
                StringBuilder values = new StringBuilder();
                if (info != null)
                {
                    values.Append("Name: " + ((MyDetectedEntityInfo)info).Name + "\n");
                    values.Append("Type: " + ((MyDetectedEntityInfo)info).Type + "\n");
                    values.Append("HitPosition: " + ((MyDetectedEntityInfo)info).HitPosition + "\n");
                    values.Append("Orientation: " + ((MyDetectedEntityInfo)info).Orientation + "\n");
                    values.Append("Velocity: " + ((MyDetectedEntityInfo)info).Velocity + "\n");
                    values.Append("Relationship: " + ((MyDetectedEntityInfo)info).Relationship + "\n");
                    values.Append("BoundingBox: " + ((MyDetectedEntityInfo)info).BoundingBox + "\n");
                    //values.Append("TimeStamp: " + ((MyDetectedEntityInfo)info).TimeStamp + "\n");
                    //values.Append("EntityId: " + ((MyDetectedEntityInfo)info).EntityId + "\n");
                };
                return values.ToString();
            }
        }
        public class Projector : BaseTerminalBlock<IMyProjector>
        {
            public Projector(string name) : base(name)
            {

            }

            public void add_X(int x)
            {
                Vector3I pos = base.obj.ProjectionOffset;
                Vector3I new_pos = new Vector3I(pos.X + x, pos.Y, pos.Z);
                base.obj.ProjectionOffset = new_pos;
                base.obj.UpdateOffsetAndRotation();
            }
            public void inc_X(int x)
            {
                Vector3I pos = base.obj.ProjectionOffset;
                Vector3I new_pos = new Vector3I(pos.X - x, pos.Y, pos.Z);
                base.obj.ProjectionOffset = new_pos;
                base.obj.UpdateOffsetAndRotation();
            }
            public void add_Y(int y)
            {
                Vector3I pos = base.obj.ProjectionOffset;
                Vector3I new_pos = new Vector3I(pos.X, pos.Y + y, pos.Z);
                base.obj.ProjectionOffset = new_pos;
                base.obj.UpdateOffsetAndRotation();
            }
            public void inc_Y(int y)
            {
                Vector3I pos = base.obj.ProjectionOffset;
                Vector3I new_pos = new Vector3I(pos.X, pos.Y - y, pos.Z);
                base.obj.ProjectionOffset = new_pos;
                base.obj.UpdateOffsetAndRotation();
            }
            public void add_Z(int z)
            {
                Vector3I pos = base.obj.ProjectionOffset;
                Vector3I new_pos = new Vector3I(pos.X, pos.Y, pos.Z + z);
                base.obj.ProjectionOffset = new_pos;
                base.obj.UpdateOffsetAndRotation();
            }
            public void inc_Z(int z)
            {
                Vector3I pos = base.obj.ProjectionOffset;
                Vector3I new_pos = new Vector3I(pos.X, pos.Y, pos.Z - z);
                base.obj.ProjectionOffset = new_pos;
                base.obj.UpdateOffsetAndRotation();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "incX":
                        base.obj.ApplyAction("IncreaseX");
                        break;
                    case "decX":
                        base.obj.ApplyAction("DecreaseX");
                        break;
                    case "incY":
                        base.obj.ApplyAction("IncreaseY");
                        break;
                    case "decY":
                        base.obj.ApplyAction("DecreaseY");
                        break;
                    case "incZ":
                        base.obj.ApplyAction("IncreaseZ");
                        break;
                    case "decZ":
                        base.obj.ApplyAction("DecreaseZ");
                        break;
                    case "rot_incX":
                        base.obj.ApplyAction("IncreaseRotX");
                        break;
                    case "rot_decX":
                        base.obj.ApplyAction("DecreaseRotX");
                        break;
                    case "rot_incY":
                        base.obj.ApplyAction("IncreaseRotY");
                        break;
                    case "rot_decY":
                        base.obj.ApplyAction("DecreaseRotY");
                        break;
                    case "rot_incZ":
                        base.obj.ApplyAction("IncreaseRotZ");
                        break;
                    case "rot_decZ":
                        base.obj.ApplyAction("DecreaseRotZ");
                        break;
                    default:
                        break;
                }

                if (updateSource == UpdateType.Update10)
                {

                }

            }
        }
        public class SolarPanel : BaseListTerminalBlock<IMySolarPanel>
        {
            public SolarPanel(string name_obj) : base(name_obj)
            {
            }
            public SolarPanel(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public float MaxOutput { get { return this.list_obj.Sum(s => s.MaxOutput); } }
            public float CurrentOutput { get { return this.list_obj.Sum(s => s.CurrentOutput); } }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                //БАТАРЕЯ: [10 - 10] [0.0MW / 0.0MW]
                //|- ЗАР:  [''''''''''''''''''''''''']-0%
                values.Append("СОЛН. ПАНЕЛЬ: [" + Count + "] " + PText.GetCurrentOfMax(CurrentOutput, MaxOutput, "MW") + "\n");
                values.Append("|- ВЫХ:  " + PText.GetScalePersent(CurrentOutput / MaxOutput, 20) + "\n");
                return values.ToString();
            }
        }
        public class MotorStator : BaseTerminalBlock<IMyMotorStator>
        {
            public MotorStator(string name) : base(name)
            {

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

                }

            }
        }
        public class SolarPower
        {
            public bool set_solar_vector = false;
            public bool track_sun = false;
            float speed_left_motor = 0f;       // оборот в минуту
            float speed_right_motor = 0f;       // оборот в минуту
            static Vector3D solar_vector = new Vector3D(-0.000739292178424908, -0.999999582397521, 0.000537263304268998);
            float LSolarOutput = 0, RSolarOutput = 0;
            public SolarPower()
            {

            }
            public void TrackSun()
            {
                float LOutputGain = solar_panel_left.MaxOutput - LSolarOutput;
                float ROutputGain = solar_panel_right.MaxOutput - RSolarOutput;
                if (LOutputGain < 0)
                {
                    speed_left_motor = -0.1f;
                }
                else
                {
                    speed_left_motor = 0.1f;
                }
                if (ROutputGain < 0)
                {
                    speed_right_motor = 0.1f;
                }
                else
                {
                    speed_right_motor = -0.1f;
                }
                motor_stator_spl.obj.TargetVelocityRPM = speed_left_motor;
                motor_stator_spr.obj.TargetVelocityRPM = speed_right_motor;
                LSolarOutput = solar_panel_left.MaxOutput;
                RSolarOutput = solar_panel_right.MaxOutput;
            }
            public void SetSolarVector()
            {
                double gF = solar_vector.Dot(cockpit.obj.WorldMatrix.Forward);
                double gL = solar_vector.Dot(cockpit.obj.WorldMatrix.Up);
                double gU = solar_vector.Dot(cockpit.obj.WorldMatrix.Right);
                //Получаем сигналы по тангажу и крены операцией atan2
                double TargetRoll = (float)Math.Atan2(gL, -gU); // крен
                double TargetPitch = -(float)Math.Atan2(gF, -gU); // тангаж
                double TargetYaw = cockpit.obj.RotationIndicator.Y;

                gyros.SetOverride(true, new Vector3D(TargetPitch, -TargetYaw, TargetRoll), 1);
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("ОСЬ ВРАЩЕНИЯ        : " + (set_solar_vector ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("ОТСЛЕЖИВАНИЯ СОЛНЦА : " + (track_sun ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("ЛЕВАЯ - УГОЛ : " + Math.Round(motor_stator_spl.obj.Angle, 1) + ", СК.ВР : " + Math.Round(speed_left_motor, 2) + "\n");
                values.Append(solar_panel_left.TextInfo());
                values.Append("ПРАВАЯ - УГОЛ : " + Math.Round(motor_stator_spr.obj.Angle, 1) + ", СК.ВР : " + Math.Round(speed_right_motor, 2) + "\n");
                values.Append(solar_panel_right.TextInfo());
                return values.ToString();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "SSV":
                        if (set_solar_vector)
                        {
                            set_solar_vector = false;
                        }
                        else
                        {
                            set_solar_vector = true;
                        }
                        break;
                    case "TS":
                        if (track_sun)
                        {
                            track_sun = false;
                        }
                        else
                        {
                            track_sun = true;
                        }
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    if (set_solar_vector)
                    {
                        SetSolarVector();
                    }
                    else
                    {
                        gyros.SetOverride(false, 1);
                    }
                }
                if (updateSource == UpdateType.Update100)
                {
                    if (track_sun)
                    {
                        TrackSun();
                    }
                    else
                    {
                        //
                    }
                }
            }
        }
    }
}
