using Newtonsoft.Json.Linq;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRage.Network;
using VRage.Noise.Combiners;
using VRage.Scripting;
using VRageMath;

/* Скрипт тестирования на корабле классов обмен сообщений и навигационного блока
        tag_antena = "[antena]" - на базе пометить программный блок
        tag_nav = "[nav]";    - на корабле пометить кокпит ....
        type_ship = 1; - указать тип коробля
        type_thruster = "H";  - указать тип ьрастеров
 */
namespace Ship_Test
{
    public sealed class Program : MyGridProgram
    {
        string NameObj = "[SHIP-T]";
        static string tag_antena = "[antena]";
        static string tag_nav = "[nav]";
        static int type_ship = 1; // бур
        static string type_thruster = "H";

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static LCD lcd_storage;
        static LCD lcd_info, lcd_debug;
        static LCD lcd_lstr;
        static MyStorage strg;
        static MessHandler mess_handler;
        static Connector connector_forw;
        static Connector connector_back;
        static Connector connector_down;
        static Navigation nav;
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
        public class BaseShipController
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
            lcd_lstr = new LCD(NameObj + "-LCD-Listener");
            mess_handler = new MessHandler(NameObj);
            connector_forw = new Connector(NameObj + "-Коннектор [forw]");
            connector_back = new Connector(NameObj + "-Коннектор [back]");
            connector_down = new Connector(NameObj + "-Коннектор [down]");
            nav = new Navigation(NameObj);
            strg = new MyStorage();
            strg.LoadFromStorage();
        }
        void Main(string argument, UpdateType updateSource)
        {
            mess_handler.Logic(argument, updateSource); // обработаем сообщенияя
            nav.Logic(argument, updateSource);// обработаем навигацию
        }
        public class LCD : BaseTerminalBlock<IMyTextPanel>
        {
            public LCD(string name) : base(name) { if (base.obj != null) { base.obj.SetValue("Content", (Int64)1); } }
            public void OutText(StringBuilder values) { if (base.obj != null) { base.obj.WriteText(values, false); } }
            public void OutText(string text, bool append) { if (base.obj != null) { base.obj.WriteText(text, append); } }
            public StringBuilder GetText() { StringBuilder values = new StringBuilder(); if (base.obj != null) { base.obj.ReadText(values); } return values; }
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
        public class MessHandler
        {
            public string name_ship { get; set; }
            public enum type_base : int
            {
                none = 0,
                planet = 1,   // планета
                space = 2,    // космос
                metior = 3,   // метиорит
                relocat = 4,  // перемещаемая
            };
            public static string[] name_type_base = { "?", "ПЛАНЕТАРНАЯ", "ОРБИТАЛЬНАЯ", "МЕТЕОРНАЯ", "ТРАНСПОРТ" };
            public class base_point
            {
                public string name { get; set; }
                public long addr { get; set; }
                public type_base type { get; set; }
                public MatrixD dm { get; set; }
                public Vector3D centr { get; set; }
            }
            public List<base_point> base_points = new List<base_point>();
            public IMyRadioAntenna antenna;
            //public IMyProgrammableBlock pb;
            public IMyUnicastListener ship_lstr; // Одноадресный прослушиватель базы
            public MyIGCMessage message;
            public long pb_address { get; set; }

            public MessHandler(string name)
            {
                name_ship = name;
                ship_lstr = _scr.IGC.UnicastListener;
                List<IMyRadioAntenna> list_anten = new List<IMyRadioAntenna>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(list_anten, r => r.CustomName.Contains(name));
                _scr.Echo("MessHandler : Найдено IMyRadioAntenna - " + list_anten.Count());
                //strg.SaveToStorage();
            }
            public void SendAddBase()
            {
                List<IMyProgrammableBlock> list_pb = new List<IMyProgrammableBlock>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(list_pb, r => r.CustomName.Contains(tag_antena));
                IMyProgrammableBlock pb = list_pb.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag_antena)).FirstOrDefault();
                if (pb != null)
                {
                    string command = String.Format("add_ship=name:{0};type:{1};thruster:{2}", name_ship, type_ship, type_thruster);
                    _scr.IGC.SendUnicastMessage<string>(pb.EntityId, name_ship, command);
                }
            }
            public void SendUpdBase()
            {
                List<IMyProgrammableBlock> list_pb = new List<IMyProgrammableBlock>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(list_pb, r => r.CustomName.Contains(tag_antena));
                IMyProgrammableBlock pb = list_pb.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag_antena)).FirstOrDefault();
                if (pb != null)
                {
                    string command = String.Format("upd_ship=name:{0};type:{1};thruster:{2}", name_ship, type_ship, type_thruster);
                    _scr.IGC.SendUnicastMessage<string>(pb.EntityId, name_ship, command);
                }
            }
            public bool RegistrationBase(String str, long addr)
            {
                //lcd_lstr.OutText("(args[1]:" + str, true);
                string name = strg.GetValString("name", str);
                type_base type = (type_base)strg.GetValInt("type", str);
                //Vector3D bp = new Vector3D(strg.GetValDouble("BPX", str.ToString()), strg.GetValDouble("BPY", str.ToString()), strg.GetValDouble("BPZ", str.ToString()));
                MatrixD dm = nav.GetNormTransMatrixFromMyPos();
                Vector3D pc = nav.GetPlanetCenter();
                //lcd_lstr.OutText("name:" + name, true);
                //lcd_lstr.OutText("type:" + type, true);
                //lcd_lstr.OutText("bp:" + bp.ToString(), true);
                //lcd_lstr.OutText("bp:" + pc.ToString(), true);
                base_point point = base_points.Where(s => s.addr == addr).FirstOrDefault();
                if (point == null)
                {
                    point = new base_point()
                    {
                        name = name,
                        type = type,
                        addr = addr,
                        centr = pc,
                        dm = dm,
                    };
                    base_points.Add(point);
                }
                else
                {
                    point.name = name; point.type = type; point.centr = pc; point.dm = dm;
                }
                //lcd_lstr.OutText("count:" + base_points.Count(), true);
                strg.SaveToStorage();
                return true;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "test": _scr.IGC.SendUnicastMessage<string>(pb_address, "tag", argument); break;
                    default: break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    if (ship_lstr.HasPendingMessage)
                    {
                        message = ship_lstr.AcceptMessage();
                        StringBuilder values = new StringBuilder();
                        string mess_inp = Convert.ToString(message.Data); string mess_tag = Convert.ToString(message.Tag); string mess_source = Convert.ToString(message.Source);
                        long addr = !String.IsNullOrWhiteSpace(mess_source) ? Convert.ToInt64(mess_source) : 0;
                        values.Append("Data   : " + mess_inp + "\n");
                        values.Append("Tag    : " + mess_tag + "\n");
                        values.Append("Source : " + mess_source + "\n");
                        lcd_lstr.OutText(values);
                        string[] args = mess_inp.Split('=');
                        //lcd_lstr.OutText("(args[0]:" + args[0], true);
                        if (args.Count() > 0)
                        {
                            switch (args[0])
                            {
                                case "upd_base":
                                    {
                                        bool res = RegistrationBase(args[1], addr);
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
        }
        public class Navigation
        {
            public string name_ship { get; set; }

            public Vector3D MyPos { get; private set; }
            public Vector3D MyPrevPos { get; private set; }
            public Vector3D GravVector { get; private set; }
            public bool gravity { get; private set; } = false;
            public float PhysicalMass { get; private set; } // Физическая масса
            public float TotalMass { get; private set; } // Физическая масса
            public MatrixD WMCocpit { get; private set; } //
            public BaseShipController cockpit { get; set; }
            public bool Connected { get { return connector_forw.Connected || connector_back.Connected || connector_down.Connected; } }
            public Navigation(string name)
            {
                name_ship = name;
                cockpit = new BaseShipController(name, tag_nav);
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
            public Vector3D GetPlanetCenter()
            {
                Vector3D pc = new Vector3D();
                return cockpit.obj.TryGetPlanetPosition(out pc) ? pc : Vector3D.Zero;
            }
            public void UpdateCalc()
            {
                MyPrevPos = MyPos;
                MyPos = cockpit.obj.GetPosition();
                GravVector = cockpit.obj.GetNaturalGravity();
                gravity = GravVector.LengthSquared() > 0.2f;
                PhysicalMass = cockpit.obj.CalculateShipMass().PhysicalMass;
                TotalMass = cockpit.obj.CalculateShipMass().TotalMass;
                WMCocpit = cockpit.obj.WorldMatrix;
                //UpThrust = (float)(GravVector * PhysicalMass).Dot(WMCocpit.Up);
                //VelocityVector = (MyPos - MyPrevPos) * 6;
                //UpVelocityVector = WMCocpit.Up * Vector3D.Dot(VelocityVector, WMCocpit.Up);
                //ForwVelocityVector = WMCocpit.Forward * Vector3D.Dot(VelocityVector, WMCocpit.Forward);
                //LeftVelocityVector = WMCocpit.Left * Vector3D.Dot(VelocityVector, WMCocpit.Left);
                //OrientationCocpit = cockpit.GetCockpitMatrix();
                //YMaxA = Math.Abs((float)Math.Min(thrusts.UpThrMax / PhysicalMass - GravVector.Length(), thrusts.DownThrMax / PhysicalMass + GravVector.Length()));
                //ZMaxA = (float)Math.Min(thrusts.ForwardThrMax, thrusts.BackwardThrMax) / PhysicalMass;
                //XMaxA = (float)Math.Min(thrusts.RightThrMax, thrusts.LeftThrMax) / PhysicalMass;
                //// расчтет пути торможения (посадка Down-внизу)
                //double a = (thrusts.DownThrMax / 1000) * (1 / (PhysicalMass / 1000));
                //double t = (0 - UpVelocityVector.Length()) / -a; //t = (V - V[0]) / a
                //DownBrDistance = (UpVelocityVector.Length() * t) + ((-a) * Math.Pow(t, 2)) / 2; //S = V[0] * t + ( a * t^2 ) / 2
                //// Критические уставки
                //CriticalMassReached = (PhysicalMass > CriticalMass);
                //CriticalBatteryCharge = connector_base.Connected ? bats.CurrentPersent() < 1.0f : bats.CurrentPersent() <= CriticalOnCharge;
                //CriticalHydrogenSupply = connector_base.Connected ? hydrogen_tanks_nav.AverageFilledRatio < 1.0f : hydrogen_tanks_nav.AverageFilledRatio <= CriticalOnH2;
                //EmergencySetpoint = CriticalMassReached || CriticalBatteryCharge || CriticalHydrogenSupply;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                UpdateCalc();
                switch (argument)
                {
                    case "upd_base": if (Connected) { mess_handler.SendUpdBase(); }; break;
                    default: break;
                }
                if (updateSource == UpdateType.Update10)
                {

                }
            }
        }
        public class MyStorage
        {
            public MyStorage() { }
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                int count = GetValInt("count_base", str.ToString());
                mess_handler.base_points.Clear();
                for (int i = 0; i < count; i++)
                {
                    MessHandler.base_point bs = new MessHandler.base_point()
                    {
                        name = GetValString("base[" + i + "].name", str.ToString()),
                        addr = GetValInt64("base[" + i + "].addr", str.ToString()),
                        type = (MessHandler.type_base)GetValInt("base[" + i + "].type", str.ToString()),
                        dm = GetValMatrixD("base[" + i + "].dock", str.ToString()),
                        centr = GetValVector3D("base[" + i + "].centr", str.ToString()),
                        //point = new Vector3D(GetValDouble("base[" + i + "].PX", str.ToString()), GetValDouble("base[" + i + "].PY", str.ToString()), GetValDouble("base[" + i + "].PZ", str.ToString())),
                        //centr = new Vector3D(GetValDouble("base[" + i + "].PX", str.ToString()), GetValDouble("base[" + i + "].PY", str.ToString()), GetValDouble("base[" + i + "].PZ", str.ToString())),
                    };
                }
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                int i = 0;
                foreach (MessHandler.base_point bs in mess_handler.base_points)
                {
                    values.Append("base[" + i + "].name: " + bs.name + ";\n");
                    values.Append("base[" + i + "].addr: " + bs.addr.ToString() + ";\n");
                    values.Append("base[" + i + "].type: " + ((int)bs.type).ToString() + ";\n");
                    values.Append(SetValMatrixD("base[" + i + "].dock", bs.dm) + ";\n");
                    values.Append(SetValVector3D("base[" + i + "].centr", bs.centr) + ";\n");
                    //values.Append(bs.dm.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "base[" + i + "].DM"));
                    //values.Append(bs.point.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "base[" + i + "].PX").Replace("Y", "base[" + i + "].PY").Replace("Z", "base[" + i + "].PZ") + ";\n");
                    //values.Append(bs.centr.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "base[" + i + "].CX").Replace("Y", "base[" + i + "].CY").Replace("Z", "base[" + i + "].CZ") + ";\n");
                    i++;
                }
                values.Append("count_base: " + mess_handler.base_points.Count().ToString() + ";\n");
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
            public string SetValMatrixD(string Key, MatrixD val) { return val.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", Key + "M"); }
        }
    }
}
