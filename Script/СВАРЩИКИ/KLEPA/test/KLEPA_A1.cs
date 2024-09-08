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
/// + Добавить рен на 45 град
/// 
/// </summary>
namespace KLEPA_A1_OLD
{
    /// <summary>
    /// Сварщик атмосферный -1
    /// </summary>
    public sealed class Program : MyGridProgram
    {
        // v2.3
        string NameObj = "[KLEPA_A1]";
        string NameCockpit = "[KLEPA_A1]-Промышленный кокпит [LCD]";
        string NameRemoteControl = "[KLEPA_A1]-ДУ парковка";
        string NameConnector = "[KLEPA_A1]-Коннектор парковка";
        string NameLCDInfo = "[KLEPA_A1]-LCD-INFO";

        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея

        LCD lcd_info;
        Batterys bats;
        Connector connector;
        ShipWelders welders;
        ReflectorsLight reflectors_light;
        Gyros gyros;
        Thrusts thrusts;
        Cockpit cockpit;
        RemoteControl remote_control;
        LandingGears landing_gears;
        SpecialInventory special_inventory;

        static Program _scr;

        bool ship_connect = false;
        bool horizont = false;

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
        public void KeepHorizon()
        {
            Vector3D GravityVector = cockpit._obj.GetNaturalGravity();
            Vector3D GravNorm = Vector3D.Normalize(GravityVector);

            //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
            double gF = GravNorm.Dot(cockpit._obj.WorldMatrix.Forward);
            double gL = GravNorm.Dot(cockpit._obj.WorldMatrix.Left);
            double gU = GravNorm.Dot(cockpit._obj.WorldMatrix.Up);

            //Получаем сигналы по тангажу и крены операцией atan2
            float RollInput = (float)Math.Atan2(gL, -gU);
            float PitchInput = -(float)Math.Atan2(gF, -gU);

            //На рыскание просто отправляем сигнал рыскания с контроллера. Им мы будем управлять вручную.
            float YawInput = 0;
            if (remote_control.IsUnderControl)
            {
                YawInput = remote_control._obj.RotationIndicator.Y;
            }
            else if (cockpit.IsUnderControl)
            {
                YawInput = cockpit._obj.RotationIndicator.Y;
            }
            gyros.SetGyro(YawInput, PitchInput, RollInput);
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            lcd_info = new LCD(NameLCDInfo);
            cockpit = new Cockpit(NameCockpit);
            remote_control = new RemoteControl(NameRemoteControl);
            bats = new Batterys(NameObj);
            connector = new Connector(NameConnector);
            ship_connect = connector.Connected;
            welders = new ShipWelders(NameObj);
            welders.Off();
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            gyros = new Gyros(NameObj);
            thrusts = new Thrusts(NameObj);
            landing_gears = new LandingGears(NameObj);
            special_inventory = new SpecialInventory(NameObj, "Special");
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder values_info = new StringBuilder();
            bats.Logic(argument, updateSource);
            remote_control.Logic(argument, updateSource);
            special_inventory.Logic(argument, updateSource);

            switch (argument)
            {
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
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                // Проверим корабль не припаркован
                if (!connector.Connected && !landing_gears.IsLocked())
                {
                    bats.Auto();
                    thrusts.On();
                    cockpit.Dampeners(true);
                    // Включим или отключим авто-зацеп если работают сварщики
                    landing_gears.AutoLock(!welders.Enabled());

                    // режим горизонт
                    gyros.GyroOver(horizont);
                    if (horizont)
                    {
                        KeepHorizon();
                    }
                    // Проверка кокпит не под контроллем включить тормоз
                    if (!cockpit.IsUnderControl)
                    {
                        cockpit.Dampeners(true);
                        reflectors_light.Off();
                        welders.Off();
                    }
                    if (welders.Enabled())
                    {
                        reflectors_light.On();
                    }
                }
                else
                {
                    // Припаркован
                    reflectors_light.Off();
                    welders.Off();
                    cockpit.Dampeners(false);
                    bats.Charger();
                    thrusts.Off();

                }
            }
            values_info.Append(bats.TextInfo());
            values_info.Append(connector.TextInfo());
            values_info.Append(welders.TextInfo());
            values_info.Append(remote_control.TextInfo());
            values_info.Append(special_inventory.TextInfo());
            values_info.Append("РЕЖИМ: " + (horizont ? "ГОРИЗОНТ" : "") + "\n");
            cockpit.OutText(values_info, 0);
            ship_connect = connector.Connected; // сохраним состояние

            StringBuilder test_info = new StringBuilder();
            test_info.Append("home1 : " + remote_control.home_position.ToString() + "\n");
            test_info.Append("home2 : " + remote_control.home_position1.ToString() + "\n");
            test_info.Append("home3 : " + remote_control.home_position2.ToString() + "\n");
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
            public Gyros(string name_obj) : base(name_obj)
            {
            }
            public Gyros(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public void SetGyro(float Yaw, float Pitch, float Roll)
            {
                foreach (IMyGyro gyro in base.list_obj)
                {
                    gyro.Yaw = Yaw;
                    gyro.Pitch = Pitch;
                    gyro.Roll = Roll;
                }
            }
            public void GyroOver(bool over)
            {
                foreach (IMyGyro gyro in base.list_obj)
                {
                    gyro.Yaw = 0;
                    gyro.Pitch = 0;
                    gyro.Roll = 0;
                    gyro.GyroOverride = over;
                }
            }
        }
        public class Thrusts : BaseListTerminalBlock<IMyThrust>
        {
            public Thrusts(string name_obj) : base(name_obj)
            {
            }
            public Thrusts(string name_obj, string tag) : base(name_obj, tag)
            {

            }
        }
        public class Cockpit : BaseTerminalBlock<IMyShipController>
        {
            public IMyShipController _obj { get { return obj; } }
            public bool IsUnderControl { get { return obj.IsUnderControl; } }
            public bool ControlThrusters { get { return obj.ControlThrusters; } }
            public Cockpit(string name) : base(name)
            {

            }
            public void Dampeners(bool on)
            {
                obj.DampenersOverride = on;
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
        }
        public class RemoteControl : BaseTerminalBlock<IMyRemoteControl>
        {

            public Vector3D home_position = new Vector3D();
            public Vector3D home_position1 = new Vector3D();
            public Vector3D home_position2 = new Vector3D();
            public IMyShipController _obj { get { return obj; } }
            public bool IsUnderControl { get { return obj.IsUnderControl; } }
            public bool ControlThrusters { get { return obj.ControlThrusters; } }
            public RemoteControl(string name) : base(name)
            {

            }
            public void Dampeners(bool on)
            {
                obj.DampenersOverride = on;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "home_position":

                        Vector3D vector_Forward = base.obj.WorldMatrix.Forward;
                        Vector3D vector_Up = base.obj.WorldMatrix.Up;
                        home_position = base.GetPosition() - (vector_Forward * 2);
                        home_position1 = (home_position - (vector_Forward * 20));
                        home_position2 = (home_position1 + (vector_Up * 100));
                        break;
                    case "auto_home":
                        base.obj.ClearWaypoints();
                        base.obj.AddWaypoint(home_position2, "БАЗА-Up");
                        base.obj.AddWaypoint(home_position1, "БАЗА-Forward");
                        base.obj.AddWaypoint(home_position, "БАЗА");
                        base.obj.SetAutoPilotEnabled(true);
                        break;
                    case "auto_off":
                        base.obj.SetAutoPilotEnabled(false);
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
                values.Append("ДОМ: " + home_position.ToString() + "\n");
                values.Append("АВТОПИЛОТ: " + (base.obj.IsAutoPilotEnabled ? "ВКЛ" : "ОТК") + "\n");
                return values.ToString();
            }
        }
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

    }
}