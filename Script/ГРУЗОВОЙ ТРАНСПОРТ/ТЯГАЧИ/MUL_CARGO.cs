using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRageMath;


namespace MUL_CARGO
{
    /// <summary>
    /// ГРУЗОВОЙ МОДУЛЬ MUL
    /// </summary>
    public sealed class Program : MyGridProgram
    {
        string NameObj = "[MUL-CM1]";
        string NameConnector = "[MUL-CM1]-Коннектор парковка [cargo_connectior]";
        string NameShipConnector = "[MUL-CM1]-Коннектор MUL";
        string NameCockpit = "[MUL-CM1]-Кресло пилота [LCD]";

        string NameLCDDebug = "[MUL-CM1]-LCD-DEBUG";

        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея

        static LCD lcd_info_debug;

        Batterys bats;
        GasTanks gastanks;
        Cockpit cockpit;
        Connector connector;
        Connector connector_ship;
        LandingGears landing_gears;
        SpecialInventory special_inventory;

        static Program _scr;

        static bool ship_connect = false;
        static bool cargo_connect = false;

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
            _scr = this;
            lcd_info_debug = new LCD(NameLCDDebug);
            cockpit = new Cockpit(NameCockpit);
            bats = new Batterys(NameObj);
            gastanks = new GasTanks(NameObj);
            connector = new Connector(NameConnector);
            connector_ship = new Connector(NameShipConnector);
            landing_gears = new LandingGears(NameObj);
            special_inventory = new SpecialInventory(NameObj, "Special");
        }
        public void Save()
        {

        }
        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder values_info = new StringBuilder();
            bats.Logic(argument, updateSource);
            cockpit.Logic(argument, updateSource);
            values_info.Append("Connected: " + connector.Connected + "\n");
            values_info.Append("Connector_ship: " + connector_ship.Connected + "\n");
            switch (argument)
            {
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                if (!connector_ship.Connected) {
                    landing_gears.Lock(true);
                    connector.Connect();
                }
                // Проверим корабль не припаркован
                if (!connector.Connected && !landing_gears.IsLocked())
                {
                    gastanks.Stockpile(false);
                    bats.Auto();
                }
                else
                {
                    gastanks.Stockpile(true);
                    bats.Charger();
                }
            }
            //cockpit.OutText(values_info, 0);
            lcd_info_debug.OutText(values_info);
            ship_connect = connector.Connected;         // сохраним состояние
            cargo_connect = connector_ship.Connected;   // груз подключен
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
            public virtual bool Connected { get { return base.obj.Status == MyShipConnectorStatus.Connected ? true : false; } }
            public virtual bool Unconnected { get { return base.obj.Status == MyShipConnectorStatus.Unconnected ? true : false; } }
            public virtual bool Connectable { get { return base.obj.Status == MyShipConnectorStatus.Connectable ? true : false; } }
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
            public virtual string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("КОННЕКТОР: [" + GetInfoStatus() + "]" + "\n");
                return values.ToString();
            }
            public virtual void Connect()
            {
                obj.Connect();
            }
            public virtual void Disconnect()
            {
                obj.Disconnect();
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
                values.Append("Скорость: " + base.obj.GetShipSpeed() + "\n");
                values.Append("Высота: " + current_height + "\n");
                //values.Append("LinearVelocity: " + base.obj.GetShipVelocities().LinearVelocity + "\n");
                //values.Append("LinearVelocity: " + base.obj.GetShipVelocities().LinearVelocity.Length() + "\n");
                //values.Append("AngularVelocity: " + base.obj.GetShipVelocities().AngularVelocity + "\n");
                //values.Append("AngularVelocity: " + base.obj.GetShipVelocities().AngularVelocity.Length() + "\n");
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
            public void Lock(bool on)
            {
                foreach (IMyLandingGear obj in list_obj)
                {
                    if (on)
                    {
                        obj.Lock();
                    }
                    else {
                        obj.Unlock();
                    }
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
            };

            string current_special = "";

            List<MyComp> list_all = new List<MyComp>() {
                new MyComp() { component = Component.BulletproofGlass, value = 500 },
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
