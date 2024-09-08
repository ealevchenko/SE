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
using VRage.Game.ModAPI.Ingame;
using VRageMath;
/// <summary>
/// v4.0
/// </summary>
namespace KROTIK_A1M_V2
{
    public sealed class Program : MyGridProgram
    {
        // m.v2
        string NameObj = "[KROTIK_A1]";
        string NameCockpit = "[KROTIK_A1]-Промышленный кокпит [LCD]";
        string NameRemoteControl = "[KROTIK_A1]-ДУ Парковка";
        string NameConnector = "[KROTIK_A1]-Коннектор парковка";
        string NameCameraCourse = "[KROTIK_A1]-Камера парковка";
        string NameLCDInfo = "[KROTIK_A1]-LCD-INFO";

        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея

        static LCD lcd_info;
        Batterys bats;
        Connector connector;
        ShipDrill drill;
        ReflectorsLight reflectors_light;
        Cockpit cockpit;
        Navigation navigation;

        static Program _scr;
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
            _scr = this;
            lcd_info = new LCD(NameLCDInfo);
            bats = new Batterys(NameObj);
            connector = new Connector(NameConnector);
            drill = new ShipDrill(NameObj);
            drill.Off();
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            cockpit = new Cockpit(NameCockpit);
            navigation = new Navigation(cockpit, NameObj, NameRemoteControl, NameCameraCourse);
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

            }
            //values_info.Append(bats.TextInfo());
            values_info.Append(connector.TextInfo());
            values_info.Append(drill.TextInfo());
            values_info.Append(navigation.TextInfo());
            cockpit.OutText(values_info, 0);
            //StringBuilder test_info = new StringBuilder();
            //cockpit.OutText(test_info, 1);

            StringBuilder test_info = new StringBuilder();
            test_info.Append(navigation.TextTEST());
            lcd_info.OutText(test_info);

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
        public class ShipDrill : BaseListTerminalBlock<IMyShipDrill>
        {
            public ShipDrill(string name_obj) : base(name_obj)
            {
            }
            public ShipDrill(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("БУРЫ: " + (base.Enabled() ? "ВКЛ" : "ОТК") + "\n");
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
        public class Cockpit : BaseTerminalBlock<IMyShipController>
        {
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
                    // Получить высоту над поверхностью
                    base.obj.TryGetPlanetElevation(MyPlanetElevation.Surface, out current_height);
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("Гравитация: " + base.obj.GetNaturalGravity().Length() + "\n");
                values.Append("BaseMass: " + this.BaseMass + "\n");
                values.Append("TotalMass: " + this.TotalMass + "\n");
                values.Append("PhysicalMass: " + this.PhysicalMass + "\n");
                values.Append("Скорость: " + base.obj.GetShipSpeed() + "\n");
                values.Append("Высота: " + current_height + "\n");
                //values.Append("LinearVelocity: " + base.obj.GetShipVelocities().LinearVelocity + "\n");
                //values.Append("LinearVelocity: " + base.obj.GetShipVelocities().LinearVelocity.Length() + "\n");
                //values.Append("AngularVelocity: " + base.obj.GetShipVelocities().AngularVelocity + "\n");
                //values.Append("AngularVelocity: " + base.obj.GetShipVelocities().AngularVelocity.Length() + "\n");
                return values.ToString();
            }
        }
        public class Navigation
        {
            Cockpit cockpit;
            IMyRemoteControl remote_control;
            IMyCameraBlock camera_course;
            List<IMyThrust> thrusts = new List<IMyThrust>();
            List<IMyGyro> gyros = new List<IMyGyro>();
            public bool compensate { get; private set; } = false;   // компенсируем вес
            public bool horizont { get; private set; } = false;     // держим горизонтальное направление
            public bool target { get; private set; } = false;       // рыскать на цель

            public bool height { get; private set; } = false;
            public bool curse { get; private set; } = false;

            public bool clear_velocity { get; private set; } = false;
            public bool curse_target { get; private set; } = false;
            //
            public Matrix CockpitMatrix { get; private set; } // Орентация коробля
            public Vector3D GravityVector { get; private set; } // Вектор гравитации
            public float PhysicalMass { get; private set; } // Физическая масса
            public Vector3D ShipWeight { get; private set; } // Вес коробля с учетом гравитации
            // 
            public Vector3D MyPos { get; private set; }
            public Vector3D MyPrevPos { get; private set; }
            public Vector3D VelocityVector { get; private set; }
            public Vector3D UpVelocityVector { get; private set; }
            public Vector3D ForwVelocityVector { get; private set; }
            public Vector3D LeftVelocityVector { get; private set; }
            public Vector3D LinearVelocity { get; private set; }
            public Vector3D VelocityThrust { get; private set; } // компенсация скорости

            //------------------------------------------------
            public double UpThrMax { get; private set; } = 0;
            public double DownThrMax { get; private set; } = 0;
            public double LeftThrMax { get; private set; } = 0;
            public double RightThrMax { get; private set; } = 0;
            public double ForwardThrMax { get; private set; } = 0;
            public double BackwardThrMax { get; private set; } = 0;
            //--------------------------------------------------------
            public double ForwardThrust { get; private set; } = 0;
            public double LeftThrust { get; private set; } = 0;
            public double UpThrust { get; private set; } = 0;
            public double BackwardThrust { get; private set; } = 0;
            public double RightThrust { get; private set; } = 0;
            public double DownThrust { get; private set; } = 0;
            //---------------------------------------------------------
            public float YawInput { get; private set; } = 0;
            public float RollInput { get; private set; } = 0;
            public float PitchInput { get; private set; } = 0;

            public Vector3D? TaskVector = null;
            public Vector3D? TackTarget { get; private set; }               // Точка прицеливания
            public Vector3D? TackTargetCalcPoint { get; private set; }      // Расчетная точка растояния к вектору (от центра к точке прицеливания)

            public bool IsControlCurse { get; private set; } = false;       // курс достигнут
            public bool IsControlHeight { get; private set; } = false;      // высота достигнута 
            public double TaskHeight { get; private set; } = 0;
            //public double TaskCurse { get; private set; } = 0;
            public double CurrentHeightPlanetCentr { get; private set; }    // Текущая высота относительно центра земли
            public double OldHeight { get; private set; }                   // Предыдущая высота относительно центра земли
            public double OldCurse { get; private set; }                    // Предыдущее расстояние до цели
            public double DeltaHeight { get; private set; }                 // Текущая разница высоты
            public double DeltaCurse { get; private set; }                  // Текущая разница до цели
            public double VerticalSpeedTick { get; private set; }           // Вертикальная скорость за тик
            public double VerticalSpeed { get; private set; }               // Вертикальная скорость в секунду
            public double HorizontSpeedTick { get; private set; }           // Горизонтальная скорость за тик
            public double HorizontSpeed { get; private set; }               // Горизонтальная скорость в секунду
            public float TaskVerticalSpeed { get; private set; }            // Задание Вертикальная скорость в секунду
            public float TaskHorizontSpeed { get; private set; }            // Задание Вертикальная скорость в секунду
            //
            public float KVRL { get; private set; } = 0.2f;                 // Коэф. гашения боковых скоростей
            public float KVFB { get; private set; } = 0.2f;                 // Коэф. гашения вперед\назад
            public float KVUD { get; private set; } = 0.2f;                 // Коэф. гашения вверх\вниз

            public double minDeltaHeight { get; private set; } = 0.6f;      // Минимальный диапазон высоты если  -0.6>delta<0.6
            public double minVerticalSpeed { get; private set; } = 0.6f;      // Минимальный диапазон вертикальной скорости если  -0.6>delta<0.6
            public double minDeltaCurse { get; private set; } = 1.0f;      // Минимальный диапазон до точки по курсу если  0>delta<1.0
            public double minHorizontSpeed { get; private set; } = 1.0f;      // Минимальный диапазон горизонтальной скорости если  0>delta<1.0
            public string move_ud { get; private set; }
            public string move_fb { get; private set; }
            public string move1 { get; private set; }

            public Vector3D PlanetCentr = new Vector3D(0.50, 0.50, 0.50);
            public Vector3D Target1 = new Vector3D(53634.1408339977, -26848.4945197565, 11835.781022294); // GPS:Target1:53634.1408339977:-26848.4945197565:11835.781022294:
            public Vector3D Target2 = new Vector3D(54247.1045229673, -28025.4557401103, 9975.66911975904);  // GPS:Target2:54247.1045229673:-28025.4557401103:9975.66911975904:
            public Navigation(Cockpit cockpit, string NameObj, string NameRemoteControl, string NameCameraCourse)
            {
                this.cockpit = cockpit;
                remote_control = _scr.GridTerminalSystem.GetBlockWithName(NameRemoteControl) as IMyRemoteControl;
                camera_course = _scr.GridTerminalSystem.GetBlockWithName(NameCameraCourse) as IMyCameraBlock;
                _scr.GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusts, r => (r.CustomName.Contains(NameObj)));
                _scr.GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros, r => (r.CustomName.Contains(NameObj)));
                _scr.Echo("remote_control: " + ((remote_control != null) ? ("Ок") : ("not block")));
                _scr.Echo("camera_course: " + ((camera_course != null) ? ("Ок") : ("not block")));
                _scr.Echo("thrusts: " + ((thrusts.Count() > 0) ? thrusts.Count().ToString() + "шт." : ("not block")));
                _scr.Echo("gyros: " + ((gyros.Count() > 0) ? gyros.Count().ToString() + "шт." : ("not block")));
            }
            public void SetThrustOverridePersent(float up, float down, float left, float right, float forward, float backward)
            {
                Matrix ThrusterMatrix = new MatrixD();
                foreach (IMyThrust thrust in this.thrusts)
                {
                    thrust.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == CockpitMatrix.Up)
                    {
                        thrust.ThrustOverridePercentage = up;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Down)
                    {
                        thrust.ThrustOverridePercentage = down;
                    }
                    //X
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Left)
                    {
                        thrust.ThrustOverridePercentage = left;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Right)
                    {
                        thrust.ThrustOverridePercentage = right;
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Forward)
                    {
                        thrust.ThrustOverridePercentage = forward;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Backward)
                    {
                        thrust.ThrustOverridePercentage = backward;
                    }
                }
            }
            public void ClearThrustOverridePersent()
            {
                SetThrustOverridePersent(0f, 0f, 0f, 0f, 0f, 0f);
            }
            public void SetGyro(float Yaw, float Pitch, float Roll)
            {
                foreach (IMyGyro gyro in gyros)
                {
                    gyro.Yaw = Yaw;
                    gyro.Pitch = Pitch;
                    gyro.Roll = Roll;
                }
            }
            public void GyroOver(bool over)
            {
                foreach (IMyGyro gyro in gyros)
                {
                    gyro.Yaw = 0;
                    gyro.Pitch = 0;
                    gyro.Roll = 0;
                    gyro.GyroOverride = over;
                }
            }
            public void Update()
            {
                GravityVector = remote_control.GetNaturalGravity();
                PhysicalMass = remote_control.CalculateShipMass().PhysicalMass;
                ShipWeight = GravityVector * PhysicalMass;
                MyPrevPos = MyPos;
                MyPos = remote_control.GetPosition();
                VelocityVector = (MyPos - MyPrevPos) * 6;
                UpVelocityVector = remote_control.WorldMatrix.Up * Vector3D.Dot(VelocityVector, remote_control.WorldMatrix.Up);
                ForwVelocityVector = remote_control.WorldMatrix.Forward * Vector3D.Dot(VelocityVector, remote_control.WorldMatrix.Forward);
                LeftVelocityVector = remote_control.WorldMatrix.Left * Vector3D.Dot(VelocityVector, remote_control.WorldMatrix.Left);
                LinearVelocity = remote_control.GetShipVelocities().LinearVelocity;
                CurrentHeightPlanetCentr = (PlanetCentr - MyPos).Length();
                //На рыскание просто отправляем сигнал рыскания с контроллера. Им мы будем управлять вручную если не указан вектр.
                YawInput = 0;
                // Точка задана ?
                if (TackTarget != null)
                {
                    // Наводится на точку
                    if (target)
                    {
                        TaskVector = (Vector3D)TackTarget - MyPos;
                        //TaskVector = GetTackTargetCalcVector((Vector3D)TackTarget);
                        //TaskVector = GetTackTargetAimingVector((Vector3D)TackTarget);
                        //вектор на точку
                        Vector3D T = Vector3D.Normalize((Vector3D)TaskVector);
                        //Vector3D V1 = (Vector3D)TaskVector;
                        //Vector3D V2 = remote_control.WorldMatrix.Forward;
                        //Vector3D T = Vector3D.ProjectOnVector(ref V2, ref V1);
                        //Vector3D T = Vector3D.Normalize((Vector3D)TaskVector);
                        //Рысканием прицеливаемся на точку Target.
                        double tF = T.Dot(remote_control.WorldMatrix.Forward);
                        double tL = T.Dot(remote_control.WorldMatrix.Left);
                        YawInput = -(float)Math.Atan2(tL, tF);
                    }
                    // Контроль высоты
                    TaskHeight = (PlanetCentr - (Vector3D)TackTarget).Length();
                    DeltaHeight = CurrentHeightPlanetCentr - TaskHeight;
                    VerticalSpeedTick = (CurrentHeightPlanetCentr - OldHeight);
                    VerticalSpeed = VerticalSpeedTick * 6; // Вертикальная скорость
                    OldHeight = CurrentHeightPlanetCentr;
                    TaskVerticalSpeed = (float)Math.Sqrt(2 * Math.Abs(DeltaHeight) * GravityVector.Length()) / 2; // Определим скорость
                    // Контроль курса приближения
                    Vector3D VectorShipTarget = GetTackTargetCalcVector((Vector3D)TackTarget);
                    DeltaCurse = (VectorShipTarget).Length();
                    HorizontSpeedTick = ForwVelocityVector.Length() / 6;
                    HorizontSpeed = ForwVelocityVector.Length(); // Вертикальная скорость
                    OldCurse = DeltaCurse;
                    TaskHorizontSpeed = (float)Math.Sqrt(2 * Math.Abs(DeltaCurse) * GravityVector.Length()) / 5;// Определим скорость
                }
                else
                {
                    if (remote_control.IsUnderControl)
                    {
                        YawInput = remote_control.RotationIndicator.Y;
                    }
                    else if (cockpit.IsUnderControl)
                    {
                        YawInput = cockpit._obj.RotationIndicator.Y;
                    }
                }
                // Орентация коробля
                Matrix CPMatrix = new MatrixD();
                Matrix ThrusterMatrix = new MatrixD();
                remote_control.Orientation.GetMatrix(out CPMatrix);
                CockpitMatrix = CPMatrix;
                // Максимальная эфиктивность двигателей
                UpThrMax = 0;
                DownThrMax = 0;
                LeftThrMax = 0;
                RightThrMax = 0;
                ForwardThrMax = 0;
                BackwardThrMax = 0;
                foreach (IMyThrust thrust in thrusts)
                {
                    thrust.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == CockpitMatrix.Up)
                    {
                        UpThrMax += thrust.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Down)
                    {
                        DownThrMax += thrust.MaxEffectiveThrust;
                    }
                    //X
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Left)
                    {
                        LeftThrMax += thrust.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Right)
                    {
                        RightThrMax += thrust.MaxEffectiveThrust;
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Forward)
                    {
                        ForwardThrMax += thrust.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Backward)
                    {
                        BackwardThrMax += thrust.MaxEffectiveThrust;
                    }
                }
            }
            public Vector3D GetTackTargetCalcVector(Vector3D TackTarget)
            {
                Vector3D VectorTarget = PlanetCentr - (Vector3D)TackTarget;
                Vector3D VectorShip = PlanetCentr - MyPos;
                //return Vector3D.Reject(VectorTarget, Vector3D.Normalize(VectorShip));
                return Vector3D.Reject(VectorShip, Vector3D.Normalize(VectorTarget));
            }
            public Vector3D GetTackTargetCalcPoint(Vector3D TackTarget)
            {
                return GetTackTargetCalcVector(TackTarget) + MyPos;
            }
            public Vector3D GetTackTargetAimingVector(Vector3D TackTarget)
            {
                Vector3D VectorShipTarget = GetTackTargetCalcVector(TackTarget);
                return VectorShipTarget + (Vector3D.Normalize(VectorShipTarget) * 1000);
            }
            public Vector3D GetTackTargetAimingPoint(Vector3D TackTarget)
            {
                return GetTackTargetAimingVector(TackTarget) + MyPos;
            }
            public void ForwardBackward(float power, bool clear)
            {
                Vector3D CurseVelocityThrust = new Vector3D();
                if (clear)
                {
                    // Компенсируем скорость
                    //CurseVelocityThrust = VelocityThrust * KVFB;
                    CurseVelocityThrust = GravityVector * ShipWeight * LinearVelocity * KVFB;
                }
                ForwardThrust = (ShipWeight + CurseVelocityThrust).Dot(remote_control.WorldMatrix.Forward);
                BackwardThrust = -ForwardThrust;
                if (!clear)
                {
                    if (power > 0) // вперед
                    {
                        double delta = BackwardThrMax - BackwardThrust;
                        if (delta > 0) { BackwardThrust = BackwardThrust + (delta * Math.Abs(power)); }
                        ForwardThrust = 0;
                        move_fb = "Backward :" + power.ToString();
                    }
                    else if (power < 0) // Назад
                    {
                        double delta = ForwardThrMax - ForwardThrust;
                        if (delta > 0) { ForwardThrust = ForwardThrust + (delta * Math.Abs(power)); }
                        BackwardThrust = 0;
                        move_fb = "Forward :" + power.ToString();
                    }
                }
            }
            public void UpDown(float power, bool clear)
            {
                Vector3D HeightVelocityThrust = new Vector3D();

                if (clear)
                {
                    // Компенсируем скорость
                    //HeightVelocityThrust = VelocityThrust * KVUD;
                    HeightVelocityThrust = GravityVector * ShipWeight * LinearVelocity * KVUD;
                }
                UpThrust = (ShipWeight + HeightVelocityThrust).Dot(remote_control.WorldMatrix.Up);
                DownThrust = -UpThrust;
                if (!clear)
                {
                    if (power > 0) // Поднять
                    {
                        double delta = DownThrMax - DownThrust;
                        if (delta > 0) { DownThrust = DownThrust + (delta * Math.Abs(power)); }
                        UpThrust = 0;
                        move_ud = "DOWN :" + power.ToString();
                    }
                    else if (power < 0) // Опустить
                    {
                        double delta = UpThrMax - UpThrust;
                        if (delta > 0) { UpThrust = UpThrust + (delta * Math.Abs(power)); }
                        DownThrust = 0;
                        move_ud = "UP :" + power.ToString();
                    }
                }
            }
            public void LeftRight(float power, bool clear)
            {
                Vector3D RLVelocityThrust = new Vector3D();
                if (clear)
                {
                    // Компенсируем скорость
                    //RLVelocityThrust = VelocityThrust * KVRL;
                    RLVelocityThrust = GravityVector * ShipWeight * LinearVelocity * KVRL;
                }
                LeftThrust = (ShipWeight + RLVelocityThrust).Dot(remote_control.WorldMatrix.Left);
                RightThrust = -LeftThrust;
                if (!clear)
                {
                    if (power > 0) // Поднять
                    {
                        double delta = RightThrMax - RightThrust;
                        if (delta > 0) { RightThrust = RightThrust + (delta * Math.Abs(power)); }
                        LeftThrust = 0;
                        move_ud = "Right :" + power.ToString();
                    }
                    else if (power < 0) // Опустить
                    {
                        double delta = LeftThrMax - LeftThrust;
                        if (delta > 0) { LeftThrust = LeftThrust + (delta * Math.Abs(power)); }
                        RightThrust = 0;
                        move_ud = "Left :" + power.ToString();
                    }
                }
            }
            public bool ControlHeight()
            {
                if (DeltaHeight > -minDeltaHeight && DeltaHeight < minDeltaHeight && VerticalSpeed >= -minVerticalSpeed && VerticalSpeed < minVerticalSpeed)
                {
                    move_ud = "STOP";
                    UpDown(0.0f, true);
                    return true;
                }
                else
                {
                    if (VerticalSpeed < -minVerticalSpeed)
                    {
                        // летим вниз
                        if (DeltaHeight < -minDeltaHeight)
                        {
                            // а, надо вверх - ТОРМОЗИМ
                            if (TaskVerticalSpeed < 10f || Math.Abs(DeltaHeight) < 100f)
                            {
                                UpDown(0.3f, false);
                                //UpDown(0.0f, true);

                            }
                            else { UpDown(1.0f, false); }
                        }
                        else
                        {
                            if (Math.Abs(VerticalSpeed) < TaskVerticalSpeed - Math.Abs(VerticalSpeedTick))
                            {
                                // разгон
                                UpDown((float)-(TaskVerticalSpeed - Math.Abs(VerticalSpeed)) / TaskVerticalSpeed, false); // разгон вверх
                            }
                            else if (Math.Abs(VerticalSpeed) > TaskVerticalSpeed)
                            {
                                // тормоз
                                if (TaskVerticalSpeed < 10f || Math.Abs(DeltaHeight) < 100f)
                                {
                                    UpDown(0.3f, false);
                                    //UpDown(0.0f, true);
                                }
                                else { UpDown(1.0f, false); }
                            }
                            else
                            {
                                move_ud = "COMPENSATE";// летим с компенсацией
                            }
                        }
                    }
                    else if (VerticalSpeed > minVerticalSpeed)
                    {
                        // летим вверх
                        if (DeltaHeight > minDeltaHeight)
                        {
                            // а, надо вниз - ТОРМОЗИМ
                            if (TaskVerticalSpeed < 10f || Math.Abs(DeltaHeight) < 100f)
                            {
                                UpDown(-0.3f, false);
                                //UpDown(0.0f, true);
                            }
                            else { UpDown(-1.0f, false); }
                        }
                        else
                        {
                            if (Math.Abs(VerticalSpeed) < TaskVerticalSpeed - Math.Abs(VerticalSpeedTick))
                            {
                                // разгон
                                //Down((float)(YTaskSpeed - Math.Abs(VerticalSpeed)) / YTaskSpeed); // разгон вверх
                                if (TaskVerticalSpeed < 10f || Math.Abs(DeltaHeight) < 100f)
                                {
                                    UpDown(0.3f, false);// разгон вверх
                                }
                                else
                                {
                                    UpDown(1.0f, false);// разгон вверх
                                }
                            }
                            else if (Math.Abs(VerticalSpeed) > TaskVerticalSpeed)
                            {
                                // тормоз

                                if (TaskVerticalSpeed < 10f || Math.Abs(DeltaHeight) < 100f)
                                {
                                    UpDown(-0.3f, false);
                                    //UpDown(0.0f, true);
                                }
                                else { UpDown(-1.0f, false); }
                            }
                            else
                            {
                                move_ud = "COMPENSATE";// летим с компенсацией
                            }
                        }
                    }
                    else
                    {
                        if (DeltaHeight < -minDeltaHeight)
                        {
                            UpDown(0.3f, false);// вверх
                        }
                        else if (DeltaHeight > minDeltaHeight)
                        {
                            UpDown(-0.3f, false);// Вниз
                        }
                    }
                }
                return false;
            }
            public bool ControlCurse()
            {
                if (TackTarget != null)
                {
                    if (DeltaCurse > 0f && DeltaCurse < minDeltaCurse && HorizontSpeed >= 0 && HorizontSpeed < minHorizontSpeed)
                    {
                        // стоп попали
                        move_fb = "STOP";
                        ForwardBackward(0.0f, true);
                        return true;
                    }
                    else
                    {
                        if (Math.Abs(HorizontSpeed) < TaskHorizontSpeed - HorizontSpeedTick)
                        {
                            // разгон
                            //Backward((float)(TaskHorizontSpeed - Math.Abs(HorizontSpeed)) / TaskHorizontSpeed); // разгон вверх
                            if (TaskHorizontSpeed < 10 || DeltaCurse < 100)
                            {
                                ForwardBackward(0.3f, false);
                            }
                            else { ForwardBackward(1.0f, false); }
                        }
                        else if (Math.Abs(HorizontSpeed) > TaskHorizontSpeed)
                        {
                            // тормоз
                            if (TaskHorizontSpeed < 10 || DeltaCurse < 100)
                            {
                                ForwardBackward(-0.3f, false);
                                //ForwardBackward(0.0f, true);
                            }
                            else { ForwardBackward(-1.0f, false); }
                        }
                        else
                        {
                            move_fb = "COMPENSATE";// летим с компенсацией
                        }
                    }

                }
                return false;
            }
            public void CompensateWeight()
            {
                bool IsControlHeight = false;
                bool IsControlCurse = false;
                // Компенсация боковой скорости
                //Vector3D RLVelocityThrust = new Vector3D();
                //VelocityThrust = GravityVector * ShipWeight * LinearVelocity;
                //ShipWeight = GravityVector * PhysicalMass;
                ////RLVelocityThrust = GravityVector * ShipWeight * LinearVelocity * KVRL;
                //RLVelocityThrust = VelocityThrust * KVRL;
                ////
                //LeftThrust = (ShipWeight + RLVelocityThrust).Dot(remote_control.WorldMatrix.Left);
                //RightThrust = -LeftThrust;
                if (clear_velocity)
                {
                    height = false;
                    curse = false;
                    UpDown(0.0f, true);
                    ForwardBackward(0.0f, true);
                    LeftRight(0.0f, true);
                }
                //LeftRight(0.0f, clear_velocity);
                if (height)
                {
                    IsControlHeight = ControlHeight();
                    LeftRight(0.0f, true);
                }
                else
                {
                    UpDown(0.0f, clear_velocity);

                }
                if (curse)
                {
                    IsControlCurse = ControlCurse();
                    LeftRight(0.0f, true);
                }
                else
                {
                    ForwardBackward(0.0f, clear_velocity);
                }
                //if (res_h && res_c)
                //{
                //    clear_velocity = true;
                //    height = false;
                //    curse = false;
                //}
                Matrix ThrusterMatrix = new MatrixD();
                foreach (IMyThrust thrust in thrusts)
                {
                    thrust.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == CockpitMatrix.Up)
                    {
                        thrust.ThrustOverridePercentage = (float)(UpThrust / UpThrMax);
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Down)
                    {
                        thrust.ThrustOverridePercentage = (float)(DownThrust / DownThrMax);
                    }
                    //X
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Left)
                    {
                        thrust.ThrustOverridePercentage = (float)(LeftThrust / LeftThrMax);
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Right)
                    {
                        thrust.ThrustOverridePercentage = (float)(RightThrust / RightThrMax);
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Forward)
                    {
                        thrust.ThrustOverridePercentage = (float)(ForwardThrust / ForwardThrMax);
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Backward)
                    {
                        thrust.ThrustOverridePercentage = (float)(BackwardThrust / BackwardThrMax);
                    }
                }
            }
            public void Horizon()
            {
                Vector3D GravNorm = Vector3D.Normalize(GravityVector);
                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(remote_control.WorldMatrix.Forward);
                double gL = GravNorm.Dot(remote_control.WorldMatrix.Left);
                double gU = GravNorm.Dot(remote_control.WorldMatrix.Up);
                //Получаем сигналы по тангажу и крены операцией atan2
                RollInput = (float)Math.Atan2(gL, -gU); // крен
                PitchInput = -(float)Math.Atan2(gF, -gU); // тангаж
                SetGyro(YawInput, PitchInput, RollInput);
            }
            public void Target(Vector3D trg)
            {
                target = true;
                TackTarget = trg;
                clear_velocity = false;
                compensate = true;
                horizont = true;
                height = true;
                curse = true;
            }
            public void TargetHeight(Vector3D trg)
            {
                target = true;
                TackTarget = trg;
                compensate = true;
                horizont = true;
                height = true;
                curse = false;
            }
            public void TargetCurse(Vector3D trg)
            {
                target = true;
                TackTarget = trg;
                compensate = true;
                horizont = true;
                height = false;
                curse = true;
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("СКОРОСТЬ   : " + Math.Round(remote_control.GetShipSpeed(), 2) + "\n");
                values.Append("ГОРИЗОНТ   : " + (horizont ? "ВКЛ" : "ВЫК") + "\n");
                values.Append("КОМПЕНСАЦИЯ: " + (compensate ? "ВКЛ" : "ВЫК") + "\n");
                values.Append("К.ВЫСОТЫ   : " + (height ? "ВКЛ" : "ВЫК") + "\n");
                values.Append("К.КУРСА    : " + (curse ? "ВКЛ" : "ВЫК") + "\n");
                values.Append("СБРОС      : " + (clear_velocity ? "ВКЛ" : "ВЫК") + "\n");
                values.Append("T1: " + PText.GetGPS("Target1", Target1) + "\n");
                values.Append("T2: " + PText.GetGPS("Target2", Target2) + "\n");
                return values.ToString();
            }
            public string TextTEST()
            {
                StringBuilder values = new StringBuilder();
                //values.Append("GravityVector: " + Math.Round(GravityVector.Length(), 2) + "\n");
                //values.Append("PhysicalMass: " + Math.Round(PhysicalMass, 2) + "\n");
                //values.Append("ShipWeight: " + Math.Round(ShipWeight.Length(), 2) + "\n");
                values.Append("move_horizont : " + move_ud + "\n");
                values.Append("move_curse    : " + move_fb + "\n");
                values.Append("-----------------------------------------" + "\n");
                values.Append("DeltaCurse: " + Math.Round(DeltaCurse, 2) + "\n");
                values.Append("ForwV : " + Math.Round(ForwVelocityVector.Length(), 2) + "\n");
                values.Append("TaskHorizontSpeed : " + Math.Round(TaskHorizontSpeed, 2) + "\n");
                values.Append("HorizontSpeed     : " + Math.Round(HorizontSpeed, 2) + "\n");
                values.Append("-----------------------------------------" + "\n");
                values.Append("DeltaHeight: " + Math.Round(DeltaHeight, 2) + "\n");
                values.Append("TaskVerticalSpeed : " + Math.Round(TaskVerticalSpeed, 2) + "\n");
                values.Append("UpV   : " + Math.Round(UpVelocityVector.Length(), 2) + "\n");
                values.Append("VerticalSpeed     : " + Math.Round(VerticalSpeed, 2) + "\n");
                values.Append("-----------------------------------------" + "\n");
                values.Append("LeftV : " + Math.Round(LeftVelocityVector.Length(), 2) + "\n");
                values.Append("Yaw: " + Math.Round(YawInput, 2) + "\n");
                values.Append("Roll: " + Math.Round(RollInput, 2) + "\n");
                values.Append("Pitch: " + Math.Round(PitchInput, 2) + "\n");

                values.Append("UP       : " + PText.GetThrust((float)UpThrust) + "\t, MAX : " + PText.GetThrust((float)UpThrMax) + "\n");
                values.Append("DOWN     : " + PText.GetThrust((float)DownThrust) + "\t, MAX : " + PText.GetThrust((float)DownThrMax) + "\n");
                values.Append("Forward  : " + PText.GetThrust((float)ForwardThrust) + "\t, MAX : " + PText.GetThrust((float)ForwardThrMax) + "\n");
                values.Append("Backward : " + PText.GetThrust((float)BackwardThrust) + "\t, MAX : " + PText.GetThrust((float)BackwardThrMax) + "\n");
                values.Append("Left     : " + PText.GetThrust((float)LeftThrust) + "\t, MAX : " + PText.GetThrust((float)LeftThrMax) + "\n");
                values.Append("Right    : " + PText.GetThrust((float)RightThrust) + "\t, MAX : " + PText.GetThrust((float)RightThrMax) + "\n");

                return values.ToString();
            }
            public void CurseTarget()
            {
                clear_velocity = false;
                compensate = true;
                horizont = true;
                target = true;
                height = false;
                curse = false;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "clear_velocity_on":
                        clear_velocity = true;
                        break;
                    case "clear_velocity_off":
                        clear_velocity = false;
                        break;
                    case "compensate_on":
                        compensate = true;
                        horizont = true;
                        target = false;
                        break;
                    case "compensate_off":
                        compensate = false;
                        horizont = false;
                        clear_velocity = false;
                        target = false;
                        height = false;
                        curse = false;
                        curse_target = false;
                        ClearThrustOverridePersent();
                        break;
                    case "horizont_on":
                        horizont = true;
                        break;
                    case "horizont_off":
                        horizont = false;
                        break;
                    case "horizont":
                        if (horizont)
                        {
                            horizont = false;
                        }
                        else
                        {
                            horizont = true;
                        }
                        break;
                    case "T1":
                        clear_velocity = false;
                        Target(Target1);
                        break;
                    case "T2":
                        clear_velocity = false;
                        Target(Target2);
                        break;
                    case "TH1":
                        TargetHeight(Target1);
                        break;
                    case "TH2":
                        clear_velocity = false;
                        TargetHeight(Target2);
                        break;
                    case "TC1":
                        clear_velocity = false;
                        TargetCurse(Target1);
                        break;
                    case "TC2":
                        clear_velocity = false;
                        TargetCurse(Target2);
                        break;
                    case "CT1":
                        clear_velocity = false;
                        TackTarget = Target1;
                        curse_target = true;
                        break;
                    case "CT2":
                        clear_velocity = false;
                        TackTarget = Target2;
                        curse_target = true;
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    // Обновим состояние навигации
                    Update();
                    remote_control.DampenersOverride = !compensate;
                    GyroOver(horizont);
                    if (compensate)
                    {
                        CompensateWeight();
                    }
                    if (horizont)
                    {
                        Horizon();
                    }
                    if (curse_target)
                    {
                        CurseTarget();
                    }
                }
            }
        }
    }
}