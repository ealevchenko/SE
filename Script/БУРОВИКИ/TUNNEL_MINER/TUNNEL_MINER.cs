using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VRage.Game.ModAPI.Ingame;
using VRage.Noise.Combiners;
using VRageMath;
using static VRage.Game.MyObjectBuilder_CurveDefinition;
/// <summary>
/// v1.0  Тунельный шахтер (Добыча льда в озере тунельной шахтой)
/// </summary>
namespace TUNNEL_MINER
{
    public sealed class Program : MyGridProgram
    {
        string NameObj = "[PB-ER-1]-[TM-01]";
        string tag_lighting_warning = "[lighting-warning]";
        static float SpeedOpen = 0.1f;
        static float SpeedClose = 0.1f;
        static float TackDistance = 30.0f;

        const char green = '\uE001';
        const char blue = '\uE002';
        const char red = '\uE003';
        const char yellow = '\uE004';
        const char darkGrey = '\uE00F';
        static LCD lcd_storage;
        static LCD lcd_debug;
        static LCD lcd_work1;
        static LCD lcd_status;
        static Batterys bats;
        static Connector connector_forw, connector_back;
        static ShipMergeBlock mergeblock_forw, mergeblock_back;
        static ShipDrill drill;
        static ShipWelders welders;
        static ShipGrinder grinder;
        static PistonsBase pistones;
        static Lightings light_warning;
        static ReflectorsLight reflectors_light;
        static Cockpit cockpit;
        static Cargos cargos;
        static Upr upr;
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
            static public string GetCurrentOfMinMax(float min, float cur, float max, string units) { return "[ " + Math.Round(min, 1) + units + " / " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]"; }
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
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            lcd_work1 = new LCD(NameObj + "-LCD-Work 1");
            lcd_status = new LCD(NameObj + "-LCD-status");
            bats = new Batterys(NameObj);
            connector_forw = new Connector(NameObj + "-Коннектор forw");
            connector_back = new Connector(NameObj + "-Коннектор back");
            mergeblock_forw = new ShipMergeBlock(NameObj + "-Соединитель forw");
            mergeblock_back = new ShipMergeBlock(NameObj + "-Соединитель back");
            drill = new ShipDrill(NameObj);
            drill.Off();
            welders = new ShipWelders(NameObj);
            welders.Off();
            grinder = new ShipGrinder(NameObj);
            grinder.Off();
            pistones = new PistonsBase(NameObj, null);
            light_warning = new Lightings(NameObj, tag_lighting_warning); // Внимание работает
            light_warning.Off();
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            cockpit = new Cockpit(NameObj + "-Cocpit [LCD]");
            cargos = new Cargos(NameObj);
            upr = new Upr();
        }
        public void Save()
        {

        }
        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder values_info = new StringBuilder();
            bats.Logic(argument, updateSource);
            upr.Logic(argument, updateSource);
            switch (argument)
            {
                default:
                    break;
            }
            values_info.Append(bats.TextInfo());
            values_info.Append(connector_forw.TextInfo("КОН-FORW"));
            values_info.Append(mergeblock_forw.TextInfo("СОЕД-FORW"));
            values_info.Append(connector_back.TextInfo("КОН-BACK"));
            values_info.Append(mergeblock_back.TextInfo("СОЕД-BACK"));
            values_info.Append(drill.TextInfo());
            values_info.Append(welders.TextInfo());
            values_info.Append(grinder.TextInfo());
            values_info.Append(pistones.TextInfo("Порш."));
            values_info.Append(upr.TextStatus());
            lcd_debug.OutText(values_info);
            lcd_status.OutText(values_info);
            //cockpit.OutText(values_info, 0);
            //StringBuilder values_info1 = new StringBuilder();
            //values_info1.Append(upr.TextCritical());
            //cockpit.OutText(values_info1, 1);
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
            //         public int count_work_batterys { get { return list_obj.Where(n => !((IMyTerminalBlock)n).CustomName.Contains(tag_batterys_duty)).Count(); } }
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
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append(name + ": " + (Connected ? green.ToString() : (Connectable ? yellow.ToString() : red.ToString())) + "\n");
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
                values.Append("БУРЫ: " + (base.Enabled() ? green.ToString() : red.ToString()) + "\n");
                return values.ToString();
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
                values.Append("СВАРЩИКИ: " + (base.Enabled() ? green.ToString() : red.ToString()) + "\n");
                return values.ToString();
            }
        }
        public class ShipGrinder : BaseListTerminalBlock<IMyShipGrinder>
        {
            public ShipGrinder(string name_obj) : base(name_obj)
            {

            }
            public ShipGrinder(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("РЕЗАКИ: " + (base.Enabled() ? green.ToString() : red.ToString()) + "\n");
                return values.ToString();
            }
        }
        public class PistonsBase : BaseListTerminalBlock<IMyPistonBase>
        {
            public float speed_piston { get; set; } = 0.5f;
            public float multiply_speed { get; set; } = 1f;
            public float? new_position { get; set; } = null;
            public float Position { get { return this.list_obj.Sum(s => s.CurrentPosition); } }
            public float MinLimit { get { return this.list_obj.Sum(s => s.MinLimit); } }
            public float MaxLimit { get { return this.list_obj.Sum(s => s.MaxLimit); } }
            public float Velocity { get { return this.list_obj.Average(s => s.Velocity); } }
            public PistonsBase(string name_obj, string tag) : base(name_obj) { if (!String.IsNullOrWhiteSpace(tag)) { list_obj = list_obj.Where(n => n.CustomName.Contains(tag)).ToList(); } _scr.Echo("Найдено PistonBase:[" + tag + "]: " + list_obj.Count()); }
            public void SetVelocity(float speed) { foreach (IMyPistonBase p in base.list_obj) { p.Velocity = speed; } }
            public void Open(float speed) { foreach (IMyPistonBase p in base.list_obj) { p.Velocity = speed; p.Extend(); } }
            public void Close(float speed) { foreach (IMyPistonBase p in base.list_obj) { p.Velocity = speed; p.Retract(); } }
            public void Open() { Open(this.speed_piston); }
            public void Close() { Close(this.speed_piston); }
            public void SetPosition()
            {
                float speed = 0f;
                if (new_position != null)
                {
                    double curennt_position = this.Position;
                    if (curennt_position > new_position)
                    {
                        speed = -(float)(Math.Abs(curennt_position - (float)new_position) * multiply_speed);
                        SetVelocity(speed);
                    }
                    else if (curennt_position < new_position)
                    {
                        speed = (float)(Math.Abs((float)new_position - curennt_position) * multiply_speed);
                        SetVelocity(speed);
                    }
                    else
                    {
                        SetVelocity(speed);
                        new_position = null;
                    }
                }


            }
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append(name + ": [" + Count + "] " + PText.GetCurrentOfMinMax(MinLimit, Position, MaxLimit, "m") + "\n");
                values.Append("|- Поз:  " + PText.GetScalePersent((Position - MinLimit) / (MaxLimit - MinLimit), 20) + "\n");
                return values.ToString();
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
                    if (new_position != null)
                    {
                        SetPosition();
                    }
                }
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
        public class ReflectorsLight : BaseListTerminalBlock<IMyReflectorLight>
        {
            public ReflectorsLight(string name_obj) : base(name_obj)
            {
            }
            public ReflectorsLight(string name_obj, string tag) : base(name_obj, tag)
            {

            }
        }
        public class Cockpit : BaseController
        {
            public Cockpit(string name) : base(name)
            {

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
        public class Upr
        {
            public enum programm : int
            {
                none = 0,
                start_drill = 1,        // начать бурение
            };
            public static string[] name_programm = { "", "НАЧАТЬ БУРЕНИЕ" };
            programm curent_programm = programm.none;
            public enum mode : int
            {
                none = 0,
                drill_forward = 1,
                drill_back = 2,
                new_point = 3,
            };
            public static string[] name_mode = { "", "ВПЕРЕД", "НАЗАД", "НОВАЯ ТОЧКА" };
            mode curent_mode = mode.none;
            //------------------------------
            public Vector3D MyStartPos { get; private set; }
            public Vector3D MyPos { get; private set; }
            public float CalcDistance { get; private set; } = 0f;

            public bool paused = false;
            public bool stop_dreel = false;
            public Upr()
            {
                LoadFromStorage();
            }
            //----------------------------------------------
            //-----------------------------------------------
            public void StartDrill()
            {
                if (curent_mode == mode.none)
                {
                    stop_dreel = false;
                    drill.On();
                    MyStartPos = cockpit.obj.GetPosition();
                    if (pistones.Position > 18.0f && connector_forw.Connected && mergeblock_forw.Locked)
                    {
                        curent_mode = mode.new_point; // Подтянуть
                        SaveToStorage();
                    }
                    if (pistones.Position < 3.0f && connector_back.Connected && mergeblock_back.Locked)
                    {
                        curent_mode = mode.drill_forward; // Выдвинуть
                        SaveToStorage();
                    }
                }
                else
                {
                    MyPos = cockpit.obj.GetPosition();
                    CalcDistance = (float)(MyStartPos - MyPos).Length();
                    if (CalcDistance >= TackDistance)
                    {
                        stop_dreel = true;
                    }
                    if (curent_mode == mode.new_point && NewPoint())
                    {
                        if (!stop_dreel) { curent_mode = mode.drill_forward; } else { Stop(); }
                        SaveToStorage();
                    }
                    if (curent_mode == mode.drill_forward && DrillForward())
                    {
                        if (!stop_dreel) { curent_mode = mode.new_point; } else { Stop(); }
                        SaveToStorage();
                    }
                }
            }
            //-----------------------------------------------
            public void UpdateCalc()
            {
                cargos.Update();
            }
            public void Pause(bool enable)
            {
                if (enable)
                {

                    drill.Off();
                    reflectors_light.Off();

                    paused = true;
                }
                else
                {
                    paused = false;
                    if (curent_mode != mode.none)
                    {
                        drill.On();
                        reflectors_light.On();
                    }
                }
                SaveToStorage();
            }
            //public void Clear()
            //{
            //    curent_mode = mode.none;
            //    SaveToStorage();
            //}
            public void Stop()
            {
                curent_mode = mode.none;
                curent_programm = programm.none;
                paused = false;
                stop_dreel = false;
                mergeblock_forw.On();
                connector_forw.Connect();
                mergeblock_back.On();
                connector_back.Connect();
                pistones.Close(0);
                drill.Off();
                grinder.Off();
                welders.Off();
                reflectors_light.Off();
                SaveToStorage();
            }
            public bool DrillForward()
            {
                bool Complete = false;
                if (pistones.Position <= 18.0f)
                {
                    pistones.Open(!paused ? SpeedOpen : 0);
                    if (connector_forw.Connected)
                    {
                        connector_forw.Disconnect();
                        mergeblock_forw.Off();
                    }
                    if (pistones.Position > 5.0f && connector_forw.Unconnected)
                    {
                        mergeblock_forw.On();
                        welders.On();
                    }
                }
                else
                {
                    if (connector_forw.Connectable)
                    {
                        connector_forw.Connect();
                    }
                    if (mergeblock_forw.Locked)
                    {
                        welders.Off();
                    }
                    if (connector_forw.Connected && mergeblock_forw.Locked)
                    {
                        Complete = true;
                    }

                }
                return Complete;
            }
            public bool NewPoint()
            {
                bool Complete = false;
                if (pistones.Position >= 3.0f)
                {
                    pistones.Close(pistones.Position > 12.0f ? SpeedClose : SpeedClose * 5);

                    if (connector_back.Connected)
                    {
                        connector_back.Disconnect();
                        mergeblock_back.Off();
                        grinder.On();
                    }
                    if (pistones.Position < 15.0f && connector_back.Unconnected)
                    {
                        mergeblock_back.On();
                    }
                }
                else
                {
                    if (connector_back.Connectable)
                    {
                        connector_back.Connect();
                    }
                    if (mergeblock_back.Locked)
                    {
                        grinder.Off();
                    }
                    if (connector_back.Connected && mergeblock_back.Locked)
                    {
                        Complete = true;
                    }
                }
                return Complete;
            }
            //-------------------------------------------------
            public void OutStatusMode(float MaxFSpeed, float MaxUSpeed, float MaxLSpeed, float UpAccel)
            {
                //StringBuilder values = new StringBuilder();
                //values.Append(" STATUS\n");
                //Vector3D MyPosDrill = Vector3D.Transform(MyPos, DrillMatrix) - DrillPoint;
                //Vector3D MyPosDrill = Vector3D.Transform(MyPos, DockMatrix) - DrillPoint;
                //values.Append("My_Length   : " + Math.Round(MyPosDrill.Length(), 2) + "\n");
                //values.Append("YMaxA (U-D) : " + Math.Round(YMaxA, 2).ToString() + "MaxUSpeed: " + Math.Round(MaxUSpeed, 2).ToString() + "\n");
                //values.Append("MyPosDrill[0]   : " + Math.Round(MyPosDrill.GetDim(0), 2) + "\n");
                //values.Append("MyPosDrill[1]   : " + Math.Round(MyPosDrill.GetDim(1), 2) + "\n");
                ////values.Append("UpAccel   : " + Math.Round(UpAccel, 2) + "\n");

                //values.Append("MyPosDrill[2]   : " + Math.Round(MyPosDrill.GetDim(2), 2) + "\n");
                //values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                //values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                //values.Append("DeltaHeight: " + Math.Round(FlyHeight - (MyPos - PlanetCenter).Length()).ToString() + "\n");
                //values.Append("Distance: " + Math.Round(Distance).ToString() + "\n");
                //values.Append("------------------------------------------\n");
                //values.Append("ZMaxA (F-B) : " + Math.Round(ZMaxA, 2).ToString() + "MaxFSpeed: " + Math.Round(MaxFSpeed, 2).ToString() + "\n");
                //values.Append("YMaxA (U-D) : " + Math.Round(YMaxA, 2).ToString() + "MaxUSpeed: " + Math.Round(MaxUSpeed, 2).ToString() + "\n");
                //values.Append("XMaxA (L-R) : " + Math.Round(XMaxA, 2).ToString() + "MaxLSpeed: " + Math.Round(MaxLSpeed, 2).ToString() + "\n");
                //values.Append(thrusts.TextInfo());
                //lcd_debug.OutText(values);
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
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                curent_programm = (programm)GetValInt("curent_programm", str.ToString());
                curent_mode = (mode)GetValInt("curent_mode", str.ToString());
                paused = GetValBool("pause", str.ToString());
                stop_dreel = GetValBool("stop_dreel", str.ToString());
                MyStartPos = new Vector3D(GetVal("SPX", str.ToString()), GetVal("SPY", str.ToString()), GetVal("SPZ", str.ToString()));
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                values.Append("curent_programm: " + ((int)curent_programm).ToString() + ";\n");
                values.Append("curent_mode: " + ((int)curent_mode).ToString() + ";\n");
                values.Append("pause: " + paused.ToString() + ";\n");
                values.Append("stop_dreel: " + stop_dreel.ToString() + ";\n");
                values.Append(MyStartPos.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "SPX").Replace("Y", "SPY").Replace("Z", "SPZ") + ";\n");
                lcd_storage.OutText(values);
            }
            public string TextStatus()
            {
                StringBuilder values = new StringBuilder();
                values.Append("--------------------------------------\n");
                values.Append("ПРОГРАММА : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП      : " + name_mode[(int)curent_mode] + "\n");
                values.Append("ПАУЗА     : " + (paused ? green.ToString() : red.ToString()) + ", ");
                values.Append("СТОП      : " + (stop_dreel ? green.ToString() : red.ToString()) + "\n");
                values.Append("--------------------------------------\n");
                values.Append("ЗАД/ДИСТ  : " + Math.Round(TackDistance).ToString() + " / " + Math.Round(CalcDistance).ToString() + "\n");
                values.Append("--------------------------------------\n");
                return values.ToString();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "load":
                        LoadFromStorage();
                        break;
                    case "save":
                        SaveToStorage();
                        break;
                    case "pause":
                        Pause(!paused);
                        break;
                    case "pause_on":
                        paused = true;
                        Pause(paused);
                        break;
                    case "pause_off":
                        paused = false;
                        Pause(paused);
                        break;
                    case "stop":
                        if (curent_programm == programm.start_drill)
                        {
                            stop_dreel = true;
                        }
                        break;
                    //case "clear":
                    //    Clear();
                    //    curent_programm = programm.none;
                    //    break;
                    case "start_drill":
                        curent_programm = programm.start_drill;
                        SaveToStorage();
                        break;

                    case "drill_forward":
                        curent_mode = mode.drill_forward;
                        SaveToStorage();
                        break;
                    case "new_point":
                        curent_mode = mode.new_point;
                        SaveToStorage();
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    cockpit.Logic(argument, updateSource);
                    // Обновим состояние навигации
                    //UpdateCalc();

                    if (curent_programm != programm.none || curent_mode != mode.none)
                    {
                        lcd_work1.On(); light_warning.On();
                    }
                    else
                    {
                        lcd_work1.Off(); light_warning.Off();
                    }

                    if (curent_programm == programm.none)
                    {
                        if (drill.Enabled())
                        {
                            reflectors_light.On();
                        }
                        if (curent_mode == mode.drill_forward)
                        {
                            if (DrillForward() && curent_programm == programm.none)
                            {
                                curent_mode = mode.none;
                            }
                        }
                        if (curent_mode == mode.new_point)
                        {
                            if (NewPoint() && curent_programm == programm.none)
                            {
                                curent_mode = mode.none;
                            }
                        }
                    }
                    if (curent_programm == programm.start_drill)
                    {
                        StartDrill();
                    }
                }
            }
        }
    }
}